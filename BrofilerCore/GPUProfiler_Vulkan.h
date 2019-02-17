#pragma once

#include "Common.h"
#include "GPUProfiler.h"

// GPU Support for Vulkan
#if !defined(BRO_ENABLE_GPU_VULKAN)
#define BRO_ENABLE_GPU_VULKAN (BRO_ENABLE_GPU /*&& 0*/)
#endif

#if BRO_ENABLE_GPU_VULKAN

#include <vulkan/vulkan.h>

// VS TODO: Implement
namespace Brofiler
{
	class GPUProfilerVulkan : public GPUProfiler
	{
	protected:
		struct Frame
		{
			VkCommandBuffer commandBuffer;
			VkFence fence;
			Frame() : commandBuffer(nullptr), fence(nullptr) {}
		};

		struct NodePayload
		{
			VkDevice			device;
			VkPhysicalDevice	physicalDevice;
			VkQueue				queue;
			VkQueryPool			queryPool;
			VkCommandPool		commandPool;

			std::array<Frame, NUM_FRAMES_DELAY> frames;

		//	NodePayload() : device(nullptr), physicalDevice(nullptr), queue(nullptr), queryPool(nullptr), commandPool(nullptr) {}
			~NodePayload();
		};
		std::vector<NodePayload*> nodePayloads;

		void ResolveTimestamps(VkCommandBuffer commandBuffer, uint32_t startIndex, uint32_t count);
		void WaitForFrame(uint64_t frameNumber);

	public:
		GPUProfilerVulkan();
		~GPUProfilerVulkan();

		void InitDevice(VkDevice* devices, VkPhysicalDevice* physicalDevices, VkQueue* cmdQueues, uint32_t* cmdQueuesFamily, uint32_t nodeCount);
		void QueryTimestamp(VkCommandBuffer commandBuffer, int64_t* outCpuTimestamp);


		// Interface implementation
		ClockSynchronization GetClockSynchronization(uint32_t nodeIndex) override;

		void QueryTimestamp(void* context, int64_t* outCpuTimestamp) override
		{
			QueryTimestamp((VkCommandBuffer)context, outCpuTimestamp);
		}

		void Flip(void* swapChain) override;
	};
}

#endif //BRO_ENABLE_GPU_VULKAN