#pragma once
#include "Common.h"
#include "MemoryPool.h"
#include "easyhook.h"
#include "SymEngine.h"
#include "Event.h"
#include <vector>

namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct HookDescription
{
	EventDescription* description;
	std::string nameString;
	std::string fileString;

	HookDescription() : description(nullptr) {}
	void Init(const Symbol& symbol);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef void (*HookSlotFunction)();
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// Packed structure for assembler (4x8byte = 32 bytes)
struct HookData
{
	void* returnAddress;
	void* originalAddress;

	Profiler::EventData* eventData;
	Profiler::EventDescription* eventDescription;

	void Setup(Profiler::EventDescription* desc, void* address)
	{
		returnAddress = nullptr;
		originalAddress = address;
		eventDescription = desc;
		eventData = nullptr;
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct HookSlotWrapper
{
	void* functionAddress;
	HOOK_TRACE_INFO traceInfo;

	HookData* hookData;
	HookSlotFunction	hookFunction;

	bool IsEmpty() const { return functionAddress == nullptr; }

	bool Clear();
	bool Install(const Symbol& symbol, unsigned long threadID);

	HookSlotWrapper();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class Hook
{
	Hook();
public:
	static Hook inst;

	static const uint SLOT_COUNT = 128;
	std::array<HookSlotWrapper, SLOT_COUNT> slots;
	std::vector<HookSlotWrapper*> availableSlots;
	MemoryPool<HookDescription> descriptions;
		
	bool Install(const Symbol& symbol, const std::vector<ulong>& threadIDs);
	bool Clear(const Symbol& symbol);
	bool ClearAll();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}