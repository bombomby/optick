// Copyright(c) 2019 Vadim Slyusarev

#pragma once

#ifdef OPTICK_UE4_GPU

#define UE_4_24_OR_LATER (ENGINE_MAJOR_VERSION == 4 && ENGINE_MINOR_VERSION >= 24)

// RenderCore
#include "GPUProfiler.h"
#include "ProfilingDebugging/RealtimeGPUProfiler.h"
#include "ProfilingDebugging/TracingProfiler.h"

#define REALTIME_GPU_PROFILER_EVENT_TRACK_FRAME_NUMBER (TRACING_PROFILER || DO_CHECK)

#ifdef UE_4_24_OR_LATER

/*-----------------------------------------------------------------------------
FRealTimeGPUProfilerEvent class
-----------------------------------------------------------------------------*/
class FRealtimeGPUProfilerEventImpl
{
public:
	static const uint64 InvalidQueryResult = 0xFFFFFFFFFFFFFFFFull;

public:
	bool GatherQueryResults(FRHICommandListImmediate& RHICmdList)
	{
		QUICK_SCOPE_CYCLE_COUNTER(STAT_SceneUtils_GatherQueryResults);

		// Get the query results which are still outstanding
		check(StartQuery.IsValid() && EndQuery.IsValid());

		if (StartResultMicroseconds == InvalidQueryResult)
		{
			if (!RHICmdList.GetRenderQueryResult(StartQuery.GetQuery(), StartResultMicroseconds, false))
			{
				StartResultMicroseconds = InvalidQueryResult;
			}
		}

		if (EndResultMicroseconds == InvalidQueryResult)
		{
			if (!RHICmdList.GetRenderQueryResult(EndQuery.GetQuery(), EndResultMicroseconds, false))
			{
				EndResultMicroseconds = InvalidQueryResult;
			}
		}

		return HasValidResult();
	}

	bool HasValidResult() const
	{
		return StartResultMicroseconds != InvalidQueryResult && EndResultMicroseconds != InvalidQueryResult;
	}

	FRHIPooledRenderQuery StartQuery;
	FRHIPooledRenderQuery EndQuery;

	FName Name;
	STAT(FName StatName;)

	uint64 StartResultMicroseconds;
	uint64 EndResultMicroseconds;

#if REALTIME_GPU_PROFILER_EVENT_TRACK_FRAME_NUMBER
	uint32 FrameNumber;
#endif

#if DO_CHECK
	bool bInsideQuery;
#endif
};


class FRealtimeGPUProfilerFrameImpl
{
public:
	struct FGPUEventTimeAggregate
	{
		uint32 ExclusiveTimeUs;
		uint32 InclusiveTimeUs;
	};

	static constexpr uint32 GPredictedMaxNumEvents = 100u;
	static constexpr uint32 GPredictedMaxNumEventsUpPow2 = 128u;
	static constexpr uint32 GPredictedMaxStackDepth = 32u;

	int32 NextEventIdx;
	int32 OverflowEventCount;
	int32 NextResultPendingEventIdx;

	uint32& QueryCount;
	FRenderQueryPoolRHIRef RenderQueryPool;

	TArray<FRealtimeGPUProfilerEventImpl, TInlineAllocator<GPredictedMaxNumEvents>> GpuProfilerEvents;
	TArray<int32, TInlineAllocator<GPredictedMaxNumEvents>> GpuProfilerEventParentIndices;
	TArray<int32, TInlineAllocator<GPredictedMaxStackDepth>> EventStack;
	TArray<FGPUEventTimeAggregate, TInlineAllocator<GPredictedMaxNumEvents>> EventAggregates;
};


#else

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

#endif

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

#endif //OPTICK_UE4_GPU