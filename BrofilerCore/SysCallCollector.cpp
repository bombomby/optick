#include "SysCallCollector.h"
#include "Core.h"

#include <unordered_set>

namespace Brofiler
{
//////////////////////////////////////////////////////////////////////////
void SysCallCollector::Add(const SysCallDesc& desc)
{
	if (uint64* storage = syscallPool.TryAdd(2))
	{
		storage[0] = desc.timestamp;
		storage[1] = desc.id;

	} else
	{
		uint64& item0 = syscallPool.Add();
		uint64& item1 = syscallPool.Add();

		item0 = desc.timestamp;
		item1 = desc.id;
	}
}
//////////////////////////////////////////////////////////////////////////
void SysCallCollector::Clear()
{
	syscallPool.Clear(false);
}


//////////////////////////////////////////////////////////////////////////
bool SysCallCollector::Serialize(OutputDataStream& stream)
{
	stream << syscallPool;

	if (!syscallPool.IsEmpty())
	{
		syscallPool.Clear(false);
		return true;
	}

	return false;
}

//////////////////////////////////////////////////////////////////////////
}