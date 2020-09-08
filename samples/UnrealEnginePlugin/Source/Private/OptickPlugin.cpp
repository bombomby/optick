// Copyright(c) 2019 Vadim Slyusarev

// Core
#include "CoreMinimal.h"
#include "Containers/Ticker.h"
#include "GenericPlatform/GenericPlatformFile.h"
#include "HAL/PlatformFilemanager.h"
#include "HAL/PlatformProcess.h"
#include "Misc/EngineVersion.h"
#include "Modules/ModuleManager.h"
#include "Stats/Stats2.h"
#include "Stats/StatsData.h"
#include "IOptickPlugin.h"

// Engine
#include "UnrealClient.h"

// Optick
#include "OptickUE4Classes.h"

#define LOCTEXT_NAMESPACE "FOptickModule"

#if WITH_EDITOR
#include "DesktopPlatformModule.h"
#include "EditorStyleSet.h"
#include "Framework/Commands/Commands.h"
#include "Editor/EditorPerformanceSettings.h"
#include "Editor/LevelEditor/Public/LevelEditor.h"
#include "Editor/UnrealEd/Public/SEditorViewportToolBarMenu.h"
#include "Projects/Public/Interfaces/IPluginManager.h"
//#include "Windows/WindowsPlatformProcess.h"

#include "OptickStyle.h"
#include "OptickCommands.h"
#endif

#include <optick.h>

DEFINE_LOG_CATEGORY(OptickLog);

#if STATS
static FName NAME_STATGROUP_CPUStalls(FStatGroup_STATGROUP_CPUStalls::GetGroupName());
static FName NAME_Wait[] =
{
	FName("STAT_FQueuedThread_Run_WaitForWork"),
	FName("STAT_TaskGraph_OtherStalls"),
};
static FName NAME_GPU_Unaccounted("Unaccounted");
#endif

static FString Optick_SCREENSHOT_NAME(TEXT("UE4_Optick_Screenshot.png"));

struct ThreadStorage
{
	Optick::EventStorage* EventStorage;
	uint64 LastTimestamp;
	ThreadStorage(Optick::EventStorage* storage = nullptr) : EventStorage(storage), LastTimestamp(0) {}
	void Reset() { LastTimestamp = 0; }
};


class FOptickPlugin : public IOptickPlugin
{
	FCriticalSection UpdateCriticalSection;

	volatile bool IsCapturing;

	const uint64 LowMask =  0x00000000FFFFFFFFULL;
	const uint64 HighMask = 0xFFFFFFFF00000000ULL;

	const uint32 WaitForScreenshotMaxFrameCount = 5;

	uint64 OriginTimestamp;

	FDelegateHandle TickDelegateHandle;
	FDelegateHandle StatFrameDelegateHandle;
	FDelegateHandle EndFrameRTDelegateHandle;

	TMap<uint32, ThreadStorage*> StorageMap;
	TMap<FName, Optick::EventDescription*> DescriptionMap;

	ThreadStorage GPUThreadStorage;
	TMap<FName, Optick::EventDescription*> GPUDescriptionMap;

	uint32 WaitingForScreenshotFrameNumber{0};
	TAtomic<bool> WaitingForScreenshot;

	bool Tick(float DeltaTime);

	void GetDataFromStatsThread(int64 CurrentFrame);

#if WITH_EDITOR
	struct EditorSettings
	{
		bool bCPUThrottleEnabled;
		EditorSettings() : bCPUThrottleEnabled(true) {}
	};
	EditorSettings BaseSettings;

	TSharedPtr<const FExtensionBase> ToolbarExtension;
	TSharedPtr<FExtensibilityManager> ExtensionManager;
	TSharedPtr<FExtender> ToolbarExtender;

	TSharedPtr<class FUICommandList> PluginCommands;

	void AddToolbarExtension(FToolBarBuilder& ToolbarBuilder);
#endif

	void OnScreenshotProcessed();

	uint64 Convert32bitCPUTimestamp(int64 timestamp) const;

#ifdef OPTICK_UE4_GPU
	void OnEndFrameRT();
	uint64 ConvertGPUTimestamp(uint64 timestamp);

	FGPUTimingCalibrationTimestamp CalibrationTimestamp;
#endif

public:
	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

	void StartCapture();
	void StopCapture();

	void OnOpenGUI();

	void RequestScreenshot();
	bool IsReadyToDumpCapture() const;
	bool IsScreenshotReady() const;
};

IMPLEMENT_MODULE( FOptickPlugin, OptickPlugin )
DECLARE_DELEGATE_OneParam(FStatFrameDelegate, int64);


bool FOptickPlugin::Tick(float DeltaTime)
{
	//static const FString OptickMessage(TEXT("OptickPlugin is running!"));
	//GEngine->AddOnScreenDebugMessage(31313, 5.f, FColor::Yellow, OptickMessage);

	Optick::Update();
	return true;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


#if WITH_EDITOR

void FOptickPlugin::OnOpenGUI()
{
	//UE_LOG(OptickLog, Display, TEXT("OnOpenGUI!"));

	const FString OptickPath = IPluginManager::Get().FindPlugin("OptickPlugin")->GetBaseDir() / TEXT("GUI/Optick.exe");
	const FString OptickFullPath = FPaths::ConvertRelativePathToFull(OptickPath);
	const FString OptickArgs;
	FPlatformProcess::CreateProc(*OptickFullPath, *OptickArgs, true, false, false, NULL, 0, NULL, NULL);
}

void FOptickPlugin::AddToolbarExtension(FToolBarBuilder& ToolbarBuilder)
{
	//UE_LOG(OptickLog, Log, TEXT("Attaching toolbar extension..."));

	ToolbarBuilder.BeginSection("Optick");
	{
		ToolbarBuilder.AddToolBarButton(FOptickCommands::Get().PluginAction);
	}
	ToolbarBuilder.EndSection();
}

#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool OnOptickStateChanged(Optick::State::Type state)
{
	FOptickPlugin& plugin = (FOptickPlugin&)IOptickPlugin::Get();

	//UE_LOG(OptickLog, Display, TEXT("State => %d"), state);

	switch (state)
	{
	case Optick::State::START_CAPTURE:
		plugin.StartCapture();
		break;

	case Optick::State::STOP_CAPTURE:
		plugin.RequestScreenshot();
		plugin.StopCapture();
		break;

	case Optick::State::DUMP_CAPTURE:
		if (!plugin.IsReadyToDumpCapture())
			return false;

		Optick::AttachSummary("Platform", FPlatformProperties::PlatformName());
		Optick::AttachSummary("UnrealVersion", TCHAR_TO_ANSI(*FEngineVersion::Current().ToString(EVersionComponent::Changelist)));
		Optick::AttachSummary("GPU", TCHAR_TO_ANSI(*FPlatformMisc::GetPrimaryGPUBrand()));

		if (plugin.IsScreenshotReady())
		{
			FString FullName = Optick_SCREENSHOT_NAME;
			FScreenshotRequest::CreateViewportScreenShotFilename(FullName);

			IPlatformFile& PlatformFile = FPlatformFileManager::Get().GetPlatformFile();

			if (PlatformFile.FileExists(*FullName))
			{
				const int64 FileSize = PlatformFile.FileSize(*FullName);
				TArray<uint8> Image;
				Image.Reserve(FileSize);

				IFileHandle* FileHandle = PlatformFile.OpenRead(*FullName);
				if (FileHandle)
				{
					FileHandle->Read(Image.GetData(), FileSize);
					delete FileHandle;
				}

				Optick::AttachFile(Optick::File::OPTICK_IMAGE, "Screenshot.png", Image.GetData(), (uint32_t)FileSize);
			}
		}


		break;
	}
	return true;
}

void FOptickPlugin::StartupModule()
{
	UE_LOG(OptickLog, Display, TEXT("OptickPlugin Loaded!"));

	GPUThreadStorage.EventStorage = Optick::RegisterStorage(TCHAR_TO_ANSI(*FPlatformMisc::GetPrimaryGPUBrand()), (uint64_t)-1, Optick::ThreadMask::GPU);

	// Subscribing for Ticker
	TickDelegateHandle = FTicker::GetCoreTicker().AddTicker(FTickerDelegate::CreateRaw(this, &FOptickPlugin::Tick));

	// Register Optick callback
	Optick::SetStateChangedCallback(OnOptickStateChanged);

#if WITH_EDITOR
	FOptickStyle::Initialize();
	FOptickStyle::ReloadTextures();
	FOptickCommands::Register();

	PluginCommands = MakeShareable(new FUICommandList);

	PluginCommands->MapAction(
		FOptickCommands::Get().PluginAction,
		FExecuteAction::CreateRaw(this, &FOptickPlugin::OnOpenGUI),
		FCanExecuteAction());

	FLevelEditorModule& LevelEditorModule = FModuleManager::LoadModuleChecked<FLevelEditorModule>("LevelEditor");
	ExtensionManager = LevelEditorModule.GetToolBarExtensibilityManager();
	ToolbarExtender = MakeShareable(new FExtender);
	ToolbarExtension = ToolbarExtender->AddToolBarExtension("Game", EExtensionHook::After, PluginCommands, FToolBarExtensionDelegate::CreateLambda([this](FToolBarBuilder& ToolbarBuilder) { AddToolbarExtension(ToolbarBuilder); })	);
	ExtensionManager->AddExtender(ToolbarExtender);
#endif

#ifdef OPTICK_UE4_GPU
	EndFrameRTDelegateHandle = FCoreDelegates::OnEndFrameRT.AddRaw(this, &FOptickPlugin::OnEndFrameRT);
#endif
}


void FOptickPlugin::ShutdownModule()
{
	// Remove delegate
	FTicker::GetCoreTicker().RemoveTicker(TickDelegateHandle);
 	FCoreDelegates::OnEndFrameRT.Remove(EndFrameRTDelegateHandle);

	// Stop capture if needed
	StopCapture();

#if WITH_EDITOR
	// This function may be called during shutdown to clean up your module.  For modules that support dynamic reloading,
	// we call this function before unloading the module.
	FOptickStyle::Shutdown();

	FOptickCommands::Unregister();
#endif

	UE_LOG(OptickLog, Display, TEXT("OptickPlugin UnLoaded!"));
}

void FOptickPlugin::StartCapture()
{
	FScopeLock ScopeLock(&UpdateCriticalSection);

	if (!IsCapturing)
	{
#ifdef OPTICK_UE4_GPU
		CalibrationTimestamp = FGPUTiming::GetCalibrationTimestamp();

		GPUThreadStorage.Reset();
		for (auto& pair : StorageMap)
			pair.Value->Reset();
#endif

#if WITH_EDITOR
		UEditorPerformanceSettings* Settings = GetMutableDefault<UEditorPerformanceSettings>();
		check(Settings);
		BaseSettings.bCPUThrottleEnabled = Settings->bThrottleCPUWhenNotForeground;
		Settings->bThrottleCPUWhenNotForeground = false;
		Settings->PostEditChange();
		Settings->SaveConfig();
#endif

		OriginTimestamp = FPlatformTime::Cycles64();

		IsCapturing = true;

#if STATS
		FThreadStats::MasterEnableAdd(1);

		FStatsThreadState& Stats = FStatsThreadState::GetLocalState();

		// Set up our delegate to gather data from the stats thread for safe consumption on game thread.
		StatFrameDelegateHandle = Stats.NewFrameDelegate.Add(FStatFrameDelegate::CreateRaw(this, &FOptickPlugin::GetDataFromStatsThread));
#endif
	}
}

void FOptickPlugin::StopCapture()
{
	FScopeLock ScopeLock(&UpdateCriticalSection);

	if (IsCapturing)
	{
		IsCapturing = false;

#if STATS
		FThreadStats::MasterEnableSubtract(1);

		FStatsThreadState& Stats = FStatsThreadState::GetLocalState();

		// Set up our delegate to gather data from the stats thread for safe consumption on game thread.
		Stats.NewFrameDelegate.Remove(StatFrameDelegateHandle);
		StatFrameDelegateHandle.Reset();
#endif

#if WITH_EDITOR
		UEditorPerformanceSettings* Settings = GetMutableDefault<UEditorPerformanceSettings>();
		check(Settings);
		Settings->bThrottleCPUWhenNotForeground = BaseSettings.bCPUThrottleEnabled;
		Settings->PostEditChange();
		Settings->SaveConfig();
#endif
	}
	
}

void FOptickPlugin::RequestScreenshot()
{
	// Requesting screenshot
	//UE_LOG(OptickLog, Display, TEXT("Screenshot requested!"));
	WaitingForScreenshot = true;
	WaitingForScreenshotFrameNumber = GFrameNumber;
	FScreenshotRequest::OnScreenshotRequestProcessed().AddRaw(this, &FOptickPlugin::OnScreenshotProcessed);
	FScreenshotRequest::RequestScreenshot(Optick_SCREENSHOT_NAME, true, false);
}

bool FOptickPlugin::IsScreenshotReady() const
{
	return (WaitingForScreenshot == false);
}

bool FOptickPlugin::IsReadyToDumpCapture() const
{
	return	IsScreenshotReady() ||
			(GFrameNumber - WaitingForScreenshotFrameNumber) > WaitForScreenshotMaxFrameCount ||
			(GIsServer && !GIsClient);
}

void FOptickPlugin::OnScreenshotProcessed()
{
	//UE_LOG(OptickLog, Display, TEXT("Screenshot processed!"));
	WaitingForScreenshot = false;
	FScreenshotRequest::OnScreenshotRequestProcessed().RemoveAll(this);
}

uint64 FOptickPlugin::Convert32bitCPUTimestamp(int64 timestamp) const
{
	uint64 result = (OriginTimestamp & HighMask) | (timestamp & LowMask);

	// Handle Overflow
	if (result < OriginTimestamp)
		result += (1ULL << 32ULL);

	return result;
}

#ifdef OPTICK_UE4_GPU
#if UE_4_24_OR_LATER
void FOptickPlugin::OnEndFrameRT()
{
	FScopeLock ScopeLock(&UpdateCriticalSection);

	if (!IsCapturing || !Optick::IsActive(Optick::Mode::GPU))
		return;

	QUICK_SCOPE_CYCLE_COUNTER(STAT_FOptickPlugin_UpdRT);

	if (FRealtimeGPUProfilerImpl* gpuProfiler = reinterpret_cast<FRealtimeGPUProfilerImpl*>(FRealtimeGPUProfiler::Get()))
	{
		if (FRealtimeGPUProfilerFrameImpl* Frame = gpuProfiler->Frames[gpuProfiler->ReadBufferIndex])
		{
			FRHICommandListImmediate& RHICmdList = FRHICommandListExecutor::GetImmediateCommandList();

			// Gather any remaining results and check all the results are ready
			const int32 NumEventsThisFramePlusOne = Frame->NextEventIdx;

			for (int32 i = Frame->NextResultPendingEventIdx; i < NumEventsThisFramePlusOne; ++i)
			{
				FRealtimeGPUProfilerEventImpl& Event = Frame->GpuProfilerEvents[i];

				if (!Event.GatherQueryResults(RHICmdList))
				{
#if !(UE_BUILD_SHIPPING || UE_BUILD_TEST)
					UE_LOG(OptickLog, Warning, TEXT("Query is not ready."));
#endif
					// The frame isn't ready yet. Don't update stats - we'll try again next frame. 
					return;
				}
			}

			for (int32 i = 1; i < NumEventsThisFramePlusOne; ++i)
			{
				FRealtimeGPUProfilerEventImpl& Event = Frame->GpuProfilerEvents[i];

				const FName Name = Event.Name;

				Optick::EventDescription* Description = nullptr;

				if (Optick::EventDescription** ppDescription = GPUDescriptionMap.Find(Name))
				{
					Description = *ppDescription;
				}
				else
				{
					Description = Optick::EventDescription::CreateShared(TCHAR_TO_ANSI(*Name.ToString()));
					GPUDescriptionMap.Add(Name, Description);
				}

				uint64 startTimestamp = ConvertGPUTimestamp(Event.StartResultMicroseconds);
				uint64 endTimestamp = ConvertGPUTimestamp(Event.EndResultMicroseconds);

				if (Name == NAME_GPU_Unaccounted)
				{
					OPTICK_FRAME_FLIP(Optick::FrameType::GPU, startTimestamp);

					if (GPUThreadStorage.LastTimestamp != 0)
					{
						OPTICK_STORAGE_POP(GPUThreadStorage.EventStorage, GPUThreadStorage.LastTimestamp);
					}
						
					OPTICK_STORAGE_PUSH(GPUThreadStorage.EventStorage, Optick::GetFrameDescription(Optick::FrameType::GPU), startTimestamp)
				}
				else
				{
					OPTICK_STORAGE_EVENT(GPUThreadStorage.EventStorage, Description, startTimestamp, endTimestamp);
				}
				GPUThreadStorage.LastTimestamp = FMath::Max<uint64>(GPUThreadStorage.LastTimestamp, endTimestamp);
			}
		}
	}
}
#else
void FOptickPlugin::OnEndFrameRT()
{
	FScopeLock ScopeLock(&UpdateCriticalSection);

	if (!IsCapturing || !Optick::IsActive(Optick::Mode::GPU))
		return;

	QUICK_SCOPE_CYCLE_COUNTER(STAT_FOptickPlugin_UpdRT);

	if (FRealtimeGPUProfilerImpl* gpuProfiler = reinterpret_cast<FRealtimeGPUProfilerImpl*>(FRealtimeGPUProfiler::Get()))
	{
		if (FRealtimeGPUProfilerFrameImpl* Frame = gpuProfiler->Frames[gpuProfiler->ReadBufferIndex])
		{
			FRHICommandListImmediate& RHICmdList = FRHICommandListExecutor::GetImmediateCommandList();

			bool bAnyEventFailed = false;
			bool bAllQueriesAllocated = true;

			for (int i = 0; i < Frame->GpuProfilerEvents.Num(); ++i)
			{
				FRealtimeGPUProfilerEventImpl* Event = Frame->GpuProfilerEvents[i];
				check(Event != nullptr);

				if (!Event->HasValidResult())
				{
					Event->GatherQueryResults(RHICmdList);
				}

				if (!Event->HasValidResult())
				{
#if UE_BUILD_DEBUG
					UE_LOG(OptickLog, Warning, TEXT("Query '%s' not ready."), *Event->GetName().ToString());
#endif
					// The frame isn't ready yet. Don't update stats - we'll try again next frame. 
					bAnyEventFailed = true;
					continue;
				}

				if (!Event->HasQueriesAllocated())
				{
					bAllQueriesAllocated = false;
				}
			}

			if (bAnyEventFailed)
			{
				return;
			}

			if (!bAllQueriesAllocated)
			{
				static bool bWarned = false;

				if (!bWarned)
				{
					bWarned = true;
					UE_LOG(OptickLog, Warning, TEXT("Ran out of GPU queries! Results for this frame will be incomplete"));
				}
			}


			for (int i = 0; i < Frame->GpuProfilerEvents.Num(); ++i)
			{
				FRealtimeGPUProfilerEventImpl* Event = Frame->GpuProfilerEvents[i];
				check(Event != nullptr);

				const FName Name = Event->Name;

				Optick::EventDescription* Description = nullptr;

				if (Optick::EventDescription** ppDescription = GPUDescriptionMap.Find(Name))
				{
					Description = *ppDescription;
				}
				else
				{
					Description = Optick::EventDescription::CreateShared(TCHAR_TO_ANSI(*Name.ToString()));
					GPUDescriptionMap.Add(Name, Description);
				}

				uint64 startTimestamp = ConvertGPUTimestamp(Event->StartResultMicroseconds);
				uint64 endTimestamp = ConvertGPUTimestamp(Event->EndResultMicroseconds);

				if (Name == NAME_GPU_Unaccounted)
				{
					OPTICK_FRAME_FLIP(Optick::FrameType::GPU, startTimestamp);

					if (GPUThreadStorage.LastTimestamp != 0)
					{
						OPTICK_STORAGE_POP(GPUThreadStorage.EventStorage, GPUThreadStorage.LastTimestamp);
					}

					OPTICK_STORAGE_PUSH(GPUThreadStorage.EventStorage, Optick::GetFrameDescription(Optick::FrameType::GPU), startTimestamp)
				}
				else
				{
					OPTICK_STORAGE_EVENT(GPUThreadStorage.EventStorage, Description, startTimestamp, endTimestamp);
				}
				GPUThreadStorage.LastTimestamp = FMath::Max<uint64>(GPUThreadStorage.LastTimestamp, endTimestamp);
			}
		}
	}
}
#endif

uint64 FOptickPlugin::ConvertGPUTimestamp(uint64 timestamp)
{
	if (CalibrationTimestamp.CPUMicroseconds == 0 || CalibrationTimestamp.GPUMicroseconds == 0)
	{
		CalibrationTimestamp.CPUMicroseconds = uint64(FPlatformTime::ToSeconds64(FPlatformTime::Cycles64()) * 1e6);
		CalibrationTimestamp.GPUMicroseconds = timestamp;
	}

	const uint64 cpuTimestampUs = timestamp - CalibrationTimestamp.GPUMicroseconds + CalibrationTimestamp.CPUMicroseconds;
	const uint64 cpuTimestamp = cpuTimestampUs * 1e-6 / FPlatformTime::GetSecondsPerCycle64();
	return cpuTimestamp;
}
#endif

void FOptickPlugin::GetDataFromStatsThread(int64 CurrentFrame)
{
	if (!IsCapturing)
		return;

	QUICK_SCOPE_CYCLE_COUNTER(STAT_FOptickPlugin_Upd);

#if STATS
	const FStatsThreadState& Stats = FStatsThreadState::GetLocalState();
	const FStatPacketArray& Frame = Stats.GetStatPacketArray(CurrentFrame);

	for (int32 PacketIndex = 0; PacketIndex < Frame.Packets.Num(); PacketIndex++)
	{
		FStatPacket const& Packet = *Frame.Packets[PacketIndex];
		const FName ThreadName = Stats.GetStatThreadName(Packet);

		ThreadStorage* Storage = nullptr;

		if (ThreadStorage** ppStorage = StorageMap.Find(Packet.ThreadId))
		{
			Storage = *ppStorage;
		}
		else
		{
			Storage = new ThreadStorage(Optick::RegisterStorage(TCHAR_TO_ANSI(*ThreadName.ToString()), Packet.ThreadId));
			StorageMap.Add(Packet.ThreadId, Storage);
		}

		for (FStatMessage const& Item : Packet.StatMessages)
		{
			EStatOperation::Type Op = Item.NameAndInfo.GetField<EStatOperation>();

			if (Op == EStatOperation::CycleScopeStart || Op == EStatOperation::CycleScopeEnd)
			{
				FName name = Item.NameAndInfo.GetRawName();

				uint64 Timestamp = Convert32bitCPUTimestamp(Item.GetValue_int64());

				if (Op == EStatOperation::CycleScopeStart)
				{
					Optick::EventDescription* Description = nullptr;

					if (Optick::EventDescription** ppDescription = DescriptionMap.Find(name))
					{
						Description = *ppDescription;
					}
					else
					{
						const FName shortName = Item.NameAndInfo.GetShortName();
						const FName groupName = Item.NameAndInfo.GetGroupName();

						uint32 color = 0;

						if (NAME_STATGROUP_CPUStalls == groupName)
							color = Optick::Color::White;

						for (int i = 0; i < sizeof(NAME_Wait) / sizeof(NAME_Wait[0]); ++i)
							if (NAME_Wait[i] == shortName)
								color = Optick::Color::White;

						Description = Optick::EventDescription::CreateShared(TCHAR_TO_ANSI(*shortName.ToString()), nullptr, 0, color);

						DescriptionMap.Add(name, Description);
					}

					OPTICK_STORAGE_PUSH(Storage->EventStorage, Description, Timestamp);
				}
				else
				{
					Storage->LastTimestamp = FMath::Max<uint64>(Storage->LastTimestamp, Timestamp);
					OPTICK_STORAGE_POP(Storage->EventStorage, Timestamp);
				}
					
			}
			else if (Op == EStatOperation::AdvanceFrameEventGameThread)
			{
				if (Storage->LastTimestamp != 0)
				{
					const Optick::EventDescription* cpuFrameDescription = Optick::GetFrameDescription(Optick::FrameType::CPU);
					// Pop the previous frame event
					OPTICK_STORAGE_POP(Storage->EventStorage, Storage->LastTimestamp);
					// Push the new one
					OPTICK_STORAGE_PUSH(Storage->EventStorage, cpuFrameDescription, Storage->LastTimestamp);

					OPTICK_FRAME_FLIP(Optick::FrameType::CPU, Storage->LastTimestamp, Packet.ThreadId);
				}
			}
			else if (Op == EStatOperation::AdvanceFrameEventRenderThread && (GIsClient || !GIsServer))
			{
				if (Storage->LastTimestamp != 0)
				{
					const Optick::EventDescription* renderFrameDescription = Optick::GetFrameDescription(Optick::FrameType::Render);
					// Pop the previous frame event
					OPTICK_STORAGE_POP(Storage->EventStorage, Storage->LastTimestamp);
					// Push the new one
					OPTICK_STORAGE_PUSH(Storage->EventStorage, renderFrameDescription, Storage->LastTimestamp);

					OPTICK_FRAME_FLIP(Optick::FrameType::Render, Storage->LastTimestamp, Packet.ThreadId);
				}
			}
		}
	}
#endif
}

#undef LOCTEXT_NAMESPACE