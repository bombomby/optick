#include "CallstackCollector.h"
#include "SymEngine.h"

#include <unordered_map>

namespace Brofiler
{
//////////////////////////////////////////////////////////////////////////
void CallstackCollector::Add(const CallstackDesc& desc)
{
	if (uint64* storage = callstacksPool.TryAdd(desc.count + 3))
	{
		storage[0] = desc.threadID;
		storage[1] = desc.timestamp;
		storage[2] = desc.count;
		memcpy(&storage[3], desc.callstack, desc.count);
	}
	else
	{
		callstacksPool.Add() = desc.threadID;
		callstacksPool.Add() = desc.timestamp;
		callstacksPool.Add() = desc.count;

		for (uint8 i = 0; i < desc.count; ++i)
			callstacksPool.Add() = desc.callstack[i];
	}
}
//////////////////////////////////////////////////////////////////////////
void CallstackCollector::Clear()
{
	callstacksPool.Clear(false);
}

bool CallstackCollector::SerializeSymbols(OutputDataStream& stream)
{
	//std::unordered_map<uint64, Symbol*> symbolMap;
	BRO_UNUSED(stream);
	return true;
}

//////////////////////////////////////////////////////////////////////////
bool CallstackCollector::SerializeCallstacks(OutputDataStream& stream)
{
	if (!callstacksPool.IsEmpty())
	{
		stream << callstacksPool;
		return true;
	}

	return false;
}

//////////////////////////////////////////////////////////////////////////
}