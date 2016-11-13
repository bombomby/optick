#include "CallstackCollector.h"
#include "Core.h"
#include "Platform/SymbolEngine.h"

#include <unordered_set>

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
		memcpy(&storage[3], desc.callstack, desc.count * sizeof(uint64));
	}
	else
	{
		callstacksPool.Add() = desc.threadID;
		callstacksPool.Add() = desc.timestamp;
		callstacksPool.Add() = desc.count;

		for (uint64 i = 0; i < desc.count; ++i)
			callstacksPool.Add() = desc.callstack[i];
	}
}
//////////////////////////////////////////////////////////////////////////
void CallstackCollector::Clear()
{
	callstacksPool.Clear(false);
}

//////////////////////////////////////////////////////////////////////////
bool CallstackCollector::SerializeSymbols(OutputDataStream& stream)
{
	typedef std::unordered_set<uint64> SymbolSet;
	SymbolSet symbolSet;

	for (CallstacksPool::const_iterator it = callstacksPool.begin(); it != callstacksPool.end();)
	{
		CallstacksPool::const_iterator startIt = it;
		MT_UNUSED(startIt);

		uint64 threadID = *it;
		MT_UNUSED(threadID);
		++it; //Skip ThreadID
		uint64 timestamp = *it;
		MT_UNUSED(timestamp);
		++it; //Skip Timestamp
		uint64 count = *it;
		++it; //Skip Count
		for (uint64 i = 0; i < count; ++i)
		{
			uint64 address = *it;
			++it;
			symbolSet.insert(address);
		}
	}

	SymbolEngine* symEngine = Core::Get().symbolEngine;

	std::vector<const Symbol*> symbols;
	symbols.reserve(symbolSet.size());
	for(auto it = symbolSet.begin(); it != symbolSet.end(); ++it)
	{
		uint64 address = *it;
		if (const Symbol* symbol = symEngine->GetSymbol(address))
		{
			symbols.push_back(symbol);
		}
	}

	stream << symbols;
	return true;
}

//////////////////////////////////////////////////////////////////////////
bool CallstackCollector::SerializeCallstacks(OutputDataStream& stream)
{
	if (!callstacksPool.IsEmpty())
	{
		stream << callstacksPool;
		callstacksPool.Clear(false);
		return true;
	}

	return false;
}

//////////////////////////////////////////////////////////////////////////
bool CallstackCollector::IsEmpty() const
{
	return callstacksPool.IsEmpty();
}
//////////////////////////////////////////////////////////////////////////

}