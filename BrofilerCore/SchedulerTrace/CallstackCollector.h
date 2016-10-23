#pragma once
#include <MTTypes.h>

#include "Brofiler.h"
#include "MemoryPool.h"
#include "Serialization.h"


namespace Brofiler
{
	//////////////////////////////////////////////////////////////////////////
	struct CallstackDesc
	{
		uint64 threadID;
		uint64 timestamp;
		size_t* callstack;
		uint8 count;
	};
	//////////////////////////////////////////////////////////////////////////
	class CallstackCollector
	{
		// Packed callstack list: {ThreadID, Timestamp, Count, u64[Count]}
		MemoryPool<uint64, 1024 * 32> callstacksPool;
	public:
		void Add(const CallstackDesc& desc);
		void Clear();

		bool SerializeSymbols(OutputDataStream& stream);
		bool SerializeCallstacks(OutputDataStream& stream);
	};
	//////////////////////////////////////////////////////////////////////////
}