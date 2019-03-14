#if _WIN32
#include "SymbolEngine.h"

#if BRO_ENABLE_SYMENGINE

#define USE_DBG_HELP (BRO_PC)

#if USE_DBG_HELP
#include <DbgHelp.h>
#pragma comment( lib, "DbgHelp.Lib" )
#endif

#include "Serialization.h"


// Forward declare kernel functions
#pragma pack(push,8)
typedef struct _MODULEINFO {
	LPVOID lpBaseOfDll;
	DWORD SizeOfImage;
	LPVOID EntryPoint;
} MODULEINFO, *LPMODULEINFO;
#pragma pack(pop)
#ifndef EnumProcessModulesEx
#define EnumProcessModulesEx        K32EnumProcessModulesEx
EXTERN_C DWORD WINAPI K32EnumProcessModulesEx(HANDLE hProcess, HMODULE *lphModule, DWORD cb, LPDWORD lpcbNeeded, DWORD dwFilterFlag);
#endif
#ifndef GetModuleInformation
#define GetModuleInformation        K32GetModuleInformation
EXTERN_C DWORD WINAPI K32GetModuleInformation(HANDLE hProcess, HMODULE hModule, LPMODULEINFO lpmodinfo, DWORD cb);
#endif

#ifndef GetModuleFileNameExA
#define GetModuleFileNameExA        K32GetModuleFileNameExA
EXTERN_C DWORD WINAPI K32GetModuleFileNameExA(HANDLE hProcess, HMODULE hModule, LPSTR lpFilename, DWORD nSize);
#endif



namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//void ReportLastError()
//{
//	LPVOID lpMsgBuf;
//	DWORD dw = GetLastError();
//
//	FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
//								NULL, dw, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), 
//								(LPTSTR)&lpMsgBuf, 0, NULL);
//
//	MessageBox(NULL, (LPCTSTR)lpMsgBuf, TEXT("Error"), MB_OK);
//	LocalFree(lpMsgBuf);
//}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef std::array<uintptr_t, 512> CallStackBuffer;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class WinSymbolEngine : public SymbolEngine
{
	HANDLE hProcess;

	bool isInitialized;

	bool needRestorePreviousSettings;
	uint32 previousOptions;
	static const size_t MAX_SEARCH_PATH_LENGTH = 2048;
	char previousSearchPath[MAX_SEARCH_PATH_LENGTH];

	void InitSystemModules();
	void InitApplicationModules();
public:
	WinSymbolEngine();
	~WinSymbolEngine();

	void Init();
	void Close();

	// Get Symbol from PDB file
	virtual const Symbol * const GetSymbol(uint64 dwAddress) override;

	virtual const std::vector<Module>& GetModules() override;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


WinSymbolEngine::WinSymbolEngine() : isInitialized(false), hProcess(GetCurrentProcess()), needRestorePreviousSettings(false), previousOptions(0)
{
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
WinSymbolEngine::~WinSymbolEngine()
{
	Close();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const Symbol * const WinSymbolEngine::GetSymbol(uint64 address)
{
	if (address == 0)
		return nullptr;

	Init();

	Symbol& symbol = cache[address];

	if (symbol.address != 0)
		return &symbol;

	if (!isInitialized)
		return nullptr;

	symbol.address = address;

#if USE_DBG_HELP
	DWORD64 dwAddress = static_cast<DWORD64>(address);

	// FileName and Line
	IMAGEHLP_LINEW64 lineInfo;
	memset(&lineInfo, 0, sizeof(IMAGEHLP_LINEW64));
	lineInfo.SizeOfStruct = sizeof(lineInfo);
	DWORD dwDisp;
	if (SymGetLineFromAddrW64(hProcess, dwAddress, &dwDisp, &lineInfo))
	{
		symbol.file = lineInfo.FileName;
		symbol.line = lineInfo.LineNumber;
	}

	const size_t length = (sizeof(SYMBOL_INFOW) + MAX_SYM_NAME*sizeof(WCHAR) + sizeof(ULONG64) - 1) / sizeof(ULONG64) + 1;

	// Function Name
	ULONG64 buffer[length];
	PSYMBOL_INFOW dbgSymbol = (PSYMBOL_INFOW)buffer;
	memset(dbgSymbol, 0, sizeof(buffer));
	dbgSymbol->SizeOfStruct = sizeof(SYMBOL_INFOW);
	dbgSymbol->MaxNameLen = MAX_SYM_NAME;

	DWORD64 offset = 0;
	if (SymFromAddrW(hProcess, dwAddress, &offset, dbgSymbol))
	{
		symbol.function.resize(dbgSymbol->NameLen);
		memcpy(&symbol.function[0], &dbgSymbol->Name[0], sizeof(WCHAR) * dbgSymbol->NameLen);
	}

	symbol.offset = static_cast<uintptr_t>(offset);
#endif

	return &symbol;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const std::vector<Module>& WinSymbolEngine::GetModules()
{
	if (modules.empty())
	{
		InitSystemModules();
		InitApplicationModules();
	}

	return SymbolEngine::GetModules();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// const char* USER_SYMBOL_SEARCH_PATH = "http://msdl.microsoft.com/download/symbols";
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WinSymbolEngine::Init()
{
	if (!isInitialized)
	{
#if USE_DBG_HELP
		previousOptions = SymGetOptions();

		memset(previousSearchPath, 0, MAX_SEARCH_PATH_LENGTH);
		SymGetSearchPath(hProcess, previousSearchPath, MAX_SEARCH_PATH_LENGTH);

		SymSetOptions(SymGetOptions() | SYMOPT_LOAD_LINES | SYMOPT_DEFERRED_LOADS | SYMOPT_UNDNAME | SYMOPT_INCLUDE_32BIT_MODULES | SYMOPT_LOAD_ANYTHING);
		if (!SymInitialize(hProcess, NULL, TRUE))
		{
			needRestorePreviousSettings = true;
			SymCleanup(hProcess);

			if (SymInitialize(hProcess, NULL, TRUE))
				isInitialized = true;
		}
		else
		{
			isInitialized = true;
		}

		const std::vector<Module>& loadedModules = GetModules();
			for each (const Module& module in loadedModules)
				SymLoadModule64(hProcess, NULL, module.path.c_str(), NULL, (DWORD64)module.address, (DWORD)module.size);
#else
		isInitialized = true;
#endif
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef DWORD(__stdcall *pZwQuerySystemInformation)(DWORD, LPVOID, DWORD, DWORD*);
#define SystemModuleInformation 11 // SYSTEMINFOCLASS
#define MAXIMUM_FILENAME_LENGTH 256

struct SYSTEM_MODULE_INFORMATION
{
	DWORD reserved1;
	DWORD reserved2;
	PVOID mappedBase;
	PVOID imageBase;
	DWORD imageSize;
	DWORD flags;
	WORD loadOrderIndex;
	WORD initOrderIndex;
	WORD loadCount;
	WORD moduleNameOffset;
	CHAR imageName[MAXIMUM_FILENAME_LENGTH];
};

#pragma warning (push)
#pragma warning(disable : 4200)
struct MODULE_LIST
{
	DWORD dwModules;
	SYSTEM_MODULE_INFORMATION pModulesInfo[];
};
#pragma warning (pop)

void WinSymbolEngine::InitSystemModules()
{
	ULONG returnLength = 0;
	ULONG systemInformationLength = 0;
	MODULE_LIST* pModuleList = nullptr;

#pragma warning (push)
#pragma warning(disable : 4191)
	pZwQuerySystemInformation ZwQuerySystemInformation = (pZwQuerySystemInformation)GetProcAddress(GetModuleHandle(TEXT("ntdll.dll")), "ZwQuerySystemInformation");
#pragma warning (pop)

	ZwQuerySystemInformation(SystemModuleInformation, pModuleList, systemInformationLength, &returnLength);
	systemInformationLength = returnLength;
	pModuleList = (MODULE_LIST*)Memory::Alloc(systemInformationLength);
	DWORD status = ZwQuerySystemInformation(SystemModuleInformation, pModuleList, systemInformationLength, &returnLength);
	if (status == ERROR_SUCCESS)
	{
		char systemRootPath[MAXIMUM_FILENAME_LENGTH] = { 0 };
#if BRO_PC
		ExpandEnvironmentStringsA("%SystemRoot%", systemRootPath, MAXIMUM_FILENAME_LENGTH);
#else
		strcpy_s(systemRootPath, "C:\\Windows");
#endif

		const char* systemRootPattern = "\\SystemRoot";

		modules.reserve(modules.size() + pModuleList->dwModules);

		for (uint32_t i = 0; i < pModuleList->dwModules; ++i)
		{
			SYSTEM_MODULE_INFORMATION& module = pModuleList->pModulesInfo[i];

			char path[MAXIMUM_FILENAME_LENGTH] = { 0 };

			if (strstr(module.imageName, systemRootPattern) == module.imageName)
			{
				strcpy_s(path, systemRootPath);
				strcat_s(path, module.imageName + strlen(systemRootPattern));
			}
			else
			{
				strcpy_s(path, module.imageName);
			}

			modules.push_back(Module(path, (void*)module.imageBase, module.imageSize));
		}
	}
	else
	{
		BRO_FAILED("Can't query System Module Information!");
	}

	if (pModuleList)
	{
		Memory::Free(pModuleList);
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WinSymbolEngine::InitApplicationModules()
{
	HANDLE processHandle = GetCurrentProcess();
	HMODULE hModules[256];
	DWORD modulesSize = 0;
	EnumProcessModulesEx(processHandle, hModules, sizeof(hModules), &modulesSize, 0);

	int moduleCount = modulesSize / sizeof(HMODULE);
	
	modules.reserve(modules.size() + moduleCount);

	for (int i = 0; i < moduleCount; ++i)
	{
		MODULEINFO info = { 0 };
		if (GetModuleInformation(processHandle, hModules[i], &info, sizeof(MODULEINFO)))
		{
			char name[MAX_PATH] = "UnknownModule";
			GetModuleFileNameExA(processHandle, hModules[i], name, MAX_PATH);

			modules.push_back(Module(name, info.lpBaseOfDll, info.SizeOfImage));
		}
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WinSymbolEngine::Close()
{
	if (isInitialized)
	{
#if USE_DBG_HELP
		SymCleanup(hProcess);
		if (needRestorePreviousSettings)
		{
			HANDLE currentProcess = GetCurrentProcess();

			SymSetOptions(previousOptions);
			SymSetSearchPath(currentProcess, previousSearchPath);
			SymInitialize(currentProcess, NULL, TRUE);

			needRestorePreviousSettings = false;
		}
#endif

		isInitialized = false;
	}
}


//////////////////////////////////////////////////////////////////////////
SymbolEngine* SymbolEngine::Get()
{
	static WinSymbolEngine pdbSymbolEngine;
	return &pdbSymbolEngine;
}

}
#endif //BRO_ENABLE_SYMENGINE
#endif //_WIN32