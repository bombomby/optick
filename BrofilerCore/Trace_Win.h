#pragma once

#ifdef _WIN32

#include "Trace.h"
#include <unordered_map>

#define INITGUID  // Causes definition of SystemTraceControlGuid in evntrace.h.
#include <strsafe.h>
#include <wmistr.h>
#include <evntrace.h>
#include <evntcons.h>

#if _MSC_VER <= 1600
#define EVENT_DESCRIPTOR_DEF
#define EVENT_HEADER_DEF
#define EVENT_HEADER_EXTENDED_DATA_ITEM_DEF
#define EVENT_RECORD_DEF
#endif

///////////////////////////////////////////////////////////////////////////////
#define PROCESS_TRACE_MODE_REAL_TIME                0x00000100
#define PROCESS_TRACE_MODE_RAW_TIMESTAMP            0x00001000
#define PROCESS_TRACE_MODE_EVENT_RECORD             0x10000000
///////////////////////////////////////////////////////////////////////////////
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

	virtual CaptureStatus::Type Start(int mode, const ThreadList& threads) override;
	virtual bool Stop() override;

	DWORD GetProcessID() const { return currentProcessId; }
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

#endif