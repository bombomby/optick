#include "SysCallCollector.h"
#include "Core.h"

#include <unordered_set>

namespace Brofiler
{
//////////////////////////////////////////////////////////////////////////
OutputDataStream & operator<<(OutputDataStream &stream, const SysCallDesc &ob)
{
	return stream << ob.timestamp << ob.id;
}
//////////////////////////////////////////////////////////////////////////
void SysCallCollector::Add(const SysCallDesc& desc)
{
	syscallPool.Add() = desc;
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