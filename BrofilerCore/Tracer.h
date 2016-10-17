#pragma once

#include "Brofiler.h"
#include "EtwStatus.h"

#if USE_BROFILER_ETW

#include "MemoryPool.h"
#include "ETW.h"

namespace Brofiler
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

	EtwStatus Start();
	bool Stop();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

#endif
