#pragma once

#ifdef _WIN32

#include "..\SchedulerTrace.h"
#include "ETW.h"

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class ETW : public SchedulerTrace
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

	virtual CaptureStatus::Type Start(int mode, const ThreadList& threads) override;
	virtual bool Stop();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

#endif
