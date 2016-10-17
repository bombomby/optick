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

	CleanupThreads();
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
void Core::CleanupThreads()
{
	MT::ScopedGuard guard(lock);

	for (ThreadList::iterator it = threads.begin(); it != threads.end();)
	{
		if (!(*it)->isAlive)
		{
			(*it)->~ThreadEntry();
			MT::Memory::Free(*it);
			it = threads.erase(it);
		}
		else
		{
			++it;
		}
	}

	for (ThreadList::iterator it = fibers.begin(); it != fibers.end();)
	{
		if (!(*it)->isAlive)
		{
			(*it)->~ThreadEntry();
			MT::Memory::Free(*it);
			it = fibers.erase(it);
		}
		else
		{
			++it;
		}
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Core::Core() : progressReportedLastTimestampMS(0), isActive(false)
{
	schedulerTracer = ISchedulerTracer::Get();
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
void Core::ReportSwitchContext(const SwitchContextDesc& desc)
{
	for (size_t i = 0; i < threads.size(); ++i)
	{
		ThreadEntry* entry = threads[i];

		if (entry->description.threadID.AsUInt64() == desc.oldThreadId)
		{
			if (SyncData* time = entry->storage.synchronizationBuffer.Back())
			{
				time->finish = desc.timestamp;
				time->reason = desc.reason;
			}
		}

		if (entry->description.threadID.AsUInt64() == desc.newThreadId)
		{
			SyncData& time = entry->storage.synchronizationBuffer.Add();
			time.start = desc.timestamp;
			time.finish = time.start;
			time.core = desc.cpuId;
		}
	}
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
			SchedulerTraceStatus::Type status = schedulerTracer->Start();
			SendHandshakeResponse(status);
		}
		else
		{
			schedulerTracer->Stop();
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
void Core::SendHandshakeResponse(SchedulerTraceStatus::Type status)
{
	OutputDataStream stream;
	stream << (uint32)status;
	Server::Get().Send(DataResponse::Handshake, stream);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::RegisterThread(const ThreadDescription& description, EventStorage** slot)
{
	MT::ScopedGuard guard(lock);
	ThreadEntry* entry = new (MT::Memory::Alloc(sizeof(ThreadEntry), BRO_CACHE_LINE_SIZE)) ThreadEntry(description, slot);
	threads.push_back(entry);

	if (isActive)
		*slot = &entry->storage;

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::UnRegisterThread(MT::ThreadId threadID)
{
	MT::ScopedGuard guard(lock);
	for (ThreadList::iterator it = threads.begin(); it != threads.end(); ++it)
	{
		ThreadEntry* entry = *it;
		if (entry->description.threadID.IsEqual(threadID) && entry->isAlive)
		{
			if (!isActive)
			{
				entry->~ThreadEntry();
				MT::Memory::Free(entry);
				threads.erase(it);
				return true;
			}
			else
			{
				entry->isAlive = false;
				return true;
			}
		}
	}

	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::RegisterFiber(const ThreadDescription& description, EventStorage** slot)
{
	MT::ScopedGuard guard(lock);
	ThreadEntry* entry = new (MT::Memory::Alloc(sizeof(ThreadEntry), BRO_CACHE_LINE_SIZE)) ThreadEntry(description, slot);
	fibers.push_back(entry);

	if (isActive)
		*slot = &entry->storage;

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Core::~Core()
{
	MT::ScopedGuard guard(lock);

	for (ThreadList::iterator it = threads.begin(); it != threads.end(); ++it)
	{
		(*it)->~ThreadEntry();
		MT::Memory::Free((*it));
	}
	threads.clear();

	for (ThreadList::iterator it = fibers.begin(); it != fibers.end(); ++it)
	{
		(*it)->~ThreadEntry();
		MT::Memory::Free((*it));
	}
	fibers.clear();
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
	return stream << description.threadID.AsUInt64() << description.name;
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
BROFILER_API bool UnRegisterThread()
{
	return Core::Get().UnRegisterThread(MT::ThreadId::Self());
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
	if (!isAlive)
		return;

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
