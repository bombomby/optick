#pragma once

#include "Common.h"
#include "../GPUProfiler.h"

// GPU Support for Vulkan
#if !defined(BRO_ENABLE_GPU_VULKAN)
#define BRO_ENABLE_GPU_VULKAN (BRO_ENABLE_GPU && 0)
#endif

#if BRO_ENABLE_GPU_VULKAN

// VS TODO: Implement
namespace Brofiler
{
	class GPUProfilerVulkan : public GPUProfiler
	{
	public:
		// Interface implementation
		void QueryTimestamp(void* context, uint64_t* outCpuTimestamp) override {}
		void Flip(void* swapChain) override {}
		virtual void Start(uint32 mode) override {}
		virtual void Stop(uint32 mode) override {}
		virtual void Dump(uint32 mode) override {}
	};
}

#endif //BRO_ENABLE_GPU_VULKAN