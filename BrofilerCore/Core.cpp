#include "Core.h"
#include "Common.h"
#include "Event.h"
#include "Timer.h"
#include "ProfilerServer.h"
#include "EventDescriptionBoard.h"

#include "Platform/SchedulerTrace.h"
#include "Platform/SymbolEngine.h"


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
// VS TODO: Replace with random access iterator for MemoryPool
template<class T, uint32 SIZE>
void SortMemoryPool(MemoryPool<T, SIZE>& memoryPool)
{
	size_t count = memoryPool.Size();
	if (count == 0)
		return;

	std::vector<T> memoryArray;
	memoryArray.resize(count);
	memoryPool.ToArray(&memoryArray[0]);

	std::sort(memoryArray.begin(), memoryArray.end());

	memoryPool.Clear(true);

	for (const T& item : memoryArray)
		memoryPool.Add(item);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
ThreadDescription::ThreadDescription(const char* threadName, ThreadID id, bool _fromOtherProcess, int32 _maxDepth /*= 1*/, int32 _priority /*= 0*/, uint32 _mask /*= 0*/)
	: threadID(id), fromOtherProcess(_fromOtherProcess), maxDepth(_maxDepth), priority(_priority), mask(_mask)
{
	strcpy_s(name, threadName);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
int64_t GetHighPrecisionTime()
{
	return GetTime();
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
int64_t GetHighPrecisionFrequency()
{
	return GetFrequency();
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpProgress(const char* message)
{
	progressReportedLastTimestampMS = GetTimeMilliSeconds();

	OutputDataStream stream;
	stream << message;

	Server::Get().Send(DataResponse::ReportProgress, stream);
}


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpEvents(EventStorage& entry, const EventTime& timeSlice, ScopeData& scope)
{
	if (!entry.eventBuffer.IsEmpty())
	{
		const EventData* rootEvent = nullptr;

		entry.eventBuffer.ForEach([&](const EventData& data)
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

		entry.eventBuffer.Clear(false);
	}
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpTags(EventStorage& entry, ScopeData& scope)
{
	if (!entry.tagFloatBuffer.IsEmpty() ||
		!entry.tagS32Buffer.IsEmpty() ||
		!entry.tagU32Buffer.IsEmpty() ||
		!entry.tagU64Buffer.IsEmpty() ||
		!entry.tagPointBuffer.IsEmpty() ||
		!entry.tagStringBuffer.IsEmpty())
	{
		OutputDataStream tagStream;
		tagStream << scope.header.boardNumber << scope.header.threadNumber;
		tagStream  
			<< (uint32)0
			<< entry.tagFloatBuffer
			<< entry.tagU32Buffer
			<< entry.tagS32Buffer
			<< entry.tagU64Buffer
			<< entry.tagPointBuffer
			<< (uint32)0
			<< (uint32)0
			<< entry.tagStringBuffer;
		Server::Get().Send(DataResponse::TagsPack, tagStream);

		entry.ClearTags(false);
	}
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpThread(ThreadEntry& entry, const EventTime& timeSlice, ScopeData& scope)
{
	// We need to sort events for all the custom thread storages
	if (entry.description.threadID == INVALID_THREAD_ID)
		entry.Sort();

	// Events
	DumpEvents(entry.storage, timeSlice, scope);
	DumpTags(entry.storage, scope);
	BRO_ASSERT(entry.storage.fiberSyncBuffer.IsEmpty(), "Fiber switch events in native threads?");
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpFiber(FiberEntry& entry, const EventTime& timeSlice, ScopeData& scope)
{
	// Events
	DumpEvents(entry.storage, timeSlice, scope);

	if (!entry.storage.fiberSyncBuffer.IsEmpty())
	{
		OutputDataStream fiberSynchronizationStream;
		fiberSynchronizationStream << scope.header.boardNumber;
		fiberSynchronizationStream << scope.header.fiberNumber;
		fiberSynchronizationStream << entry.storage.fiberSyncBuffer;
		Server::Get().Send(DataResponse::FiberSynchronizationData, fiberSynchronizationStream);
	}
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpFrames(uint32 mode)
{
	if (frames.empty() || threads.empty())
		return;

	++boardNumber;

	DumpProgress("Generating summary...");

	if (stateCallback != nullptr)
	{
		DumpProgress("Generating summary...");
		stateCallback(BRO_DUMP_CAPTURE);
	}

	DumpSummary();

	DumpProgress("Collecting Frame Events...");

	EventTime timeSlice;
	timeSlice.start = frames.front().start;
	timeSlice.finish = frames.back().finish;

	DumpBoard(mode, timeSlice);

	ScopeData threadScope;
	threadScope.header.boardNumber = boardNumber;
	threadScope.header.fiberNumber = -1;

	if (gpuProfiler)
		gpuProfiler->Dump(mode);

	for (size_t i = 0; i < threads.size(); ++i)
	{
		threadScope.header.threadNumber = (uint32)i;
		DumpThread(*threads[i], timeSlice, threadScope);
	}

	ScopeData fiberScope;
	fiberScope.header.boardNumber = (uint32)boardNumber;
	fiberScope.header.threadNumber = -1;
	for (size_t i = 0; i < fibers.size(); ++i)
	{
		fiberScope.header.fiberNumber = (uint32)i;
		DumpFiber(*fibers[i], timeSlice, fiberScope);
	}

	frames.clear();
	CleanupThreadsAndFibers();

	{
		DumpProgress("Serializing SwitchContexts");
		OutputDataStream switchContextsStream;
		switchContextsStream << boardNumber;
		switchContextCollector.Serialize(switchContextsStream);
		Server::Get().Send(DataResponse::SynchronizationData, switchContextsStream);
	}

	{
		DumpProgress("Serializing SysCalls");
		OutputDataStream callstacksStream;
		callstacksStream << boardNumber;
		syscallCollector.Serialize(callstacksStream);
		Server::Get().Send(DataResponse::SyscallPack, callstacksStream);
	}

	if (!callstackCollector.IsEmpty())
	{
		DumpProgress("Resolving callstacks");
		OutputDataStream symbolsStream;
		symbolsStream << boardNumber;
		callstackCollector.SerializeSymbols(symbolsStream);
		Server::Get().Send(DataResponse::CallstackDescriptionBoard, symbolsStream);

		DumpProgress("Serializing callstacks");
		OutputDataStream callstacksStream;
		callstacksStream << boardNumber;
		callstackCollector.SerializeCallstacks(callstacksStream);
		Server::Get().Send(DataResponse::CallstackPack, callstacksStream);
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpSummary()
{
	OutputDataStream stream;

	// Board Number
	stream << boardNumber;

	// Frames
	double frequency = (double)GetFrequency();
	stream << (uint32_t)frames.size();
	for (const EventTime& frame : frames)
	{
		double frameTimeMs = 1000.0 * (frame.finish - frame.start) / frequency;
		stream << (float)frameTimeMs;
	}

	// Summary
	stream << (uint32_t)summary.size();
	for (size_t i = 0; i < summary.size(); ++i)
		stream << summary[i].first << summary[i].second;
	summary.clear();

	// Attachments
	stream << (uint32_t)attachments.size();
	for (const Attachment& att : attachments)
		stream << (uint32_t)att.type << att.name << att.data;
	attachments.clear();

	// Send
	Server::Get().Send(DataResponse::SummaryPack, stream);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::CleanupThreadsAndFibers()
{
	std::lock_guard<std::recursive_mutex> lock(coreLock);

	for (ThreadList::iterator it = threads.begin(); it != threads.end();)
	{
		if (!(*it)->isAlive)
		{
			Memory::Delete(*it);
			it = threads.erase(it);
		}
		else
		{
			++it;
		}
	}
}

void Core::DumpBoard(uint32 mode, EventTime timeSlice)
{
	uint32 mainThreadIndex = 0;

	for (size_t i = 0; i < threads.size(); ++i)
		if (threads[i]->description.threadID == mainThreadID)
			mainThreadIndex = (uint32)i;

	OutputDataStream boardStream;

	boardStream << boardNumber;
	boardStream << GetFrequency();
	boardStream << (uint64)0; // Origin
	boardStream << (uint32)0; // Precision
	boardStream << timeSlice;
	boardStream << threads;
	boardStream << fibers;
	boardStream << mainThreadIndex;
	boardStream << EventDescriptionBoard::Get();
	boardStream << (uint32)0; // Tags
	boardStream << (uint32)0; // Run
	boardStream << (uint32)0; // Filters
	boardStream << (uint32)0; // ThreadDescs
	boardStream << mode; // Mode
	Server::Get().Send(DataResponse::FrameDescriptionBoard, boardStream);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Core::Core() : progressReportedLastTimestampMS(0), isActive(false), stateCallback(nullptr), boardNumber(0), gpuProfiler(nullptr), frameNumber(0)
{
	mainThreadID = GetThreadID();
	schedulerTrace = SchedulerTrace::Get();
	symbolEngine = SymbolEngine::Get();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
uint32_t Core::Update()
{
	std::lock_guard<std::recursive_mutex> lock(coreLock);
	
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

	return ++frameNumber;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::UpdateEvents()
{
	Server::Get().Update();
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::ReportSwitchContext(const SwitchContextDesc& desc)
{
	if (!schedulerTrace)
	{
		return false;
	}

	// finalize work interval
	// auto oldThreadIt = schedulerTrace->activeThreadsIDs.find(desc.oldThreadId);
	// if (oldThreadIt != schedulerTrace->activeThreadsIDs.end())
	{
		switchContextCollector.Add(desc);
	}

	return true;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::ReportStackWalk(const CallstackDesc& desc)
{
	if (!schedulerTrace)
	{
		return false;
	}

	auto it = schedulerTrace->activeThreadsIDs.find(desc.threadID);
	if (it == schedulerTrace->activeThreadsIDs.end())
	{
		return false;
	}

	callstackCollector.Add(desc);
	return true;
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

		if (active)
		{
			CaptureStatus::Type status = schedulerTrace->Start(SchedulerTrace::ALL, threads, true);
			// Let's retry with more narrow setup
			if (status != CaptureStatus::OK)
				status = schedulerTrace->Start(SchedulerTrace::SWITCH_CONTEXTS, threads, true);

			if (gpuProfiler)
				gpuProfiler->Start(0);

			SendHandshakeResponse(status);
		}
		else
		{
			schedulerTrace->Stop();

			if (gpuProfiler)
				gpuProfiler->Stop(0);
		}

		if (stateCallback != nullptr)
			stateCallback(isActive ? BRO_START_CAPTURE : BRO_STOP_CAPTURE);
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpCapturingProgress()
{
	std::stringstream stream;

	if (isActive)
		stream << "Capturing Frame " << (uint32)frames.size() << std::endl;

	DumpProgress(stream.str().c_str());
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::IsTimeToReportProgress() const
{
	return GetTimeMilliSeconds() > progressReportedLastTimestampMS + 200;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::SendHandshakeResponse(CaptureStatus::Type status)
{
	OutputDataStream stream;
	stream << (uint32)status;
	Server::Get().Send(DataResponse::Handshake, stream);
}


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::IsRegistredThread(ThreadID id)
{
	std::lock_guard<std::recursive_mutex> lock(coreLock);

	for (ThreadList::iterator it = threads.begin(); it != threads.end(); ++it)
	{
		ThreadEntry* entry = *it;
		if (entry->description.threadID == id)
		{
			return true;
		}
	}
	return false;
}


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
ThreadEntry* Core::RegisterThread(const ThreadDescription& description, EventStorage** slot)
{
	std::lock_guard<std::recursive_mutex> lock(coreLock);

	ThreadEntry* entry = Memory::New<ThreadEntry>(description, slot);
	threads.push_back(entry);

	if (isActive && slot != nullptr)
		*slot = &entry->storage;

	return entry;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::UnRegisterThread(ThreadID threadID)
{
	std::lock_guard<std::recursive_mutex> lock(coreLock);

	for (ThreadList::iterator it = threads.begin(); it != threads.end(); ++it)
	{
		ThreadEntry* entry = *it;
		if (entry->description.threadID == threadID && entry->isAlive)
		{
			if (!isActive)
			{
				Memory::Delete(entry);
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
bool Core::RegisterFiber(const FiberDescription& description, EventStorage** slot)
{
	std::lock_guard<std::recursive_mutex> lock(coreLock);
	FiberEntry* entry = Memory::New<FiberEntry>(description);
	fibers.push_back(entry);
	entry->storage.isFiberStorage = true;
	*slot = &entry->storage;
	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::SetStateChangedCallback(BroStateCallback cb)
{
	stateCallback = cb;
	return stateCallback != nullptr;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::AttachSummary(const char* key, const char* value)
{
	summary.push_back(make_pair(std::string(key), std::string(value)));
	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::AttachFile(BroFile::Type type, const char* name, const uint8_t* data, size_t size)
{
	attachments.push_back(Attachment(type, name));
	Attachment& attachment = attachments.back();
	attachment.data = std::vector<uint8_t>(data, data + size);
	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::InitGPUProfiler(GPUProfiler* profiler)
{
	BRO_ASSERT(gpuProfiler == nullptr, "Can't reinitialize GPU profiler! Not supported yet!");
	Memory::Delete<GPUProfiler>(gpuProfiler);
	gpuProfiler = profiler;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Core::~Core()
{
	std::lock_guard<std::recursive_mutex> lock(coreLock);

	for (ThreadList::iterator it = threads.begin(); it != threads.end(); ++it)
	{
		Memory::Delete(*it);
	}
	threads.clear();

	for (FiberList::iterator it = fibers.begin(); it != fibers.end(); ++it)
	{
		Memory::Delete(*it);
	}
	fibers.clear();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const std::vector<ThreadEntry*>& Core::GetThreads() const
{
	return threads;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bro_thread_local EventStorage* Core::storage = nullptr;
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
	return stream << header.boardNumber << header.threadNumber << header.fiberNumber << header.event;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const ScopeData& ob)
{
	return stream << ob.header << ob.categories << ob.events;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const ThreadDescription& description)
{
	return stream << description.threadID << description.name << description.maxDepth << description.priority << description.mask;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const ThreadEntry* entry)
{
	return stream << entry->description;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const FiberDescription& description)
{
	return stream << description.id;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const FiberEntry* entry)
{
	return stream << entry->description;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API bool SetStateChangedCallback(BroStateCallback cb)
{
	return Core::Get().SetStateChangedCallback(cb);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API bool AttachSummary(const char* key, const char* value)
{
	return Core::Get().AttachSummary(key, value);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API bool AttachFile(BroFile::Type type, const char* name, const uint8_t* data, size_t size)
{
	return Core::Get().AttachFile(type, name, data, size);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const BroPoint& ob)
{
	return stream << ob.x << ob.y << ob.z;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API uint32_t NextFrame()
{
	return Core::NextFrame();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API bool IsActive()
{
	return Core::Get().isActive;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API EventStorage** GetEventStorageSlotForCurrentThread()
{
	return &Core::Get().storage;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API bool IsFiberStorage(EventStorage* fiberStorage)
{
	return fiberStorage->isFiberStorage;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API bool RegisterThread(const char* name)
{
	return Core::Get().RegisterThread(ThreadDescription(name, GetThreadID(), false), &Core::storage) != nullptr;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API bool RegisterThread(const wchar_t* name)
{
	char mbName[ThreadDescription::THREAD_NAME_LENGTH];
	size_t numConverter = 0;
	wcstombs_s(&numConverter, mbName, name, ThreadDescription::THREAD_NAME_LENGTH);
	return Core::Get().RegisterThread(ThreadDescription(mbName, GetThreadID(), false), &Core::storage) != nullptr;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API bool UnRegisterThread()
{
	return Core::Get().UnRegisterThread(GetThreadID());
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API bool RegisterFiber(uint64 fiberId, EventStorage** slot)
{
	return Core::Get().RegisterFiber(FiberDescription(fiberId), slot);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API EventStorage* RegisterStorage(const char* name)
{
	ThreadEntry* entry = Core::Get().RegisterThread(ThreadDescription(name, INVALID_THREAD_ID, false), nullptr);
	return entry ? &entry->storage : nullptr;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API void GpuFlip(void* swapChain)
{
	if (GPUProfiler* gpuProfiler = Core::Get().gpuProfiler)
		gpuProfiler->Flip(swapChain);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API GPUContext SetGpuContext(GPUContext context)
{
	if (EventStorage* storage = Core::storage)
	{
		GPUContext prevContext = storage->gpuStorage.context;
		storage->gpuStorage.context = context;
		return prevContext;
	}
	return GPUContext();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventStorage::EventStorage(): isFiberStorage(false), pushPopEventStackIndex(0)
{
	 
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void ThreadEntry::Activate(bool isActive)
{
	if (!isAlive)
		return;

	if (isActive)
		storage.Clear(true);

	if (threadTLS != nullptr)
	{
		*threadTLS = isActive ? &storage : nullptr;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void ThreadEntry::Sort()
{
	SortMemoryPool(storage.eventBuffer);
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
void EventStorage::GPUStorage::Clear(bool preserveMemory)
{
	for (int i = 0; i < MAX_GPU_NODES; ++i)
		for (int j = 0; j < GPU_QUEUE_COUNT; ++j)
			gpuBuffer[i][j].Clear(preserveMemory);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventData* EventStorage::GPUStorage::Start(const EventDescription &desc)
{
	if (GPUProfiler* gpuProfiler = Core::Get().gpuProfiler)
	{
		EventData& result = gpuBuffer[context.node][context.queue].Add();
		result.description = &desc;
		result.start = EventTime::INVALID_TIMESTAMP;
		result.finish = EventTime::INVALID_TIMESTAMP;
		gpuProfiler->QueryTimestamp(context.cmdBuffer, &result.start);
		return &result;
	}
	return nullptr;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void EventStorage::GPUStorage::Stop(EventData& data)
{
	if (GPUProfiler* gpuProfiler = Core::Get().gpuProfiler)
	{
		gpuProfiler->QueryTimestamp(context.cmdBuffer, &data.finish);
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
