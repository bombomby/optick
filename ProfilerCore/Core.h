#pragma once
#include "Event.h"
#include "Serialization.h"
#include "Sampler.h"
#include "MemoryPool.h"
#include "Concurrency.h"
#include <map>

namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct ScopeHeader
{
	EventTime event;
	uint32 boardNumber;
	uint32 threadNumber;

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
			categories.push_back(data);
	}

	void InitRootEvent(const EventData& data)
	{
		header.event = data;
		AddEvent(data);
	}

	void Send();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator << ( OutputDataStream& stream, const ScopeData& ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef MemoryPool<EventData, 1024> EventBuffer;
typedef MemoryPool<const EventData*, 32> CategoryBuffer;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct EventStorage
{
	EventBuffer eventBuffer;
	CategoryBuffer categoryBuffer; 

	volatile uint isSampling;

	EventStorage();

	BRO_INLINE EventData& NextEvent() 
	{
		return eventBuffer.Add(); 
	}

	BRO_INLINE void RegisterCategory(const EventData& eventData) 
	{ 
		categoryBuffer.Add() = &eventData;
	}

	void Reset()
	{
		eventBuffer.Clear(true);
		categoryBuffer.Clear(true);
	}

	// Free all temporary memory
	void Clear(bool preserveContent)
	{
		eventBuffer.Clear(preserveContent);
		categoryBuffer.Clear(preserveContent);
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct Frame : public EventStorage
{
	const void* threadUniqueID;
	EventTime		frameTime;

	Frame() : threadUniqueID(nullptr)
	{

	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct ThreadEntry
{
	ThreadDescription description;
	EventStorage storage;
	EventStorage** threadTLS;

	ThreadEntry(const ThreadDescription& desc, EventStorage** tls) : description(desc), threadTLS(tls) {}
	void Activate(bool isActive);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class Core
{
	CriticalSection lock;

	HANDLE workerThread;
	uint32 mainThreadID;

	std::vector<ThreadEntry*> threads;

	int64 progressReportedLastTimestampMS;

	std::vector<EventTime> frames;

	void UpdateEvents();
	void Update();

	Core();
	~Core();

	static Core notThreadSafeInstance;

	void DumpCapturingProgress();
	void SendHandshakeResponse();
public:
	void Activate(bool active);
	bool isActive;

	// Active Frame (is used as buffer)
	static __declspec(thread) EventStorage* storage;

	// Controls sampling routine
	Sampler sampler;

	// Returns thread collection
	const std::vector<ThreadEntry*>& GetThreads() const;

	// Starts sampling process
	void StartSampling();

	// Serialize and send current profiling progress
	void DumpProgress(const char* message = "");

	// Too much time from last report
	bool IsTimeToReportProgress() const;

	// Serialize and send frames
	void DumpFrames();

	// Serialize and send sampling data
	void DumpSamplingData();

	// Registers thread and create EventStorage
	bool RegisterThread(const ThreadDescription& description);

	// NOT Thread Safe singleton (performance)
	static BRO_INLINE Core& Get() { return notThreadSafeInstance; }

	// Main Update Function
	static void NextFrame() { Get().Update(); }

	// Get Active ThreadID
	static BRO_INLINE uint32 GetThreadID() { return Get().mainThreadID; }
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}