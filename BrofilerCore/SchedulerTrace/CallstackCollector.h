#pragma once
#include <MTTypes.h>

#include "Brofiler.h"
#include "MemoryPool.h"
#include "Serialization.h"


namespace Brofiler
{
	//////////////////////////////////////////////////////////////////////////
	class SymEngine;
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
		typedef MemoryPool<uint64, 1024 * 32> CallstacksPool;
		CallstacksPool callstacksPool;
	public:
		void Add(const CallstackDesc& desc);
		void Clear();

		bool SerializeSymbols(OutputDataStream& stream);
		bool SerializeCallstacks(OutputDataStream& stream);

		bool IsEmpty() const;
	};
	//////////////////////////////////////////////////////////////////////////
}