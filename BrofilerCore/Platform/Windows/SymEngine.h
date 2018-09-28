#pragma once


#include "Common.h"
#include <string>
#include <unordered_map>
#include <array>
#include "Serialization.h"
#include "../SymbolEngine.h"

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef std::array<uintptr_t, 512> CallStackBuffer;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class SymEngine : public SymbolEngine
{
	HANDLE hProcess;

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
	virtual const Symbol * const GetSymbol(uint64 dwAddress) override;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}


