#include "Common.h"
#include "Core.h"
#include "Event.h"
#include "ProfilerServer.h"
#include "EventDescriptionBoard.h"
#include "Thread.h"
#include "HPTimer.h"

//#include "Hook.h"

extern "C" Profiler::EventData* NextEvent()
{
	if (Profiler::EventStorage* storage = Profiler::Core::storage)
	{
		return &storage->NextEvent();
	}

	return nullptr;
}


namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
static const uint32 INVALID_THREAD_ID = (uint32)-1;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpProgress(const char* message)
{
	progressReportedLastTimestampMS = GetTimeMilliSeconds();

	OutputDataStream stream;
	stream << message;

	Server::Get().Send(DataResponse::ReportProgress, stream);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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
		if (threads[i]->description.threadID == mainThreadID)
			mainThreadIndex = (uint32)i;

	EventTime timeSlice;
	timeSlice.start = frames.front().start;
	timeSlice.finish = frames.back().finish;

	OutputDataStream boardStream;

	static uint32 boardNumber = 0;
	boardStream << ++boardNumber;
	boardStream << GetFrequency();
	boardStream << timeSlice;
	boardStream << threads;
	boardStream << mainThreadIndex;
	boardStream << EventDescriptionBoard::Get();
	Server::Get().Send(DataResponse::FrameDescriptionBoard, boardStream);

	ScopeData scope;
	scope.header.boardNumber = (uint32)boardNumber;

	std::vector<EventTime> syncronization;

	for (size_t i = 0; i < threads.size(); ++i)
	{
		ThreadEntry* entry = threads[i];
		scope.header.threadNumber = (uint32)i;

		syncronization.resize(entry->storage.synchronizationBuffer.Size());

		if (!syncronization.empty())
			entry->storage.synchronizationBuffer.ToArray(&syncronization[0]);

		size_t synchronizationIndex = 0;

		// Events
		if (!entry->storage.eventBuffer.IsEmpty())
		{
			const EventData* rootEvent = nullptr;

			entry->storage.eventBuffer.ForEach([&](EventData& data)
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
						synchronizationIndex = scope.AddSynchronization(syncronization, synchronizationIndex);
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

			synchronizationIndex = scope.AddSynchronization(syncronization, synchronizationIndex);
			scope.Send();
		}
	}

	frames.clear();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpSamplingData()
{
	if (sampler.StopSampling())
	{
		DumpProgress("Collecting Sampling Events...");

		OutputDataStream stream;
		sampler.Serialize(stream);

		DumpProgress("Sending Message With Sampling Data...");
		Server::Get().Send(DataResponse::SamplingFrame, stream);
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Core::Core() : mainThreadID(INVALID_THREAD_ID), isActive(false), progressReportedLastTimestampMS(0)
{
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::Update()
{
	CRITICAL_SECTION(lock);
	
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
	DWORD currentThreadID = GetCurrentThreadId();

	if (mainThreadID == INVALID_THREAD_ID)
		mainThreadID = currentThreadID;

	Server::Get().Update();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::StartSampling()
{
	sampler.StartSampling(threads);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::Activate( bool active )
{
	if (isActive != active)
	{
		isActive = active;

		for each (ThreadEntry* entry in threads)
			entry->Activate(active);

		if (active)
		{
			ETW::Status status = etw.Start();
			SendHandshakeResponse(status);
		}
		else
		{
			etw.Stop();
		}
	}

}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpCapturingProgress()
{
	std::stringstream stream;

	if (isActive)
		stream << "Capturing Frame " << (uint32)frames.size() << std::endl;

	if (sampler.IsActive())
		stream << "Sample Count " << (uint32)sampler.GetCollectedCount() << std::endl;

	DumpProgress(stream.str().c_str());
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::IsTimeToReportProgress() const
{
	return GetTimeMilliSeconds() > progressReportedLastTimestampMS + 200;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::SendHandshakeResponse(ETW::Status status)
{
	OutputDataStream stream;
	stream << (uint32)status;
	Server::Get().Send(DataResponse::Handshake, stream);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool InstallSynchronizationHooks(DWORD threadID);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::RegisterThread(const ThreadDescription& description)
{
	CRITICAL_SECTION(lock);

	for each (const ThreadEntry* entry in threads)
		if (entry->description.threadID == description.threadID)
			return false;

	ThreadEntry* entry = new ThreadEntry(description, &storage);
	threads.push_back(entry);

	InstallSynchronizationHooks(description.threadID);

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Core::~Core()
{
	for each (ThreadEntry* entry in threads)
		delete entry;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const std::vector<ThreadEntry*>& Core::GetThreads() const
{
	return threads;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventStorage* Core::storage = nullptr;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Core Core::notThreadSafeInstance;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
ScopeHeader::ScopeHeader() : threadNumber(0), boardNumber(0)
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
	return stream << ob.header << ob.categories << ob.synchronization << ob.events;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const ThreadDescription& description)
{
	return stream << description.threadID << description.name;
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
EventStorage::EventStorage(): isSampling(0)
{
	 
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
ThreadDescription::ThreadDescription(const char* threadName /*= "MainThread"*/) : name(threadName), threadID(GetCurrentThreadId())
{
	Core::Get().RegisterThread(*this);
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
	if (!scope.categories.empty() || scope.synchronization.empty())
		return false;

	for each (const EventData& data in scope.events)
		if (data.description->color != Color::White)
			return false;

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
	synchronization.clear();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
