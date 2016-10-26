#pragma once
#include "Core.h"
#include <unordered_set>

namespace Brofiler
{
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	struct Symbol
	{
		uint64 address;
		uint64 offset;
		std::wstring module;
		std::wstring file;
		std::wstring function;
		uint32 line;
		Symbol()
			: address(0)
			, offset(0)
			, line(0)
		{}
	};

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	OutputDataStream& operator<<(OutputDataStream& os, const Symbol * const symbol);
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


	struct SymbolEngine
	{
		// Get Symbol from address
		virtual const Symbol* const GetSymbol(uint64 dwAddress) = 0;

		virtual ~SymbolEngine() {};

		static SymbolEngine* Get();
	};

}