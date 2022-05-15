// Copyright(c) 2019 Vadim Slyusarev

// Core
#include "CoreMinimal.h"
#include "Containers/Ticker.h"
#include "GenericPlatform/GenericPlatformFile.h"
#include "HAL/PlatformFileManager.h"
#include "HAL/PlatformProcess.h"
#include "Misc/EngineVersion.h"
#include "Misc/CoreDelegates.h"
#include "Modules/ModuleManager.h"
#include "Stats/Stats2.h"
#include "Stats/StatsData.h"
#include "IOptickPlugin.h"

// Engine
#include "UnrealClient.h"

// Optick
#include "OptickUE4Classes.h"

#if WITH_EDITOR
#include "DesktopPlatformModule.h"
#include "EditorStyleSet.h"
#include "Framework/Commands/Commands.h"
#include "Framework/MultiBox/MultiBoxBuilder.h"
#include "Editor/EditorPerformanceSettings.h"
#include "Editor/LevelEditor/Public/LevelEditor.h"
#include "Editor/UnrealEd/Public/SEditorViewportToolBarMenu.h"
#include "Projects/Public/Interfaces/IPluginManager.h"
#include "ToolMenus.h"
//#include "Windows/WindowsPlatformProcess.h"

#include "OptickStyle.h"
#include "OptickCommands.h"
#endif

#include <optick.h>

#define LOCTEXT_NAMESPACE "FOptickModule"

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

	FTSTicker::FDelegateHandle TickDelegateHandle;
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
	void RegisterMenus();
#endif

	void OnScreenshotProcessed();

	uint64 Convert32bitCPUTimestamp(int64 timestamp) const;

#ifdef OPTICK_UE4_GPU

	void OnEndFrameRT();
	uint64 ConvertGPUTimestamp(uint64 timestamp, int GPUIndex);

	bool UpdateCalibrationTimestamp(FRealtimeGPUProfilerFrameImpl* Frame, int GPUIndex);
	FGPUTimingCalibrationTimestamp CalibrationTimestamps[MAX_NUM_GPUS];
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

void FOptickPlugin::RegisterMenus()
{
	FToolMenuOwnerScoped OwnerScoped(this);

	if (UToolMenu* ProfileMenu = UToolMenus::Get()->ExtendMenu("MainFrame.MainMenu.Tools"))
	{
		FToolMenuSection& Section = ProfileMenu->AddSection("Optick Profiler", FText::FromString(TEXT("Optick Profiler")));
		Section.AddMenuEntry("OpenOptickProfiler",
			LOCTEXT("OpenOptickProfiler_Label", "Open Optick Profiler"),
			LOCTEXT("OpenOptickProfiler_Desc", "Open Optick Profiler"),
			FSlateIcon(FOptickStyle::GetStyleSetName(), "Optick.PluginAction"),
			FUIAction(FExecuteAction::CreateRaw(this, &FOptickPlugin::OnOpenGUI), FCanExecuteAction())
		);
	}
	else
	{
		UE_LOG(OptickLog, Error, TEXT("Can't find 'MainFrame.MainMenu.Tools' menu section"))
	}
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
	TickDelegateHandle = FTSTicker::GetCoreTicker().AddTicker(FTickerDelegate::CreateRaw(this, &FOptickPlugin::Tick));

	// Register Optick callback
	Optick::SetStateChangedCallback(OnOptickStateChanged);

#if WITH_EDITOR
	if (GIsEditor)
	{
		FOptickStyle::Initialize();
		FOptickStyle::ReloadTextures();
		FOptickCommands::Register();

		RegisterMenus();

		PluginCommands = MakeShareable(new FUICommandList);

		PluginCommands->MapAction(
			FOptickCommands::Get().PluginAction,
			FExecuteAction::CreateRaw(this, &FOptickPlugin::OnOpenGUI),
			FCanExecuteAction());

		//FLevelEditorModule& LevelEditorModule = FModuleManager::LoadModuleChecked<FLevelEditorModule>("LevelEditor");
		//ExtensionManager = LevelEditorModule.GetToolBarExtensibilityManager();
		//ToolbarExtender = MakeShareable(new FExtender);
		//ToolbarExtension = ToolbarExtender->AddToolBarExtension("Game", EExtensionHook::After, PluginCommands, FToolBarExtensionDelegate::CreateLambda([this](FToolBarBuilder& ToolbarBuilder) { AddToolbarExtension(ToolbarBuilder); })	);
		//ExtensionManager->AddExtender(ToolbarExtender);

		//if (UToolMenu* ToolbarMenu = UToolMenus::Get()->ExtendMenu("LevelEditor.LevelEditorToolBar"))
		//{
		//	FToolMenuSection& Section = ToolbarMenu->AddSection("Optick Profiler", FText::FromString(TEXT("Optick Profiler")));
		//	FToolMenuEntry& Entry = Section.AddEntry(FToolMenuEntry::InitToolBarButton(FOptickCommands::Get().PluginAction));
		//	Entry.SetCommandList(PluginCommands);
		//}
		//else
		//{
		//	UE_LOG(OptickLog, Error, TEXT("Can't find 'LevelEditor.LevelEditorToolBar' menu section"))
		//}
	}
#endif

#ifdef OPTICK_UE4_GPU
	EndFrameRTDelegateHandle = FCoreDelegates::OnEndFrameRT.AddRaw(this, &FOptickPlugin::OnEndFrameRT);
#endif
}


void FOptickPlugin::ShutdownModule()
{
	// Remove delegate
	FTSTicker::GetCoreTicker().RemoveTicker(TickDelegateHandle);
	FCoreDelegates::OnEndFrameRT.Remove(EndFrameRTDelegateHandle);

	// Stop capture if needed
	StopCapture();

#if WITH_EDITOR
	if (GIsEditor)
	{
		// This function may be called during shutdown to clean up your module.  For modules that support dynamic reloading,
		// we call this function before unloading the module.
		FOptickStyle::Shutdown();

		FOptickCommands::Unregister();
	}
#endif

	UE_LOG(OptickLog, Display, TEXT("OptickPlugin UnLoaded!"));
}

void FOptickPlugin::StartCapture()
{
	FScopeLock ScopeLock(&UpdateCriticalSection);

	if (!IsCapturing)
	{
#ifdef OPTICK_UE4_GPU
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
	return IsScreenshotReady() || (GFrameNumber - WaitingForScreenshotFrameNumber) > WaitForScreenshotMaxFrameCount;
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

bool FOptickPlugin::UpdateCalibrationTimestamp(FRealtimeGPUProfilerFrameImpl* Frame, int GPUIndex)
{
	FGPUTimingCalibrationTimestamp& CalibrationTimestamp = CalibrationTimestamps[GPUIndex];
	CalibrationTimestamp = FGPUTimingCalibrationTimestamp{ 0, 0 };

	if (Frame->TimestampCalibrationQuery.IsValid())
	{
		CalibrationTimestamp.GPUMicroseconds = Frame->TimestampCalibrationQuery->GPUMicroseconds[GPUIndex];
		CalibrationTimestamp.CPUMicroseconds = Frame->TimestampCalibrationQuery->CPUMicroseconds[GPUIndex];
	}

	if (CalibrationTimestamp.GPUMicroseconds == 0 || CalibrationTimestamp.CPUMicroseconds == 0) // Unimplemented platforms, or invalid on the first frame
	{
		if (Frame->GpuProfilerEvents.Num() > 1)
		{
			// Align CPU and GPU frames
			CalibrationTimestamp.GPUMicroseconds = Frame->GpuProfilerEvents[1].GetStartResultMicroseconds(0);
			CalibrationTimestamp.CPUMicroseconds = FPlatformTime::ToSeconds64(Frame->CPUFrameStartTimestamp) * 1000 * 1000;
		}
		else
		{
			// Fallback to legacy
			CalibrationTimestamp = FGPUTiming::GetCalibrationTimestamp();
		}
	}

	return CalibrationTimestamp.GPUMicroseconds != 0 && CalibrationTimestamp.CPUMicroseconds != 0;
}

struct TimeRange
{
	uint64 Start;
	uint64 Finish;
	bool IsOverlap(TimeRange other) const
	{
		return !((Finish < other.Start) || (other.Finish < Start));
	}
	bool IsValid() const
	{
		return Start != 0 && Finish != 0 && Finish > Start;
	}
	TimeRange() : Start(0), Finish(0) {}
	TimeRange(uint64 start, uint64 finish) : Start(start), Finish(finish) {}
};

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

			TArray<TimeRange> EventStack;

			if (NumEventsThisFramePlusOne <= 1)
				return;

			// VS TODO: Add MGPU support
			uint32 GPUIndex = 0;

			// Can't collect GPU data without valid calibration between CPU and GPU timestamps
			if (!UpdateCalibrationTimestamp(Frame, GPUIndex))
				return;

			uint64 lastTimeStamp = FMath::Max(CalibrationTimestamps[GPUIndex].CPUMicroseconds, GPUThreadStorage.LastTimestamp);

			const FRealtimeGPUProfilerEventImpl& FirstEvent = Frame->GpuProfilerEvents[1];
			uint64 frameStartTimestamp = FMath::Max(ConvertGPUTimestamp(FirstEvent.GetStartResultMicroseconds(GPUIndex), GPUIndex), lastTimeStamp);

			OPTICK_FRAME_FLIP(Optick::FrameType::GPU, frameStartTimestamp);
			OPTICK_STORAGE_PUSH(GPUThreadStorage.EventStorage, Optick::GetFrameDescription(Optick::FrameType::GPU), frameStartTimestamp)

			for (int32 Idx = 1; Idx < NumEventsThisFramePlusOne; ++Idx)
			{
				const FRealtimeGPUProfilerEventImpl& Event = Frame->GpuProfilerEvents[Idx];

				if (Event.GetGPUMask().Contains(GPUIndex))
				{
					const FName Name = Event.Name;

					if (Name == NAME_GPU_Unaccounted)
						continue;

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

					uint64 startTimestamp = ConvertGPUTimestamp(Event.GetStartResultMicroseconds(GPUIndex), GPUIndex);
					uint64 endTimestamp = ConvertGPUTimestamp(Event.GetEndResultMicroseconds(GPUIndex), GPUIndex);

					// Fixing potential errors
					startTimestamp = FMath::Max(startTimestamp, lastTimeStamp);
					endTimestamp = FMath::Max(endTimestamp, startTimestamp);

					// Ensuring correct hierarchy
					while (EventStack.Num() && (EventStack.Last().Finish <= startTimestamp))
						EventStack.Pop();

					// Discovered broken hierarchy, skipping event
					if (EventStack.Num() && (endTimestamp < EventStack.Last().Start))
						continue;

					// Clamp range against the parent counter
					if (EventStack.Num())
					{
						TimeRange parent = EventStack.Last();
						startTimestamp = FMath::Clamp(startTimestamp, parent.Start, parent.Finish);
						endTimestamp = FMath::Clamp(endTimestamp, parent.Start, parent.Finish);
					}

					// Ignore invalid events
					if (startTimestamp == endTimestamp)
						continue;

					//if (Name == NAME_GPU_Unaccounted)
					//{
					//	OPTICK_FRAME_FLIP(Optick::FrameType::GPU, startTimestamp);


					//	OPTICK_STORAGE_PUSH(GPUThreadStorage.EventStorage, Optick::GetFrameDescription(Optick::FrameType::GPU), startTimestamp)
					//}
					//else
					{
						EventStack.Add(TimeRange(startTimestamp, endTimestamp));
						OPTICK_STORAGE_EVENT(GPUThreadStorage.EventStorage, Description, startTimestamp, endTimestamp);
					}
					lastTimeStamp = FMath::Max<uint64>(lastTimeStamp, endTimestamp);
				}
			}

			OPTICK_STORAGE_POP(GPUThreadStorage.EventStorage, lastTimeStamp);
			GPUThreadStorage.LastTimestamp = lastTimeStamp;
		}
	}
}

uint64 FOptickPlugin::ConvertGPUTimestamp(uint64 timestamp, int GPUIndex)
{
	if (CalibrationTimestamps[GPUIndex].CPUMicroseconds == 0 || CalibrationTimestamps[GPUIndex].GPUMicroseconds == 0)
	{
		return (uint64)-1;
	}

	const uint64 cpuTimestampUs = timestamp - CalibrationTimestamps[GPUIndex].GPUMicroseconds + CalibrationTimestamps[GPUIndex].CPUMicroseconds;
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
						uint32 filter = 0;

						if (NAME_STATGROUP_CPUStalls == groupName)
						{
							color = Optick::Color::Tomato;
							filter = Optick::Filter::Wait;
						}

						for (int i = 0; i < sizeof(NAME_Wait) / sizeof(NAME_Wait[0]); ++i)
						{
							if (NAME_Wait[i] == shortName)
							{
								color = Optick::Color::White;
								filter = Optick::Filter::Wait;
							}
						}

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
			else if (Op == EStatOperation::AdvanceFrameEventRenderThread)
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
