#include "../Hook.h"
#include <string>
#include "../EventDescriptionBoard.h"
#include "../Core.h"
#include "../Thread.h"

extern "C"
{
	Profiler::HookData hookSlotData[Profiler::Hook::SLOT_COUNT];
}

namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Profiler::Hook Profiler::Hook::inst;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Hook::Install(const Symbol& symbol, const std::vector<ulong>& threadIDs)
{
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Hook::Clear(const Symbol& symbol)
{
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Hook::ClearAll()
{
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Hook::Hook()
{
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void HookDescription::Init(const Symbol& symbol)
{
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool InstallSynchronizationHooks(DWORD threadID)
{
	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
