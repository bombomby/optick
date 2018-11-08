#include "GPUProfilerD3D12.h"

#if BRO_ENABLE_GPU_D3D12

#include <thread>

#include "Memory.h"
#include "../BrofilerCore/Core.h"

#define BRO_CHECK(args) do { HRESULT __hr = args; BRO_ASSERT(__hr == S_OK, "Failed check"); } while(false);

namespace Brofiler
{
	template <class T> void SafeRelease(T **ppT)
	{
		if (*ppT)
		{
			(*ppT)->Release();
			*ppT = NULL;
		}
	}

	void InitGpuD3D12(void* device, void** cmdQueues, uint32_t numQueues)
	{
		GPUProfilerD3D12* gpuProfiler = Memory::New<GPUProfilerD3D12>();
		gpuProfiler->InitDevice((ID3D12Device*)device, (ID3D12CommandQueue**)cmdQueues, numQueues);
		Core::Get().InitGPUProfiler(gpuProfiler);
	}

	GPUProfilerD3D12::GPUProfilerD3D12() : currentNode(0), queryBuffer(nullptr), device(nullptr), frameNumber(0), currentState(STATE_OFF)
	{
		prevFrameStatistics = { 0 };
	}

	void GPUProfilerD3D12::InitDevice(ID3D12Device* pDevice, ID3D12CommandQueue** pCommandQueues, uint32_t numCommandQueues)
	{
		device = pDevice;

		uint32_t nodeCount = numCommandQueues; // device->GetNodeCount();

		nodes.resize(nodeCount);

		D3D12_HEAP_PROPERTIES heapDesc;
		heapDesc.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
		heapDesc.MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN;
		heapDesc.CreationNodeMask = 0;
		heapDesc.VisibleNodeMask = (1u << nodeCount) - 1u;
		heapDesc.Type = D3D12_HEAP_TYPE_READBACK;

		D3D12_RESOURCE_DESC resourceDesc;
		resourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
		resourceDesc.Alignment = 0;
		resourceDesc.Width = MAX_QUERIES_COUNT * sizeof(int64_t);
		resourceDesc.Height = 1;
		resourceDesc.DepthOrArraySize = 1;
		resourceDesc.MipLevels = 1;
		resourceDesc.Format = DXGI_FORMAT_UNKNOWN;
		resourceDesc.SampleDesc.Count = 1;
		resourceDesc.SampleDesc.Quality = 0;
		resourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
		resourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

		BRO_CHECK(device->CreateCommittedResource(
			&heapDesc,
			D3D12_HEAP_FLAG_NONE,
			&resourceDesc,
			D3D12_RESOURCE_STATE_COPY_DEST,
			nullptr,
			IID_PPV_ARGS(&queryBuffer)));

		for (Frame& frame : frames)
		{
			BRO_CHECK(device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_PPV_ARGS(&frame.commandAllocator)));
			
			for (uint32_t nodeIndex = 0; nodeIndex < nodeCount; ++nodeIndex)
			{
				BRO_CHECK(device->CreateCommandList(1u << nodeIndex, D3D12_COMMAND_LIST_TYPE_DIRECT, frame.commandAllocator, nullptr, IID_PPV_ARGS(&frame.commandList[nodeIndex])));
				BRO_CHECK(frame.commandList[nodeIndex]->Close());
			}
		}

		// Get Device Name
		LUID adapterLUID = pDevice->GetAdapterLuid();

		IDXGIFactory4* factory;
		BRO_CHECK(CreateDXGIFactory2(0, IID_PPV_ARGS(&factory)));

		IDXGIAdapter1* adapter;
		factory->EnumAdapterByLuid(adapterLUID, IID_PPV_ARGS(&adapter));
		
		DXGI_ADAPTER_DESC1 desc;
		adapter->GetDesc1(&desc);

		adapter->Release();
		factory->Release();

		char deviceName[128] = { 0 };
		wcstombs(deviceName, desc.Description, BRO_ARRAY_SIZE(deviceName) - 1);

		for (uint32_t nodeIndex = 0; nodeIndex < nodeCount; ++nodeIndex)
			InitNode(deviceName, nodeIndex, pCommandQueues[nodeIndex]);

		vsyncEventStorage = RegisterStorage("VSync");
	}

	void GPUProfilerD3D12::InitNode(const char* nodeName, uint32_t nodeIndex, ID3D12CommandQueue* pCmdQueue)
	{
		Node* node = Memory::New<Node>();
		nodes[nodeIndex] = node;
		node->commandQueue = pCmdQueue;
		node->nodeIndex = nodeIndex;

		D3D12_QUERY_HEAP_DESC queryHeapDesc;
		queryHeapDesc.Count = MAX_QUERIES_COUNT;
		queryHeapDesc.Type = D3D12_QUERY_HEAP_TYPE_TIMESTAMP;
		queryHeapDesc.NodeMask = 1u << nodeIndex;
		BRO_CHECK(device->CreateQueryHeap(&queryHeapDesc, IID_PPV_ARGS(&node->queryHeap)));

		BRO_CHECK(device->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(&node->syncFence)));

		node->gpuEventStorage = RegisterStorage(nodeName);
	}

	void GPUProfilerD3D12::QueryTimestamp(ID3D12GraphicsCommandList* context, uint64_t* outCpuTimestamp)
	{
		if (currentState == STATE_RUNNING)
		{
			Node* node = nodes[currentNode];
			uint32_t index = node->queryIndex.fetch_add(1) % MAX_QUERIES_COUNT;
			context->EndQuery(node->queryHeap, D3D12_QUERY_TYPE_TIMESTAMP, index);
			node->queryCpuTimestamps[index] = outCpuTimestamp;
		}
	}

	void GPUProfilerD3D12::ResolveTimestamps(uint32_t startIndex, uint32_t count)
	{
		if (count)
		{
			Node* node = nodes[currentNode];

			D3D12_RANGE range = { sizeof(uint64_t)*startIndex, sizeof(uint64_t)*(startIndex + count) };
			void* pData = nullptr;
			queryBuffer->Map(0, &range, &pData);
			memcpy(&node->queryGpuTimestamps[startIndex], (uint64_t*)pData + startIndex, sizeof(uint64_t) * count);
			queryBuffer->Unmap(0, 0);

			// Convert GPU timestamps => CPU Timestamps
			for (uint32_t index = startIndex; index < startIndex + count; ++index)
				*node->queryCpuTimestamps[index] = node->clock.GetCPUTimestamp(node->queryGpuTimestamps[index]);
		}
	}

	void GPUProfilerD3D12::WaitForFrame(uint64_t frameNumberToWait)
	{
		BROFILE;

		Node* node = nodes[currentNode];
		while (frameNumberToWait > node->syncFence->GetCompletedValue())
		{
			std::this_thread::sleep_for(std::chrono::milliseconds(1));
		}
	}

	void GPUProfilerD3D12::Flip(IDXGISwapChain* swapChain)
	{
		BROFILE;

		std::lock_guard<std::recursive_mutex> lock(updateLock);

		if (currentState == STATE_STARTING)
			currentState = STATE_RUNNING;

		if (currentState == STATE_RUNNING)
		{
			Frame& currentFrame = frames[frameNumber % NUM_FRAMES_DELAY];
			Frame& nextFrame = frames[(frameNumber + 1) % NUM_FRAMES_DELAY];

			Node* node = nodes[currentNode];

			ID3D12GraphicsCommandList* commandList = currentFrame.commandList[currentNode];
			ID3D12CommandAllocator* commandAllocator = currentFrame.commandAllocator;
			commandAllocator->Reset();
			commandList->Reset(commandAllocator, nullptr);

			if (EventData* frameEvent = currentFrame.frameEvents[currentNode])
				QueryTimestamp(commandList, &frameEvent->finish);

			uint32_t queryBegin = currentFrame.queryStartIndices[currentNode];
			uint32_t queryEnd = node->queryIndex;

			// Generate GPU Frame event for the next frame
			static const EventDescription* GPUFrameDescription = EventDescription::Create("GPU Frame", __FILE__, __LINE__);
			EventData& event = node->gpuEventStorage->eventBuffer.Add();
			event.description = GPUFrameDescription;
			event.start = EventTime::INVALID_TIMESTAMP;
			event.finish = EventTime::INVALID_TIMESTAMP;
			QueryTimestamp(commandList, &event.start);
			nextFrame.frameEvents[currentNode] = &event;

			static const EventDescription* FrameTagDescription = EventDescription::CreateShared("Frame");
			TagU32& tag = node->gpuEventStorage->tagU32Buffer.Add();
			tag.description = FrameTagDescription;
			tag.timestamp = EventTime::INVALID_TIMESTAMP;
			tag.data = Core::Get().GetCurrentFrame();
			QueryTimestamp(commandList, &tag.timestamp);

			if (queryBegin != (uint32_t)-1)
			{
				BRO_ASSERT(queryEnd - queryBegin <= MAX_QUERIES_COUNT, "Too many queries in one frame? Increase GPUProfiler::MAX_QUERIES_COUNT to fix the problem!");
				currentFrame.queryCountIndices[currentNode] = queryEnd - queryBegin;

				uint32_t startIndex = queryBegin % MAX_QUERIES_COUNT;
				uint32_t finishIndex = queryEnd % MAX_QUERIES_COUNT;

				if (startIndex < finishIndex)
				{
					commandList->ResolveQueryData(node->queryHeap, D3D12_QUERY_TYPE_TIMESTAMP, startIndex, queryEnd - queryBegin, queryBuffer, startIndex * sizeof(int64_t));
				}
				else
				{
					commandList->ResolveQueryData(node->queryHeap, D3D12_QUERY_TYPE_TIMESTAMP, startIndex, MAX_QUERIES_COUNT - startIndex, queryBuffer, startIndex * sizeof(int64_t));
					commandList->ResolveQueryData(node->queryHeap, D3D12_QUERY_TYPE_TIMESTAMP, 0, finishIndex, queryBuffer, 0);
				}
			}

			commandList->Close();

			node->commandQueue->ExecuteCommandLists(1, (ID3D12CommandList*const*)&commandList);
			node->commandQueue->Signal(node->syncFence, frameNumber);

			// Preparing Next Frame
			// Try resolve timestamps for the current frame
			if (frameNumber >= NUM_FRAMES_DELAY && nextFrame.queryCountIndices[currentNode])
			{
				WaitForFrame(frameNumber + 1 - NUM_FRAMES_DELAY);

				uint32_t resolveStart = nextFrame.queryStartIndices[currentNode] % MAX_QUERIES_COUNT;
				uint32_t resolveFinish = resolveStart + nextFrame.queryCountIndices[currentNode];
				ResolveTimestamps(resolveStart, std::min<uint32_t>(resolveFinish, MAX_QUERIES_COUNT) - resolveStart);
				if (resolveFinish > MAX_QUERIES_COUNT)
					ResolveTimestamps(0, resolveFinish - MAX_QUERIES_COUNT);
			}
				
			nextFrame.queryStartIndices[currentNode] = queryEnd;
			nextFrame.queryCountIndices[currentNode] = 0;

			// Process VSync
			DXGI_FRAME_STATISTICS currentFrameStatistics = { 0 };
			HRESULT result = swapChain->GetFrameStatistics(&currentFrameStatistics);
			if ((result == S_OK) && (prevFrameStatistics.PresentCount + 1 == currentFrameStatistics.PresentCount))
			{
				static const EventDescription* VSyncDescription = EventDescription::Create("VSync", __FILE__, __LINE__);
				EventData& data = vsyncEventStorage->eventBuffer.Add();
				data.description = VSyncDescription;
				data.start = prevFrameStatistics.SyncQPCTime.QuadPart;
				data.finish = currentFrameStatistics.SyncQPCTime.QuadPart;
			}
			prevFrameStatistics = currentFrameStatistics;
		}

		++frameNumber;
	}

	void GPUProfilerD3D12::Start(uint32 /*mode*/)
	{
		std::lock_guard<std::recursive_mutex> lock(updateLock);
		Reset();
		currentState = STATE_STARTING;
	}

	void GPUProfilerD3D12::Stop(uint32 /*mode*/)
	{
		std::lock_guard<std::recursive_mutex> lock(updateLock);
		currentState = STATE_OFF;
	}

	void GPUProfilerD3D12::Dump(uint32 /*mode*/)
	{
		//if (mode & Mode::GPU)
		{
			Node* node = nodes[currentNode];

			EventBuffer& gpuBuffer = node->gpuEventStorage->eventBuffer;

			const std::vector<ThreadEntry*>& threads = Core::Get().GetThreads();
			for each (ThreadEntry* thread in threads)
			{
				thread->storage.gpuStorage.gpuBuffer.ForEachChunk([&gpuBuffer](const EventData* events, int count)
				{
					gpuBuffer.AddRange(events, count);
				});
			}
		}
	}

	void GPUProfilerD3D12::Reset()
	{
		for (size_t i = 0; i < frames.size(); ++i)
			frames[i].Reset();

		for (size_t i = 0; i < nodes.size(); ++i)
			nodes[i]->UpdateClock();
	}

	void GPUProfilerD3D12::Shutdown()
	{
		WaitForFrame(frameNumber - 1);

		for (Node* node : nodes)
			Memory::Delete(node);
		nodes.clear();
			
		SafeRelease(&queryBuffer);

		for (Frame& frame : frames)
			frame.Shutdown();
	}

	void GPUProfilerD3D12::Node::UpdateClock()
	{
		clock.frequencyCPU = GetHighPrecisionFrequency();
		commandQueue->GetTimestampFrequency(&clock.frequencyGPU);
		commandQueue->GetClockCalibration(&clock.timestampGPU, &clock.timestampCPU);
	}

	GPUProfilerD3D12::Node::~Node()
	{
		SafeRelease(&queryHeap);
		SafeRelease(&syncFence);
	}

	void GPUProfilerD3D12::Frame::Shutdown()
	{
		SafeRelease(&commandAllocator);
		for (size_t i = 0; i < commandList.size(); ++i)
			SafeRelease(&commandList[i]);
	}
}

#else

namespace Brofiler
{
	void InitGpuD3D12(void* /*device*/, void** /*cmdQueues*/, uint32_t /*numQueues*/)
	{
		BRO_FAILED("BRO_ENABLE_GPU_D3D12 is disabled! Can't initialize GPU Profiler!");
	}
}

#endif //BRO_ENABLE_GPU_D3D12