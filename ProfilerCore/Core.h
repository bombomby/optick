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
struct FrameHeader
{
	EventTime event;
	int64 frequency;

	FrameHeader();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator << ( OutputDataStream& stream, const FrameHeader& ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct FrameData
{
	FrameHeader header;
	std::vector<EventData> categories;
	std::vector<EventData> events;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator << ( OutputDataStream& stream, const FrameData& ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef MemoryPool<EventData, 1024> EventBuffer;
typedef MemoryPool<const EventData*, 32> CategoryBuffer;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct Frame
{
	const void* threadUniqueID;

	EventTime		frameTime;
	EventBuffer eventBuffer;
	CategoryBuffer categoryBuffer; 

	volatile uint isSampling;

	Frame() : isSampling(0), threadUniqueID(nullptr)
	{

	}

	BRO_INLINE EventData& NextEvent() 
	{
		return eventBuffer.Add(); 
	}

	void RegisterCategory(const EventData& eventData) 
	{ 
		categoryBuffer.Add() = &eventData;
	}

	void Reset()
	{
		eventBuffer.Clear(true);
		categoryBuffer.Clear(true);
	}

	// Free all temporary memory
	void Clear()
	{
		eventBuffer.Clear(false);
		categoryBuffer.Clear(false);
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class Core
{
	CriticalSection lock;

	HANDLE workerThread;
	uint32 mainThreadID;

	// Usually very small array, pointless to handle it as map, so vector is my choice
	std::map<uint32, const FrameDescription*> activeThreads;

	int64 progressReportedLastTimestampMS;

	std::list<FrameData> frameList;

	void UpdateEvents(const FrameDescription& scope);
	void Update(const FrameDescription& scope);

	void StoreFrame(FrameData& frameData) const;

	Core();
	static Core notThreadSafeInstance;

	void DumpCapturingProgress();
	void SendHandshakeResponse();
public:
	void Activate(bool active);
	bool isActive;

	// Active Frame (is used as buffer)
	static Frame frame;

	// Controls sampling routine
	Sampler sampler;

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

	// Select working thread
	bool SetWorkingThread(uint32 threadID);

	// NOT Thread Safe singleton (performance)
	static BRO_INLINE Core& Get() { return notThreadSafeInstance; }

	// Main Update Function
	static void NextFrame(const FrameDescription& scope) { Get().Update(scope); }

	// Return current frame buffer
	static BRO_INLINE Frame& GetFrame() { return frame; }

	// Get Active ThreadID
	static BRO_INLINE uint32 GetThreadID() { return Get().mainThreadID; }
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}