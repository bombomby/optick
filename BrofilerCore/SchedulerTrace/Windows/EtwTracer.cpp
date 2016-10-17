#ifdef _WIN32

#include <windows.h>
#include <vector>
#include <MTTypes.h>
#include "EtwTracer.h"
#include "../../Core.h"

namespace Brofiler
{

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const byte SWITCH_CONTEXT_INSTRUCTION_OPCODE = 36;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct CSwitch 
{
	// New thread ID after the switch.
	uint32 NewThreadId;

	// Previous thread ID.
	uint32 OldThreadId;

	// Thread priority of the new thread.
	int8  NewThreadPriority;

	// Thread priority of the previous thread.
	int8  OldThreadPriority;

	//The index of the C-state that was last used by the processor. A value of 0 represents the lightest idle state with higher values representing deeper C-states.
	uint8  PreviousCState;

	// Not used.
	int8  SpareByte;

	// Wait reason for the previous thread. The following are the possible values:
	//       0	Executive
	//       1	FreePage
	//       2	PageIn
	//       3	PoolAllocation
	//       4	DelayExecution
	//       5	Suspended
	//       6	UserRequest
	//       7	WrExecutive
	//       8	WrFreePage
	//       9	WrPageIn
	//       10	WrPoolAllocation
	//       11	WrDelayExecution
	//       12	WrSuspended
	//       13	WrUserRequest
	//       14	WrEventPair
	//       15	WrQueue
	//       16	WrLpcReceive
	//       17	WrLpcReply
	//       18	WrVirtualMemory
	//       19	WrPageOut
	//       20	WrRendezvous
	//       21	WrKeyedEvent
	//       22	WrTerminated
	//       23	WrProcessInSwap
	//       24	WrCpuRateControl
	//       25	WrCalloutStack
	//       26	WrKernel
	//       27	WrResource
	//       28	WrPushLock
	//       29	WrMutex
	//       30	WrQuantumEnd
	//       31	WrDispatchInt
	//       32	WrPreempted
	//       33	WrYieldExecution
	//       34	WrFastMutex
	//       35	WrGuardedMutex
	//       36	WrRundown
	//       37	MaximumWaitReason
	int8  OldThreadWaitReason;

	// Wait mode for the previous thread. The following are the possible values:
	//     0 KernelMode
	//     1 UserMode
	int8  OldThreadWaitMode;

	// State of the previous thread. The following are the possible state values:
	//     0 Initialized
	//     1 Ready
	//     2 Running
	//     3 Standby
	//     4 Terminated
	//     5 Waiting
	//     6 Transition
	//     7 DeferredReady (added for Windows Server 2003)
	int8  OldThreadState;

	// Ideal wait time of the previous thread.
	int8  OldThreadWaitIdealProcessor;

	// Wait time for the new thread.
	uint32 NewThreadWaitTime;

	// Reserved.
	uint32 Reserved;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WINAPI OnRecordEvent(PEVENT_RECORD eventRecord)
{
	if (eventRecord->EventHeader.EventDescriptor.Opcode != SWITCH_CONTEXT_INSTRUCTION_OPCODE)
	{
		return;
	}

	if (sizeof(CSwitch) != eventRecord->UserDataLength)
	{
		return;
	}

	CSwitch* pSwitchEvent = (CSwitch*)eventRecord->UserData;

	Brofiler::SwitchContextDesc desc;

	desc.reason = 0;
	desc.cpuId = eventRecord->BufferContext.ProcessorNumber;
	desc.oldThreadId = (uint64)pSwitchEvent->OldThreadId;
	desc.newThreadId = (uint64)pSwitchEvent->NewThreadId;
	desc.timestamp = eventRecord->EventHeader.TimeStamp.QuadPart;
	Core::Get().ReportSwitchContext(desc);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const TRACEHANDLE INVALID_TRACEHANDLE = (TRACEHANDLE)-1;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
DWORD WINAPI ETW::RunProcessTraceThreadFunction( LPVOID parameter )
{
	ETW* etw = (ETW*)parameter;
	ProcessTrace(&etw->openedHandle, 1, 0, 0);
	return 0;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

ETW::ETW() : isActive(false), sessionHandle(INVALID_TRACEHANDLE), openedHandle(INVALID_TRACEHANDLE), processThreadHandle(INVALID_HANDLE_VALUE)
{
	ULONG bufferSize = sizeof(EVENT_TRACE_PROPERTIES) + sizeof(KERNEL_LOGGER_NAME);
	sessionProperties =(EVENT_TRACE_PROPERTIES*) malloc(bufferSize);
}

SchedulerTraceStatus::Type ETW::Start()
{
	if (!isActive) 
	{
		ULONG bufferSize = sizeof(EVENT_TRACE_PROPERTIES) + sizeof(KERNEL_LOGGER_NAME);
		ZeroMemory(sessionProperties, bufferSize);
		sessionProperties->Wnode.BufferSize = bufferSize;
		sessionProperties->LoggerNameOffset = sizeof(EVENT_TRACE_PROPERTIES);

		sessionProperties->EnableFlags = EVENT_TRACE_FLAG_CSWITCH;
		sessionProperties->LogFileMode = EVENT_TRACE_REAL_TIME_MODE;

		sessionProperties->Wnode.Flags = WNODE_FLAG_TRACED_GUID;
		sessionProperties->Wnode.ClientContext = 1;
		sessionProperties->Wnode.Guid = SystemTraceControlGuid;

		// ERROR_BAD_LENGTH(24): The Wnode.BufferSize member of Properties specifies an incorrect size. Properties does not have sufficient space allocated to hold a copy of SessionName.
		// ERROR_ALREADY_EXISTS(183): A session with the same name or GUID is already running.
		// ERROR_ACCESS_DENIED(5): Only users with administrative privileges, users in the Performance Log Users group, and services running as LocalSystem, LocalService, NetworkService can control event tracing sessions.
		// ERROR_INVALID_PARAMETER(87)
		// ERROR_BAD_PATHNAME(161)
		// ERROR_DISK_FULL(112)
		int retryCount = 2;
		ULONG status = SchedulerTraceStatus::OK;

		while (--retryCount >= 0)
		{
			status = StartTrace(&sessionHandle, KERNEL_LOGGER_NAME, sessionProperties);

			switch (status)
			{
			case ERROR_ALREADY_EXISTS:
				ControlTrace(0, KERNEL_LOGGER_NAME, sessionProperties, EVENT_TRACE_CONTROL_STOP);
				break;

			case ERROR_ACCESS_DENIED:
				return SchedulerTraceStatus::ERR_ACCESS_DENIED;

			case ERROR_SUCCESS:
				retryCount = 0;
				break;

			default:
				return SchedulerTraceStatus::FAILED;
			}
		}

		if (status != ERROR_SUCCESS)
			return SchedulerTraceStatus::FAILED;

		ZeroMemory(&logFile, sizeof(EVENT_TRACE_LOGFILE));

		logFile.LoggerName = KERNEL_LOGGER_NAME;
		logFile.ProcessTraceMode = (PROCESS_TRACE_MODE_REAL_TIME | PROCESS_TRACE_MODE_EVENT_RECORD | PROCESS_TRACE_MODE_RAW_TIMESTAMP);
		logFile.EventRecordCallback = OnRecordEvent;

		openedHandle = OpenTrace(&logFile);
		if (openedHandle == INVALID_TRACEHANDLE)
			return SchedulerTraceStatus::FAILED;

		DWORD threadID;
		processThreadHandle = CreateThread(0, 0, RunProcessTraceThreadFunction, this, 0, &threadID);
		
		isActive = true;
	}

	return SchedulerTraceStatus::OK;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool ETW::Stop()
{
	if (isActive)
	{
		ULONG controlTraceResult = ControlTrace(openedHandle, KERNEL_LOGGER_NAME, sessionProperties, EVENT_TRACE_CONTROL_STOP);

		// ERROR_CTX_CLOSE_PENDING(7007L): The call was successful. The ProcessTrace function will stop after it has processed all real-time events in its buffers (it will not receive any new events).
		// ERROR_BUSY(170L): Prior to Windows Vista, you cannot close the trace until the ProcessTrace function completes.
		// ERROR_INVALID_HANDLE(6L): One of the following is true: TraceHandle is NULL. TraceHandle is INVALID_HANDLE_VALUE.
		ULONG closeTraceStatus = CloseTrace(openedHandle);

		// Wait for ProcessThread to finish
		WaitForSingleObject(processThreadHandle, INFINITE);
		BOOL wasThreadClosed = CloseHandle(processThreadHandle);

		isActive = false;

		return wasThreadClosed && closeTraceStatus == ERROR_SUCCESS && controlTraceResult == ERROR_SUCCESS;
	}

	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
ETW::~ETW()
{
	Stop();
	delete sessionProperties;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


ISchedulerTracer* ISchedulerTracer::Get()
{
	static ETW etwTracer;
	return &etwTracer;
}

}

#endif

