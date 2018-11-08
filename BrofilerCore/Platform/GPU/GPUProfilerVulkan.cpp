#include "GPUProfilerVulkan.h"

#if BRO_ENABLE_GPU_VULKAN

#else

namespace Brofiler
{
	void InitGpuVulkan(void* /*device*/, void** /*cmdQueues*/, uint32_t /*numQueues*/)
	{
		BRO_FAILED("BRO_ENABLE_GPU_VULKAN is disabled! Can't initialize GPU Profiler!");
	}
}

#endif //BRO_ENABLE_GPU_D3D12