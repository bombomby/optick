#pragma once
#include "Common.h"
#include <string>
#include <windows.h>
#include <unordered_map>
#include <array>

namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct Symbol
{
	DWORD64 address;
	DWORD64 offset;
	std::wstring module;
	std::wstring file;
	std::wstring function;
	uint32			 line;
	Symbol() : line(0), offset(0), address(0) {}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef std::array<DWORD64, 512> CallStackBuffer;
typedef std::vector<DWORD64> CallStack;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef std::unordered_map<DWORD64, Symbol> SymbolCache;
class SymEngine
{
	HANDLE hProcess;
	SymbolCache cache;

	bool isInitialized;

	bool needRestorePreviousSettings;
	DWORD previousOptions;
	static const size_t MAX_SEARCH_PATH_LENGTH = 2048;
	char previousSearchPath[MAX_SEARCH_PATH_LENGTH];
public:
	SymEngine();
	~SymEngine();

	void Init();
	void Close();

	// Get Symbol from PDB file
	const Symbol * const GetSymbol(DWORD64 dwAddress);

	// Collects Callstack
	uint GetCallstack(HANDLE hThread, CONTEXT& context, CallStackBuffer& callstack);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}