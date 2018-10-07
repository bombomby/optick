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

#include "Platform.h"
#include "Common.h"

#if BRO_PLATFORM_WINDOWS
#include <windows.h>
#endif

#if BRO_PLATFORM_POSIX
#include <sys/time.h>
#endif

namespace Brofiler
{
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	bro_forceinline int64 GetFrequency()
	{
#if BRO_PLATFORM_WINDOWS
		LARGE_INTEGER frequency;
		QueryPerformanceFrequency(&frequency);
		return frequency.QuadPart;
#elif BRO_PLATFORM_POSIX
		return 1000000;
#elif
		#error Platform is not supported!
#endif
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	bro_forceinline int64 GetTimeMicroSeconds()
	{
#if BRO_PLATFORM_WINDOWS
		LARGE_INTEGER largeInteger;
		QueryPerformanceCounter(&largeInteger);
		return (largeInteger.QuadPart * int64(1000000)) / GetFrequency();
#elif BRO_PLATFORM_POSIX
		struct timeval te;
		gettimeofday(&te, nullptr);
		return te.tv_sec * 1000000LL + te.tv_usec;
#elif
		#error Platform is not supported!
#endif
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	bro_forceinline int64 GetTimeMilliSeconds()
	{
		return GetTimeMicroSeconds() / 1000;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	__inline int64 GetTime()
	{
#if BRO_PLATFORM_WINDOWS
		LARGE_INTEGER largeInteger;
		QueryPerformanceCounter(&largeInteger);
		return largeInteger.QuadPart;
#elif BRO_PLATFORM_POSIX
		return GetTimeMicroSeconds();
#else
		#error Platform is not supported!
#endif
	}
}