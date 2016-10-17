#pragma once

#ifdef _WIN32

#include "..\ISchedulerTrace.h"
#include "ETW.h"

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class ETW : public ISchedulerTracer
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

	virtual SchedulerTraceStatus::Type Start();
	virtual bool Stop();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

#endif
