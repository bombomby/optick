#pragma once

#ifdef _WIN32

#include "..\SchedulerTrace.h"
#include "ETW.h"

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class ETW : public SchedulerTrace
{
	EVENT_TRACE_PROPERTIES *traceProperties;
	EVENT_TRACE_LOGFILE logFile;
	TRACEHANDLE traceSessionHandle;
	TRACEHANDLE openedHandle;

	HANDLE processThreadHandle;

	bool isActive;

	static DWORD WINAPI RunProcessTraceThreadFunction(LPVOID parameter);

public:

	ETW();
	~ETW();

	virtual CaptureStatus::Type Start(int mode, const ThreadList& threads, bool autoAddUnknownThreads) override;
	virtual bool Stop();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

#endif
