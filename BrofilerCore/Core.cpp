#include "Core.h"
#include "Common.h"
#include "Event.h"
#include "ProfilerServer.h"
#include "EventDescriptionBoard.h"
#include "Thread.h"


extern "C" Brofiler::EventData* NextEvent()
{
	if (Brofiler::EventStorage* storage = Brofiler::Core::storage)
	{
		return &storage->NextEvent();
	}

	return nullptr;
}


namespace Brofiler
{


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
int64_t GetHighPrecisionTime()
{
	return MT::GetHighFrequencyTime();
}


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpProgress(const char* message)
{
	progressReportedLastTimestampMS = MT::GetTimeMilliSeconds();

	OutputDataStream stream;
	stream << message;

	Server::Get().Send(DataResponse::ReportProgress, stream);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpThread(const ThreadEntry& entry, const EventTime& timeSlice, ScopeData& scope)
{
	// Events
	if (!entry.storage.eventBuffer.IsEmpty())
	{
		const EventData* rootEvent = nullptr;

		entry.storage.eventBuffer.ForEach([&](const EventData& data)
		{
			if (data.finish >= data.start && data.start >= timeSlice.start && timeSlice.finish >= data.finish)
			{
				if (!rootEvent)
				{
					rootEvent = &data;
					scope.InitRootEvent(*rootEvent);
				} 
				else if (rootEvent->finish < data.finish)
				{
					scope.Send();

					rootEvent = &data;
					scope.InitRootEvent(*rootEvent);
				}
				else
				{
					scope.AddEvent(data);
				}
			}
		});

		scope.Send();
	}

	if (!entry.storage.synchronizationBuffer.IsEmpty())
	{
		OutputDataStream synchronizationStream;
		synchronizationStream << scope.header.boardNumber;
		synchronizationStream << scope.header.threadNumber;
		synchronizationStream << entry.storage.synchronizationBuffer;
		Server::Get().Send(DataResponse::Synchronization, synchronizationStream);
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpFrames()
{
	if (frames.empty() || threads.empty())
		return;

	DumpProgress("Collecting Frame Events...");

	//Graphics::Image image;
	//graphics.GetScreenshot(image);

	uint32 mainThreadIndex = 0;

	for (size_t i = 0; i < threads.size(); ++i)
	{
		if (threads[i]->description.threadID.IsEqual(mainThreadID))
		{
			mainThreadIndex = (uint32)i;
		}
	}

	EventTime timeSlice;
	timeSlice.start = frames.front().start;
	timeSlice.finish = frames.back().finish;

	OutputDataStream boardStream;

	static uint32 boardNumber = 0;
	boardStream << ++boardNumber;
	boardStream << MT::GetFrequency();
	boardStream << timeSlice;
	boardStream << threads;
	boardStream << fibers;
	boardStream << mainThreadIndex;
	boardStream << EventDescriptionBoard::Get();
	Server::Get().Send(DataResponse::FrameDescriptionBoard, boardStream);

	ScopeData scope;
	scope.header.boardNumber = (uint32)boardNumber;

	for (size_t i = 0; i < threads.size(); ++i)
	{
		scope.header.threadNumber = (uint32)i;
		DumpThread(*threads[i], timeSlice, scope);
	}

	for (size_t i = 0; i < fibers.size(); ++i)
	{
		scope.header.threadNumber = (uint32)(i + threads.size());
		DumpThread(*fibers[i], timeSlice, scope);
	}

	frames.clear();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpSamplingData()
{
#if USE_BROFILER_SAMPLING
	if (sampler.StopSampling())
	{
		DumpProgress("Collecting Sampling Events...");

		OutputDataStream stream;
		sampler.Serialize(stream);

		DumpProgress("Sending Message With Sampling Data...");
		Server::Get().Send(DataResponse::SamplingFrame, stream);
	}
#endif
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Core::Core() : progressReportedLastTimestampMS(0), isActive(false)
{
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::Update()
{
	MT::ScopedGuard guard(lock);
	
	if (isActive)
	{
		if (!frames.empty())
			frames.back().Stop();

		if (IsTimeToReportProgress())
			DumpCapturingProgress();		
	}

	UpdateEvents();

	if (isActive)
	{
		frames.push_back(EventTime());
		frames.back().Start();
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::UpdateEvents()
{
	if (!mainThreadID.IsValid())
	{
		mainThreadID = MT::ThreadId::Self();
	}

	Server::Get().Update();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::StartSampling()
{
#if USE_BROFILER_SAMPLING
	sampler.StartSampling(threads);
#endif
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::Activate( bool active )
{
	if (isActive != active)
	{
		isActive = active;

		for(auto it = threads.begin(); it != threads.end(); ++it)
		{
			ThreadEntry* entry = *it;
			entry->Activate(active);
		}

		for(auto it = fibers.begin(); it != fibers.end(); ++it)
		{
			ThreadEntry* entry = *it;
			entry->Activate(active);
		}

		if (active)
		{
#if USE_BROFILER_ETW
			EtwStatus status = etw.Start();
#else
			EtwStatus status = ETW_OK;
#endif
			SendHandshakeResponse(status);
		}
		else
		{
#if USE_BROFILER_ETW
			etw.Stop();
#endif
		}
	}

}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpCapturingProgress()
{
	std::stringstream stream;

	if (isActive)
		stream << "Capturing Frame " << (uint32)frames.size() << std::endl;

#if USE_BROFILER_SAMPLING
	if (sampler.IsActive())
		stream << "Sample Count " << (uint32)sampler.GetCollectedCount() << std::endl;
#endif

	DumpProgress(stream.str().c_str());
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::IsTimeToReportProgress() const
{
	return MT::GetTimeMilliSeconds() > progressReportedLastTimestampMS + 200;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::SendHandshakeResponse(EtwStatus status)
{
	OutputDataStream stream;
	stream << (uint32)status;
	Server::Get().Send(DataResponse::Handshake, stream);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::RegisterThread(const ThreadDescription& description, EventStorage** slot)
{
	MT::ScopedGuard guard(lock);
	ThreadEntry* entry = new ThreadEntry(description, slot);
	threads.push_back(new ThreadEntry(description, slot));

	if (isActive)
		*slot = &entry->storage;

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::RegisterFiber(const ThreadDescription& description, EventStorage** slot)
{
	MT::ScopedGuard guard(lock);
	ThreadEntry* entry = new ThreadEntry(description, slot);
	fibers.push_back(entry);

	if (isActive)
		*slot = &entry->storage;

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Core::~Core()
{
	for(auto it = threads.begin(); it != threads.end(); ++it)
	{
		ThreadEntry* entry = *it;
		delete entry;
	}

	for(auto it = fibers.begin(); it != fibers.end(); ++it)
	{
		ThreadEntry* entry = *it;
		delete entry;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const std::vector<ThreadEntry*>& Core::GetThreads() const
{
	return threads;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
mt_thread_local EventStorage* Core::storage = nullptr;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Core Core::notThreadSafeInstance;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
ScopeHeader::ScopeHeader() : boardNumber(0), threadNumber(0)
{

}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const ScopeHeader& header)
{
	return stream << header.boardNumber << header.threadNumber << header.event;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const ScopeData& ob)
{
	return stream << ob.header << ob.categories << ob.events;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const ThreadDescription& description)
{
	//FIXME, TODO: Remove cast to uint32
	return stream << (uint32)description.threadID.AsUInt64() << description.name;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const ThreadEntry* entry)
{
	return stream << entry->description;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API void NextFrame()
{
	return Core::NextFrame();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API bool IsActive()
{
	return Core::Get().isActive;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API EventStorage** GetEventStorageSlot()
{
	return &Core::Get().storage;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API bool RegisterThread(const char* name)
{
	return Core::Get().RegisterThread(ThreadDescription(name, MT::ThreadId::Self()), &Core::storage);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API bool RegisterFiber(const char* name, EventStorage** slot)
{
	return Core::Get().RegisterFiber(ThreadDescription(name, MT::ThreadId()), slot);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventStorage::EventStorage(): isSampling(0)
{
	 
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void ThreadEntry::Activate(bool isActive)
{
	if (isActive)
		storage.Clear(true);

	*threadTLS = isActive ? &storage : nullptr;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool IsSleepOnlyScope(const ScopeData& scope)
{
	if (!scope.categories.empty())
		return false;

	const std::vector<EventData>& events = scope.events;
	for(auto it = events.begin(); it != events.end(); ++it)
	{
		const EventData& data = *it;

		if (data.description->color != Color::White)
		{
			return false;
		}
	}

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void ScopeData::Send()
{
	if (!events.empty() || !categories.empty())
	{
		if (!IsSleepOnlyScope(*this))
		{
			OutputDataStream frameStream;
			frameStream << *this;
			Server::Get().Send(DataResponse::EventFrame, frameStream);
		}
	}

	Clear();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void ScopeData::Clear()
{
	events.clear();
	categories.clear();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
