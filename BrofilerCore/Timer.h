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

#if defined(BRO_GCC_COMPILER_FAMILY)
#include <sys/time.h>
#endif

namespace Brofiler
{
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	BRO_FORCEINLINE int64 GetFrequency()
	{
#if defined(BRO_MSVC_COMPILER_FAMILY)
		LARGE_INTEGER frequency;
		QueryPerformanceFrequency(&frequency);
		return frequency.QuadPart;
#elif defined(BRO_GCC_COMPILER_FAMILY)
		return 1000000;
#elif
		#error Platform is not supported!
#endif
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	BRO_FORCEINLINE int64 GetTimeMicroSeconds()
	{
#if defined(BRO_MSVC_COMPILER_FAMILY)
		LARGE_INTEGER largeInteger;
		QueryPerformanceCounter(&largeInteger);
		return (largeInteger.QuadPart * int64(1000000)) / GetFrequency();
#elif defined(BRO_GCC_COMPILER_FAMILY)
		struct timeval te;
		gettimeofday(&te, nullptr);
		return te.tv_sec * 1000000LL + te.tv_usec;
#elif
		#error Platform is not supported!
#endif
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	BRO_FORCEINLINE int64 GetTimeMilliSeconds()
	{
		return GetTimeMicroSeconds() / 1000;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	BRO_FORCEINLINE int64 GetTime()
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
}
