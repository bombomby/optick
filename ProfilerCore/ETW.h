#pragma once

#include "Types.h"
#include "MemoryPool.h"

#define INITGUID  // Causes definition of SystemTraceControlGuid in evntrace.h.
#include <windows.h>
#include <strsafe.h>
#include <wmistr.h>
#include <evntrace.h>
#include <evntcons.h>

namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class ETW
{
	EVENT_TRACE_PROPERTIES *sessionProperties;
	EVENT_TRACE_LOGFILE logFile;
	TRACEHANDLE sessionHandle;
	TRACEHANDLE openedHandle;

	HANDLE processThreadHandle;

	bool isActive;

	static DWORD WINAPI RunProcessTraceThreadFunction(LPVOID parameter);
public:
	ETW();
	~ETW();

	bool Start();
	bool Stop();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}