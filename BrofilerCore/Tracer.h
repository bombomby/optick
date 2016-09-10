#pragma once

#include "Types.h"
#include "MemoryPool.h"
#include "ETW.h"

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class ETW
{
#if USE_BROFILER_ETW
	EVENT_TRACE_PROPERTIES *sessionProperties;
	EVENT_TRACE_LOGFILE logFile;
	TRACEHANDLE sessionHandle;
	TRACEHANDLE openedHandle;

	HANDLE processThreadHandle;

	bool isActive;

	static DWORD WINAPI RunProcessTraceThreadFunction(LPVOID parameter);
#endif
public:
	enum Status
	{
		ETW_OK = 0,
		ETW_ERROR_ALREADY_EXISTS = 1,
		ETW_ERROR_ACCESS_DENIED = 2,
		ETW_FAILED = 3,
	};

	ETW();
	~ETW();

	Status Start();
	bool Stop();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
