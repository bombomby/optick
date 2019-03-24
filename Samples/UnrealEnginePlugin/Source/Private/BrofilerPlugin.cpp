// Copyright 1998-2018 Epic Games, Inc. All Rights Reserved.

// Core
#include "CoreMinimal.h"
#include "Containers/Ticker.h"
#include "HAL/PlatformFilemanager.h"
#include "Misc/EngineVersion.h"
#include "Modules/ModuleManager.h"
#include "Stats/StatsData.h"
#include "DesktopPlatformModule.h"
#include "IBrofilerPlugin.h"

// Engine
#include "UnrealClient.h"

#if WITH_EDITOR
#include "Editor/EditorPerformanceSettings.h"
#endif

#include <BrofilerCore/Brofiler.h>

DEFINE_LOG_CATEGORY(BrofilerLog);

static FName NAME_STATGROUP_CPUStalls(FStatGroup_STATGROUP_CPUStalls::GetGroupName());
static FName NAME_Wait[] =
{
	FName("STAT_FQueuedThread_Run_WaitForWork"),
	FName("STAT_TaskGraph_OtherStalls"),
};
static FName NAME_STAT_ROOT("STAT_FEngineLoop_Tick_CallAllConsoleVariableSinks");
static FName NAME_STAT_ROOT_RAW = FName();
static FString BROFILER_SCREENSHOT_NAME(TEXT("UE4_Brofiler_Screenshot.png"));


class FBrofilerPlugin : public IBrofilerPlugin
{
	bool IsCapturing;

	const uint64 LowMask =  0x00000000FFFFFFFFULL;
	const uint64 HighMask = 0xFFFFFFFF00000000ULL;

	uint64 OriginTimestamp;

	FDelegateHandle TickDelegateHandle;
	FDelegateHandle StatFrameDelegateHandle;

	TMap<uint32, Brofiler::EventStorage*> StorageMap;
	TMap<FName, Brofiler::EventDescription*> DescriptionMap;

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
#endif

	void OnScreenshotProcessed();
public:
	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

	void StartCapture();
	void StopCapture();

	void RequestScreenshot();
	bool IsReadyToDumpCapture() const;
};

IMPLEMENT_MODULE( FBrofilerPlugin, BlankPlugin )
DECLARE_DELEGATE_OneParam(FStatFrameDelegate, int64);


bool FBrofilerPlugin::Tick(float DeltaTime)
{
	Brofiler::NextFrame();
	//BROFILER_FRAME("FBrofilerPlugin");

	return true;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool OnBrofilerStateChanged(Brofiler::BroState state)
{
	FBrofilerPlugin& plugin = (FBrofilerPlugin&)IBrofilerPlugin::Get();

	UE_LOG(BrofilerLog, Display, TEXT("State => %d"), state);

	switch (state)
	{
	case Brofiler::BRO_START_CAPTURE:
		plugin.StartCapture();
		break;

	case Brofiler::BRO_STOP_CAPTURE:
		plugin.RequestScreenshot();
		plugin.StopCapture();
		break;

	case Brofiler::BRO_DUMP_CAPTURE:
		if (!plugin.IsReadyToDumpCapture())
			return false;

		Brofiler::AttachSummary("Platform", FPlatformProperties::PlatformName());
		Brofiler::AttachSummary("Build", __DATE__ " " __TIME__);
		Brofiler::AttachSummary("UnrealVersion", TCHAR_TO_ANSI(*FEngineVersion::Current().ToString(EVersionComponent::Changelist)));
		Brofiler::AttachSummary("GPU", TCHAR_TO_ANSI(*FPlatformMisc::GetPrimaryGPUBrand()));

		FString FullName = BROFILER_SCREENSHOT_NAME;
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

			Brofiler::AttachFile(Brofiler::BroFile::BRO_IMAGE, "Screenshot.png", Image.GetData(), (uint32_t)FileSize);

			//PlatformFile.DeleteFile(*FullName);
		}


		// Attach text file
		//char* textFile = "Hello World!";
		//Brofiler::AttachFile(Brofiler::BroFile::BRO_OTHER, "Test.txt", (uint8_t*)textFile, strlen(textFile));

		break;
	}
	return true;
}

void FBrofilerPlugin::StartupModule()
{
	UE_LOG(BrofilerLog, Display, TEXT("BrofilerPlugin Loaded!"));

	// Subscribing for Ticker
	TickDelegateHandle = FTicker::GetCoreTicker().AddTicker(FTickerDelegate::CreateRaw(this, &FBrofilerPlugin::Tick));

	// Register Brofiler callback
	Brofiler::SetStateChangedCallback(OnBrofilerStateChanged);
}


void FBrofilerPlugin::ShutdownModule()
{
	// Remove delegate
	FTicker::GetCoreTicker().RemoveTicker(TickDelegateHandle);

	// Stop capture if needed
	StopCapture();

	UE_LOG(BrofilerLog, Display, TEXT("BrofilerPlugin UnLoaded!"));
}

void FBrofilerPlugin::StartCapture()
{
	if (!IsCapturing)
	{
		IsCapturing = true;

#if WITH_EDITOR
		UEditorPerformanceSettings* Settings = GetMutableDefault<UEditorPerformanceSettings>();
		check(Settings);
		BaseSettings.bCPUThrottleEnabled = Settings->bThrottleCPUWhenNotForeground;
		Settings->bThrottleCPUWhenNotForeground = false;
		Settings->PostEditChange();
		Settings->SaveConfig();
#endif

		OriginTimestamp = FPlatformTime::Cycles64();

		FThreadStats::MasterEnableAdd(1);

		FStatsThreadState& Stats = FStatsThreadState::GetLocalState();

		// Set up our delegate to gather data from the stats thread for safe consumption on game thread.
		StatFrameDelegateHandle = Stats.NewFrameDelegate.Add(FStatFrameDelegate::CreateRaw(this, &FBrofilerPlugin::GetDataFromStatsThread));
	}
}

void FBrofilerPlugin::StopCapture()
{
	if (IsCapturing)
	{
		IsCapturing = false;
		FThreadStats::MasterEnableSubtract(1);

		FStatsThreadState& Stats = FStatsThreadState::GetLocalState();

		// Set up our delegate to gather data from the stats thread for safe consumption on game thread.
		Stats.NewFrameDelegate.Remove(StatFrameDelegateHandle);
		StatFrameDelegateHandle.Reset();

#if WITH_EDITOR
		UEditorPerformanceSettings* Settings = GetMutableDefault<UEditorPerformanceSettings>();
		check(Settings);
		Settings->bThrottleCPUWhenNotForeground = BaseSettings.bCPUThrottleEnabled;
		Settings->PostEditChange();
		Settings->SaveConfig();
#endif
	}
	
}

void FBrofilerPlugin::RequestScreenshot()
{
	// Requesting screenshot
	UE_LOG(BrofilerLog, Display, TEXT("Screenshot requested!"));
	WaitingForScreenshot = true;
	FScreenshotRequest::OnScreenshotRequestProcessed().AddRaw(this, &FBrofilerPlugin::OnScreenshotProcessed);
	FScreenshotRequest::RequestScreenshot(BROFILER_SCREENSHOT_NAME, true, false);
}

bool FBrofilerPlugin::IsReadyToDumpCapture() const
{
	return WaitingForScreenshot == false;
}

void FBrofilerPlugin::OnScreenshotProcessed()
{
	UE_LOG(BrofilerLog, Display, TEXT("Screenshot processed!"));
	WaitingForScreenshot = false;
	FScreenshotRequest::OnScreenshotRequestProcessed().RemoveAll(this);
}

void FBrofilerPlugin::GetDataFromStatsThread(int64 CurrentFrame)
{
	QUICK_SCOPE_CYCLE_COUNTER(STAT_FBrofilerPlugin_Upd);

#if STATS
	const FStatsThreadState& Stats = FStatsThreadState::GetLocalState();
	const FStatPacketArray& Frame = Stats.GetStatPacketArray(CurrentFrame);

	for (int32 PacketIndex = 0; PacketIndex < Frame.Packets.Num(); PacketIndex++)
	{
		FStatPacket const& Packet = *Frame.Packets[PacketIndex];
		const FName ThreadName = Stats.GetStatThreadName(Packet);

		Brofiler::EventStorage* Storage = nullptr;

		if (Brofiler::EventStorage** ppStorage = StorageMap.Find(Packet.ThreadId))
		{
			Storage = *ppStorage;
		}
		else
		{
			Storage = Brofiler::RegisterStorage(TCHAR_TO_ANSI(*ThreadName.ToString()), Packet.ThreadId);
			StorageMap.Add(Packet.ThreadId, Storage);
		}

		for (FStatMessage const& Item : Packet.StatMessages)
		{
			EStatOperation::Type Op = Item.NameAndInfo.GetField<EStatOperation>();

			if (Op == EStatOperation::CycleScopeStart || Op == EStatOperation::CycleScopeEnd)
			{
				FName name = Item.NameAndInfo.GetRawName();

				uint64 Timestamp = (OriginTimestamp & HighMask) | (Item.GetValue_int64() & LowMask);
				// Handle Overflow
				if (Timestamp < OriginTimestamp)
					Timestamp += (1ULL << 32ULL);

				if (Op == EStatOperation::CycleScopeStart)
				{
					Brofiler::EventDescription* Description = nullptr;

					if (Brofiler::EventDescription** ppDescription = DescriptionMap.Find(name))
					{
						Description = *ppDescription;
					}
					else
					{
						const FName shortName = Item.NameAndInfo.GetShortName();
						const FName groupName = Item.NameAndInfo.GetGroupName();

						uint32 color = 0;

						if (shortName == NAME_STAT_ROOT)
							NAME_STAT_ROOT_RAW = name;

						if (NAME_STATGROUP_CPUStalls == groupName)
							color = Brofiler::Color::White;

						for (int i = 0; i < sizeof(NAME_Wait) / sizeof(NAME_Wait[0]); ++i)
							if (NAME_Wait[i] == shortName)
								color = Brofiler::Color::White;

						Description = Brofiler::EventDescription::CreateShared(TCHAR_TO_ANSI(*shortName.ToString()), nullptr, 0, color);

						DescriptionMap.Add(name, Description);
					}

					// Processing root event of the frame
					if (name == NAME_STAT_ROOT_RAW)
					{
						const Brofiler::EventDescription* cpuFrameDescription = Brofiler::GetFrameDescription(Brofiler::FrameType::CPU);
						// Pop the previous frame event
						BROFILER_STORAGE_POP(Storage, Timestamp);
						// Push the new one
						BROFILER_STORAGE_PUSH(Storage, cpuFrameDescription, Timestamp);
					}
					
					BROFILER_STORAGE_PUSH(Storage, Description, Timestamp);
				}
				else
				{
					BROFILER_STORAGE_POP(Storage, Timestamp);
				}
					
			}
		}
	}
#endif
}
