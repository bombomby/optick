// Copyright(c) 2019 Vadim Slyusarev

#pragma once

#if OPTICK_UE4_GPU

// RenderCore
#include "GPUProfiler.h"
#include "ProfilingDebugging/RealtimeGPUProfiler.h"

/*-----------------------------------------------------------------------------
FRealTimeGPUProfilerEvent class
-----------------------------------------------------------------------------*/
class FRealtimeGPUProfilerEventImpl
{
public:
	static const uint64 InvalidQueryResult = 0xFFFFFFFFFFFFFFFFull;

public:
	bool HasQueriesAllocated() const
	{
		return StartQuery.GetQuery() != nullptr;
	}

	bool GatherQueryResults(FRHICommandListImmediate& RHICmdList)
	{
		QUICK_SCOPE_CYCLE_COUNTER(STAT_Optick_GatherQueryResults);
		// Get the query results which are still outstanding
		check(GFrameNumberRenderThread != FrameNumber);
		if (HasQueriesAllocated())
		{
			if (StartResultMicroseconds == InvalidQueryResult)
			{
				if (!RHICmdList.GetRenderQueryResult(StartQuery.GetQuery(), StartResultMicroseconds, false))
				{
					StartResultMicroseconds = InvalidQueryResult;
				}
				bBeginQueryInFlight = false;
			}
			if (EndResultMicroseconds == InvalidQueryResult)
			{
				if (!RHICmdList.GetRenderQueryResult(EndQuery.GetQuery(), EndResultMicroseconds, false))
				{
					EndResultMicroseconds = InvalidQueryResult;
				}
				bEndQueryInFlight = false;
			}
		}
		else
		{
			// If we don't have a query allocated, just set the results to zero
			EndResultMicroseconds = StartResultMicroseconds = 0;
		}
		return HasValidResult();
	}

	bool HasValidResult() const
	{
		return StartResultMicroseconds != FRealtimeGPUProfilerEventImpl::InvalidQueryResult && EndResultMicroseconds != FRealtimeGPUProfilerEventImpl::InvalidQueryResult;
	}

	FRHIPooledRenderQuery StartQuery;
	FRHIPooledRenderQuery EndQuery;
#if STATS
	FName StatName;
#endif
	FName Name;
	uint64 StartResultMicroseconds;
	uint64 EndResultMicroseconds;
	uint32 FrameNumber;

	bool bInsideQuery;
	bool bBeginQueryInFlight;
	bool bEndQueryInFlight;
};

class FRealtimeGPUProfilerFrameImpl
{
public:
	uint32& QueryCount;

	TArray<FRealtimeGPUProfilerEventImpl*> GpuProfilerEvents;
	TArray<int32> EventStack;

	struct FRealtimeGPUProfilerTimelineEvent
	{
		enum class EType { PushEvent, PopEvent };
		EType Type;
		int32 EventIndex;
	};

	TArray<FRealtimeGPUProfilerTimelineEvent> GpuProfilerTimelineEvents;

	struct FGPUEventTimeAggregate
	{
		float ExclusiveTime;
		float InclusiveTime;
	};
	TArray<FGPUEventTimeAggregate> EventAggregates;

	uint32 FrameNumber;
	FRenderQueryPoolRHIRef RenderQueryPool;
};

class FRealtimeGPUProfilerImpl
{
public:
	TArray<FRealtimeGPUProfilerFrameImpl*> Frames;
	int32 WriteBufferIndex;
	int32 ReadBufferIndex;
	uint32 WriteFrameNumber;
	uint32 QueryCount;
	FRenderQueryPoolRHIRef RenderQueryPool;
	bool bStatGatheringPaused;
	bool bInBeginEndBlock;
};

static_assert(sizeof(FRealtimeGPUProfilerImpl) == sizeof(FRealtimeGPUProfiler), "Size mismatch");
//static_assert(offsetof(FRealtimeGPUProfilerImpl, Frames) == offsetof(FRealtimeGPUProfiler, Frames), "FRealtimeGPUProfiler::Frames offset mismatch");
//static_assert(offsetof(FRealtimeGPUProfilerImpl, WriteBufferIndex) == offsetof(FRealtimeGPUProfiler, WriteBufferIndex), "FRealtimeGPUProfiler::WriteBufferIndex offset mismatch");
//static_assert(offsetof(FRealtimeGPUProfilerImpl, ReadBufferIndex) == offsetof(FRealtimeGPUProfiler, ReadBufferIndex), "FRealtimeGPUProfiler::ReadBufferIndex offset mismatch");
//static_assert(offsetof(FRealtimeGPUProfilerImpl, WriteFrameNumber) == offsetof(FRealtimeGPUProfiler, WriteFrameNumber), "FRealtimeGPUProfiler::WriteFrameNumber offset mismatch");
//static_assert(offsetof(FRealtimeGPUProfilerImpl, QueryCount) == offsetof(FRealtimeGPUProfiler, QueryCount), "FRealtimeGPUProfiler::QueryCount offset mismatch");
//static_assert(offsetof(FRealtimeGPUProfilerImpl, RenderQueryPool) == offsetof(FRealtimeGPUProfiler, RenderQueryPool), "FRealtimeGPUProfiler::RenderQueryPool offset mismatch");
//static_assert(offsetof(FRealtimeGPUProfilerImpl, bStatGatheringPaused) == offsetof(FRealtimeGPUProfiler, bStatGatheringPaused), "FRealtimeGPUProfiler::bStatGatheringPaused offset mismatch");
//static_assert(offsetof(FRealtimeGPUProfilerImpl, bInBeginEndBlock) == offsetof(FRealtimeGPUProfiler, bInBeginEndBlock), "FRealtimeGPUProfiler::bInBeginEndBlock offset mismatch");


#endif //OPTICK_UE4_GPU