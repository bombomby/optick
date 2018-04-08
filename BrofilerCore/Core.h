#pragma once
#include "Brofiler.h"

#include "Event.h"
#include "MemoryPool.h"
#include "Serialization.h"
#include "CallstackCollector.h"
#include "SysCallCollector.h"
#include "SwitchContextCollector.h"

#include <map>

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct SchedulerTrace;
struct SamplingProfiler;
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
OutputDataStream& operator << ( OutputDataStream& stream, const ScopeData& ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef MemoryPool<EventData, 1024> EventBuffer;
typedef MemoryPool<const EventData*, 32> CategoryBuffer;
typedef MemoryPool<SyncData, 1024> SynchronizationBuffer;
typedef MemoryPool<FiberSyncData, 1024> FiberSyncBuffer;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct EventStorage
{
	EventBuffer eventBuffer;
	CategoryBuffer categoryBuffer; 
	FiberSyncBuffer fiberSyncBuffer;

	MT::Atomic32<uint32> isSampling;
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
	MT::ThreadId threadID;
	int32 maxDepth;
	int32 priority;
	uint32 mask;
	bool fromOtherProcess;

	ThreadDescription(const char* threadName, const MT::ThreadId& id, bool _fromOtherProcess, int32 maxDepth = 1, int32 priority = 0, uint32 mask = 0);
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
	MT::Mutex lock;

	MT::ThreadId mainThreadID;

	ThreadList threads;
	FiberList fibers;

	int64 progressReportedLastTimestampMS;

	std::vector<EventTime> frames;

	CallstackCollector callstackCollector;
	SysCallCollector syscallCollector;
	SwitchContextCollector switchContextCollector;

	void UpdateEvents();
	void Update();

	Core();
	~Core();

	static Core notThreadSafeInstance;

	void DumpCapturingProgress();
	void SendHandshakeResponse(CaptureStatus::Type status);


	void DumpEvents(const EventStorage& entry, const EventTime& timeSlice, ScopeData& scope);
	void DumpThread(const ThreadEntry& entry, const EventTime& timeSlice, ScopeData& scope);
	void DumpFiber(const FiberEntry& entry, const EventTime& timeSlice, ScopeData& scope);

	void CleanupThreadsAndFibers();

	uint32 DumpBoard(uint32 mode, EventTime timeSlice);
public:
	void Activate(bool active);
	bool isActive;

	// Active Frame (is used as buffer)
	static mt_thread_local EventStorage* storage;

	// Controls sampling routine
	SamplingProfiler* samplingProfiler;

	// Resolves symbols
	SymbolEngine* symbolEngine;

	// Controls GPU activity
	// Graphics graphics;

	// System scheduler trace
	SchedulerTrace* schedulerTrace;

	// Returns thread collection
	const std::vector<ThreadEntry*>& GetThreads() const;

	// Report switch context event
	bool ReportSwitchContext(const SwitchContextDesc& desc);

	// Report switch context event
	bool ReportStackWalk(const CallstackDesc& desc);

	// Report syscall event
	void ReportSysCall(const SysCallDesc& desc);

	// Starts sampling process
	void StartSampling();

	// Serialize and send current profiling progress
	void DumpProgress(const char* message = "");

	// Too much time from last report
	bool IsTimeToReportProgress() const;

	// Serialize and send frames
	void DumpFrames(uint32 mode = Mode::DEFAULT);

	// Serialize and send sampling data
	void DumpSamplingData();

	// Registers thread and create EventStorage
	bool RegisterThread(const ThreadDescription& description, EventStorage** slot);

	// UnRegisters thread
	bool UnRegisterThread(MT::ThreadId threadId);

	// Check is registered thread
	bool IsRegistredThread(MT::ThreadId id);

	// Registers finer and create EventStorage
	bool RegisterFiber(const FiberDescription& description, EventStorage** slot);

	// NOT Thread Safe singleton (performance)
	static BRO_INLINE Core& Get() { return notThreadSafeInstance; }

	// Main Update Function
	static void NextFrame() { Get().Update(); }

	// Get Active ThreadID
	//static BRO_INLINE uint32 GetThreadID() { return Get().mainThreadID; }
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
