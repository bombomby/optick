#pragma once

#include "Common.h"
#include "GPUProfiler.h"

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

#include "Core.h"

namespace Brofiler
{
	class GPUProfilerD3D12 : public GPUProfiler
	{
		struct Frame
		{
			ID3D12CommandAllocator* commandAllocator;
			std::array<ID3D12GraphicsCommandList*, MAX_GPU_NODES> commandList;
			
			std::array<EventData*, MAX_GPU_NODES> frameEvents;
			
			std::array<uint32_t, MAX_GPU_NODES> queryStartIndices;
			std::array<uint32_t, MAX_GPU_NODES> queryCountIndices;

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

		struct NodePayload
		{
			ID3D12CommandQueue* commandQueue;
			ID3D12QueryHeap* queryHeap;
			ID3D12Fence* syncFence;

			NodePayload() : commandQueue(nullptr), queryHeap(nullptr), syncFence(nullptr) {}
			~NodePayload();
		};
		std::vector<NodePayload*> nodePayloads;

		std::array<Frame, NUM_FRAMES_DELAY> frames;
		
		ID3D12Resource* queryBuffer;
		ID3D12Device* device;

		// VSync Stats
		DXGI_FRAME_STATISTICS prevFrameStatistics;

		//void UpdateRange(uint32_t start, uint32_t finish)
		void InitNode(const char* nodeName, uint32_t nodeIndex, ID3D12CommandQueue* pCmdQueue);

		void ResolveTimestamps(uint32_t startIndex, uint32_t count);

		void WaitForFrame(uint64_t frameNumber);
		
	public:
		GPUProfilerD3D12();
		~GPUProfilerD3D12();

		void InitDevice(ID3D12Device* pDevice, ID3D12CommandQueue** pCommandQueues, uint32_t numCommandQueues);

		void QueryTimestamp(ID3D12GraphicsCommandList* context, int64_t* outCpuTimestamp);

		void Flip(IDXGISwapChain* swapChain);


		// Interface implementation
		ClockSynchronization GetClockSynchronization(uint32_t nodeIndex) override;

		void QueryTimestamp(void* context, int64_t* outCpuTimestamp) override
		{
			QueryTimestamp((ID3D12GraphicsCommandList*)context, outCpuTimestamp);
		}

		void Flip(void* swapChain) override
		{
			Flip(static_cast<IDXGISwapChain*>(swapChain));
		}
	};
}

#endif //BRO_ENABLE_GPU_D3D12