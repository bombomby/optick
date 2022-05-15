// Copyright(c) 2019 Vadim Slyusarev

#pragma once

#ifdef OPTICK_UE4_GPU

// RenderCore
#include "GPUProfiler.h"
#include "ProfilingDebugging/RealtimeGPUProfiler.h"
#include "ProfilingDebugging/TracingProfiler.h"
#include "Runtime/Launch/Resources/Version.h"

#define REALTIME_GPU_PROFILER_EVENT_TRACK_FRAME_NUMBER (TRACING_PROFILER || DO_CHECK)

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
		//QUICK_SCOPE_CYCLE_COUNTER(STAT_SceneUtils_GatherQueryResults);

		// Get the query results which are still outstanding
		check(GFrameNumberRenderThread != FrameNumber);
		check(StartQuery.IsValid() && EndQuery.IsValid());

		for (uint32 GPUIndex : GPUMask)
		{
			if (StartResultMicroseconds[GPUIndex] == InvalidQueryResult)
			{
				if (!RHICmdList.GetRenderQueryResult(StartQuery.GetQuery(), StartResultMicroseconds[GPUIndex], false, GPUIndex))
				{
					StartResultMicroseconds[GPUIndex] = InvalidQueryResult;
				}
			}

			if (EndResultMicroseconds[GPUIndex] == InvalidQueryResult)
			{
				if (!RHICmdList.GetRenderQueryResult(EndQuery.GetQuery(), EndResultMicroseconds[GPUIndex], false, GPUIndex))
				{
					EndResultMicroseconds[GPUIndex] = InvalidQueryResult;
				}
			}
		}

		return HasValidResult();
	}

	uint64 GetResultUs(uint32 GPUIndex) const
	{
		check(HasValidResult(GPUIndex));

		if (StartResultMicroseconds[GPUIndex] > EndResultMicroseconds[GPUIndex])
		{
			return 0llu;
		}

		return EndResultMicroseconds[GPUIndex] - StartResultMicroseconds[GPUIndex];
	}

	bool HasValidResult(uint32 GPUIndex) const
	{
		return StartResultMicroseconds[GPUIndex] != InvalidQueryResult && EndResultMicroseconds[GPUIndex] != InvalidQueryResult;
	}

	bool HasValidResult() const
	{
		for (uint32 GPUIndex : GPUMask)
		{
			if (!HasValidResult(GPUIndex))
			{
				return false;
			}
		}
		return true;
	}

#if STATS
	const FName& GetStatName() const
	{
		return StatName;
	}
#endif

	const FName& GetName() const
	{
		return Name;
	}

	FRHIGPUMask GetGPUMask() const
	{
		return GPUMask;
	}

	uint64 GetStartResultMicroseconds(uint32 GPUIndex) const
	{
		return StartResultMicroseconds[GPUIndex];
	}

	uint64 GetEndResultMicroseconds(uint32 GPUIndex) const
	{
		return EndResultMicroseconds[GPUIndex];
	}

	uint32 GetFrameNumber() const
	{
		return FrameNumber;
	}

	TStaticArray<uint64, MAX_NUM_GPUS> StartResultMicroseconds;
	TStaticArray<uint64, MAX_NUM_GPUS> EndResultMicroseconds;

	FRHIPooledRenderQuery StartQuery;
	FRHIPooledRenderQuery EndQuery;

	FName Name;
	STAT(FName StatName;)

	FRHIGPUMask GPUMask;

	uint32 FrameNumber;

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

	uint64 CPUFrameStartTimestamp;
	FTimestampCalibrationQueryRHIRef TimestampCalibrationQuery;

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
	bool bLocked;

#if GPUPROFILERTRACE_ENABLED
	FRealtimeGPUProfilerHistoryByDescription HistoryByDescription;
#endif
};

static_assert(sizeof(FRealtimeGPUProfilerImpl) == sizeof(FRealtimeGPUProfiler), "Size mismatch");

#endif //OPTICK_UE4_GPU