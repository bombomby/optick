#pragma once
#include <MTTypes.h>

#include "Brofiler.h"
#include "MemoryPool.h"
#include "Serialization.h"


namespace Brofiler
{
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	struct SysCallDesc
	{
		int64_t timestamp;
		uint64 id;
	};

	//////////////////////////////////////////////////////////////////////////
	class SysCallCollector
	{
		// Packed syscall events: {timestamp, id}
		typedef MemoryPool<uint64, 1024 * 32> SysCallPool;
		SysCallPool syscallPool;
	public:
		void Add(const SysCallDesc& desc);
		void Clear();

		bool Serialize(OutputDataStream& stream);
	};
	//////////////////////////////////////////////////////////////////////////
}