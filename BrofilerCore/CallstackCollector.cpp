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

		for (uint64 i = 0; i < desc.count; ++i)
		{
			storage[3 + i] = desc.callstack[desc.count - i - 1];
		}
	} else
	{
		uint64& item0 = callstacksPool.Add();
		uint64& item1 = callstacksPool.Add();
		uint64& item2 = callstacksPool.Add();

		item0 = desc.threadID;
		item1 = desc.timestamp;
		item2 = desc.count;

		for (uint64 i = 0; i < desc.count; ++i)
		{
			callstacksPool.Add() = desc.callstack[desc.count - i - 1];
		}
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
		MT_UNUSED(startIt);

		uint64 threadID = *it;
		MT_UNUSED(threadID);
		++it; //Skip ThreadID
		uint64 timestamp = *it;
		MT_UNUSED(timestamp);
		++it; //Skip Timestamp
		uint64 count = *it;
		count = (count & 0xFF);
		++it; //Skip Count

		bool isBadAddrFound = false;

		for (uint64 i = 0; i < count; ++i)
		{
			uint64 address = *it;
			++it;

			if (address == 0)
			{
				isBadAddrFound = true;
			}

			if (!isBadAddrFound)
			{
				symbolSet.insert(address);
			}
		}
	}

	SymbolEngine* symEngine = Core::Get().symbolEngine;

	std::vector<const Symbol*> symbols;
	symbols.reserve(symbolSet.size());

	std::stringstream msg;

	size_t callstacksCount = symbolSet.size();
	size_t callstackIndex = 0;

	for(auto it = symbolSet.begin(); it != symbolSet.end(); ++it)
	{
		callstackIndex++;
		msg.str("");

		uint64 address = *it;
		if (const Symbol* symbol = symEngine->GetSymbol(address))
		{
			symbols.push_back(symbol);
		}

		msg << "Resolving callstack " << (uint32)callstackIndex << " of " << (uint32)(callstacksCount+1) << std::endl;

		Core::Get().DumpProgress(msg.str().c_str());
	}

	stream << symbols;
	return true;
}

//////////////////////////////////////////////////////////////////////////
bool CallstackCollector::SerializeCallstacks(OutputDataStream& stream)
{
	stream << callstacksPool;

	if (!callstacksPool.IsEmpty())
	{
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