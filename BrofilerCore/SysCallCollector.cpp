#include "SysCallCollector.h"
#include "Core.h"

#include <unordered_set>

namespace Brofiler
{
//////////////////////////////////////////////////////////////////////////
OutputDataStream & operator<<(OutputDataStream &stream, const SysCallData &ob)
{
	return stream << (const EventData&)ob << ob.threadID << ob.id;
}
//////////////////////////////////////////////////////////////////////////
SysCallData& SysCallCollector::Add()
{
	return syscallPool.Add();
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