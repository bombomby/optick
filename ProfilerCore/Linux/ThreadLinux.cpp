#include "../Thread.h"
#include <pthread.h>
#include <time.h>

namespace Profiler
{

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
DWORD CurrentThreadID()
{
	return pthread_self();	
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void ThreadSleep(DWORD milliseconds)
{
	struct timespec sleepTime;
	sleepTime.tv_sec = milliseconds / 1000;
	sleepTime.tv_nsec = (milliseconds % 1000) * 1000000; 
	nanosleep(&sleepTime, nullptr);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void AtomicIncrement(volatile uint* value)
{
	__sync_fetch_and_add(value, 1);
}

void AtomicDecrement(volatile uint* value)
{
	__sync_fetch_and_add(value, -1);	
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool SystemThread::Create( DWORD WINAPI Action( LPVOID lpParam ), LPVOID lpParam )
{
	int result = pthread_create(&threadId, NULL, (void* (*)(void*))Action, lpParam);
	if (result == 0)
	{
		threadId = 0;
	}
	return result != 0;
}

void SystemThread::Terminate()
{
	if (threadId)
	{
		int cancelResult = pthread_cancel(threadId);
		if (cancelResult == 0)
		{
			void* retval;
			pthread_join(threadId, &retval);
		}
		threadId = 0;
	}
}
	
}