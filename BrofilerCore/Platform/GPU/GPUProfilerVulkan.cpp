#include "GPUProfilerVulkan.h"

#if BRO_ENABLE_GPU_VULKAN

#include "../BrofilerCore/Core.h"

#define BRO_VK_CHECK(args) do { VkResult __hr = args; BRO_ASSERT(__hr == VK_SUCCESS, "Failed check"); } while(false);

namespace Brofiler
{
	void InitGpuVulkan(VkDevice* devices, VkPhysicalDevice* physicalDevices, VkQueue* cmdQueues, uint32_t* cmdQueuesFamily, uint32_t numQueues)
	{
		GPUProfilerVulkan* gpuProfiler = Memory::New<GPUProfilerVulkan>();
		gpuProfiler->InitDevice(devices, physicalDevices, cmdQueues, cmdQueuesFamily, numQueues);
		Core::Get().InitGPUProfiler(gpuProfiler);
	}

	GPUProfilerVulkan::GPUProfilerVulkan()
	{
	}

	void GPUProfilerVulkan::InitDevice(VkDevice* devices, VkPhysicalDevice* physicalDevices, VkQueue* cmdQueues, uint32_t* cmdQueuesFamily, uint32_t nodeCount)
	{
		VkQueryPoolCreateInfo queryPoolCreateInfo;
		queryPoolCreateInfo.sType = VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO;
		queryPoolCreateInfo.pNext = 0;
		queryPoolCreateInfo.flags = 0;
		queryPoolCreateInfo.queryType = VK_QUERY_TYPE_TIMESTAMP;
		queryPoolCreateInfo.queryCount = MAX_QUERIES_COUNT + 1;

		VkCommandPoolCreateInfo commandPoolCreateInfo;
		commandPoolCreateInfo.sType = VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO;
		commandPoolCreateInfo.pNext = 0;
		commandPoolCreateInfo.flags = VK_COMMAND_POOL_CREATE_TRANSIENT_BIT | VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT;

		nodes.resize(nodeCount);
		nodePayloads.resize(nodeCount);

		VkResult r;
		for (uint32_t i = 0; i < nodeCount; ++i)
		{
			VkPhysicalDeviceProperties properties = { 0 };
			vkGetPhysicalDeviceProperties(physicalDevices[i], &properties);
			GPUProfiler::InitNode(properties.deviceName, i);

			NodePayload* nodePayload = Memory::New<NodePayload>();
			nodePayloads[i] = nodePayload;
			nodePayload->device = devices[i];
			nodePayload->physicalDevice = physicalDevices[i];
			nodePayload->queue = cmdQueues[i];
			
			r = vkCreateQueryPool(devices[i], &queryPoolCreateInfo, 0, &nodePayload->queryPool);
			BRO_ASSERT(r == VK_SUCCESS, "Failed");

			commandPoolCreateInfo.queueFamilyIndex = cmdQueuesFamily[i];
			r = vkCreateCommandPool(nodePayload->device, &commandPoolCreateInfo, 0, &nodePayload->commandPool);
			BRO_ASSERT(r == VK_SUCCESS, "Failed");

			for (uint32_t j = 0; j < nodePayload->frames.size(); ++j)
			{
				Frame& frame = nodePayload->frames[j];

				VkCommandBufferAllocateInfo allocInfo;
				allocInfo.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO;
				allocInfo.pNext = 0;
				allocInfo.commandBufferCount = 1;
				allocInfo.commandPool = nodePayload->commandPool;
				allocInfo.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
				r = vkAllocateCommandBuffers(nodePayload->device, &allocInfo, &frame.commandBuffer);
				BRO_ASSERT(r == VK_SUCCESS, "Failed");

				VkFenceCreateInfo fenceCreateInfo;
				fenceCreateInfo.sType = VK_STRUCTURE_TYPE_FENCE_CREATE_INFO;
				fenceCreateInfo.pNext = 0;
				fenceCreateInfo.flags = j == 0 ? 0 : VK_FENCE_CREATE_SIGNALED_BIT;
				r = vkCreateFence(nodePayload->device, &fenceCreateInfo, 0, &frame.fence);
				BRO_ASSERT(r == VK_SUCCESS, "Failed");
				if (j == 0)
				{
					VkCommandBufferBeginInfo commandBufferBeginInfo;
					commandBufferBeginInfo.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO;
					commandBufferBeginInfo.pNext = 0;
					commandBufferBeginInfo.pInheritanceInfo = 0;
					commandBufferBeginInfo.flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;
					vkBeginCommandBuffer(frame.commandBuffer, &commandBufferBeginInfo);
					vkCmdResetQueryPool(frame.commandBuffer, nodePayload->queryPool, 0, MAX_QUERIES_COUNT);
					vkEndCommandBuffer(frame.commandBuffer);

					VkSubmitInfo submitInfo = {};
					submitInfo.sType = VK_STRUCTURE_TYPE_SUBMIT_INFO;
					submitInfo.pNext = nullptr;
					submitInfo.waitSemaphoreCount = 0;
					submitInfo.pWaitSemaphores = nullptr;
					submitInfo.commandBufferCount = 1;
					submitInfo.pCommandBuffers = &frame.commandBuffer;
					submitInfo.signalSemaphoreCount = 0;
					submitInfo.pSignalSemaphores = nullptr;
					vkQueueSubmit(nodePayload->queue, 1, &submitInfo, frame.fence);
					vkWaitForFences(nodePayload->device, 1, &frame.fence, 1, (uint64_t)-1);
					vkResetCommandBuffer(frame.commandBuffer, VK_COMMAND_BUFFER_RESET_RELEASE_RESOURCES_BIT);
				}
			}
		}
	}

	void GPUProfilerVulkan::QueryTimestamp(VkCommandBuffer commandBuffer, int64_t* outCpuTimestamp)
	{
		if (currentState == STATE_RUNNING)
		{
			uint32_t index = nodes[currentNode]->QueryTimestamp(outCpuTimestamp);
			vkCmdWriteTimestamp(commandBuffer, VK_PIPELINE_STAGE_ALL_GRAPHICS_BIT, nodePayloads[currentNode]->queryPool, index);
		}
	}

	void GPUProfilerVulkan::ResolveTimestamps(VkCommandBuffer commandBuffer, uint32_t startIndex, uint32_t count)
	{
		if (count)
		{
			Node* node = nodes[currentNode];

			NodePayload* payload = nodePayloads[currentNode];

			BRO_VK_CHECK(vkGetQueryPoolResults(payload->device, payload->queryPool, startIndex, count, 8 * count, &nodes[currentNode]->queryGpuTimestamps[startIndex], 8, VK_QUERY_RESULT_64_BIT | VK_QUERY_RESULT_PARTIAL_BIT));
			vkCmdResetQueryPool(commandBuffer, payload->queryPool, startIndex, count);

			// Convert GPU timestamps => CPU Timestamps
			for (uint32_t index = startIndex; index < startIndex + count; ++index)
				*node->queryCpuTimestamps[index] = node->clock.GetCPUTimestamp(node->queryGpuTimestamps[index]);
		}
	}

	void GPUProfilerVulkan::WaitForFrame(uint64_t frameNumberToWait)
	{
		BROFILE;
		int r = VK_SUCCESS;
		do
		{
			NodePayload& payload = *nodePayloads[currentNode];
			r = vkWaitForFences(nodePayloads[currentNode]->device, 1, &payload.frames[frameNumberToWait % payload.frames.size()].fence, 1, 1000 * 30);
		} while (r != VK_SUCCESS);
	}

	void GPUProfilerVulkan::Flip(void* /*swapChain*/)
	{
		BROFILE;

		std::lock_guard<std::recursive_mutex> lock(updateLock);

		if (currentState == STATE_STARTING)
			currentState = STATE_RUNNING;

		if (currentState == STATE_RUNNING)
		{
			Node& node = *nodes[currentNode];
			NodePayload& payload = *nodePayloads[currentNode];

			uint32_t currentFrameIndex = frameNumber % NUM_FRAMES_DELAY;
			uint32_t nextFrameIndex = (frameNumber + 1) % NUM_FRAMES_DELAY;

			QueryFrame& currentFrame = node.queryGpuframes[currentFrameIndex];
			QueryFrame& nextFrame = node.queryGpuframes[nextFrameIndex];

			VkCommandBuffer commandBuffer = payload.frames[currentFrameIndex].commandBuffer;
			VkFence fence = payload.frames[currentFrameIndex].fence;
			VkDevice device = payload.device;
			VkQueue queue = payload.queue;

			vkWaitForFences(device, 1, &fence, 1, (uint64_t)-1);

			VkCommandBufferBeginInfo commandBufferBeginInfo;
			commandBufferBeginInfo.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO;
			commandBufferBeginInfo.pNext = 0;
			commandBufferBeginInfo.pInheritanceInfo = 0;
			commandBufferBeginInfo.flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;
			BRO_VK_CHECK(vkBeginCommandBuffer(commandBuffer, &commandBufferBeginInfo));
			vkResetFences(device, 1, &fence);

			if (EventData* frameEvent = currentFrame.frameEvent)
				QueryTimestamp(commandBuffer, &frameEvent->finish);

			// Generate GPU Frame event for the next frame
			EventData& event = AddFrameEvent();
			QueryTimestamp(commandBuffer, &event.start);
			QueryTimestamp(commandBuffer, &AddFrameTag().timestamp);
			nextFrame.frameEvent = &event;

			BRO_VK_CHECK(vkEndCommandBuffer(commandBuffer));
			VkSubmitInfo submitInfo = {};
			submitInfo.sType = VK_STRUCTURE_TYPE_SUBMIT_INFO;
			submitInfo.pNext = nullptr;
			submitInfo.waitSemaphoreCount = 0;
			submitInfo.pWaitSemaphores = nullptr;
			submitInfo.commandBufferCount = 1;
			submitInfo.pCommandBuffers = &commandBuffer;
			submitInfo.signalSemaphoreCount = 0;
			submitInfo.pSignalSemaphores = nullptr;
			BRO_VK_CHECK(vkQueueSubmit(queue, 1, &submitInfo, fence));

			uint32_t queryBegin = currentFrame.queryIndexStart;
			uint32_t queryEnd = node.queryIndex;

			if (queryBegin != (uint32_t)-1)
			{
				currentFrame.queryIndexCount = queryEnd - queryBegin;
			}

			// Preparing Next Frame
			// Try resolve timestamps for the current frame
			if (nextFrame.queryIndexStart != (uint32_t)-1)
			{
				uint32_t startIndex = nextFrame.queryIndexStart % MAX_QUERIES_COUNT;
				uint32_t finishIndex = (startIndex + nextFrame.queryIndexCount) % MAX_QUERIES_COUNT;

				if (startIndex < finishIndex)
				{
					ResolveTimestamps(commandBuffer, startIndex, finishIndex - startIndex);
				}
				else
				{
					ResolveTimestamps(commandBuffer, startIndex, MAX_QUERIES_COUNT - startIndex);
					ResolveTimestamps(commandBuffer, 0, finishIndex);
				}
			}

			nextFrame.queryIndexStart = queryEnd;
			nextFrame.queryIndexCount = 0;
		}

		++frameNumber;
	}

	GPUProfiler::ClockSynchronization GPUProfilerVulkan::GetClockSynchronization(uint32_t nodeIndex)
	{
		GPUProfiler::ClockSynchronization clock;

		NodePayload& node = *nodePayloads[nodeIndex];
		Frame& currentFrame = node.frames[frameNumber % NUM_FRAMES_DELAY];
		
		VkCommandBufferBeginInfo commandBufferBeginInfo;
		commandBufferBeginInfo.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO;
		commandBufferBeginInfo.pNext = 0;
		commandBufferBeginInfo.pInheritanceInfo = 0;
		commandBufferBeginInfo.flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;
		VkCommandBuffer CB = currentFrame.commandBuffer;
		VkDevice Device = node.device;
		VkFence Fence = currentFrame.fence;

		vkWaitForFences(Device, 1, &Fence, 1, (uint64_t)-1);
		vkResetFences(Device, 1, &Fence);
		vkResetCommandBuffer(CB, VK_COMMAND_BUFFER_RESET_RELEASE_RESOURCES_BIT);
		vkBeginCommandBuffer(CB, &commandBufferBeginInfo);
		vkCmdResetQueryPool(CB, nodePayloads[nodeIndex]->queryPool, 0, 1);
		vkCmdWriteTimestamp(CB, VK_PIPELINE_STAGE_ALL_GRAPHICS_BIT, nodePayloads[nodeIndex]->queryPool, 0);
		vkEndCommandBuffer(CB);

		VkSubmitInfo submitInfo = {};
		submitInfo.sType = VK_STRUCTURE_TYPE_SUBMIT_INFO;
		submitInfo.pNext = nullptr;
		submitInfo.waitSemaphoreCount = 0;
		submitInfo.pWaitSemaphores = nullptr;
		submitInfo.commandBufferCount = 1;
		submitInfo.pCommandBuffers = &CB;
		submitInfo.signalSemaphoreCount = 0;
		submitInfo.pSignalSemaphores = nullptr;
		vkQueueSubmit(nodePayloads[nodeIndex]->queue, 1, &submitInfo, Fence);
		vkWaitForFences(Device, 1, &Fence, 1, (uint64_t)-1);

		clock.timestampGPU = 0;
		vkGetQueryPoolResults(Device, nodePayloads[nodeIndex]->queryPool, 0, 1, 8, &clock.timestampGPU, 8, VK_QUERY_RESULT_64_BIT);
		clock.timestampCPU = GetHighPrecisionTime();
		clock.frequencyCPU = GetHighPrecisionFrequency();

		VkPhysicalDeviceProperties Properties;
		vkGetPhysicalDeviceProperties(nodePayloads[nodeIndex]->physicalDevice, &Properties);
		clock.frequencyGPU = (uint64_t)(1000000000ll / Properties.limits.timestampPeriod);

		return clock;
	}

	GPUProfilerVulkan::NodePayload::~NodePayload()
	{
		vkDestroyCommandPool(device, commandPool, nullptr);
		vkDestroyQueryPool(device, queryPool, nullptr);
	}

	GPUProfilerVulkan::~GPUProfilerVulkan()
	{
		WaitForFrame(frameNumber - 1);

		for (NodePayload* payload : nodePayloads)
		{
			for (Frame& frame : payload->frames)
			{
				vkDestroyFence(payload->device, frame.fence, nullptr);
				vkFreeCommandBuffers(payload->device, payload->commandPool, 1, &frame.commandBuffer);
			}

			Memory::Delete(payload);
		}

		nodePayloads.clear();
	}
}
#else

void InitGpuVulkan(VkDevice* /*devices*/, VkPhysicalDevice* /*physicalDevices*/, VkQueue* /*cmdQueues*/, uint32_t* /*cmdQueuesFamily*/, uint32_t /*numQueues*/)
{
	BRO_FAILED("BRO_ENABLE_GPU_VULKAN is disabled! Can't initialize GPU Profiler!");
}

#endif //BRO_ENABLE_GPU_D3D12