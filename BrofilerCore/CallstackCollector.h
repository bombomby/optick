#pragma once
#include "Types.h"
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
		uint64* callstack;
		uint8 count;
	};


	//////////////////////////////////////////////////////////////////////////
	class CallstackCollector
	{
		// Packed callstack list: {ThreadID, Timestamp, Count, Callstack[Count]}
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