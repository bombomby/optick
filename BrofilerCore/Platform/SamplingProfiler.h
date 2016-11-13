#pragma once
#include "Core.h"
#include <unordered_set>

namespace Brofiler
{
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	typedef std::vector<uintptr_t> CallStack;


	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	struct CallStackTreeNode
	{
		uint64 dwArddress;
		uint32 invokeCount;

		std::list<CallStackTreeNode> children;

		CallStackTreeNode() : dwArddress(0), invokeCount(0)
		{
		}

		CallStackTreeNode(uint64 address) : dwArddress(address), invokeCount(0)
		{
		} 

		bool Merge(const CallStack& callstack, size_t index)
		{
			++invokeCount;
			if (index == 0)
			{
				return true;
			}

			// I suppose, that usually sampling function has only several children.. so linear search will be fast enough
			uint64 address = callstack[index];
			for (auto it = children.begin(); it != children.end(); ++it)
			{
				if (it->dwArddress == address)
				{
					return it->Merge(callstack, index - 1);
				}
			}

			// Didn't find node => create one
			children.push_back(CallStackTreeNode(address));
			return children.back().Merge(callstack, index - 1); 
		}

		void CollectAddresses(std::unordered_set<uint64>& addresses) const
		{
			addresses.insert(dwArddress);
			for(auto it = children.begin(); it != children.end(); ++it)
			{
				it->CollectAddresses(addresses);
			}
		}

		OutputDataStream& Serialize(OutputDataStream& stream) const
		{
			stream << (uint64)dwArddress << invokeCount;

			stream << (uint32)children.size();
			for(auto it = children.begin(); it != children.end(); ++it)
			{
				it->Serialize(stream);
			}

			return stream;
		}
	};

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	struct SamplingProfiler
	{
		virtual bool IsSamplingScope() const = 0;

		virtual bool IsActive() const = 0;

		virtual void StartSampling(const std::vector<ThreadEntry*>& threads, uint32 samplingInterval = 300) = 0;
		virtual bool StopSampling() = 0;

		virtual size_t GetCollectedCount() const = 0;

		virtual ~SamplingProfiler() {};
		virtual OutputDataStream& Serialize(OutputDataStream& stream);

		static SamplingProfiler* Get();


	protected:

		std::list<CallStack> callstacks;

	};

}