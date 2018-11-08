#pragma once
#include <mutex>
#include <thread>

#include "Common.h"
#include "ThreadID.h"

#include "Event.h"
#include "MemoryPool.h"
#include "Serialization.h"
#include "CallstackCollector.h"
#include "SysCallCollector.h"
#include "SwitchContextCollector.h"

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
	BroString<N>& operator=(const char* text) { strcpy_s(data, text); return *this; }
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
		EventBuffer gpuBuffer;
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
		gpuStorage.gpuBuffer.Clear(preserveContent);
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
struct ThreadDescription
{
	static const int THREAD_NAME_LENGTH = 128;

	char name[THREAD_NAME_LENGTH];
	ThreadID threadID;
	int32 maxDepth;
	int32 priority;
	uint32 mask;
	bool fromOtherProcess;

	ThreadDescription(const char* threadName, ThreadID id, bool _fromOtherProcess, int32 maxDepth = 1, int32 priority = 0, uint32 mask = 0);
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
public:
	void Activate(bool active);
	bool isActive;

	// Active Frame (is used as buffer)
	static bro_thread_local EventStorage* storage;

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

	// Sets state change callback
	bool SetStateChangedCallback(BroStateCallback cb);

	// Attaches a key-value pair to the next capture
	bool AttachSummary(const char* key, const char* value);

	// Attaches a screenshot to the current capture
	bool AttachFile(BroFile::Type type, const char* name, const uint8_t* data, size_t size);

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
