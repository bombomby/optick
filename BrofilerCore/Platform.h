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
// Target Platform
////////////////////////////////////////////////////////////////////////
#if   _WIN32
#define BRO_PLATFORM_WINDOWS (1)
#elif __APPLE_CC__
#define BRO_PLATFORM_OSX (1)
#elif defined(__linux__)
#define BRO_PLATFORM_LINUX (1)
#endif

////////////////////////////////////////////////////////////////////////
// Compiler family
////////////////////////////////////////////////////////////////////////
#if defined(__clang__)
#define BRO_CLANG_COMPILER_FAMILY (1)
#define BRO_GCC_COMPILER_FAMILY (1)
#elif defined(__GNUC__)
#define BRO_GCC_COMPILER_FAMILY (1)
#elif defined(_MSC_VER)
#define BRO_MSVC_COMPILER_FAMILY (1)
#endif

////////////////////////////////////////////////////////////////////////
// Compiler support for C++11
////////////////////////////////////////////////////////////////////////
#if defined(__STDC_VERSION__) && (__STDC_VERSION__ >= 201112L)
#define BRO_CPP11_SUPPORTED (1)
#endif

////////////////////////////////////////////////////////////////////////
// BRO_THREAD_LOCAL
////////////////////////////////////////////////////////////////////////
#if defined(BRO_CPP11_SUPPORTED)
#define BRO_THREAD_LOCAL _Thread_local
#elif defined(BRO_GCC_COMPILER_FAMILY)
#define BRO_THREAD_LOCAL __thread
#elif defined(BRO_MSVC_COMPILER_FAMILY)
#define BRO_THREAD_LOCAL __declspec(thread)
#else
#error Can not define BRO_THREAD_LOCAL. Unknown platform.
#endif

#if defined(BRO_GCC_COMPILER_FAMILY)
#include <sys/time.h>
#include <pthread.h>
#include <unistd.h>
#endif

#if defined (BRO_PLATFORM_LINUX)
#include <sys/types.h>
#include <sys/syscall.h>
#endif

namespace Brofiler
{
	typedef uint64 ThreadID;
	static const ThreadID INVALID_THREAD_ID = (ThreadID)-1;

	typedef uint32 ProcessID;
	static const ProcessID INVALID_PROCESS_ID = (ProcessID)-1;

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
	};

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	BRO_INLINE ThreadID GetThreadID()
	{
#if defined(BRO_PLATFORM_WINDOWS)
		return GetCurrentThreadId();
#elif defined(BRO_PLATFORM_LINUX) || defined(BRO_PLATFORM_OSX)
		return syscall(SYS_gettid); // (uint64)pthread_self();
#else
		#error Platform is not supported!
#endif
	}
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	BRO_INLINE ProcessID GetProcessID()
	{
#if defined(BRO_PLATFORM_WINDOWS)
		return GetCurrentProcessId();
#elif defined(BRO_PLATFORM_LINUX) || defined(BRO_PLATFORM_OSX)
		return (ProcessID)getpid();
#else
		#error Platform is not supported!
#endif
	}
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	BRO_INLINE int64 GetFrequency()
	{
#if defined(BRO_MSVC_COMPILER_FAMILY)
		LARGE_INTEGER frequency;
		QueryPerformanceFrequency(&frequency);
		return frequency.QuadPart;
#elif defined(BRO_GCC_COMPILER_FAMILY)
		return 1000000000;
#else
	#error Platform is not supported!
#endif
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	BRO_INLINE int64 GetTimeMicroSeconds()
	{
#if defined(BRO_MSVC_COMPILER_FAMILY)
		LARGE_INTEGER largeInteger;
		QueryPerformanceCounter(&largeInteger);
		return (largeInteger.QuadPart * int64(1000000)) / GetFrequency();
#elif defined(BRO_GCC_COMPILER_FAMILY)
		struct timespec ts;
		clock_gettime(CLOCK_MONOTONIC, &ts);
		return ts.tv_sec * 1000000000LL + ts.tv_nsec;
#else
	#error Platform is not supported!
#endif
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	BRO_INLINE int64 GetTimeMilliSeconds()
	{
		return GetTimeMicroSeconds() / 1000;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	BRO_INLINE int64 GetTime()
	{
#if defined(BRO_MSVC_COMPILER_FAMILY)
		LARGE_INTEGER largeInteger;
		QueryPerformanceCounter(&largeInteger);
		return largeInteger.QuadPart;
#elif defined(BRO_GCC_COMPILER_FAMILY)
		return GetTimeMicroSeconds();
#else
	#error Platform is not supported!
#endif
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	BRO_INLINE Platform::ID GetPlatform()
	{
#if defined(BRO_PLATFORM_WINDOWS)
		return Platform::Windows;
#elif defined(BRO_PLATFORM_LINUX)
		return Platform::Linux;
#elif defined(BRO_PLATFORM_OSX)
		return Platform::MacOS;
#elif defined(BRO_PLATFORM_XBOX)
		return Platform::XBox;
#elif defined(BRO_PLATFORM_PS)
		return Platform::Playstation;
#else
		#error Platform is not supported!
#endif
	}
}