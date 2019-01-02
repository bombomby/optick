#pragma once
#include <mutex>
#include <thread>

#include "Common.h"
#include "ThreadID.h"

#include "Event.h"
#include "MemoryPool.h"
#include "Serialization.h"

#include "Platform/GPUProfiler.h"

#include <array>
#include <atomic>
#include <map>
#include <list>

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct SchedulerTrace;
struct SymbolEngine;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct ScopeHeader
{
	EventTime event;
	uint32 boardNumber;
	int32 threadNumber;
	int32 fiberNumber;

	ScopeHeader();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator << ( OutputDataStream& stream, const ScopeHeader& ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct ScopeData
{
	ScopeHeader header;
	std::vector<EventData> categories;
	std::vector<EventData> events;

	void AddEvent(const EventData& data)
	{
		events.push_back(data);
		if (data.description->color != Color::Null)
		{
			categories.push_back(data);
		}
	}

	void InitRootEvent(const EventData& data)
	{
		header.event = data;
		AddEvent(data);
	}

	void Send();
	void Clear();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<int N>
struct BroString
{
	char data[N];
	BroString() {}
	BroString<N>& operator=(const char* text) { strncpy(data, text, N - 1); return *this; }
	BroString(const char* text) { *this = text; }
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct BroPoint
{
	float x, y, z;
	BroPoint() {}
	BroPoint(float _x, float _y, float _z) : x(_x), y(_y), z(_z) {}
	BroPoint(float pos[3]) : x(pos[0]), y(pos[1]), z(pos[2]) {}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<int N>
OutputDataStream& operator<<(OutputDataStream& stream, const BroString<N>& ob)
{
	return stream << ob.data;
}
OutputDataStream& operator<<(OutputDataStream& stream, const BroPoint& ob);
OutputDataStream& operator<<(OutputDataStream& stream, const ScopeData& ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef MemoryPool<EventData, 1024> EventBuffer;
typedef MemoryPool<const EventData*, 32> CategoryBuffer;
typedef MemoryPool<SyncData, 1024> SynchronizationBuffer;
typedef MemoryPool<FiberSyncData, 1024> FiberSyncBuffer;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef BroString<32> ShortString;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef TagData<float> TagFloat;
typedef TagData<int32> TagS32;
typedef TagData<uint32> TagU32;
typedef TagData<uint64> TagU64;
typedef TagData<BroPoint> TagPoint;
typedef TagData<ShortString> TagString;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef MemoryPool<TagFloat, 128> TagFloatBuffer;
typedef MemoryPool<TagS32, 128> TagS32Buffer;
typedef MemoryPool<TagU32, 128> TagU32Buffer;
typedef MemoryPool<TagU64, 128> TagU64Buffer;
typedef MemoryPool<TagPoint, 64> TagPointBuffer;
typedef MemoryPool<TagString, 64> TagStringBuffer;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct EventStorage
{
	EventBuffer eventBuffer;
	CategoryBuffer categoryBuffer; 
	FiberSyncBuffer fiberSyncBuffer;

	TagFloatBuffer tagFloatBuffer;
	TagS32Buffer tagS32Buffer;
	TagU32Buffer tagU32Buffer;
	TagU64Buffer tagU64Buffer;
	TagPointBuffer tagPointBuffer;
	TagStringBuffer tagStringBuffer;

	struct GPUStorage
	{
		std::array<std::array<EventBuffer, GPU_QUEUE_COUNT>, MAX_GPU_NODES> gpuBuffer;
		GPUContext context;

		void Clear(bool preserveMemory);
		
		EventData* Start(const EventDescription& desc);
		void Stop(EventData& data);
	};
	GPUStorage gpuStorage;

	uint32					   pushPopEventStackIndex;
	std::array<EventData*, 32> pushPopEventStack;

	bool isFiberStorage;

	EventStorage();

	BRO_INLINE EventData& NextEvent() 
	{
		return eventBuffer.Add(); 
	}

	BRO_INLINE void RegisterCategory(const EventData& eventData) 
	{ 
		categoryBuffer.Add() = &eventData;
	}

	// Free all temporary memory
	void Clear(bool preserveContent)
	{
		eventBuffer.Clear(preserveContent);
		categoryBuffer.Clear(preserveContent);
		fiberSyncBuffer.Clear(preserveContent);
		gpuStorage.Clear(preserveContent);
		ClearTags(preserveContent);

		while (pushPopEventStackIndex)
		{
			pushPopEventStack[--pushPopEventStackIndex] = nullptr;
		}
	}

	void ClearTags(bool preserveContent)
	{
		tagFloatBuffer.Clear(preserveContent);
		tagS32Buffer.Clear(preserveContent);
		tagU32Buffer.Clear(preserveContent);
		tagU64Buffer.Clear(preserveContent);
		tagPointBuffer.Clear(preserveContent);
		tagStringBuffer.Clear(preserveContent);
	}

	void Reset()
	{
		Clear(true);
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct ProcessDescription
{
	std::string name;
	ProcessID processID;
	uint64 uniqueKey;
	ProcessDescription(const char* processName, ProcessID pid, uint64 key);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct ThreadDescription
{
	std::string name;
	ThreadID threadID;
	ProcessID processID;
	int32 maxDepth;
	int32 priority;
	uint32 mask;

	ThreadDescription(const char* threadName, ThreadID tid, ProcessID pid, int32 maxDepth = 1, int32 priority = 0, uint32 mask = 0);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct FiberDescription
{
	uint64 id; 

	FiberDescription(uint64 _id)
		: id(_id)
	{}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct ThreadEntry
{
	ThreadDescription description;
	EventStorage storage;
	EventStorage** threadTLS;

	bool isAlive;

	ThreadEntry(const ThreadDescription& desc, EventStorage** tls) : description(desc), threadTLS(tls), isAlive(true) {}
	void Activate(bool isActive);
	void Sort();
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct FiberEntry
{
	FiberDescription description;
	EventStorage storage;

	FiberEntry(const FiberDescription& desc) : description(desc) {}
};


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef std::vector<ThreadEntry*> ThreadList;
typedef std::vector<FiberEntry*> FiberList;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct SysCallData : EventData
{
	uint64 id;
	uint64 threadID;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream &operator << (OutputDataStream &stream, const SysCallData &ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class SysCallCollector
{
	typedef MemoryPool<SysCallData, 1024 * 32> SysCallPool;
public:
	SysCallPool syscallPool;

	SysCallData& Add();
	void Clear();

	bool Serialize(OutputDataStream& stream);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct CallstackDesc
{
	uint64 threadID;
	uint64 timestamp;
	uint64* callstack;
	uint8 count;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class CallstackCollector
{
	// Packed callstack list: {ThreadID, Timestamp, Count, Callstack[Count]}
	typedef MemoryPool<uint64, 1024 * 32> CallstacksPool;
	CallstacksPool callstacksPool;
public:
	void Add(const CallstackDesc& desc);
	void Clear();

	bool SerializeSymbols(OutputDataStream& stream);
	bool SerializeCallstacks(OutputDataStream& stream);

	bool IsEmpty() const;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct SwitchContextDesc
{
	int64_t timestamp;
	uint64 oldThreadId;
	uint64 newThreadId;
	uint8 cpuId;
	uint8 reason;
};
//////////////////////////////////////////////////////////////////////////
OutputDataStream &operator << (OutputDataStream &stream, const SwitchContextDesc &ob);
//////////////////////////////////////////////////////////////////////////
class SwitchContextCollector
{
	typedef MemoryPool<SwitchContextDesc, 1024 * 32> SwitchContextPool;
	SwitchContextPool switchContextPool;
public:
	void Add(const SwitchContextDesc& desc);
	void Clear();
	bool Serialize(OutputDataStream& stream);
};
//////////////////////////////////////////////////////////////////////////


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct CaptureStatus
{
	enum Type
	{
		OK = 0,
		ERR_TRACER_ALREADY_EXISTS = 1,
		ERR_TRACER_ACCESS_DENIED = 2,
		FAILED = 3,
	};
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class Core
{
	std::recursive_mutex coreLock;
	ThreadID mainThreadID;

	ThreadList threads;
	FiberList fibers;

	int64 progressReportedLastTimestampMS;

	std::vector<EventTime> frames;
	uint32 boardNumber;

	CallstackCollector callstackCollector;
	SwitchContextCollector switchContextCollector;

	std::vector<std::pair<std::string, std::string>> summary;

	std::atomic<uint32_t> frameNumber;

	struct Attachment
	{
		std::string name;
		std::vector<uint8_t> data;
		BroFile::Type type;
		Attachment(BroFile::Type t, const char* n) : type(t), name(n) {}
	};
	std::list<Attachment> attachments;

	BroStateCallback stateCallback;

	std::vector<ProcessDescription> processDescs;
	std::vector<ThreadDescription> threadDescs;


	void UpdateEvents();
	uint32_t Update();

	Core();
	~Core();

	static Core notThreadSafeInstance;

	void DumpCapturingProgress();
	void SendHandshakeResponse(CaptureStatus::Type status);


	void DumpEvents(EventStorage& entry, const EventTime& timeSlice, ScopeData& scope);
	void DumpTags(EventStorage& entry, ScopeData& scope);
	void DumpThread(ThreadEntry& entry, const EventTime& timeSlice, ScopeData& scope);
	void DumpFiber(FiberEntry& entry, const EventTime& timeSlice, ScopeData& scope);

	void CleanupThreadsAndFibers();

	void DumpBoard(uint32 mode, EventTime timeSlice);

	void GenerateCommonSummary();
public:
	void Activate(bool active);
	bool isActive;

	// Active Frame (is used as buffer)
	static BRO_THREAD_LOCAL EventStorage* storage;

	// Resolves symbols
	SymbolEngine* symbolEngine;

	// Controls GPU activity
	// Graphics graphics;

	// System scheduler trace
	SchedulerTrace* schedulerTrace;

	// SysCall Collector
	SysCallCollector syscallCollector;

	// GPU Profiler
	GPUProfiler* gpuProfiler;

	// Returns thread collection
	const std::vector<ThreadEntry*>& GetThreads() const;

	// Report switch context event
	bool ReportSwitchContext(const SwitchContextDesc& desc);

	// Report switch context event
	bool ReportStackWalk(const CallstackDesc& desc);

	// Serialize and send current profiling progress
	void DumpProgress(const char* message = "");

	// Too much time from last report
	bool IsTimeToReportProgress() const;

	// Serialize and send frames
	void DumpFrames(uint32 mode = Mode::DEFAULT);

	// Serialize and send frames
	void DumpSummary();

	// Registers thread and create EventStorage
	ThreadEntry* RegisterThread(const ThreadDescription& description, EventStorage** slot);

	// UnRegisters thread
	bool UnRegisterThread(ThreadID threadId);

	// Check is registered thread
	bool IsRegistredThread(ThreadID id);

	// Registers finer and create EventStorage
	bool RegisterFiber(const FiberDescription& description, EventStorage** slot);

	// Registers ProcessDescription
	bool RegisterProcessDescription(const ProcessDescription& description);

	// Registers ThreaDescription (used for threads from other processes)
	bool RegisterThreadDescription(const ThreadDescription& description);

	// Sets state change callback
	bool SetStateChangedCallback(BroStateCallback cb);

	// Attaches a key-value pair to the next capture
	bool AttachSummary(const char* key, const char* value);

	// Attaches a screenshot to the current capture
	bool AttachFile(BroFile::Type type, const char* name, const uint8_t* data, uint32_t size);

	// Initalizes GPU profiler
	void InitGPUProfiler(GPUProfiler* profiler);

	// Current Frame Number (since the game started)
	uint32_t GetCurrentFrame() const { return frameNumber; }


	// NOT Thread Safe singleton (performance)
	static BRO_INLINE Core& Get() { return notThreadSafeInstance; }

	// Main Update Function
	static uint32_t NextFrame() { return Get().Update(); }

	// Get Active ThreadID
	//static BRO_INLINE uint32 GetThreadID() { return Get().mainThreadID; }
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
