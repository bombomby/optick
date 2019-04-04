#include "optick.config.h"

#if USE_OPTICK

#include "optick_core.h"
#include "optick_server.h"

#include "SymbolEngine.h"

#include <algorithm>
#include <fstream>

//////////////////////////////////////////////////////////////////////////
// Start of the Platform-specific stuff
//////////////////////////////////////////////////////////////////////////
#if defined(OPTICK_MSVC)
#include "optick_core.win.h"
#endif
#if defined(OPTICK_LINUX)
#include "optick_core.linux.h"
#endif
#if defined(OPTICK_OSX)
#include "optick_core.macos.h"
#endif
#if defined(OPTICK_PS4)
#include "optick_core.ps4.h"
#endif
//////////////////////////////////////////////////////////////////////////
// End of the Platform-specific stuff
//////////////////////////////////////////////////////////////////////////

extern "C" Optick::EventData* NextEvent()
{
	if (Optick::EventStorage* storage = Optick::Core::storage)
	{
		return &storage->NextEvent();
	}

	return nullptr;
}

namespace Optick
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void* (*Memory::allocate)(size_t) = operator new;
void  (*Memory::deallocate)(void* p) = operator delete;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
uint64_t MurmurHash64A(const void * key, int len, uint64_t seed)
{
	const uint64_t m = 0xc6a4a7935bd1e995;
	const int r = 47;

	uint64_t h = seed ^ (len * m);

	const uint64_t * data = (const uint64_t *)key;
	const uint64_t * end = data + (len / 8);

	while (data != end)
	{
		uint64_t k = *data++;

		k *= m;
		k ^= k >> r;
		k *= m;

		h ^= k;
		h *= m;
	}

	const unsigned char * data2 = (const unsigned char*)data;

	switch (len & 7)
	{
	case 7: h ^= uint64_t(data2[6]) << 48;
	case 6: h ^= uint64_t(data2[5]) << 40;
	case 5: h ^= uint64_t(data2[4]) << 32;
	case 4: h ^= uint64_t(data2[3]) << 24;
	case 3: h ^= uint64_t(data2[2]) << 16;
	case 2: h ^= uint64_t(data2[1]) << 8;
	case 1: h ^= uint64_t(data2[0]);
		h *= m;
	};

	h ^= h >> r;
	h *= m;
	h ^= h >> r;

	return h;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
uint64_t StringHash::CalcHash(const char* str)
{
	return MurmurHash64A(str, (int)strlen(str), 0);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Base 64
// https://renenyffenegger.ch/notes/development/Base64/Encoding-and-decoding-base-64-with-cpp
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
static const string base64_chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
static inline bool is_base64(unsigned char c) {
	return (isalnum(c) || (c == '+') || (c == '/'));
}
string base64_decode(string const& encoded_string) {
	int in_len = (int)encoded_string.size();
	int i = 0;
	int j = 0;
	int in_ = 0;
	unsigned char char_array_4[4], char_array_3[3];
	string ret;

	while (in_len-- && (encoded_string[in_] != '=') && is_base64(encoded_string[in_])) {
		char_array_4[i++] = encoded_string[in_]; in_++;
		if (i == 4) {
			for (i = 0; i < 4; i++)
				char_array_4[i] = (unsigned char)base64_chars.find(char_array_4[i]);

			char_array_3[0] = (char_array_4[0] << 2) + ((char_array_4[1] & 0x30) >> 4);
			char_array_3[1] = ((char_array_4[1] & 0xf) << 4) + ((char_array_4[2] & 0x3c) >> 2);
			char_array_3[2] = ((char_array_4[2] & 0x3) << 6) + char_array_4[3];

			for (i = 0; (i < 3); i++)
				ret += char_array_3[i];
			i = 0;
		}
	}

	if (i) {
		for (j = i; j < 4; j++)
			char_array_4[j] = 0;

		for (j = 0; j < 4; j++)
			char_array_4[j] = (unsigned char)base64_chars.find(char_array_4[j]);

		char_array_3[0] = (char_array_4[0] << 2) + ((char_array_4[1] & 0x30) >> 4);
		char_array_3[1] = ((char_array_4[1] & 0xf) << 4) + ((char_array_4[2] & 0x3c) >> 2);
		char_array_3[2] = ((char_array_4[2] & 0x3) << 6) + char_array_4[3];

		for (j = 0; (j < i - 1); j++) ret += char_array_3[j];
	}

	return ret;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Get current time in milliseconds
int64 GetTimeMilliSeconds()
{
	return Platform::GetTime() * 1000 / Platform::GetFrequency();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<class T>
OutputDataStream& operator<<(OutputDataStream& stream, const TagData<T>& ob)
{
	return stream << ob.timestamp << ob.description->index << ob.data;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// VS TODO: Replace with random access iterator for MemoryPool
template<class T, uint32 SIZE>
void SortMemoryPool(MemoryPool<T, SIZE>& memoryPool)
{
	size_t count = memoryPool.Size();
	if (count == 0)
		return;

	vector<T> memoryArray;
	memoryArray.resize(count);
	memoryPool.ToArray(&memoryArray[0]);

	std::sort(memoryArray.begin(), memoryArray.end());

	memoryPool.Clear(true);

	for (const T& item : memoryArray)
		memoryPool.Add(item);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription* EventDescription::Create(const char* eventName, const char* fileName, const unsigned long fileLine, const unsigned long eventColor /*= Color::Null*/, const unsigned long filter /*= 0*/)
{
	return EventDescriptionBoard::Get().CreateDescription(eventName, fileName, fileLine, eventColor, filter);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription* EventDescription::CreateShared(const char* eventName, const char* fileName, const unsigned long fileLine, const unsigned long eventColor /*= Color::Null*/, const unsigned long filter /*= 0*/)
{
	return EventDescriptionBoard::Get().CreateSharedDescription(eventName, fileName, fileLine, eventColor, filter);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription::EventDescription() : name(""), file(""), line(0), color(0)
{
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription& EventDescription::operator=(const EventDescription&)
{
	OPTICK_FAILED("It is pointless to copy EventDescription. Please, check you logic!"); return *this;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventData* Event::Start(const EventDescription& description)
{
	EventData* result = nullptr;

	if (EventStorage* storage = Core::storage)
	{
		result = &storage->NextEvent();
		result->description = &description;
		result->Start();
	}
	return result;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Event::Stop(EventData& data)
{
	if (EventStorage* storage = Core::storage)
	{
		data.Stop();
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void OPTICK_INLINE PushEvent(EventStorage* pStorage, const EventDescription* description, int64_t timestampStart)
{
	if (EventStorage* storage = pStorage)
	{
		EventData& result = storage->NextEvent();
		result.description = description;
		result.start = timestampStart;
		result.finish = EventTime::INVALID_TIMESTAMP;
		storage->pushPopEventStack[storage->pushPopEventStackIndex++] = &result;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void OPTICK_INLINE PopEvent(EventStorage* pStorage, int64_t timestampFinish)
{
	if (EventStorage* storage = pStorage)
		if (storage->pushPopEventStackIndex > 0)
			storage->pushPopEventStack[--storage->pushPopEventStackIndex]->finish = timestampFinish;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Event::Push(const char* name)
{
	if (EventStorage* storage = Core::storage)
	{
		EventDescription* desc = EventDescription::CreateShared(name);
		Push(*desc);
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Event::Push(const EventDescription& description)
{
	PushEvent(Core::storage, &description, GetHighPrecisionTime());
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Event::Pop()
{
	PopEvent(Core::storage, GetHighPrecisionTime());
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Event::Add(EventStorage* storage, const EventDescription* description, int64_t timestampStart, int64_t timestampFinish)
{
	EventData& data = storage->eventBuffer.Add();
	data.description = description;
	data.start = timestampStart;
	data.finish = timestampFinish;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Event::Push(EventStorage* storage, const EventDescription* description, int64_t timestampStart)
{
	PushEvent(storage, description, timestampStart);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Event::Pop(EventStorage* storage, int64_t timestampFinish)
{
	PopEvent(storage, timestampFinish);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventData* GPUEvent::Start(const EventDescription& description)
{
	EventData* result = nullptr;

	if (EventStorage* storage = Core::storage)
		result = storage->gpuStorage.Start(description);

	return result;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void GPUEvent::Stop(EventData& data)
{
	if (EventStorage* storage = Core::storage)
		storage->gpuStorage.Stop(data);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void FiberSyncData::AttachToThread(EventStorage* storage, uint64_t threadId)
{
	if (storage)
	{
		FiberSyncData& data = storage->fiberSyncBuffer.Add();
		data.Start();
		data.finish = EventTime::INVALID_TIMESTAMP;
		data.threadId = threadId;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void FiberSyncData::DetachFromThread(EventStorage* storage)
{
	if (storage)
	{
		if (FiberSyncData* syncData = storage->fiberSyncBuffer.Back())
		{
			syncData->Stop();
		}
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, float val)
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagFloatBuffer.Add(TagFloat(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, int32_t val)
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagS32Buffer.Add(TagS32(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, uint32_t val)
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagU32Buffer.Add(TagU32(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, uint64_t val)
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagU64Buffer.Add(TagU64(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, float val[3])
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagPointBuffer.Add(TagPoint(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, const char* val)
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagStringBuffer.Add(TagString(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream & operator<<(OutputDataStream &stream, const EventDescription &ob)
{
	byte flags = 0;
	return stream << ob.name << ob.file << ob.line << ob.filter << ob.color << (float)0.0f << flags;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const EventTime& ob)
{
	return stream << ob.start << ob.finish;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const EventData& ob)
{
	return stream << (EventTime)(ob) << (ob.description ? ob.description->index : (uint32)-1);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const SyncData& ob)
{
	return stream << (EventTime)(ob) << ob.core << ob.reason << ob.newThreadId;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const FiberSyncData& ob)
{
	return stream << (EventTime)(ob) << ob.threadId;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
static std::mutex& GetBoardLock()
{
	// Initialize as static local variable to prevent problems with static initialization order
	static std::mutex lock;
	return lock;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescriptionBoard& EventDescriptionBoard::Get()
{
	return instance;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const EventDescriptionList& EventDescriptionBoard::GetEvents() const
{
	return boardDescriptions;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription* EventDescriptionBoard::CreateDescription(const char* name, const char* file /*= nullptr*/, uint32_t line /*= 0*/, uint32_t color /*= Color::Null*/, uint32_t filter /*= 0*/)
{
	std::lock_guard<std::mutex> lock(GetBoardLock());

	size_t index = boardDescriptions.Size();

	EventDescription& desc = boardDescriptions.Add();
	desc.index = (uint32)index;
	desc.name = name;
	desc.file = file;
	desc.line = line;
	desc.color = color;
	desc.filter = filter;

	return &desc;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription* EventDescriptionBoard::CreateSharedDescription(const char* name, const char* file /*= nullptr*/, uint32_t line /*= 0*/, uint32_t color /*= Color::Null*/, uint32_t filter /*= 0*/)
{
	StringHash nameHash(name);

	std::lock_guard<std::mutex> lock(sharedLock);

	std::pair<DescriptionMap::iterator, bool> cached = sharedDescriptions.insert({ nameHash, nullptr });

	if (cached.second)
	{
		const char* nameCopy = sharedNames.Add(name, strlen(name) + 1, false);
		cached.first->second = CreateDescription(nameCopy, file, line, color, filter);
	}

	return cached.first->second;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator << (OutputDataStream& stream, const EventDescriptionBoard& ob)
{
	std::lock_guard<std::mutex> lock(GetBoardLock());
	stream << ob.GetEvents();
	return stream;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescriptionBoard EventDescriptionBoard::instance;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
ProcessDescription::ProcessDescription(const char* processName, ProcessID pid, uint64 key) : name(processName), processID(pid), uniqueKey(key)
{
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
ThreadDescription::ThreadDescription(const char* threadName, ThreadID tid, ProcessID pid, int32 _maxDepth /*= 1*/, int32 _priority /*= 0*/, uint32 _mask /*= 0*/)
	: name(threadName), threadID(tid), processID(pid), maxDepth(_maxDepth), priority(_priority), mask(_mask)
{
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
int64_t GetHighPrecisionTime()
{
	return Platform::GetTime();
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
int64_t GetHighPrecisionFrequency()
{
	return Platform::GetFrequency();
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream & operator<<(OutputDataStream &stream, const SysCallData &ob)
{
	return stream << (const EventData&)ob << ob.threadID << ob.id;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
SysCallData& SysCallCollector::Add()
{
	return syscallPool.Add();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void SysCallCollector::Clear()
{
	syscallPool.Clear(false);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool SysCallCollector::Serialize(OutputDataStream& stream)
{
	stream << syscallPool;

	if (!syscallPool.IsEmpty())
	{
		syscallPool.Clear(false);
		return true;
	}

	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void CallstackCollector::Add(const CallstackDesc& desc)
{
	if (uint64* storage = callstacksPool.TryAdd(desc.count + 3))
	{
		storage[0] = desc.threadID;
		storage[1] = desc.timestamp;
		storage[2] = desc.count;

		for (uint64 i = 0; i < desc.count; ++i)
		{
			storage[3 + i] = desc.callstack[desc.count - i - 1];
		}
	}
	else
	{
		uint64& item0 = callstacksPool.Add();
		uint64& item1 = callstacksPool.Add();
		uint64& item2 = callstacksPool.Add();

		item0 = desc.threadID;
		item1 = desc.timestamp;
		item2 = desc.count;

		for (uint64 i = 0; i < desc.count; ++i)
		{
			callstacksPool.Add() = desc.callstack[desc.count - i - 1];
		}
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void CallstackCollector::Clear()
{
	callstacksPool.Clear(false);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool CallstackCollector::SerializeModules(OutputDataStream& stream)
{
	if (SymbolEngine* symEngine = Core::Get().symbolEngine)
	{
		stream << symEngine->GetModules();
		return true;
	}
	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool CallstackCollector::SerializeSymbols(OutputDataStream& stream)
{
	typedef unordered_set<uint64> SymbolSet;
	SymbolSet symbolSet;

	for (CallstacksPool::const_iterator it = callstacksPool.begin(); it != callstacksPool.end();)
	{
		CallstacksPool::const_iterator startIt = it;
		OPTICK_UNUSED(startIt);

		uint64 threadID = *it;
		OPTICK_UNUSED(threadID);
		++it; //Skip ThreadID
		uint64 timestamp = *it;
		OPTICK_UNUSED
		(timestamp);
		++it; //Skip Timestamp
		uint64 count = *it;
		count = (count & 0xFF);
		++it; //Skip Count

		bool isBadAddrFound = false;

		for (uint64 i = 0; i < count; ++i)
		{
			uint64 address = *it;
			++it;

			if (address == 0)
			{
				isBadAddrFound = true;
			}

			if (!isBadAddrFound)
			{
				symbolSet.insert(address);
			}
		}
	}

	SymbolEngine* symEngine = Core::Get().symbolEngine;

	vector<const Symbol*> symbols;
	symbols.reserve(symbolSet.size());

	size_t callstackIndex = 0;

	Core::Get().DumpProgress("Resolving addresses... ");

	if (symEngine)
	{
		for (auto it = symbolSet.begin(); it != symbolSet.end(); ++it)
		{
			callstackIndex++;

			uint64 address = *it;
			if (const Symbol* symbol = symEngine->GetSymbol(address))
			{
				symbols.push_back(symbol);
			}
		}
	}

	stream << symbols;
	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool CallstackCollector::SerializeCallstacks(OutputDataStream& stream)
{
	stream << callstacksPool;

	if (!callstacksPool.IsEmpty())
	{
		callstacksPool.Clear(false);
		return true;
	}

	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool CallstackCollector::IsEmpty() const
{
	return callstacksPool.IsEmpty();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream & operator<<(OutputDataStream &stream, const SwitchContextDesc &ob)
{
	return stream << ob.timestamp << ob.oldThreadId << ob.newThreadId << ob.cpuId << ob.reason;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void SwitchContextCollector::Add(const SwitchContextDesc& desc)
{
	switchContextPool.Add() = desc;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void SwitchContextCollector::Clear()
{
	switchContextPool.Clear(false);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool SwitchContextCollector::Serialize(OutputDataStream& stream)
{
	stream << switchContextPool;

	if (!switchContextPool.IsEmpty())
	{
		switchContextPool.Clear(false);
		return true;
	}

	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if defined(OPTICK_MSVC)
#define CPUID(INFO, ID) __cpuid(INFO, ID)
#include <intrin.h> 
#elif defined(OPTICK_GCC)
#include <cpuid.h>
#define CPUID(INFO, ID) __cpuid(ID, INFO[0], INFO[1], INFO[2], INFO[3])
#else
#error Platform is not supported!
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
string GetCPUName()
{
	int cpuInfo[4] = { -1 };
	char cpuBrandString[0x40] = { 0 };
	CPUID(cpuInfo, 0x80000000);
	unsigned nExIds = cpuInfo[0];
	for (unsigned i = 0x80000000; i <= nExIds; ++i)
	{
		CPUID(cpuInfo, i);
		if (i == 0x80000002)
			memcpy(cpuBrandString, cpuInfo, sizeof(cpuInfo));
		else if (i == 0x80000003)
			memcpy(cpuBrandString + 16, cpuInfo, sizeof(cpuInfo));
		else if (i == 0x80000004)
			memcpy(cpuBrandString + 32, cpuInfo, sizeof(cpuInfo));
	}
	return string(cpuBrandString);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::StartCapture()
{
	pendingState = State::START_CAPTURE;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::StopCapture()
{
	pendingState = State::STOP_CAPTURE;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpCapture()
{
	pendingState = State::DUMP_CAPTURE;
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
	OPTICK_ASSERT(entry.storage.fiberSyncBuffer.IsEmpty(), "Fiber switch events in native threads?");
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
EventTime CalculateRange(const ThreadEntry& entry, const EventDescription* rootDescription)
{
	EventTime timeSlice = { INT64_MAX, INT64_MIN };
	entry.storage.eventBuffer.ForEach([&](const EventData& data)
	{
		if (data.description == rootDescription)
		{
			timeSlice.start = std::min(timeSlice.start, data.start);
			timeSlice.finish = std::max(timeSlice.finish, data.finish);
		}
	});
	return timeSlice;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpFrames(uint32 mode)
{
    std::lock_guard<std::recursive_mutex> lock(threadsLock);
    
	if (frames.empty() || threads.empty())
		return;

	++boardNumber;

	DumpProgress("Generating summary...");

	GenerateCommonSummary();
	DumpSummary();

	DumpProgress("Collecting Frame Events...");

	uint32 mainThreadIndex = 0;
	for (size_t i = 0; i < threads.size(); ++i)
		if (threads[i]->description.threadID == mainThreadID)
			mainThreadIndex = (uint32)i;

	EventTime timeSlice = CalculateRange(*threads[mainThreadIndex], GetFrameDescription(FrameType::CPU)); 
	if (timeSlice.start >= timeSlice.finish)
	{
		timeSlice.start = frames.front().start;
		timeSlice.finish = frames.back().finish;
	}

	DumpBoard(mode, timeSlice, mainThreadIndex);

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
		callstackCollector.SerializeModules(symbolsStream);
		callstackCollector.SerializeSymbols(symbolsStream);
		Server::Get().Send(DataResponse::CallstackDescriptionBoard, symbolsStream);

		DumpProgress("Serializing callstacks");
		OutputDataStream callstacksStream;
		callstacksStream << boardNumber;
		callstackCollector.SerializeCallstacks(callstacksStream);
		Server::Get().Send(DataResponse::CallstackPack, callstacksStream);
	}

	Server::Get().Send(DataResponse::NullFrame, OutputDataStream::Empty);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpSummary()
{
	OutputDataStream stream;

	// Board Number
	stream << boardNumber;

	// Frames
	double frequency = (double)Platform::GetFrequency();
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
	std::lock_guard<std::recursive_mutex> lock(threadsLock);

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

void Core::DumpBoard(uint32 mode, EventTime timeSlice, uint32 mainThreadIndex)
{
	OutputDataStream boardStream;

	boardStream << boardNumber;
	boardStream << Platform::GetFrequency();
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
	boardStream << processDescs;
	boardStream << threadDescs;
	boardStream << (uint32)Platform::GetProcessID();
	boardStream << (uint32)std::thread::hardware_concurrency();
	Server::Get().Send(DataResponse::FrameDescriptionBoard, boardStream);

	// Cleanup
	processDescs.clear();
	threadDescs.clear();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::GenerateCommonSummary()
{
	AttachSummary("Platform", Platform::GetName());
	AttachSummary("CPU", GetCPUName().c_str());
	if (gpuProfiler)
		AttachSummary("GPU", gpuProfiler->GetName().c_str());
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Core::Core() 
	: progressReportedLastTimestampMS(0)
	, boardNumber(0)
	, frameNumber(0)
	, stateCallback(nullptr)
	, currentState(State::DUMP_CAPTURE)
	, pendingState(State::DUMP_CAPTURE)
	, isActive(false)
	, gpuProfiler(nullptr)
	, tracer(nullptr)
	, symbolEngine(nullptr)
{
	mainThreadID = Platform::GetThreadID();
#if OPTICK_ENABLE_TRACING

	tracer = Platform::GetTrace();
	symbolEngine = SymbolEngine::Get();
#endif

	frameDescriptions[FrameType::CPU] = EventDescription::Create("CPU Frame", __FILE__, __LINE__);
	frameDescriptions[FrameType::GPU] = EventDescription::Create("GPU Frame", __FILE__, __LINE__);
	frameDescriptions[FrameType::Render] = EventDescription::Create("Render Frame", __FILE__, __LINE__);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::UpdateState()
{
	if (currentState != pendingState)
	{
		State::Type nextState = pendingState;
		if (pendingState == State::DUMP_CAPTURE && currentState == State::START_CAPTURE)
			nextState = State::STOP_CAPTURE;

		if ((stateCallback != nullptr) && !stateCallback(nextState))
			return false;

		switch (nextState)
		{
		case State::START_CAPTURE:
			Activate(true);
			break;

		case State::STOP_CAPTURE:
			Activate(false);
			break;

		case State::DUMP_CAPTURE:
			DumpFrames();
			break;
		}
		currentState = nextState;
		return true;
	}
	return false;
}


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

	while (UpdateState()) {}

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
	switchContextCollector.Add(desc);
	return true;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::ReportStackWalk(const CallstackDesc& desc)
{
	callstackCollector.Add(desc);
	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::Activate( bool active )
{
	if (isActive != active)
	{
		isActive = active;

        {
            std::lock_guard<std::recursive_mutex> lock(threadsLock);
            for(auto it = threads.begin(); it != threads.end(); ++it)
            {
                ThreadEntry* entry = *it;
                entry->Activate(active);
            }
        }


		if (active)
		{
			CaptureStatus::Type status = CaptureStatus::ERR_TRACER_FAILED;

			if (tracer)
			{
                std::lock_guard<std::recursive_mutex> lock(threadsLock);
				status = tracer->Start(Trace::ALL, threads);
				// Let's retry with more narrow setup
				if (status != CaptureStatus::OK)
					status = tracer->Start(Trace::SWITCH_CONTEXTS, threads);
			}

			if (gpuProfiler)
				gpuProfiler->Start(0);

			SendHandshakeResponse(status);
		}
		else
		{
			if (tracer)
				tracer->Stop();

			if (gpuProfiler)
				gpuProfiler->Stop(0);
		}
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::DumpCapturingProgress()
{
	stringstream stream;

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
	stream << Platform::GetName();
	stream << Server::Get().GetHostName();
	Server::Get().Send(DataResponse::Handshake, stream);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::IsRegistredThread(ThreadID id)
{
	std::lock_guard<std::recursive_mutex> lock(threadsLock);

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
	std::lock_guard<std::recursive_mutex> lock(threadsLock);

	ThreadEntry* entry = Memory::New<ThreadEntry>(description, slot);
	threads.push_back(entry);

	if (isActive && slot != nullptr)
		*slot = &entry->storage;

	return entry;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::UnRegisterThread(ThreadID threadID, bool keepAlive)
{
	std::lock_guard<std::recursive_mutex> lock(threadsLock);

	for (ThreadList::iterator it = threads.begin(); it != threads.end(); ++it)
	{
		ThreadEntry* entry = *it;
		if (entry->description.threadID == threadID && entry->isAlive)
		{
			if (!isActive && !keepAlive)
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
bool Core::RegisterProcessDescription(const ProcessDescription& description)
{
	processDescs.push_back(description);
	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::RegisterThreadDescription(const ThreadDescription& description)
{
	threadDescs.push_back(description);
	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::SetStateChangedCallback(StateCallback cb)
{
	stateCallback = cb;
	return stateCallback != nullptr;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::AttachSummary(const char* key, const char* value)
{
	summary.push_back(make_pair(string(key), string(value)));
	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::AttachFile(File::Type type, const char* name, const uint8_t* data, uint32_t size)
{
	if (size > 0)
	{
		attachments.push_back(Attachment(type, name));
		Attachment& attachment = attachments.back();
		attachment.data.resize(size);
		memcpy(&attachment.data[0], data, size);
		return true;
	}
	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::AttachFile(File::Type type, const char* name, std::istream& stream)
{
	std::streampos beg = stream.tellg();
	stream.seekg(0, std::ios::end);
	std::streampos end = stream.tellg();
	stream.seekg(beg, std::ios::beg);

	size_t size =(size_t)(end - beg);
	void* buffer = Memory::Alloc(size);

	stream.read((char*)buffer, size);
	bool result = AttachFile(type, name, (uint8*)buffer, (uint32_t)size);

	Memory::Free(buffer);
	return result;

}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::AttachFile(File::Type type, const char* name, const char* path)
{
    std::ifstream stream(path, std::ios::binary);
	return AttachFile(type, name, stream);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::AttachFile(File::Type type, const char* name, const wchar_t* path)
{
#if defined(OPTICK_MSVC)
	std::ifstream stream(path, std::ios::binary);
	return AttachFile(type, name, stream);
#else
	char p[256] = { 0 };
	wcstombs(p, path, sizeof(p));
    std::ifstream stream(p, std::ios::binary);
	return AttachFile(type, name, stream);
#endif
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Core::InitGPUProfiler(GPUProfiler* profiler)
{
	OPTICK_ASSERT(gpuProfiler == nullptr, "Can't reinitialize GPU profiler! Not supported yet!");
	Memory::Delete<GPUProfiler>(gpuProfiler);
	gpuProfiler = profiler;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Core::SetPassword(const string& encodedPassword)
{
	if (tracer)
	{
		string decoded = base64_decode(encodedPassword);
		tracer->SetPassword(decoded.c_str());
		return true;
	}
	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const EventDescription* Core::GetFrameDescription(FrameType::Type frame) const
{
	return frameDescriptions[frame];
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Core::~Core()
{
	std::lock_guard<std::recursive_mutex> lock(threadsLock);

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
const vector<ThreadEntry*>& Core::GetThreads() const
{
	return threads;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_THREAD_LOCAL EventStorage* Core::storage = nullptr;
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
	return stream << description.threadID << description.processID << description.name << description.maxDepth << description.priority << description.mask;
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
OutputDataStream& operator<<(OutputDataStream& stream, const ProcessDescription& description)
{
	return stream << description.processID << description.name << description.uniqueKey;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API bool SetStateChangedCallback(StateCallback cb)
{
	return Core::Get().SetStateChangedCallback(cb);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API bool AttachSummary(const char* key, const char* value)
{
	return Core::Get().AttachSummary(key, value);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API bool AttachFile(File::Type type, const char* name, const uint8_t* data, uint32_t size)
{
	return Core::Get().AttachFile(type, name, data, size);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API bool AttachFile(File::Type type, const char* name, const char* path)
{
	return Core::Get().AttachFile(type, name, path);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API bool AttachFile(File::Type type, const char* name, const wchar_t* path)
{
	return Core::Get().AttachFile(type, name, path);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const Point& ob)
{
	return stream << ob.x << ob.y << ob.z;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API uint32_t NextFrame()
{
	return Core::NextFrame();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API bool IsActive()
{
	return Core::Get().isActive;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API EventStorage** GetEventStorageSlotForCurrentThread()
{
	return &Core::Get().storage;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API bool IsFiberStorage(EventStorage* fiberStorage)
{
	return fiberStorage->isFiberStorage;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API bool RegisterThread(const char* name)
{
	return Core::Get().RegisterThread(ThreadDescription(name, Platform::GetThreadID(), Platform::GetProcessID()), &Core::storage) != nullptr;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API bool RegisterThread(const wchar_t* name)
{
	const int THREAD_NAME_LENGTH = 128;
	char mbName[THREAD_NAME_LENGTH];
	wcstombs_s(mbName, name, THREAD_NAME_LENGTH);

	return Core::Get().RegisterThread(ThreadDescription(mbName, Platform::GetThreadID(), Platform::GetProcessID()), &Core::storage) != nullptr;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API bool UnRegisterThread(bool keepAlive)
{
	return Core::Get().UnRegisterThread(Platform::GetThreadID(), keepAlive);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API bool RegisterFiber(uint64 fiberId, EventStorage** slot)
{
	return Core::Get().RegisterFiber(FiberDescription(fiberId), slot);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API EventStorage* RegisterStorage(const char* name, uint64_t threadID, ThreadMask::Type type)
{
	ThreadEntry* entry = Core::Get().RegisterThread(ThreadDescription(name, threadID, Platform::GetProcessID(), 1, 0, type), nullptr);
	return entry ? &entry->storage : nullptr;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API void GpuFlip(void* swapChain)
{
	if (GPUProfiler* gpuProfiler = Core::Get().gpuProfiler)
		gpuProfiler->Flip(swapChain);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OPTICK_API GPUContext SetGpuContext(GPUContext context)
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
OPTICK_API const EventDescription* GetFrameDescription(FrameType::Type frame)
{
	return Core::Get().GetFrameDescription(frame);

}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventStorage::EventStorage(): pushPopEventStackIndex(0), isFiberStorage(false)
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
	//if (!scope.categories.empty())
	//	return false;

	const vector<EventData>& events = scope.events;
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
	for (size_t i = 0; i < gpuBuffer.size(); ++i)
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

#endif //USE_OPTICK