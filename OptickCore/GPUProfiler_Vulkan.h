#pragma once

#include "Common.h"
#include "GPUProfiler.h"

// GPU Support for Vulkan
#if !defined(OPTICK_ENABLE_GPU_VULKAN)
#define OPTICK_ENABLE_GPU_VULKAN (OPTICK_ENABLE_GPU /*&& 0*/)
#endif

#if OPTICK_ENABLE_GPU_VULKAN

#include <vulkan/vulkan.h>

// VS TODO: Implement
namespace Optick
{
	class GPUProfilerVulkan : public GPUProfiler
	{
	protected:
		struct Frame
		{
			VkCommandBuffer commandBuffer;
			VkFence fence;
			Frame() : commandBuffer(VK_NULL_HANDLE), fence(VK_NULL_HANDLE) {}
		};

		struct NodePayload
		{
			VkDevice			device;
			VkPhysicalDevice	physicalDevice;
			VkQueue				queue;
			VkQueryPool			queryPool;
			VkCommandPool		commandPool;

			std::array<Frame, NUM_FRAMES_DELAY> frames;

			NodePayload() : device(VK_NULL_HANDLE), physicalDevice(VK_NULL_HANDLE), queue(VK_NULL_HANDLE), queryPool(VK_NULL_HANDLE), commandPool(VK_NULL_HANDLE) {}
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

#endif //OPTICK_ENABLE_GPU_VULKAN
