#ifdef _WIN32

#include <windows.h>
#include <vector>
#include <MTTypes.h>
#include "EtwTracer.h"
#include "../../Core.h"

/*
Event Tracing Functions - API
https://msdn.microsoft.com/en-us/library/windows/desktop/aa363795(v=vs.85).aspx
*/

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
	uint64 Stack0;

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
struct SysCallEnter
{
	uintptr_t SysCallAddress;

	static const byte OPCODE = 51;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct SysCallExit 
{
	uint32 SysCallNtStatus;

	static const byte OPCODE = 52;
};


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// ce1dbfb4-137e-4da6-87b0-3f59aa102cbc 
DEFINE_GUID(SampledProfileGuid, 0xce1dbfb4, 0x137e, 0x4da6, 0x87, 0xb0, 0x3f, 0x59, 0xaa, 0x10, 0x2c, 0xbc);

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// 3d6fa8d1-fe05-11d0-9dda-00c04fd7ba7c
DEFINE_GUID(CSwitchProfileGuid, 0x3d6fa8d1, 0xfe05, 0x11d0, 0x9d, 0xda, 0x00, 0xc0, 0x4f, 0xd7, 0xba, 0x7c);


static DWORD currentProcessId = 0;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WINAPI OnRecordEvent(PEVENT_RECORD eventRecord)
{
	//static uint8 cpuCoreIsExecutingThreadFromOurProcess[256] = { 0 };

	const byte opcode = eventRecord->EventHeader.EventDescriptor.Opcode;

	if (opcode == CSwitch::OPCODE)
	{
		if (sizeof(CSwitch) == eventRecord->UserDataLength)
		{
			CSwitch* pSwitchEvent = (CSwitch*)eventRecord->UserData;

			Brofiler::SwitchContextDesc desc;
			desc.reason = pSwitchEvent->OldThreadWaitReason;
			desc.cpuId = eventRecord->BufferContext.ProcessorNumber;
			desc.oldThreadId = (uint64)pSwitchEvent->OldThreadId;
			desc.newThreadId = (uint64)pSwitchEvent->NewThreadId;
			desc.timestamp = eventRecord->EventHeader.TimeStamp.QuadPart;
			Core::Get().ReportSwitchContext(desc);
		}
	}
	else if (opcode == StackWalk_Event::OPCODE)
	{
		if (eventRecord->UserData && eventRecord->UserDataLength >= sizeof(StackWalk_Event))
		{
			//TODO: Support x86 windows kernels
			const size_t osKernelPtrSize = sizeof(uint64);

			StackWalk_Event* pStackWalkEvent = (StackWalk_Event*)eventRecord->UserData;
			uint32 count = 1 + (eventRecord->UserDataLength - sizeof(StackWalk_Event)) / osKernelPtrSize;

			if (count && pStackWalkEvent->StackThread != 0)
			{
				if (pStackWalkEvent->StackProcess == currentProcessId)
				{
					CallstackDesc desc;
					desc.threadID = pStackWalkEvent->StackThread;
					desc.timestamp = pStackWalkEvent->EventTimeStamp;

					static_assert(osKernelPtrSize == sizeof(uint64), "Incompatible types!");
					desc.callstack = &pStackWalkEvent->Stack0;

					desc.count = (uint8)count;
					Core::Get().ReportStackWalk(desc);
				}
			}
		}
	}
	else if (opcode == SampledProfile::OPCODE)
	{
		SampledProfile* pEvent = (SampledProfile*)eventRecord->UserData;
		BRO_UNUSED(pEvent);
	} 
	else if (opcode == SysCallEnter::OPCODE)
	{
		if (eventRecord->UserDataLength >= sizeof(SysCallEnter))
		{
			//uint8 cpuId = eventRecord->BufferContext.ProcessorNumber;

			// report event, but only if our process working on this physical core
			// if (cpuCoreIsExecutingThreadFromOurProcess[cpuId])
			{
				SysCallEnter* pEventEnter = (SysCallEnter*)eventRecord->UserData;

				SysCallDesc desc;
				desc.timestamp = eventRecord->EventHeader.TimeStamp.QuadPart;
				desc.id = pEventEnter->SysCallAddress;
				Core::Get().ReportSysCall(desc);
			}
		}
	} 
	else if (opcode == SysCallExit::OPCODE)
	{
		SysCallExit* pEventExit = (SysCallExit*)eventRecord->UserData;
		BRO_UNUSED(pEventExit);
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
void ETW::AdjustPrivileges()
{
	HANDLE token = 0;
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
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

ETW::ETW() 
	: isActive(false)
	, traceSessionHandle(INVALID_TRACEHANDLE)
	, openedHandle(INVALID_TRACEHANDLE)
	, processThreadHandle(INVALID_HANDLE_VALUE)
{
	ULONG bufferSize = sizeof(EVENT_TRACE_PROPERTIES) + sizeof(KERNEL_LOGGER_NAME);
	traceProperties = (EVENT_TRACE_PROPERTIES*)malloc(bufferSize);

	currentProcessId = GetCurrentProcessId();
}

CaptureStatus::Type ETW::Start(int mode, const ThreadList& threads, bool autoAddUnknownThreads)
{
	if (!isActive) 
	{
		AdjustPrivileges();

		CaptureStatus::Type res = SchedulerTrace::Start(mode, threads, autoAddUnknownThreads);
		if (res != CaptureStatus::OK)
		{
			OutputDebugStringA("SchedulerTrace::Start - failed\n");
			return res;
		}

		ULONG bufferSize = sizeof(EVENT_TRACE_PROPERTIES) + sizeof(KERNEL_LOGGER_NAME);
		ZeroMemory(traceProperties, bufferSize);
		traceProperties->Wnode.BufferSize = bufferSize;
		traceProperties->LoggerNameOffset = sizeof(EVENT_TRACE_PROPERTIES);
		traceProperties->EnableFlags = 0;

		if (mode & SWITCH_CONTEXTS)
		{
			traceProperties->EnableFlags |= EVENT_TRACE_FLAG_CSWITCH;
		}

		if (mode & STACK_WALK)
		{
			traceProperties->EnableFlags |= EVENT_TRACE_FLAG_PROFILE;
		}

		if (mode & SYS_CALLS)
		{
			traceProperties->EnableFlags |= EVENT_TRACE_FLAG_SYSTEMCALL;
		}

		traceProperties->LogFileMode = EVENT_TRACE_REAL_TIME_MODE;
		traceProperties->Wnode.Flags = WNODE_FLAG_TRACED_GUID;
		//
		// https://msdn.microsoft.com/en-us/library/windows/desktop/aa364160(v=vs.85).aspx
		// Clock resolution = QPC
		traceProperties->Wnode.ClientContext = 1;
		traceProperties->Wnode.Guid = SystemTraceControlGuid;

		// ERROR_BAD_LENGTH(24): The Wnode.BufferSize member of Properties specifies an incorrect size. Properties does not have sufficient space allocated to hold a copy of SessionName.
		// ERROR_ALREADY_EXISTS(183): A session with the same name or GUID is already running.
		// ERROR_ACCESS_DENIED(5): Only users with administrative privileges, users in the Performance Log Users group, and services running as LocalSystem, LocalService, NetworkService can control event tracing sessions.
		// ERROR_INVALID_PARAMETER(87)
		// ERROR_BAD_PATHNAME(161)
		// ERROR_DISK_FULL(112)
		// ERROR_NO_SUCH_PRIVILEGE(1313)
		int retryCount = 4;
		ULONG status = CaptureStatus::OK;

		while (--retryCount >= 0)
		{
			status = StartTrace(&traceSessionHandle, KERNEL_LOGGER_NAME, traceProperties);

			switch (status)
			{
			case ERROR_NO_SUCH_PRIVILEGE:
				AdjustPrivileges();
				break;

			case ERROR_ALREADY_EXISTS:
				ControlTrace(0, KERNEL_LOGGER_NAME, traceProperties, EVENT_TRACE_CONTROL_STOP);
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
		{
			OutputDebugStringA("StartTrace - failed\n");
			return CaptureStatus::FAILED;
		}

		CLASSIC_EVENT_ID callstackSamples[4];
		int callstackCountSamplesCount = 0;

		if (mode & STACK_WALK)
		{
			callstackSamples[callstackCountSamplesCount].EventGuid = SampledProfileGuid;
			callstackSamples[callstackCountSamplesCount].Type = SampledProfile::OPCODE;
			++callstackCountSamplesCount;
		}

		if (mode & SYS_CALLS)
		{
			callstackSamples[callstackCountSamplesCount].EventGuid = SampledProfileGuid;
			callstackSamples[callstackCountSamplesCount].Type = SysCallEnter::OPCODE;
			++callstackCountSamplesCount;
		}

/*
			callstackSamples[callstackCountSamplesCount].EventGuid = CSwitchProfileGuid;
			callstackSamples[callstackCountSamplesCount].Type = CSwitch::OPCODE;
			++callstackCountSamplesCount;
*/


/*		
		https://msdn.microsoft.com/en-us/library/windows/desktop/dd392328%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
		Typically, on 64-bit computers, you cannot capture the kernel stack in certain contexts when page faults are not allowed. To enable walking the kernel stack on x64, set
		the DisablePagingExecutive Memory Management registry value to 1. The DisablePagingExecutive registry value is located under the following registry key:
		HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager\Memory Management
*/
		if (callstackCountSamplesCount > 0)
		{
			status = TraceSetInformation(traceSessionHandle, TraceStackTracingInfo, &callstackSamples[0], sizeof(CLASSIC_EVENT_ID) * callstackCountSamplesCount);
			if (status != ERROR_SUCCESS)
			{
				OutputDebugStringA("TraceSetInformation - failed\n");
				return CaptureStatus::FAILED;
			}
		}

		bool highFrequencySampling = false;
		if (highFrequencySampling)
		{
			TRACE_PROFILE_INTERVAL itnerval = { 0 };
			memset(&itnerval, 0, sizeof(TRACE_PROFILE_INTERVAL));
			itnerval.Interval = 1221;
			status = TraceSetInformation(traceSessionHandle, TraceSampledProfileIntervalInfo, &itnerval, sizeof(TRACE_PROFILE_INTERVAL));
			if (status != ERROR_SUCCESS)
			{
				OutputDebugStringA("TraceSetInformation - failed\n");
			}
		}

		ZeroMemory(&logFile, sizeof(EVENT_TRACE_LOGFILE));
		logFile.LoggerName = KERNEL_LOGGER_NAME;
		logFile.ProcessTraceMode = (PROCESS_TRACE_MODE_REAL_TIME | PROCESS_TRACE_MODE_EVENT_RECORD | PROCESS_TRACE_MODE_RAW_TIMESTAMP);
		logFile.EventRecordCallback = OnRecordEvent;
		logFile.BufferCallback = OnBufferRecord;
		openedHandle = OpenTrace(&logFile);
		if (openedHandle == INVALID_TRACEHANDLE)
		{
			OutputDebugStringA("OpenTrace - failed\n");
			return CaptureStatus::FAILED;
		}

		DWORD threadID;
		processThreadHandle = CreateThread(0, 0, RunProcessTraceThreadFunction, this, 0, &threadID);
		
		isActive = true;

		OutputDebugStringA("Start - done\n");
	}

	return CaptureStatus::OK;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool ETW::Stop()
{
	if (!isActive)
	{
		return false;
	}

	OutputDebugStringA("Stop\n");

	ULONG controlTraceResult = ControlTrace(openedHandle, KERNEL_LOGGER_NAME, traceProperties, EVENT_TRACE_CONTROL_STOP);

	// ERROR_CTX_CLOSE_PENDING(7007L): The call was successful. The ProcessTrace function will stop after it has processed all real-time events in its buffers (it will not receive any new events).
	// ERROR_BUSY(170L): Prior to Windows Vista, you cannot close the trace until the ProcessTrace function completes.
	// ERROR_INVALID_HANDLE(6L): One of the following is true: TraceHandle is NULL. TraceHandle is INVALID_HANDLE_VALUE.
	ULONG closeTraceStatus = CloseTrace(openedHandle);

	// Wait for ProcessThread to finish
	WaitForSingleObject(processThreadHandle, INFINITE);
	BOOL wasThreadClosed = CloseHandle(processThreadHandle);

	isActive = false;

	if (!SchedulerTrace::Stop())
	{
		return false;
	}

	return wasThreadClosed && (closeTraceStatus == ERROR_SUCCESS) && (controlTraceResult == ERROR_SUCCESS);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
ETW::~ETW()
{
	Stop();
	free (traceProperties);
	traceProperties = nullptr;
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