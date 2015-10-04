#include "../Thread.h"
#include "../HPTimer.h"
#include <winnt.h>

namespace Profiler
{

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
DWORD CurrentThreadID()
{
	return GetCurrentThreadId();
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void ThreadSleep(DWORD milliseconds)
{
	Sleep(milliseconds);	
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BRO_INLINE void AtomicIncrement(volatile uint* value)
{
	InterlockedIncrement(value);
}

BRO_INLINE void AtomicDecrement(volatile uint* value)
{
	InterlockedDecrement(value);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

bool SystemThread::Create( DWORD WINAPI Action( LPVOID lpParam ), LPVOID lpParam )
{
	threadId = CreateThread(NULL, 0, Action, lpParam, 0, NULL);
	return threadId != 0;
}

bool SystemThread::Join()
{
	DWORD result = WaitForSingleObject(workerThread, INFINITE);
	return result != WAIT_OBJECT_0;
}

bool SystemThread::Terminate()
{
	bool result = true;
	if (threadId)
	{
		TerminateThread(threadId, 0);
		DWORD resultCode = WaitForSingleObject(threadId, INFINITE);
		if (resultCode == WAIT_OBJECT_0)
		{
			result = false;
		}
		CloseHandle(threadId);
		threadId = 0;
	}
	return result;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
SystemSyncEvent::SystemSyncEvent()
{
	eventHandler[0] = CreateEvent(NULL, false, false, 0);
}

SystemSyncEvent::~SystemSyncEvent()
{
	CloseHandle(eventHandler[0]);
	eventHandler[0] = 0;
}
	
void SystemSyncEvent::Notify()
{
	SetEvent(eventHandler[0]);
}

bool SystemSyncEvent::WaitForEvent( int millisecondsTimeout )
{
	if (WaitForSingleObject(eventHandler[0], 0) == WAIT_TIMEOUT)
	{
		SpinSleep(millisecondsTimeout);
		return true;
	}
	else
	{
		return false;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}