// The MIT License (MIT)
// 
// 	Copyright (c) 2018 Sergey Makeev, Vadim Slyusarev
// 
// 	Permission is hereby granted, free of charge, to any person obtaining a copy
// 	of this software and associated documentation files (the "Software"), to deal
// 	in the Software without restriction, including without limitation the rights
// 	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// 	copies of the Software, and to permit persons to whom the Software is
// 	furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
// 	all copies or substantial portions of the Software.
// 
// 	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// 	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// 	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// 	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// 	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// 	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// 	THE SOFTWARE.

#pragma once

#include "Common.h"

////////////////////////////////////////////////////////////////////////
// Compiler support for C++11
////////////////////////////////////////////////////////////////////////
#if defined(__STDC_VERSION__) && (__STDC_VERSION__ >= 201112L)
#define OPTICK_CPP11_SUPPORTED (1)
#endif

////////////////////////////////////////////////////////////////////////
// OPTICK_THREAD_LOCAL
////////////////////////////////////////////////////////////////////////
#if defined(OPTICK_CPP11_SUPPORTED)
#define OPTICK_THREAD_LOCAL _Thread_local
#elif defined(OPTICK_GCC)
#define OPTICK_THREAD_LOCAL __thread
#elif defined(OPTICK_MSVC)
#define OPTICK_THREAD_LOCAL __declspec(thread)
#else
#error Can not define OPTICK_THREAD_LOCAL. Unknown platform.
#endif

#if defined(OPTICK_GCC)
#include <sys/syscall.h>
#include <sys/time.h>
#include <sys/types.h>
#include <pthread.h>
#include <unistd.h>
#endif

#if defined(OPTICK_OSX)
#import <mach/mach_time.h>
#endif

namespace Optick
{
	typedef uint64 ThreadID;
	static const ThreadID INVALID_THREAD_ID = (ThreadID)-1;

	typedef uint32 ProcessID;
	static const ProcessID INVALID_PROCESS_ID = (ProcessID)-1;

	ThreadID GetThreadID();
	ProcessID GetProcessID();

	struct Platform
	{
		enum ID
		{
			Unknown,
			Windows,
			Linux,
			MacOS,
			XBox,
			Playstation,
		};

		static ID Get();
	};

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	OPTICK_INLINE int64 GetFrequency()
	{
#if defined(OPTICK_MSVC)
		LARGE_INTEGER frequency;
		QueryPerformanceFrequency(&frequency);
		return frequency.QuadPart;
#elif defined(OPTICK_GCC)
		return 1000000000;
#else
	#error Platform is not supported!
#endif
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	OPTICK_INLINE int64 GetTimeNanoSeconds()
	{
#if defined(OPTICK_MSVC)
		LARGE_INTEGER largeInteger;
		QueryPerformanceCounter(&largeInteger);
		return (largeInteger.QuadPart * 1000000000LL) / GetFrequency();
#elif defined(OPTICK_OSX)
        struct timespec ts;
        clock_gettime(CLOCK_REALTIME, &ts);
        return ts.tv_sec * 1000000000LL + ts.tv_nsec;
#elif defined(OPTICK_GCC)
		struct timespec ts;
		clock_gettime(CLOCK_MONOTONIC, &ts);
		return ts.tv_sec * 1000000000LL + ts.tv_nsec;
#else
	#error Platform is not supported!
#endif
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	OPTICK_INLINE int64 GetTimeMilliSeconds()
	{
		return GetTimeNanoSeconds() / 1000000;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	OPTICK_INLINE int64 GetTime()
	{
#if defined(OPTICK_MSVC)
		LARGE_INTEGER largeInteger;
		QueryPerformanceCounter(&largeInteger);
		return largeInteger.QuadPart;
#elif defined(OPTICK_GCC)
        return GetTimeNanoSeconds();
#else
	#error Platform is not supported!
#endif
	}
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
