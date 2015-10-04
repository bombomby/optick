#include "../Thread.h"
#include <pthread.h>
#include <ctime>
#include <condition_variable>
#include <thread>
#include <chrono>
#include <signal.h>
#include <cstring>

namespace Profiler
{

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
DWORD CurrentThreadID()
{
	static_assert(sizeof(DWORD) >= sizeof(pthread_t), "Information will be lost otherwise");
	return pthread_self();	
}

HANDLE GetThreadHandleByThreadID(DWORD threadId)
{
	static_assert(sizeof(HANDLE) >= sizeof(DWORD), "Information will be lost otherwise");
	return threadId;
}

void ReleaseThreadHandle(HANDLE threadId)
{
}

bool PauseThread(HANDLE threadId)
{
	return 0 == pthread_kill(threadId, SIGSTOP);
}

bool ContinueThread(HANDLE threadId)
{
	return 0 == pthread_kill(threadId, SIGCONT);
}

bool RetrieveThreadContext(HANDLE threadHandle, CONTEXT& context)
{
	memset(&context, 0, sizeof(CONTEXT));
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

bool SystemThread::Join()
{
	void* retval;
	pthread_join(threadId, &retval);
	return true;
}

bool SystemThread::Terminate()
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
	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
SystemSyncEvent::SystemSyncEvent()
{
	static_assert(sizeof(std::condition_variable) <= sizeof(eventHandler) * sizeof(eventHandler[0]), "Increase size of eventHandler");
	static_assert(sizeof(std::mutex) <= sizeof(eventHandlerMutex) * sizeof(eventHandlerMutex[0]), "Increase size of eventHandlerMutex");
	new (reinterpret_cast<void*>(&eventHandlerMutex[0])) std::mutex;
	new (reinterpret_cast<void*>(&eventHandler[0])) std::condition_variable;
}

SystemSyncEvent::~SystemSyncEvent()
{
	reinterpret_cast<std::mutex*>(&eventHandlerMutex[0])->~mutex();
	reinterpret_cast<std::condition_variable*>(&eventHandler[0])->~condition_variable();
}
	
void SystemSyncEvent::Notify()
{
	std::condition_variable *event = reinterpret_cast<std::condition_variable*>(&eventHandler[0]);
	event->notify_all();
}

bool SystemSyncEvent::WaitForEvent( int millisecondsTimeout )
{
	std::unique_lock<std::mutex> mutexLock(*reinterpret_cast<std::mutex*>(&eventHandlerMutex[0]));
	std::condition_variable *event = reinterpret_cast<std::condition_variable*>(&eventHandler[0]);
	if( std::cv_status::no_timeout == event->wait_for(mutexLock, std::chrono::milliseconds(millisecondsTimeout) ) )
	{
		return false;
	}
	else
	{
		return true;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}