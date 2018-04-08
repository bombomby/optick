#pragma once
#include <MTTypes.h>

#include "Brofiler.h"
#include "MemoryPool.h"
#include "Serialization.h"


namespace Brofiler
{
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	struct SwitchContextDesc
	{
		int64_t timestamp;
		uint64 oldThreadId;
		uint64 newThreadId;
		uint8 cpuId;
		uint8 reason;
	};
	//////////////////////////////////////////////////////////////////////////
	OutputDataStream &operator << (OutputDataStream &stream, const SwitchContextDesc &ob);
	//////////////////////////////////////////////////////////////////////////
	class SwitchContextCollector
	{
		typedef MemoryPool<SwitchContextDesc, 1024 * 32> SwitchContextPool;
		SwitchContextPool switchContextPool;
	public:
		void Add(const SwitchContextDesc& desc);
		void Clear();
		bool Serialize(OutputDataStream& stream);
	};
	//////////////////////////////////////////////////////////////////////////
}