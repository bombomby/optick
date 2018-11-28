#pragma once

#include "Platform.h"
#include "Types.h"
#include "Common.h"

#if defined(BRO_PLATFORM_POSIX) || defined(BRO_PLATFORM_OSX)
#include <pthread.h>
#endif

namespace Brofiler
{
	typedef uint64 ThreadID;
	static const ThreadID INVALID_THREAD_ID = (ThreadID)-1;

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	BRO_FORCEINLINE ThreadID GetThreadID()
	{
#if defined(BRO_PLATFORM_WINDOWS)
		return GetCurrentThreadId();
#elif defined(BRO_PLATFORM_POSIX) || defined(BRO_PLATFORM_OSX)
		return (uint64)pthread_self();
#else
		#error Platform is not supported!
#endif
	}
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}
