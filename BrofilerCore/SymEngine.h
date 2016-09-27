#pragma once

#if USE_BROFILER_SAMPLING
#include "Common.h"
#include <string>
#include <unordered_map>
#include <array>

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct Symbol
{
	uintptr_t address;
	uintptr_t offset;
	std::wstring module;
	std::wstring file;
	std::wstring function;
	uint32 line;
	Symbol() : line(0), offset(0), address(0) {}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef std::array<uintptr_t, 512> CallStackBuffer;
typedef std::vector<uintptr_t> CallStack;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef std::unordered_map<uintptr_t, Symbol> SymbolCache;
class SymEngine
{
	MW_HANDLE hProcess;
	SymbolCache cache;

	bool isInitialized;

	bool needRestorePreviousSettings;
	uint32 previousOptions;
	static const size_t MAX_SEARCH_PATH_LENGTH = 2048;
	char previousSearchPath[MAX_SEARCH_PATH_LENGTH];
public:
	SymEngine();
	~SymEngine();

	void Init();
	void Close();

	// Get Symbol from PDB file
	const Symbol * const GetSymbol(uintptr_t dwAddress);

	// Collects Callstack
	uint32 GetCallstack(MW_HANDLE hThread, MW_CONTEXT& context, CallStackBuffer& callstack);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

#endif
