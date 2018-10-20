#pragma once

#ifdef _WIN32

#include "../SchedulerTrace.h"
#include "ETW.h"

#include <unordered_map>

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
	DWORD currentProcessId;


	bool isActive;

	static DWORD WINAPI RunProcessTraceThreadFunction(LPVOID parameter);
	static void AdjustPrivileges();

	std::unordered_map<uint64_t, const EventDescription*> syscallDescriptions;

	void ResolveSysCalls();
public:

	ETW();
	~ETW();

	virtual CaptureStatus::Type Start(int mode, const ThreadList& threads, bool autoAddUnknownThreads) override;
	virtual bool Stop() override;

	DWORD GetProcessID() const { return currentProcessId; }
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

#endif