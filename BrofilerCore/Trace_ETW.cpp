#if _WIN32

#include "Trace.h"

#if BRO_ENABLE_TRACING

#include <array>
#include <vector>
#include "Core.h"
#include "SymbolEngine.h"

#include <psapi.h>

/*
Event Tracing Functions - API
https://msdn.microsoft.com/en-us/library/windows/desktop/aa363795(v=vs.85).aspx
*/

#define DECLARE_ETW (!WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP))

#if DECLARE_ETW
// Copied from Windows SDK
#ifndef WMIAPI
#ifndef MIDL_PASS
#ifdef _WMI_SOURCE_
#define WMIAPI __stdcall
#else
#define WMIAPI DECLSPEC_IMPORT __stdcall
#endif // _WMI_SOURCE
#endif // MIDL_PASS
#endif // WMIAPI
#include <guiddef.h>
#if defined(_NTDDK_) || defined(_NTIFS_) || defined(_WMIKM_)
#define _EVNTRACE_KERNEL_MODE
#endif
#if !defined(_EVNTRACE_KERNEL_MODE)
#include <wmistr.h>
#endif

#if _MSC_VER <= 1600
#define EVENT_DESCRIPTOR_DEF
#define EVENT_HEADER_DEF
#define EVENT_HEADER_EXTENDED_DATA_ITEM_DEF
#define EVENT_RECORD_DEF
#endif

#ifndef _TRACEHANDLE_DEFINED
#define _TRACEHANDLE_DEFINED
typedef ULONG64 TRACEHANDLE, *PTRACEHANDLE;
#endif

//
// EventTraceGuid is used to identify a event tracing session
//
DEFINE_GUID( /* 68fdd900-4a3e-11d1-84f4-0000f80464e3 */
	EventTraceGuid,
	0x68fdd900,
	0x4a3e,
	0x11d1,
	0x84, 0xf4, 0x00, 0x00, 0xf8, 0x04, 0x64, 0xe3
);

//
// SystemTraceControlGuid. Used to specify event tracing for kernel
//
DEFINE_GUID( /* 9e814aad-3204-11d2-9a82-006008a86939 */
	SystemTraceControlGuid,
	0x9e814aad,
	0x3204,
	0x11d2,
	0x9a, 0x82, 0x00, 0x60, 0x08, 0xa8, 0x69, 0x39
);

//
// EventTraceConfigGuid. Used to report system configuration records
//
DEFINE_GUID( /* 01853a65-418f-4f36-aefc-dc0f1d2fd235 */
	EventTraceConfigGuid,
	0x01853a65,
	0x418f,
	0x4f36,
	0xae, 0xfc, 0xdc, 0x0f, 0x1d, 0x2f, 0xd2, 0x35
);

//
// DefaultTraceSecurityGuid. Specifies the default event tracing security
//
DEFINE_GUID( /* 0811c1af-7a07-4a06-82ed-869455cdf713 */
	DefaultTraceSecurityGuid,
	0x0811c1af,
	0x7a07,
	0x4a06,
	0x82, 0xed, 0x86, 0x94, 0x55, 0xcd, 0xf7, 0x13
);


///////////////////////////////////////////////////////////////////////////////
#define PROCESS_TRACE_MODE_REAL_TIME                0x00000100
#define PROCESS_TRACE_MODE_RAW_TIMESTAMP            0x00001000
#define PROCESS_TRACE_MODE_EVENT_RECORD             0x10000000
///////////////////////////////////////////////////////////////////////////////
#define EVENT_HEADER_FLAG_EXTENDED_INFO				0x0001
#define EVENT_HEADER_FLAG_PRIVATE_SESSION			0x0002
#define EVENT_HEADER_FLAG_STRING_ONLY				0x0004
#define EVENT_HEADER_FLAG_TRACE_MESSAGE				0x0008
#define EVENT_HEADER_FLAG_NO_CPUTIME				0x0010
#define EVENT_HEADER_FLAG_32_BIT_HEADER				0x0020
#define EVENT_HEADER_FLAG_64_BIT_HEADER				0x0040
#define EVENT_HEADER_FLAG_CLASSIC_HEADER			0x0100
#define EVENT_HEADER_FLAG_PROCESSOR_INDEX			0x0200
///////////////////////////////////////////////////////////////////////////////
#define KERNEL_LOGGER_NAMEW							L"NT Kernel Logger"
///////////////////////////////////////////////////////////////////////////////
#define EVENT_TRACE_REAL_TIME_MODE          0x00000100  // Real time mode on
///////////////////////////////////////////////////////////////////////////////
#define EVENT_TRACE_CONTROL_STOP            1
///////////////////////////////////////////////////////////////////////////////

//
// Enable flags for Kernel Events
//
#define EVENT_TRACE_FLAG_PROCESS            0x00000001  // process start & end
#define EVENT_TRACE_FLAG_THREAD             0x00000002  // thread start & end
#define EVENT_TRACE_FLAG_IMAGE_LOAD         0x00000004  // image load

#define EVENT_TRACE_FLAG_DISK_IO            0x00000100  // physical disk IO
#define EVENT_TRACE_FLAG_DISK_FILE_IO       0x00000200  // requires disk IO

#define EVENT_TRACE_FLAG_MEMORY_PAGE_FAULTS 0x00001000  // all page faults
#define EVENT_TRACE_FLAG_MEMORY_HARD_FAULTS 0x00002000  // hard faults only

#define EVENT_TRACE_FLAG_NETWORK_TCPIP      0x00010000  // tcpip send & receive

#define EVENT_TRACE_FLAG_REGISTRY           0x00020000  // registry calls
#define EVENT_TRACE_FLAG_DBGPRINT           0x00040000  // DbgPrint(ex) Calls

//
// Enable flags for Kernel Events on Vista and above
//
#define EVENT_TRACE_FLAG_PROCESS_COUNTERS   0x00000008  // process perf counters
#define EVENT_TRACE_FLAG_CSWITCH            0x00000010  // context switches
#define EVENT_TRACE_FLAG_DPC                0x00000020  // deffered procedure calls
#define EVENT_TRACE_FLAG_INTERRUPT          0x00000040  // interrupts
#define EVENT_TRACE_FLAG_SYSTEMCALL         0x00000080  // system calls

#define EVENT_TRACE_FLAG_DISK_IO_INIT       0x00000400  // physical disk IO initiation
#define EVENT_TRACE_FLAG_ALPC               0x00100000  // ALPC traces
#define EVENT_TRACE_FLAG_SPLIT_IO           0x00200000  // split io traces (VolumeManager)

#define EVENT_TRACE_FLAG_DRIVER             0x00800000  // driver delays
#define EVENT_TRACE_FLAG_PROFILE            0x01000000  // sample based profiling
#define EVENT_TRACE_FLAG_FILE_IO            0x02000000  // file IO
#define EVENT_TRACE_FLAG_FILE_IO_INIT       0x04000000  // file IO initiation

#define EVENT_TRACE_FLAG_PMC_PROFILE		0x80000000	// sample based profiling (PMC) - NOT CONFIRMED!

//
// Enable flags for Kernel Events on Win7 and above
//
#define EVENT_TRACE_FLAG_DISPATCHER         0x00000800  // scheduler (ReadyThread)
#define EVENT_TRACE_FLAG_VIRTUAL_ALLOC      0x00004000  // VM operations

//
// Enable flags for Kernel Events on Win8 and above
//
#define EVENT_TRACE_FLAG_VAMAP              0x00008000  // map/unmap (excluding images)
#define EVENT_TRACE_FLAG_NO_SYSCONFIG       0x10000000  // Do not do sys config rundown

///////////////////////////////////////////////////////////////////////////////

#pragma warning(push)
#pragma warning (disable:4201) 

#ifndef EVENT_DESCRIPTOR_DEF
#define EVENT_DESCRIPTOR_DEF
typedef struct _EVENT_DESCRIPTOR {

	USHORT      Id;
	UCHAR       Version;
	UCHAR       Channel;
	UCHAR       Level;
	UCHAR       Opcode;
	USHORT      Task;
	ULONGLONG   Keyword;

} EVENT_DESCRIPTOR, *PEVENT_DESCRIPTOR;
typedef const EVENT_DESCRIPTOR *PCEVENT_DESCRIPTOR;
#endif
///////////////////////////////////////////////////////////////////////////////
#ifndef EVENT_HEADER_DEF
#define EVENT_HEADER_DEF
typedef struct _EVENT_HEADER {

	USHORT              Size;
	USHORT              HeaderType;
	USHORT              Flags;
	USHORT              EventProperty;
	ULONG               ThreadId;
	ULONG               ProcessId;
	LARGE_INTEGER       TimeStamp;
	GUID                ProviderId;
	EVENT_DESCRIPTOR    EventDescriptor;
	union {
		struct {
			ULONG       KernelTime;
			ULONG       UserTime;
		} DUMMYSTRUCTNAME;
		ULONG64         ProcessorTime;

	} DUMMYUNIONNAME;
	GUID                ActivityId;

} EVENT_HEADER, *PEVENT_HEADER;
#endif
///////////////////////////////////////////////////////////////////////////////
#ifndef EVENT_HEADER_EXTENDED_DATA_ITEM_DEF
#define EVENT_HEADER_EXTENDED_DATA_ITEM_DEF
typedef struct _EVENT_HEADER_EXTENDED_DATA_ITEM {

	USHORT      Reserved1;                      // Reserved for internal use
	USHORT      ExtType;                        // Extended info type 
	struct {
		USHORT  Linkage : 1;       // Indicates additional extended 
								   // data item
		USHORT  Reserved2 : 15;
	};
	USHORT      DataSize;                       // Size of extended info data
	ULONGLONG   DataPtr;                        // Pointer to extended info data

} EVENT_HEADER_EXTENDED_DATA_ITEM, *PEVENT_HEADER_EXTENDED_DATA_ITEM;
#endif
///////////////////////////////////////////////////////////////////////////////
#ifndef ETW_BUFFER_CONTEXT_DEF
#define ETW_BUFFER_CONTEXT_DEF
typedef struct _ETW_BUFFER_CONTEXT {
	union {
		struct {
			UCHAR ProcessorNumber;
			UCHAR Alignment;
		} DUMMYSTRUCTNAME;
		USHORT ProcessorIndex;
	} DUMMYUNIONNAME;
	USHORT  LoggerId;
} ETW_BUFFER_CONTEXT, *PETW_BUFFER_CONTEXT;
#endif
///////////////////////////////////////////////////////////////////////////////
#ifndef EVENT_RECORD_DEF
#define EVENT_RECORD_DEF
typedef struct _EVENT_RECORD {
	EVENT_HEADER        EventHeader;
	ETW_BUFFER_CONTEXT  BufferContext;
	USHORT              ExtendedDataCount;

	USHORT              UserDataLength;
	PEVENT_HEADER_EXTENDED_DATA_ITEM ExtendedData;
	PVOID               UserData;
	PVOID               UserContext;
} EVENT_RECORD, *PEVENT_RECORD;
#endif
///////////////////////////////////////////////////////////////////////////////
typedef struct _EVENT_TRACE_PROPERTIES {
	WNODE_HEADER Wnode;
	//
	// data provided by caller
	ULONG BufferSize;                   // buffer size for logging (kbytes)
	ULONG MinimumBuffers;               // minimum to preallocate
	ULONG MaximumBuffers;               // maximum buffers allowed
	ULONG MaximumFileSize;              // maximum logfile size (in MBytes)
	ULONG LogFileMode;                  // sequential, circular
	ULONG FlushTimer;                   // buffer flush timer, in seconds
	ULONG EnableFlags;                  // trace enable flags
	union {
		LONG  AgeLimit;                 // unused
		LONG  FlushThreshold;           // Number of buffers to fill before flushing
	} DUMMYUNIONNAME;

	// data returned to caller
	ULONG NumberOfBuffers;              // no of buffers in use
	ULONG FreeBuffers;                  // no of buffers free
	ULONG EventsLost;                   // event records lost
	ULONG BuffersWritten;               // no of buffers written to file
	ULONG LogBuffersLost;               // no of logfile write failures
	ULONG RealTimeBuffersLost;          // no of rt delivery failures
	HANDLE LoggerThreadId;              // thread id of Logger
	ULONG LogFileNameOffset;            // Offset to LogFileName
	ULONG LoggerNameOffset;             // Offset to LoggerName
} EVENT_TRACE_PROPERTIES, *PEVENT_TRACE_PROPERTIES;

typedef struct _EVENT_TRACE_HEADER {        // overlays WNODE_HEADER
	USHORT          Size;                   // Size of entire record
	union {
		USHORT      FieldTypeFlags;         // Indicates valid fields
		struct {
			UCHAR   HeaderType;             // Header type - internal use only
			UCHAR   MarkerFlags;            // Marker - internal use only
		} DUMMYSTRUCTNAME;
	} DUMMYUNIONNAME;
	union {
		ULONG       Version;
		struct {
			UCHAR   Type;                   // event type
			UCHAR   Level;                  // trace instrumentation level
			USHORT  Version;                // version of trace record
		} Class;
	} DUMMYUNIONNAME2;
	ULONG           ThreadId;               // Thread Id
	ULONG           ProcessId;              // Process Id
	LARGE_INTEGER   TimeStamp;              // time when event happens
	union {
		GUID        Guid;                   // Guid that identifies event
		ULONGLONG   GuidPtr;                // use with WNODE_FLAG_USE_GUID_PTR
	} DUMMYUNIONNAME3;
	union {
		struct {
			ULONG   KernelTime;             // Kernel Mode CPU ticks
			ULONG   UserTime;               // User mode CPU ticks
		} DUMMYSTRUCTNAME;
		ULONG64     ProcessorTime;          // Processor Clock
		struct {
			ULONG   ClientContext;          // Reserved
			ULONG   Flags;                  // Event Flags
		} DUMMYSTRUCTNAME2;
	} DUMMYUNIONNAME4;
} EVENT_TRACE_HEADER, *PEVENT_TRACE_HEADER;

typedef struct _EVENT_TRACE {
	EVENT_TRACE_HEADER      Header;             // Event trace header
	ULONG                   InstanceId;         // Instance Id of this event
	ULONG                   ParentInstanceId;   // Parent Instance Id.
	GUID                    ParentGuid;         // Parent Guid;
	PVOID                   MofData;            // Pointer to Variable Data
	ULONG                   MofLength;          // Variable Datablock Length
	union {
		ULONG               ClientContext;
		ETW_BUFFER_CONTEXT  BufferContext;
	} DUMMYUNIONNAME;
} EVENT_TRACE, *PEVENT_TRACE;

typedef struct _TRACE_LOGFILE_HEADER {
	ULONG           BufferSize;         // Logger buffer size in Kbytes
	union {
		ULONG       Version;            // Logger version
		struct {
			UCHAR   MajorVersion;
			UCHAR   MinorVersion;
			UCHAR   SubVersion;
			UCHAR   SubMinorVersion;
		} VersionDetail;
	} DUMMYUNIONNAME;
	ULONG           ProviderVersion;    // defaults to NT version
	ULONG           NumberOfProcessors; // Number of Processors
	LARGE_INTEGER   EndTime;            // Time when logger stops
	ULONG           TimerResolution;    // assumes timer is constant!!!
	ULONG           MaximumFileSize;    // Maximum in Mbytes
	ULONG           LogFileMode;        // specify logfile mode
	ULONG           BuffersWritten;     // used to file start of Circular File
	union {
		GUID LogInstanceGuid;           // For RealTime Buffer Delivery
		struct {
			ULONG   StartBuffers;       // Count of buffers written at start.
			ULONG   PointerSize;        // Size of pointer type in bits
			ULONG   EventsLost;         // Events losts during log session
			ULONG   CpuSpeedInMHz;      // Cpu Speed in MHz
		} DUMMYSTRUCTNAME;
	} DUMMYUNIONNAME2;
#if defined(_WMIKM_)
	PWCHAR          LoggerName;
	PWCHAR          LogFileName;
	RTL_TIME_ZONE_INFORMATION TimeZone;
#else
	LPWSTR          LoggerName;
	LPWSTR          LogFileName;
	TIME_ZONE_INFORMATION TimeZone;
#endif
	LARGE_INTEGER   BootTime;
	LARGE_INTEGER   PerfFreq;           // Reserved
	LARGE_INTEGER   StartTime;          // Reserved
	ULONG           ReservedFlags;      // ClockType
	ULONG           BuffersLost;
} TRACE_LOGFILE_HEADER, *PTRACE_LOGFILE_HEADER;

typedef struct _EVENT_TRACE_LOGFILEW
EVENT_TRACE_LOGFILEW, *PEVENT_TRACE_LOGFILEW;

typedef ULONG(WINAPI * PEVENT_TRACE_BUFFER_CALLBACKW)
(PEVENT_TRACE_LOGFILEW Logfile);

typedef VOID(WINAPI *PEVENT_CALLBACK)(PEVENT_TRACE pEvent);

typedef struct _EVENT_RECORD
EVENT_RECORD, *PEVENT_RECORD;

typedef VOID(WINAPI *PEVENT_RECORD_CALLBACK) (PEVENT_RECORD EventRecord);

struct _EVENT_TRACE_LOGFILEW {
	LPWSTR                  LogFileName;      // Logfile Name
	LPWSTR                  LoggerName;       // LoggerName
	LONGLONG                CurrentTime;      // timestamp of last event
	ULONG                   BuffersRead;      // buffers read to date
	union {
		// Mode of the logfile
		ULONG               LogFileMode;
		// Processing flags used on Vista and above
		ULONG               ProcessTraceMode;
	} DUMMYUNIONNAME;
	EVENT_TRACE             CurrentEvent;     // Current Event from this stream.
	TRACE_LOGFILE_HEADER    LogfileHeader;    // logfile header structure
	PEVENT_TRACE_BUFFER_CALLBACKW             // callback before each buffer
		BufferCallback;   // is read
						  //
						  // following variables are filled for BufferCallback.
						  //
	ULONG                   BufferSize;
	ULONG                   Filled;
	ULONG                   EventsLost;
	//
	// following needs to be propaged to each buffer
	//
	union {
		// Callback with EVENT_TRACE
		PEVENT_CALLBACK         EventCallback;
		// Callback with EVENT_RECORD on Vista and above
		PEVENT_RECORD_CALLBACK  EventRecordCallback;
	} DUMMYUNIONNAME2;

	ULONG                   IsKernelTrace;    // TRUE for kernel logfile

	PVOID                   Context;          // reserved for internal use
};

#pragma warning(pop)

#define PEVENT_TRACE_BUFFER_CALLBACK    PEVENT_TRACE_BUFFER_CALLBACKW
#define EVENT_TRACE_LOGFILE             EVENT_TRACE_LOGFILEW
#define PEVENT_TRACE_LOGFILE            PEVENT_TRACE_LOGFILEW
#define KERNEL_LOGGER_NAME              KERNEL_LOGGER_NAMEW
#define GLOBAL_LOGGER_NAME              GLOBAL_LOGGER_NAMEW
#define EVENT_LOGGER_NAME               EVENT_LOGGER_NAMEW

EXTERN_C
ULONG
WMIAPI
ProcessTrace(
	_In_reads_(HandleCount) PTRACEHANDLE HandleArray,
	_In_ ULONG HandleCount,
	_In_opt_ LPFILETIME StartTime,
	_In_opt_ LPFILETIME EndTime
);

EXTERN_C
ULONG
WMIAPI
StartTraceW(
	_Out_ PTRACEHANDLE TraceHandle,
	_In_ LPCWSTR InstanceName,
	_Inout_ PEVENT_TRACE_PROPERTIES Properties
);

EXTERN_C
ULONG
WMIAPI
ControlTraceW(
	_In_ TRACEHANDLE TraceHandle,
	_In_opt_ LPCWSTR InstanceName,
	_Inout_ PEVENT_TRACE_PROPERTIES Properties,
	_In_ ULONG ControlCode
);

EXTERN_C
TRACEHANDLE
WMIAPI
OpenTraceW(
	_Inout_ PEVENT_TRACE_LOGFILEW Logfile
);

EXTERN_C
ULONG
WMIAPI
CloseTrace(
	_In_ TRACEHANDLE TraceHandle
);

EXTERN_C
ULONG
WMIAPI
TraceSetInformation(
	_In_ TRACEHANDLE SessionHandle,
	_In_ TRACE_INFO_CLASS InformationClass,
	_In_reads_bytes_(InformationLength) PVOID TraceInformation,
	_In_ ULONG InformationLength
);

EXTERN_C
ULONG
WMIAPI
TraceQueryInformation(
	_In_ TRACEHANDLE SessionHandle,
	_In_ TRACE_INFO_CLASS InformationClass,
	_Out_writes_bytes_(InformationLength) PVOID TraceInformation,
	_In_ ULONG InformationLength,
	_Out_opt_ PULONG ReturnLength
);

//////////////////////////////////////////////////////////////////////////
#define RegisterTraceGuids      RegisterTraceGuidsW
#define StartTrace              StartTraceW
#define ControlTrace            ControlTraceW
#define StopTrace               StopTraceW
#define QueryTrace              QueryTraceW
#define UpdateTrace             UpdateTraceW
#define FlushTrace              FlushTraceW
#define QueryAllTraces          QueryAllTracesW
#define OpenTrace               OpenTraceW
//////////////////////////////////////////////////////////////////////////

#else

#define INITGUID  // Causes definition of SystemTraceControlGuid in evntrace.h.
#include <wmistr.h>
#include <evntrace.h>
#include <strsafe.h>
#include <evntcons.h>

#endif //DECLARE_ETW

namespace Brofiler
{

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class ETW : public Trace
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

	virtual CaptureStatus::Type Start(int mode, const ThreadList& threads) override;
	virtual bool Stop() override;

	DWORD GetProcessID() const { return currentProcessId; }
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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
struct Thread_TypeGroup1
{
	// Process identifier of the thread involved in the event.
	uint32 ProcessId;
	// Thread identifier of the thread involved in the event.
	uint32 TThreadId;
	// Base address of the thread's stack.
	uint64 StackBase;
	// Limit of the thread's stack.
	uint64 StackLimit;
	// Base address of the thread's user-mode stack.
	uint64 UserStackBase;
	// Limit of the thread's user-mode stack.
	uint64 UserStackLimit;
	// The set of processors on which the thread is allowed to run.
	uint32 Affinity;
	// Starting address of the function to be executed by this thread.
	uint64 Win32StartAddr;
	// Thread environment block base address.
	uint64 TebBase;
	// Identifies the service if the thread is owned by a service; otherwise, zero.
	uint32 SubProcessTag;
	// The scheduler priority of the thread
	uint8  BasePriority;
	// A memory page priority hint for memory pages accessed by the thread.
	uint8  PagePriority;
	// An IO priority hint for scheduling IOs generated by the thread.
	uint8  IoPriority;
	// Not used.
	uint8  ThreadFlags;

	enum struct Opcode : uint8
	{
		Start = 1,
		End = 2,
		DCStart = 3,
		DCEnd = 4,
	};
};

size_t GetSIDSize(uint8* ptr)
{
	size_t result = 0;

	int sid = *((int*)ptr);

	if (sid != 0)
	{
		size_t tokenSize = 16;
		ptr += tokenSize;
		result += tokenSize;
		result += 8 + (4 * ((SID*)ptr)->SubAuthorityCount);
	}
	else
	{
		result = 4;
	}

	return result;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// https://github.com/Microsoft/perfview/blob/688a8564062d51321bbab53cd71d9e174a77d2ce/src/TraceEvent/TraceEvent.cs
struct Process_TypeGroup1 
{
	// The address of the process object in the kernel.
	uint64 UniqueProcessKey;
	// Global process identifier that you can use to identify a process. 
	uint32 ProcessId;
	// Unique identifier of the process that creates this process. 
	uint32 ParentId;
	// Unique identifier that an operating system generates when it creates a new session.
	uint32 SessionId;
	// Exit status of the stopped process.
	int32 ExitStatus;
	// The physical address of the page table of the process.
	uint64 DirectoryTableBase;
	// (?) uint8 Flags;
	// object UserSID;
	// string ImageFileName;
	// wstring CommandLine;

	static size_t GetSIDOffset(PEVENT_RECORD pEvent)
	{
		if (pEvent->EventHeader.EventDescriptor.Version >= 4)
			return 36;

		if (pEvent->EventHeader.EventDescriptor.Version == 3)
			return 32;

		return 24;
	}
	
	const char* GetProcessName(PEVENT_RECORD pEvent) const
	{
		BRO_ASSERT((pEvent->EventHeader.Flags & EVENT_HEADER_FLAG_64_BIT_HEADER) != 0, "32-bit is not supported! Disable BRO_ENABLE_TRACING on 32-bit platform if needed!");
		size_t sidOffset = GetSIDOffset(pEvent);
		size_t sidSize = GetSIDSize((uint8*)this + sidOffset);
		return (char*)this + sidOffset + sidSize;
	}

	enum struct Opcode
	{
		Start = 1,
		End = 2,
		DCStart = 3,
		DCEnd = 4,
		Defunct = 39,
	};
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
// https://docs.microsoft.com/en-us/windows/desktop/etw/thread
DEFINE_GUID(ThreadGuid, 0x3d6fa8d1, 0xfe05, 0x11d0, 0x9d, 0xda, 0x00, 0xc0, 0x4f, 0xd7, 0xba, 0x7c);

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// 3d6fa8d0-fe05-11d0-9dda-00c04fd7ba7c
// https://docs.microsoft.com/en-us/windows/desktop/etw/process
DEFINE_GUID(ProcessGuid, 0x3d6fa8d0, 0xfe05, 0x11d0, 0x9d, 0xda, 0x00, 0xc0, 0x4f, 0xd7, 0xba, 0x7c);

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const int MAX_CPU_CORES = 256;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct ETWRuntime
{
	std::array<ThreadID, MAX_CPU_CORES> activeCores;
	std::vector<std::pair<uint8_t, SysCallData*>> activeSyscalls;

	ETWRuntime()
	{
		Reset();
	}

	void Reset()
	{
		activeCores.fill(INVALID_THREAD_ID);
		activeSyscalls.resize(0);;
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
ETWRuntime g_ETWRuntime;
ETW g_ETW;
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

			// Assign ThreadID to the cores
			if (g_ETW.activeThreadsIDs.find(desc.newThreadId) != g_ETW.activeThreadsIDs.end())
			{
				g_ETWRuntime.activeCores[desc.cpuId] = desc.newThreadId;
			}
			else if (g_ETW.activeThreadsIDs.find(desc.oldThreadId) != g_ETW.activeThreadsIDs.end())
			{
				g_ETWRuntime.activeCores[desc.cpuId] = INVALID_THREAD_ID;
			}
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
				if (pStackWalkEvent->StackProcess == g_ETW.GetProcessID())
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
			uint8_t cpuId = eventRecord->BufferContext.ProcessorNumber;
			uint64_t threadId = g_ETWRuntime.activeCores[cpuId];

			if (threadId != INVALID_THREAD_ID)
			{
				SysCallEnter* pEventEnter = (SysCallEnter*)eventRecord->UserData;

				SysCallData& sysCall = Core::Get().syscallCollector.Add();
				sysCall.start = eventRecord->EventHeader.TimeStamp.QuadPart;
				sysCall.finish = EventTime::INVALID_TIMESTAMP;
				sysCall.threadID = threadId;
				sysCall.id = pEventEnter->SysCallAddress;
				sysCall.description = nullptr;

				g_ETWRuntime.activeSyscalls.push_back(std::make_pair(cpuId, &sysCall));
			}
		}
	} 
	else if (opcode == SysCallExit::OPCODE)
	{
		if (eventRecord->UserDataLength >= sizeof(SysCallExit))
		{
			uint8_t cpuId = eventRecord->BufferContext.ProcessorNumber;
			if (g_ETWRuntime.activeCores[cpuId] != INVALID_THREAD_ID)
			{
				for (int i = (int)g_ETWRuntime.activeSyscalls.size() - 1; i >= 0; --i)
				{
					if (g_ETWRuntime.activeSyscalls[i].first == cpuId)
					{
						g_ETWRuntime.activeSyscalls[i].second->finish = eventRecord->EventHeader.TimeStamp.QuadPart;
						g_ETWRuntime.activeSyscalls.erase(g_ETWRuntime.activeSyscalls.begin() + i);
						break;
					}
				}
			}
		}
	}
	else
	{
		// VS TODO: We might have a situation where a thread was deleted and the new thread was created with the same threadID
		//			Ignoring for now - profiling sessions are quite short - not critical
		if (IsEqualGUID(eventRecord->EventHeader.ProviderId, ThreadGuid))
		{
			if (eventRecord->UserDataLength >= sizeof(Thread_TypeGroup1))
			{
				const Thread_TypeGroup1* pThreadEvent = (const Thread_TypeGroup1*)eventRecord->UserData;
				Core::Get().RegisterThreadDescription(ThreadDescription("", pThreadEvent->TThreadId, pThreadEvent->ProcessId, 1, pThreadEvent->BasePriority));
			}
			
		}
		else if (IsEqualGUID(eventRecord->EventHeader.ProviderId, ProcessGuid))
		{
			if (eventRecord->UserDataLength >= sizeof(Process_TypeGroup1))
			{
				const Process_TypeGroup1* pProcessEvent = (const Process_TypeGroup1*)eventRecord->UserData;
				Core::Get().RegisterProcessDescription(ProcessDescription(pProcessEvent->GetProcessName(eventRecord), pProcessEvent->ProcessId, pProcessEvent->UniqueProcessKey));
			}
		}
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
	Core::Get().RegisterThreadDescription(ThreadDescription("[Brofiler] ETW", GetCurrentThreadId(), GetCurrentProcessId()));
	ETW* etw = (ETW*)parameter;
	ULONG status = ProcessTrace(&etw->openedHandle, 1, 0, 0);
	BRO_UNUSED(status);
	return 0;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void ETW::AdjustPrivileges()
{
#if BRO_PC
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
#endif
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void ETW::ResolveSysCalls()
{
	Core::Get().syscallCollector.syscallPool.ForEach([this](SysCallData& data)
	{
		auto it = syscallDescriptions.find(data.id);
		if (it == syscallDescriptions.end())
		{
			const Symbol* symbol = SymbolEngine::Get()->GetSymbol(data.id);
			if (symbol != nullptr)
			{
				std::string name(symbol->function.begin(), symbol->function.end());
				
				data.description = EventDescription::CreateShared(name.c_str(), "SysCall", (long)data.id);
				syscallDescriptions.insert(std::pair<const uint64_t, const Brofiler::EventDescription *>(data.id, data.description));
			}
		}
		else
		{
			data.description = it->second;
		}
	});
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

CaptureStatus::Type ETW::Start(int mode, const ThreadList& threads)
{
	if (!isActive) 
	{
		AdjustPrivileges();

		g_ETWRuntime.Reset();

		CaptureStatus::Type res = Trace::Start(mode, threads);
		if (res != CaptureStatus::OK)
		{
			return res;
		}

		ULONG bufferSize = sizeof(EVENT_TRACE_PROPERTIES) + sizeof(KERNEL_LOGGER_NAME);
		ZeroMemory(traceProperties, bufferSize);
		traceProperties->Wnode.BufferSize = bufferSize;
		traceProperties->LoggerNameOffset = sizeof(EVENT_TRACE_PROPERTIES);
		traceProperties->EnableFlags = 0;


		traceProperties->BufferSize = 256 << 10; // 512 Kb
		traceProperties->MinimumBuffers = 4;

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

		if (mode & PROCESSES)
		{
			traceProperties->EnableFlags |= EVENT_TRACE_FLAG_PROCESS;
			traceProperties->EnableFlags |= EVENT_TRACE_FLAG_THREAD;
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
				return CaptureStatus::ERR_TRACER_FAILED;
			}
		}

		if (status != ERROR_SUCCESS)
		{
			return CaptureStatus::ERR_TRACER_FAILED;
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
				BRO_FAILED("TraceSetInformation - failed");
				return CaptureStatus::ERR_TRACER_FAILED;
			}
		}

		bool highFrequencySampling = false;
		if (highFrequencySampling)
		{
			TRACE_PROFILE_INTERVAL itnerval = { 0 };
			memset(&itnerval, 0, sizeof(TRACE_PROFILE_INTERVAL));
			itnerval.Interval = highFrequencySampling ? 1221 : 10000;
			// The SessionHandle is irrelevant for this information class and must be zero, else the function returns ERROR_INVALID_PARAMETER.
			status = TraceSetInformation(NULL /*traceSessionHandle*/, TraceSampledProfileIntervalInfo, &itnerval, sizeof(TRACE_PROFILE_INTERVAL));
			BRO_ASSERT(status == ERROR_SUCCESS, "TraceSetInformation - failed");
		}

		ZeroMemory(&logFile, sizeof(EVENT_TRACE_LOGFILE));
		logFile.LoggerName = KERNEL_LOGGER_NAME;
		logFile.ProcessTraceMode = (PROCESS_TRACE_MODE_REAL_TIME | PROCESS_TRACE_MODE_EVENT_RECORD | PROCESS_TRACE_MODE_RAW_TIMESTAMP);
		logFile.EventRecordCallback = OnRecordEvent;
		logFile.BufferCallback = OnBufferRecord;
		openedHandle = OpenTrace(&logFile);
		if (openedHandle == INVALID_TRACEHANDLE)
		{
			BRO_FAILED("OpenTrace - failed");
			return CaptureStatus::ERR_TRACER_FAILED;
		}

		DWORD threadID;
		processThreadHandle = CreateThread(0, 0, RunProcessTraceThreadFunction, this, 0, &threadID);
		
		isActive = true;
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

	ULONG controlTraceResult = ControlTrace(openedHandle, KERNEL_LOGGER_NAME, traceProperties, EVENT_TRACE_CONTROL_STOP);

	// ERROR_CTX_CLOSE_PENDING(7007L): The call was successful. The ProcessTrace function will stop after it has processed all real-time events in its buffers (it will not receive any new events).
	// ERROR_BUSY(170L): Prior to Windows Vista, you cannot close the trace until the ProcessTrace function completes.
	// ERROR_INVALID_HANDLE(6L): One of the following is true: TraceHandle is NULL. TraceHandle is INVALID_HANDLE_VALUE.
	ULONG closeTraceStatus = CloseTrace(openedHandle);

	// Wait for ProcessThread to finish
	WaitForSingleObject(processThreadHandle, INFINITE);
	BOOL wasThreadClosed = CloseHandle(processThreadHandle);

	isActive = false;

	//VS TODO: Disabling resolving of the syscalls - we can't use then as EventDescriptions at the moment
	//ResolveSysCalls();
	
	if (!Trace::Stop())
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
Trace* Trace::Get()
{
	return &g_ETW;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

#endif //BRO_ENABLE_TRACING
#endif //_WIN32