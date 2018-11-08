#pragma once

#include "Common.h"

#define BRO_ENABLE_GPU (USE_BROFILER /*&& 0*/)

#if BRO_ENABLE_GPU

namespace Brofiler
{
	class GPUProfiler
	{
	public:
		static const int MAX_QUERIES_COUNT = 1024;
		static const int NUM_FRAMES_DELAY = 4;
		static const int MAX_NODES = 2;

		struct ClockSynchronization
		{
			uint64_t frequencyCPU;
			uint64_t frequencyGPU;
			uint64_t timestampCPU;
			uint64_t timestampGPU;

			uint64_t GetCPUTimestamp(uint64_t gpuTimestamp)
			{
				return timestampCPU + (gpuTimestamp - timestampGPU) * frequencyCPU / frequencyGPU;
			}

			ClockSynchronization() : frequencyCPU(0), frequencyGPU(0), timestampCPU(0), timestampGPU(0) {}
		};

		virtual void QueryTimestamp(void* context, uint64_t* cpuTimestampOut) = 0;
		virtual void Flip(void* swapChain) = 0;
		
		// Capture Controls 
		virtual void Start(uint32 mode) = 0;
		virtual void Stop(uint32 mode) = 0;
		virtual void Dump(uint32 mode) = 0;

		virtual ~GPUProfiler() {}
	};
}

#endif //BRO_ENABLE_GPU