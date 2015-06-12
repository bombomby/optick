#include "Hook.h"
#include <windows.h>
#include <string>
#include "EventDescriptionBoard.h"
#include "Core.h"
#include "Thread.h"

namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<uint N>
struct HookSlot
{
	// Description data
	static HookDescription* description;
	static void* functionAddress;

	// Runtime data
	static DWORD returnAddress;
	static EventData* eventData;

	static bool Setup(HookDescription* desc, void* address)
	{
		description = desc;
		functionAddress = address;
		return true;
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<uint N>
HookDescription* HookSlot<N>::description = nullptr;

template<uint N>
void* HookSlot<N>::functionAddress = nullptr;

template<uint N>
DWORD HookSlot<N>::returnAddress = 0;

template<uint N>
EventData* HookSlot<N>::eventData = nullptr;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This Function is set as hook through EasyHook library. 
// EasyHook doesn't support recursion - it is able to hook only first function in recursive call.
// So here you won't find recursion support.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<uint N>
__declspec(naked) void HookFunction()
{
	_asm { pushad }

	// Collecting event start time
	if (EventStorage* storate = Core::storage)
	{
		HookSlot<N>::eventData = &(storate->NextEvent());
		HookSlot<N>::eventData->description = HookSlot<N>::description->description;
		QueryPerformanceCounter((LARGE_INTEGER*)(&HookSlot<N>::eventData->start));
	}

	_asm 
	{
		popad;

		// Modification of return address to continue flow after function execution
		pop HookSlot<N>::returnAddress;

		// Call original function
		call [HookSlot<N>::functionAddress];

		// Restore return address
		push HookSlot<N>::returnAddress;

		pushad;
	}

	// Collecting event finish time
	if (Core::storage)
	{
		QueryPerformanceCounter((LARGE_INTEGER*)(&HookSlot<N>::eventData->finish));
	}

	_asm 
	{
		popad;
		ret;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Profiler::Hook Profiler::Hook::inst;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Hook::Install(const Symbol& symbol)
{
	for (auto it = slots.begin(); it != slots.end(); ++it)
		if (it->IsEmpty())
			return it->Install(symbol);

	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Hook::Clear(const Symbol& symbol)
{
	void* address = (void*)(symbol.address - symbol.offset);

	for (auto it = slots.begin(); it != slots.end(); ++it)
		if (!it->IsEmpty() && it->functionAddress == address)
			return it->Clear();

	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Hook::ClearAll()
{
	for (auto it = slots.begin(); it != slots.end(); ++it)
		it->Clear();

	if (!NT_SUCCESS(LhUninstallAllHooks()))
		return false;

	if (!NT_SUCCESS(LhWaitForPendingRemovals()))
		return false;

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// HookSlot Recursive template generation
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<uint N>
void GenerateHookFunctions(std::array<HookSlotWrapper, Hook::SLOT_COUNT>& slots)
{
	static_assert( 0 < N && N <= Hook::SLOT_COUNT, "Invalid hook index!" );
	const uint index = N - 1;
	slots[index].setupFunction = HookSlot<index>::Setup;
	slots[index].hookFunction = HookFunction<index>;
	GenerateHookFunctions<index>(slots);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<>
void GenerateHookFunctions<0>(std::array<HookSlotWrapper, Hook::SLOT_COUNT>&) {}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Hook::Hook()
{
	GenerateHookFunctions<SLOT_COUNT>(slots);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void HookDescription::Init(const Symbol& symbol)
{
	nameString = std::string(symbol.function.begin(), symbol.function.end());
	fileString = std::string(symbol.file.begin(),symbol.file.end()); 
	description = EventDescription::Create(nameString.c_str(), fileString.c_str(), symbol.line);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool HookSlotWrapper::Clear()
{
	if (!IsEmpty())
	{
		LhUninstallHook(&traceInfo);
		traceInfo.Link = nullptr;
		functionAddress = nullptr;
	}

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool HookSlotWrapper::Install(const Symbol& symbol)
{
	BRO_VERIFY(IsEmpty(), "Can't install hook twice in the same slot", return false);

	void* address = (void*)(symbol.address - symbol.offset);

	NTSTATUS status = LhInstallHook( address, hookFunction, nullptr, &traceInfo);
	if (!NT_SUCCESS(status))
		return false;

	ULONG threadID[] = {0};

	status = LhSetInclusiveACL(threadID, 1, &traceInfo);
	if (!NT_SUCCESS(status))
		return false;

	HookDescription* description = &Hook::inst.descriptions.Add();
	description->Init(symbol);

	functionAddress = address;
	setupFunction(description, functionAddress);

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HookSlotWrapper::HookSlotWrapper() : functionAddress(nullptr)
{
	traceInfo.Link = nullptr;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<class TFunc>
struct FuncOverride
{
	TFunc originalFunction;
	
	std::vector<TRACED_HOOK_HANDLE> trackHandles;

	FuncOverride(LPCSTR dllName, LPCSTR funcName) : originalFunction(nullptr)
	{
		HMODULE module = LoadLibraryA(dllName);

		if (module != nullptr)
		{
			originalFunction = (TFunc)GetProcAddress( module, funcName );
		}
	}

	bool Install(DWORD threadID, TFunc overrideFunction)
	{
		TRACED_HOOK_HANDLE trackHandle = new HOOK_TRACE_INFO();
		trackHandles.push_back(trackHandle);

		trackHandle->Link = 0;
		
		NTSTATUS status = LhInstallHook( originalFunction, overrideFunction, nullptr, trackHandle );
		if (status != 0)
			return false;
		
		DWORD threads[] = {threadID};
		status = LhSetInclusiveACL(threads, 1, trackHandle);

		return status == 0;
	}

	~FuncOverride()
	{
		for each (TRACED_HOOK_HANDLE handle in trackHandles)
			delete handle;
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef VOID (WINAPI *SleepFunction)(_In_ DWORD dwMilliseconds);
FuncOverride<SleepFunction> SleepOverride("Kernel32.dll", "Sleep");
VOID WINAPI SleepHooked(_In_ DWORD dwMilliseconds)
{
	PROFILER_CATEGORY("Sleep", Profiler::Color::White)
	SleepOverride.originalFunction(dwMilliseconds);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef VOID (WINAPI *SleepExFunction)(_In_ DWORD dwMilliseconds, _In_ BOOL  bAlertable);
FuncOverride<SleepExFunction> SleepExOverride("Kernel32.dll", "SleepEx");
VOID WINAPI SleepExHooked(_In_ DWORD dwMilliseconds, _In_ BOOL  bAlertable)
{
	PROFILER_CATEGORY("SleepEx", Profiler::Color::White)
	SleepExOverride.originalFunction(dwMilliseconds, bAlertable);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef DWORD (WINAPI *WaitForSingleObjectFunction)(_In_ HANDLE hHandle, _In_ DWORD  dwMilliseconds);
FuncOverride<WaitForSingleObjectFunction> WaitForSingleObjectOverride("Kernel32.dll", "WaitForSingleObject");
DWORD WINAPI WaitForSingleObjectHooked(_In_ HANDLE hHandle, _In_ DWORD  dwMilliseconds)
{
	PROFILER_CATEGORY("WaitForSingleObject", Profiler::Color::White)
	return WaitForSingleObjectOverride.originalFunction(hHandle, dwMilliseconds);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef DWORD (WINAPI *WaitForSingleObjectExFunction)(_In_ HANDLE hHandle, _In_ DWORD  dwMilliseconds, _In_ BOOL bAlertable);
FuncOverride<WaitForSingleObjectExFunction> WaitForSingleObjectExOverride("Kernel32.dll", "WaitForSingleObjectEx");
DWORD WINAPI WaitForSingleObjectExHooked(_In_ HANDLE hHandle, _In_ DWORD  dwMilliseconds, _In_ BOOL bAlertable)
{
	PROFILER_CATEGORY("WaitForSingleObjectEx", Profiler::Color::White)
	return WaitForSingleObjectExOverride.originalFunction(hHandle, dwMilliseconds, bAlertable);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef DWORD (WINAPI *WaitForMultipleObjectsFunction)(_In_ DWORD  nCount, _In_ const HANDLE *lpHandles, _In_ BOOL bWaitAll, _In_ DWORD dwMilliseconds);
FuncOverride<WaitForMultipleObjectsFunction> WaitForMultipleObjectsOverride("Kernel32.dll", "WaitForMultipleObjects");
DWORD WINAPI WaitForMultipleObjectsHooked(_In_ DWORD  nCount, _In_ const HANDLE *lpHandles, _In_ BOOL bWaitAll, _In_ DWORD dwMilliseconds)
{
	PROFILER_CATEGORY("WaitForMultipleObjects", Profiler::Color::White)
	return WaitForMultipleObjectsOverride.originalFunction(nCount, lpHandles, bWaitAll, dwMilliseconds);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef DWORD (WINAPI *WaitForMultipleObjectsExFunction)(_In_ DWORD  nCount, _In_ const HANDLE *lpHandles, _In_ BOOL bWaitAll, _In_ DWORD dwMilliseconds, _In_ BOOL bAlertable);
FuncOverride<WaitForMultipleObjectsExFunction> WaitForMultipleObjectsExOverride("Kernel32.dll", "WaitForMultipleObjectsEx");
DWORD WINAPI WaitForMultipleObjectsExHooked(_In_ DWORD  nCount, _In_ const HANDLE *lpHandles, _In_ BOOL bWaitAll, _In_ DWORD dwMilliseconds, _In_ BOOL bAlertable)
{
	PROFILER_CATEGORY("WaitForMultipleObjectsEx", Profiler::Color::White)
	return WaitForMultipleObjectsExOverride.originalFunction(nCount, lpHandles, bWaitAll, dwMilliseconds, bAlertable);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool InstallSynchronizationHooks(DWORD threadID)
{
	bool result = true;
	#define INSTALL_OVERRIDE(NAME) result &= NAME##Override.Install(threadID, NAME##Hooked)

	INSTALL_OVERRIDE(Sleep);
	INSTALL_OVERRIDE(SleepEx);
	INSTALL_OVERRIDE(WaitForSingleObject);
	INSTALL_OVERRIDE(WaitForSingleObjectEx);
	INSTALL_OVERRIDE(WaitForMultipleObjects);
	INSTALL_OVERRIDE(WaitForMultipleObjectsEx);

	return result;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}