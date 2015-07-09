#include "SymEngine.h"

#include <DbgHelp.h>
#pragma comment( lib, "DbgHelp.Lib" )

namespace Profiler
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
SymEngine::SymEngine() : isInitialized(false), hProcess(GetCurrentProcess()), needRestorePreviousSettings(false), previousOptions(0)
{
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
SymEngine::~SymEngine()
{
	Close();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const Symbol * const SymEngine::GetSymbol(DWORD64 dwAddress)
{
	if (dwAddress == 0)
		return nullptr;

	Symbol& symbol = cache[dwAddress];

	if (symbol.address != 0)
		return &symbol;

	if (!isInitialized)
		return nullptr;

	symbol.address = dwAddress;

	// Module Name
	IMAGEHLP_MODULEW64 moduleInfo;
	memset(&moduleInfo, 0, sizeof(IMAGEHLP_MODULEW64));
	moduleInfo.SizeOfStruct = sizeof(moduleInfo);
	if (SymGetModuleInfoW64(hProcess, dwAddress, &moduleInfo))
	{
		symbol.module = moduleInfo.ImageName;
	}


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
	if (SymFromAddrW(hProcess, dwAddress, &symbol.offset, dbgSymbol))
	{
		symbol.function.resize(dbgSymbol->NameLen);
		memcpy(&symbol.function[0], &dbgSymbol->Name[0], sizeof(WCHAR) * dbgSymbol->NameLen);
	}

	return &symbol;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// const char* USER_SYMBOL_SEARCH_PATH = "http://msdl.microsoft.com/download/symbols";
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void SymEngine::Init()
{
	if (!isInitialized)
	{
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
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void SymEngine::Close()
{
	if (isInitialized)
	{
		SymCleanup(hProcess);
		isInitialized = false;
	}

	if (needRestorePreviousSettings)
	{
		HANDLE currentProcess = GetCurrentProcess();

		SymSetOptions(previousOptions);
		SymSetSearchPath(currentProcess, previousSearchPath);
		SymInitialize(currentProcess, NULL, TRUE);

		needRestorePreviousSettings = false;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
uint SymEngine::GetCallstack(HANDLE hThread, CONTEXT& context, CallStackBuffer& callstack) 
{
	// We can't initialize dbghelp.dll here => http://microsoft.public.windbg.narkive.com/G2WkSt2k/stackwalk64-performance-problems
	// Otherwise it will be 5x times slower
	// Init();

	STACKFRAME64 stackFrame;
	memset(&stackFrame, 0, sizeof(STACKFRAME64));
	DWORD machineType;

	stackFrame.AddrPC.Mode = AddrModeFlat;
	stackFrame.AddrFrame.Mode = AddrModeFlat;
	stackFrame.AddrStack.Mode = AddrModeFlat;

#ifdef _M_IX86
	machineType = IMAGE_FILE_MACHINE_I386;
	stackFrame.AddrPC.Offset = context.Eip;
	stackFrame.AddrFrame.Offset = context.Ebp;
	stackFrame.AddrStack.Offset = context.Esp;
#elif _M_X64
	machineType = IMAGE_FILE_MACHINE_AMD64;
	stackFrame.AddrPC.Offset = context.Rip;
	stackFrame.AddrFrame.Offset = context.Rsp;
	stackFrame.AddrStack.Offset = context.Rsp;
#elif _M_IA64
	machineType = IMAGE_FILE_MACHINE_IA64;
	stackFrame.AddrPC.Offset = context.StIIP;
	stackFrame.AddrFrame.Offset = context.IntSp;
	stackFrame.AddrStack.Offset = context.IntSp;
	stackFrame.AddrBStore.Offset = context.RsBSP;
	stackFrame.AddrBStore.Mode = AddrModeFlat;
#else
#error "Platform not supported!"
#endif

	uint index = 0;
	while (	StackWalk64(machineType, GetCurrentProcess(), hThread, &stackFrame, &context, nullptr, &SymFunctionTableAccess64, &SymGetModuleBase64, nullptr) )
	{
		DWORD64 dwAddress = stackFrame.AddrPC.Offset;
		if (!dwAddress)
			break;

		if (index == callstack.size())
			return 0; // Too long callstack - possible error, let's skip it

		if (index > 0 && callstack[index - 1] == dwAddress)
			continue;

		callstack[index] = dwAddress;
		++index;
	}

	return index;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}