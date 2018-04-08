#include "SwitchContextCollector.h"

namespace Brofiler
{
//////////////////////////////////////////////////////////////////////////
OutputDataStream & operator<<(OutputDataStream &stream, const SwitchContextDesc &ob)
{
	return stream << ob.timestamp << ob.oldThreadId << ob.newThreadId << ob.cpuId << ob.reason;
}
//////////////////////////////////////////////////////////////////////////
void SwitchContextCollector::Add(const SwitchContextDesc& desc)
{
	switchContextPool.Add() = desc;
}
//////////////////////////////////////////////////////////////////////////
void SwitchContextCollector::Clear()
{
	switchContextPool.Clear(false);
}
//////////////////////////////////////////////////////////////////////////
bool SwitchContextCollector::Serialize(OutputDataStream& stream)
{
	stream << switchContextPool;

	if (!switchContextPool.IsEmpty())
	{
		switchContextPool.Clear(false);
		return true;
	}

	return false;
}
//////////////////////////////////////////////////////////////////////////
}