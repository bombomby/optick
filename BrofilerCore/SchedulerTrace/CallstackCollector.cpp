#include "CallstackCollector.h"
#include "Core.h"
#include "SymEngine.h"

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

bool CallstackCollector::SerializeSymbols(OutputDataStream& stream)
{
	typedef std::unordered_set<uint64> SymbolSet;
	SymbolSet symbolSet;

	for (CallstacksPool::const_iterator it = callstacksPool.begin(); it != callstacksPool.end();)
	{
		CallstacksPool::const_iterator startIt = it;

		uint64 threadID = *it;
		++it; //Skip ThreadID
		uint64 timestamp = *it;
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

	SymEngine& symEngine = Core::Get().symEngine;

	std::vector<const Symbol*> symbols;
	symbols.reserve(symbolSet.size());
	for each (uint64 address in symbolSet)
		if (const Symbol* symbol = symEngine.GetSymbol(address))
			symbols.push_back(symbol);

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
}