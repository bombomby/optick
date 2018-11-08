#pragma once

#include "Common.h"
#include "../GPUProfiler.h"

// GPU Support for D3D12
#if !defined(BRO_ENABLE_GPU_D3D12)
#define BRO_ENABLE_GPU_D3D12 (BRO_ENABLE_GPU /*&& 0*/)
#endif

#if BRO_ENABLE_GPU_D3D12

#include <atomic>
#include <array>
#include <string>
#include <vector>

#include <d3d12.h>
#include <dxgi.h>
#include <dxgi1_4.h>

#include "../BrofilerCore/Core.h"

namespace Brofiler
{
	class GPUProfilerD3D12 : public GPUProfiler
	{
		struct Frame
		{
			ID3D12CommandAllocator* commandAllocator;
			std::array<ID3D12GraphicsCommandList*, MAX_NODES> commandList;
			
			std::array<EventData*, MAX_NODES> frameEvents;
			
			std::array<uint32_t, MAX_NODES> queryStartIndices;
			std::array<uint32_t, MAX_NODES> queryCountIndices;

			Frame() : commandAllocator(nullptr)
			{
				commandList.fill(nullptr);
				Reset();
			}

			void Reset()
			{
				frameEvents.fill(nullptr);
				queryStartIndices.fill((uint32_t)-1);
				queryCountIndices.fill(0);
			}

			void Shutdown();

			~Frame()
			{
				Shutdown();
			}
		};

		struct Node
		{
			EventStorage* gpuEventStorage;

			ID3D12CommandQueue* commandQueue;
			ID3D12QueryHeap* queryHeap;
			ID3D12Fence* syncFence;

			std::array<uint64_t, MAX_QUERIES_COUNT> queryGpuTimestamps;
			std::array<uint64_t*, MAX_QUERIES_COUNT> queryCpuTimestamps;
			std::atomic<uint32_t> queryIndex;

			ClockSynchronization clock;
			
			uint32_t nodeIndex;

			void UpdateClock();

			Node() : commandQueue(nullptr), queryHeap(nullptr), syncFence(nullptr), nodeIndex(0), queryIndex(0), gpuEventStorage(nullptr) {}
			~Node();
		};

		std::vector<Node*> nodes;
		uint32_t currentNode;

		std::array<Frame, NUM_FRAMES_DELAY> frames;
		
		uint32_t frameNumber;

		ID3D12Resource* queryBuffer;
		ID3D12Device* device;

		enum State
		{
			STATE_OFF,
			STATE_STARTING,
			STATE_RUNNING,
			STATE_FINISHING,
		};

		std::recursive_mutex updateLock;
		volatile State currentState;

		// VSync Stats
		EventStorage* vsyncEventStorage;
		DXGI_FRAME_STATISTICS prevFrameStatistics;

		//void UpdateRange(uint32_t start, uint32_t finish)
		void InitNode(const char* nodeName, uint32_t nodeIndex, ID3D12CommandQueue* pCmdQueue);

		void ResolveTimestamps(uint32_t startIndex, uint32_t count);

		void WaitForFrame(uint64_t frameNumber);
		
	public:
		GPUProfilerD3D12();

		void InitDevice(ID3D12Device* pDevice, ID3D12CommandQueue** pCommandQueues, uint32_t numCommandQueues);

		void QueryTimestamp(ID3D12GraphicsCommandList* context, uint64_t* outCpuTimestamp);

		void Flip(IDXGISwapChain* swapChain);

		// Interface implementation
		void QueryTimestamp(void* context, uint64_t* outCpuTimestamp) override
		{
			QueryTimestamp((ID3D12GraphicsCommandList*)context, outCpuTimestamp);
		}

		void Flip(void* swapChain) override
		{
			Flip(static_cast<IDXGISwapChain*>(swapChain));
		}

		virtual void Start(uint32 mode) override;
		virtual void Stop(uint32 mode) override;
		virtual void Dump(uint32 mode) override;

		void Reset();

		void Shutdown();
	};
}

#endif //BRO_ENABLE_GPU_D3D12