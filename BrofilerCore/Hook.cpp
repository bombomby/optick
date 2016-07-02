#include "Hook.h"
#include <windows.h>
#include <string>
#include "EventDescriptionBoard.h"
#include "Core.h"
#include "Thread.h"
#include "HookFunction.h"

extern "C"
{
	Profiler::HookData hookSlotData[Profiler::Hook::SLOT_COUNT];
}

namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// EasyHook wrapper for Static Linking
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class EasyHook
{
	#if defined(_WIN64)
		#define EASYHOOK_CALL_CONVENTION
	#else
		#define EASYHOOK_CALL_CONVENTION __stdcall
	#endif

	typedef LONG (EASYHOOK_CALL_CONVENTION *Function_LhWaitForPendingRemovals)();
	Function_LhWaitForPendingRemovals function_LhWaitForPendingRemovals;

	typedef LONG (EASYHOOK_CALL_CONVENTION *Function_LhUninstallAllHooks)();
	Function_LhUninstallAllHooks function_LhUninstallAllHooks;

	typedef LONG (EASYHOOK_CALL_CONVENTION *Function_LhUninstallHook)(TRACED_HOOK_HANDLE InHandle);
	Function_LhUninstallHook function_LhUninstallHook;

	typedef LONG (EASYHOOK_CALL_CONVENTION *Function_LhSetInclusiveACL)(ULONG* InProcessIdList, ULONG InProcessCount, TRACED_HOOK_HANDLE InHandle);
	Function_LhSetInclusiveACL function_LhSetInclusiveACL;

	typedef LONG (EASYHOOK_CALL_CONVENTION *Function_LhInstallHook)(void* InEntryPoint, void* InHookProc, void* InCallback, TRACED_HOOK_HANDLE OutHandle);
	Function_LhInstallHook function_LhInstallHook;
	
public:
	EasyHook() 
		: function_LhWaitForPendingRemovals(nullptr), 
			function_LhUninstallAllHooks(nullptr),
			function_LhUninstallHook(nullptr), 
			function_LhSetInclusiveACL(nullptr), 
			function_LhInstallHook(nullptr)
	{
		

		#if defined(_WIN64)
			if (HMODULE module = LoadLibrary("EasyHook64.dll"))
			{
				function_LhWaitForPendingRemovals = (Function_LhWaitForPendingRemovals)GetProcAddress(module, "LhWaitForPendingRemovals");
				function_LhUninstallAllHooks = (Function_LhUninstallAllHooks)GetProcAddress(module, "LhUninstallAllHooks");
				function_LhUninstallHook = (Function_LhUninstallHook)GetProcAddress(module, "LhUninstallHook");
				function_LhSetInclusiveACL = (Function_LhSetInclusiveACL)GetProcAddress(module, "LhSetInclusiveACL");
				function_LhInstallHook = (Function_LhInstallHook)GetProcAddress(module, "LhInstallHook");
			}
		#else
			if (HMODULE module = LoadLibrary("EasyHook32.dll"))
			{
				function_LhWaitForPendingRemovals = (Function_LhWaitForPendingRemovals)GetProcAddress(module, "_LhWaitForPendingRemovals@0");
				function_LhUninstallAllHooks = (Function_LhUninstallAllHooks)GetProcAddress(module, "_LhUninstallAllHooks@0");
				function_LhUninstallHook = (Function_LhUninstallHook)GetProcAddress(module, "_LhUninstallHook@4");
				function_LhSetInclusiveACL = (Function_LhSetInclusiveACL)GetProcAddress(module, "_LhSetInclusiveACL@12");
				function_LhInstallHook = (Function_LhInstallHook)GetProcAddress(module, "_LhInstallHook@16");
			}
		#endif
	}

	LONG LhWaitForPendingRemovals()
	{
		return function_LhWaitForPendingRemovals ? function_LhWaitForPendingRemovals() : 0;
	}

	LONG LhUninstallAllHooks()
	{
		return function_LhUninstallAllHooks ? function_LhUninstallAllHooks() : 0;
	}

	LONG LhUninstallHook(TRACED_HOOK_HANDLE InHandle)
	{
		return function_LhUninstallHook ? function_LhUninstallHook(InHandle) : 0;
	}

	LONG LhSetInclusiveACL(ULONG* InProcessIdList, ULONG InProcessCount, TRACED_HOOK_HANDLE InHandle)
	{
		return function_LhSetInclusiveACL ? function_LhSetInclusiveACL(InProcessIdList, InProcessCount, InHandle) : 0;
	}

	LONG LhInstallHook(void* InEntryPoint, void* InHookProc, void* InCallback, TRACED_HOOK_HANDLE OutHandle)
	{
		return function_LhInstallHook ? function_LhInstallHook(InEntryPoint, InHookProc, InCallback, OutHandle) : 0;
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EasyHook easyHook;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Profiler::Hook Profiler::Hook::inst;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Hook::Install(const Symbol& symbol, const std::vector<ulong>& threadIDs)
{
	auto thread = threadIDs.begin();
	for (auto it = slots.begin(); it != slots.end() && thread != threadIDs.end(); ++it)
	{
		if (it->IsEmpty())
		{
			if (!it->Install(symbol, *thread))
				return false;

			++thread;
		}
	}

	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Hook::Clear(const Symbol& symbol)
{
	void* address = (void*)(symbol.address - symbol.offset);

	bool result = true;

	for (auto it = slots.begin(); it != slots.end(); ++it)
		if (!it->IsEmpty() && it->functionAddress == address)
			result &= it->Clear();

	return result;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Hook::ClearAll()
{
	for (auto it = slots.begin(); it != slots.end(); ++it)
		it->Clear();

	if (!NT_SUCCESS(easyHook.LhUninstallAllHooks()))
		return false;

	if (!NT_SUCCESS(easyHook.LhWaitForPendingRemovals()))
		return false;

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Hook::Hook()
{
	for (size_t index = 0; index < Hook::SLOT_COUNT; ++index)
	{
		slots[index].hookData = &hookSlotData[index];
		slots[index].hookFunction = hookSlotFunctions[index];
	}
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
		easyHook.LhUninstallHook(&traceInfo);
		traceInfo.Link = nullptr;
		functionAddress = nullptr;
	}

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool HookSlotWrapper::Install(const Symbol& symbol, ulong threadID)
{
	BRO_VERIFY(IsEmpty(), "Can't install hook twice in the same slot", return false);

	void* address = (void*)(symbol.address - symbol.offset);

	NTSTATUS status = easyHook.LhInstallHook( address, hookFunction, nullptr, &traceInfo);
	if (!NT_SUCCESS(status))
		return false;

	ULONG threadList[] = {threadID};

	status = easyHook.LhSetInclusiveACL(threadList, 1, &traceInfo);
	if (!NT_SUCCESS(status))
		return false;

	HookDescription* description = &Hook::inst.descriptions.Add();
	description->Init(symbol);

	functionAddress = address;
	hookData->Setup(description->description, functionAddress);

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
	Profiler::EventDescription* description;
	
	
	std::vector<TRACED_HOOK_HANDLE> trackHandles;

	FuncOverride(LPCSTR dllName, LPCSTR funcName) : originalFunction(nullptr), description(nullptr)
	{
		HMODULE module = LoadLibraryA(dllName);

		if (module != nullptr)
		{
			originalFunction = (TFunc)GetProcAddress( module, funcName );
			description = Profiler::EventDescription::Create(funcName, dllName, 0, (uint32)Profiler::Color::White);
		}
	}

	bool Install(DWORD threadID, TFunc overrideFunction)
	{
		TRACED_HOOK_HANDLE trackHandle = new HOOK_TRACE_INFO();
		trackHandles.push_back(trackHandle);

		trackHandle->Link = 0;
		
		NTSTATUS status = easyHook.LhInstallHook( originalFunction, overrideFunction, nullptr, trackHandle );
		if (status != 0)
			return false;
		
		DWORD threads[] = {threadID};
		status = easyHook.LhSetInclusiveACL(threads, 1, trackHandle);

		return status == 0;
	}

	~FuncOverride()
	{
		for each (TRACED_HOOK_HANDLE handle in trackHandles)
		{
			easyHook.LhUninstallHook(handle);
			delete handle;
		}
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef VOID (WINAPI *SleepFunction)(_In_ DWORD dwMilliseconds);
static FuncOverride<SleepFunction> SleepOverride("Kernel32.dll", "Sleep");
VOID WINAPI SleepHooked(_In_ DWORD dwMilliseconds)
{
	BROFILER_CATEGORY("Sleep", Profiler::Color::White)
	SleepOverride.originalFunction(dwMilliseconds);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef VOID (WINAPI *SleepExFunction)(_In_ DWORD dwMilliseconds, _In_ BOOL  bAlertable);
static FuncOverride<SleepExFunction> SleepExOverride("Kernel32.dll", "SleepEx");
VOID WINAPI SleepExHooked(_In_ DWORD dwMilliseconds, _In_ BOOL  bAlertable)
{
	BROFILER_CATEGORY("SleepEx", Profiler::Color::White)
	SleepExOverride.originalFunction(dwMilliseconds, bAlertable);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef DWORD (WINAPI *WaitForSingleObjectFunction)(_In_ HANDLE hHandle, _In_ DWORD  dwMilliseconds);
static FuncOverride<WaitForSingleObjectFunction> WaitForSingleObjectOverride("Kernel32.dll", "WaitForSingleObject");
DWORD WINAPI WaitForSingleObjectHooked(_In_ HANDLE hHandle, _In_ DWORD  dwMilliseconds)
{
	BROFILER_CATEGORY("WaitForSingleObject", Profiler::Color::White)
	return WaitForSingleObjectOverride.originalFunction(hHandle, dwMilliseconds);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef DWORD (WINAPI *WaitForSingleObjectExFunction)(_In_ HANDLE hHandle, _In_ DWORD  dwMilliseconds, _In_ BOOL bAlertable);
static FuncOverride<WaitForSingleObjectExFunction> WaitForSingleObjectExOverride("Kernel32.dll", "WaitForSingleObjectEx");
DWORD WINAPI WaitForSingleObjectExHooked(_In_ HANDLE hHandle, _In_ DWORD  dwMilliseconds, _In_ BOOL bAlertable)
{
	BROFILER_CATEGORY("WaitForSingleObjectEx", Profiler::Color::White)
	return WaitForSingleObjectExOverride.originalFunction(hHandle, dwMilliseconds, bAlertable);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef DWORD (WINAPI *WaitForMultipleObjectsFunction)(_In_ DWORD  nCount, _In_ const HANDLE *lpHandles, _In_ BOOL bWaitAll, _In_ DWORD dwMilliseconds);
static FuncOverride<WaitForMultipleObjectsFunction> WaitForMultipleObjectsOverride("Kernel32.dll", "WaitForMultipleObjects");
DWORD WINAPI WaitForMultipleObjectsHooked(_In_ DWORD  nCount, _In_ const HANDLE *lpHandles, _In_ BOOL bWaitAll, _In_ DWORD dwMilliseconds)
{
	BROFILER_CATEGORY("WaitForMultipleObjects", Profiler::Color::White)
	return WaitForMultipleObjectsOverride.originalFunction(nCount, lpHandles, bWaitAll, dwMilliseconds);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef DWORD (WINAPI *WaitForMultipleObjectsExFunction)(_In_ DWORD  nCount, _In_ const HANDLE *lpHandles, _In_ BOOL bWaitAll, _In_ DWORD dwMilliseconds, _In_ BOOL bAlertable);
static FuncOverride<WaitForMultipleObjectsExFunction> WaitForMultipleObjectsExOverride("Kernel32.dll", "WaitForMultipleObjectsEx");
DWORD WINAPI WaitForMultipleObjectsExHooked(_In_ DWORD  nCount, _In_ const HANDLE *lpHandles, _In_ BOOL bWaitAll, _In_ DWORD dwMilliseconds, _In_ BOOL bAlertable)
{
	BROFILER_CATEGORY("WaitForMultipleObjectsEx", Profiler::Color::White)
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
