#ifdef _WIN32

#include <windows.h>
#include <vector>
#include <MTTypes.h>
#include "EtwTracer.h"
#include "../../Core.h"

namespace Brofiler
{
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

	static const byte OPCODE = 36;
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct StackWalk_Event
{
	// Original event time stamp from the event header
	uint64 EventTimeStamp;

	// The process identifier of the original event
	uint32 StackProcess;
	
	// The thread identifier of the original event
	uint32 StackThread;
	
	// Callstack head
	size_t Stack0;

	static const byte OPCODE = 32;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct SampledProfile
{
	uint32 InstructionPointer;
	uint32 ThreadId;
	uint32 Count;

	static const byte OPCODE = 46;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// ce1dbfb4-137e-4da6-87b0-3f59aa102cbc 
DEFINE_GUID(SampledProfileGuid, 0xce1dbfb4, 0x137e, 0x4da6, 0x87, 0xb0, 0x3f, 0x59, 0xaa, 0x10, 0x2c, 0xbc);
const uint8 SAMPLED_PROFILE_OPCODE = 46;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WINAPI OnRecordEvent(PEVENT_RECORD eventRecord)
{
	const byte opcode = eventRecord->EventHeader.EventDescriptor.Opcode;

	if (opcode == CSwitch::OPCODE)
	{
		if (sizeof(CSwitch) != eventRecord->UserDataLength)
			return;

		CSwitch* pSwitchEvent = (CSwitch*)eventRecord->UserData;

		Brofiler::SwitchContextDesc desc;

		desc.reason = pSwitchEvent->OldThreadWaitReason;
		desc.cpuId = eventRecord->BufferContext.ProcessorNumber;
		desc.oldThreadId = (uint64)pSwitchEvent->OldThreadId;
		desc.newThreadId = (uint64)pSwitchEvent->NewThreadId;
		desc.timestamp = eventRecord->EventHeader.TimeStamp.QuadPart;
		Core::Get().ReportSwitchContext(desc);
	}
	else if (opcode == StackWalk_Event::OPCODE)
	{
		if (eventRecord->UserData && eventRecord->UserDataLength > 0)
		{
			StackWalk_Event* pStackWalkEvent = (StackWalk_Event*)eventRecord->UserData;
			uint32 count = 1 + (eventRecord->UserDataLength - sizeof(StackWalk_Event)) / sizeof(size_t);

			if (count && pStackWalkEvent->StackThread != 0)
			{
				CallstackDesc desc;
				desc.threadID = pStackWalkEvent->StackThread;
				desc.timestamp = pStackWalkEvent->EventTimeStamp;
				desc.callstack = &pStackWalkEvent->Stack0;
				desc.count = (uint8)count;
				Core::Get().ReportStackWalk(desc);
			}
		}
	}
	else if (opcode == SampledProfile::OPCODE)
	{
		SampledProfile* pEvent = (SampledProfile*)eventRecord->UserData;
		(void)pEvent->InstructionPointer;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
static ULONG WINAPI OnBufferRecord(_In_ PEVENT_TRACE_LOGFILE Buffer)
{
	BRO_UNUSED(Buffer);
	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const TRACEHANDLE INVALID_TRACEHANDLE = (TRACEHANDLE)-1;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
DWORD WINAPI ETW::RunProcessTraceThreadFunction( LPVOID parameter )
{
	ETW* etw = (ETW*)parameter;
	ULONG status = ProcessTrace(&etw->openedHandle, 1, 0, 0);
	BRO_UNUSED(status);
	return 0;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

ETW::ETW() : isActive(false), sessionHandle(INVALID_TRACEHANDLE), openedHandle(INVALID_TRACEHANDLE), processThreadHandle(INVALID_HANDLE_VALUE)
{
	ULONG bufferSize = sizeof(EVENT_TRACE_PROPERTIES) + sizeof(KERNEL_LOGGER_NAME);
	sessionProperties =(EVENT_TRACE_PROPERTIES*) malloc(bufferSize);
}

CaptureStatus::Type ETW::Start(int mode, const ThreadList& threads)
{
	if (!isActive) 
	{
		CaptureStatus::Type res = SchedulerTrace::Start(mode, threads);
		if (res != CaptureStatus::OK)
			return res;

		ULONG bufferSize = sizeof(EVENT_TRACE_PROPERTIES) + sizeof(KERNEL_LOGGER_NAME);
		ZeroMemory(sessionProperties, bufferSize);
		sessionProperties->Wnode.BufferSize = bufferSize;
		sessionProperties->LoggerNameOffset = sizeof(EVENT_TRACE_PROPERTIES);

		sessionProperties->EnableFlags = 0;

		if (mode & SWITCH_CONTEXTS)
			sessionProperties->EnableFlags |= EVENT_TRACE_FLAG_CSWITCH;

		if (mode & STACK_WALK)
			sessionProperties->EnableFlags |= EVENT_TRACE_FLAG_PROFILE;

		sessionProperties->EnableFlags |= EVENT_TRACE_FLAG_FILE_IO;

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
		// ERROR_NO_SUCH_PRIVILEGE(1313)
		int retryCount = 4;
		ULONG status = CaptureStatus::OK;
		HANDLE token = 0;

		while (--retryCount >= 0)
		{
			CLASSIC_EVENT_ID sampleEventID;
			sampleEventID.EventGuid = SampledProfileGuid;
			sampleEventID.Type = SampledProfile::OPCODE;

			status = StartTrace(&sessionHandle, KERNEL_LOGGER_NAME, sessionProperties);

			switch (status)
			{
			case ERROR_NO_SUCH_PRIVILEGE:
				if (OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &token))
				{
					TOKEN_PRIVILEGES tokenPrivileges;
					memset(&tokenPrivileges, 0, sizeof(tokenPrivileges));
					tokenPrivileges.PrivilegeCount = 1;
					tokenPrivileges.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
					LookupPrivilegeValue(NULL, SE_SYSTEM_PROFILE_NAME, &tokenPrivileges.Privileges[0].Luid);

					AdjustTokenPrivileges(token, FALSE, &tokenPrivileges, 0, (PTOKEN_PRIVILEGES)NULL, 0);
					CloseHandle(token);
				}
				break;

			case ERROR_ALREADY_EXISTS:
				ControlTrace(0, KERNEL_LOGGER_NAME, sessionProperties, EVENT_TRACE_CONTROL_STOP);
				break;

			case ERROR_ACCESS_DENIED:
				return CaptureStatus::ERR_TRACER_ACCESS_DENIED;

			case ERROR_SUCCESS:
				retryCount = 0;
				break;

			default:
				return CaptureStatus::FAILED;
			}
		}

		if (status != ERROR_SUCCESS)
			return CaptureStatus::FAILED;

		ZeroMemory(&logFile, sizeof(EVENT_TRACE_LOGFILE));

		logFile.LoggerName = KERNEL_LOGGER_NAME;
		logFile.ProcessTraceMode = (PROCESS_TRACE_MODE_REAL_TIME | PROCESS_TRACE_MODE_EVENT_RECORD | PROCESS_TRACE_MODE_RAW_TIMESTAMP);
		logFile.EventRecordCallback = OnRecordEvent;
		logFile.BufferCallback = OnBufferRecord;

		if (mode & STACK_WALK)
		{
			CLASSIC_EVENT_ID sampleEventID;
			sampleEventID.EventGuid = SampledProfileGuid;
			sampleEventID.Type = SAMPLED_PROFILE_OPCODE;

			status = TraceSetInformation(sessionHandle, TraceStackTracingInfo, &sampleEventID, sizeof(CLASSIC_EVENT_ID));
			if (status != ERROR_SUCCESS)
				return CaptureStatus::FAILED;
		}

		openedHandle = OpenTrace(&logFile);
		if (openedHandle == INVALID_TRACEHANDLE)
			return CaptureStatus::FAILED;

		DWORD threadID;
		processThreadHandle = CreateThread(0, 0, RunProcessTraceThreadFunction, this, 0, &threadID);
		
		isActive = true;
	}

	return CaptureStatus::OK;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool ETW::Stop()
{
	if (!SchedulerTrace::Stop())
		return false;

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
SchedulerTrace* SchedulerTrace::Get()
{
	static ETW etwTracer;
	return &etwTracer;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

#endif

