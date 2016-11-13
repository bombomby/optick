#include "SamplingProfiler.h"
#include "SymbolEngine.h"

namespace Brofiler
{


OutputDataStream& SamplingProfiler::Serialize(OutputDataStream& stream)
{
	BRO_VERIFY(!IsActive(), "Can't serialize active Sampler!", return stream);

	stream << (uint32)callstacks.size();

	CallStackTreeNode tree;

	Core::Get().DumpProgress("Merging CallStacks...");

	for(auto it = callstacks.begin(); it != callstacks.end(); ++it)
	{
		const CallStack& callstack = *it;
		if (!callstack.empty())
		{
			tree.Merge(callstack, callstack.size() - 1);
		}
	}

	std::unordered_set<uint64> addresses;
	tree.CollectAddresses(addresses);

	Core::Get().DumpProgress("Resolving Symbols...");

	SymbolEngine* symbolEngine = Core::Get().symbolEngine;

	std::vector<const Symbol*> symbols;
	for(auto it = addresses.begin(); it != addresses.end(); ++it)
	{
		uint64 address = *it;
		if (const Symbol* symbol = symbolEngine->GetSymbol(address))
		{
			symbols.push_back(symbol);
		}
	}

	stream << symbols;

	tree.Serialize(stream);

	return stream;


}



}