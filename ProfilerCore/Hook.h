#pragma once
#include "Common.h"
#include "MemoryPool.h"
#include "easyhook.h"
#include "SymEngine.h"
#include "Event.h"

namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class HookDescription : public EventDescription
{
	std::string nameString;
	std::string fileString;
public:
	void Init(const Symbol& symbol);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef bool (*SetupSlotFunction)(HookDescription*, void* functionAddress);
typedef void (*HookSlotFunction)();
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct HookSlotWrapper
{
	void* functionAddress;
	HOOK_TRACE_INFO traceInfo;

	SetupSlotFunction setupFunction;
	HookSlotFunction	hookFunction;

	bool IsEmpty() const { return functionAddress == nullptr; }

	bool Clear();
	bool Install(const Symbol& symbol);

	HookSlotWrapper();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class Hook
{
	Hook();
public:
	static Hook inst;

	static const uint SLOT_COUNT = 16;
	std::array<HookSlotWrapper, SLOT_COUNT> slots;
	MemoryPool<HookDescription> descriptions;
		
	bool Install(const Symbol& symbol);
	bool Clear(const Symbol& symbol);
	bool ClearAll();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}