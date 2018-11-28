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

////////////////////////////////////////////////////////////////////////
// Target Platform
////////////////////////////////////////////////////////////////////////
#if   _WIN32
#define BRO_PLATFORM_WINDOWS (1)
#elif __APPLE_CC__
#define BRO_PLATFORM_OSX (1)
#else
#define BRO_PLATFORM_POSIX (1)
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
// BRO_FORCEINLINE
////////////////////////////////////////////////////////////////////////
#if defined(BRO_MSVC_COMPILER_FAMILY)
#define BRO_FORCEINLINE __forceinline
#elif defined(BRO_GCC_COMPILER_FAMILY)
#define BRO_FORCEINLINE __attribute__((always_inline)) inline
#else
#error Can not define BRO_FORCEINLINE. Unknown platform.
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

////////////////////////////////////////////////////////////////////////
// BRO_ALIGN
////////////////////////////////////////////////////////////////////////
#if defined(BRO_MSVC_COMPILER_FAMILY)
#define BRO_ALIGN(N) __declspec( align( N ) )
#elif defined(BRO_GCC_COMPILER_FAMILY)
#define BRO_ALIGN(N) __attribute__((aligned(N)))
#else
#error Can not define BRO_ALIGN. Unknown platform.
#endif

////////////////////////////////////////////////////////////////////////
// BRO_DEBUG_BREAK
////////////////////////////////////////////////////////////////////////
#if defined(BRO_MSVC_COMPILER_FAMILY)
#define BRO_DEBUG_BREAK __debugbreak()
#elif defined(BRO_GCC_COMPILER_FAMILY)
#define BRO_DEBUG_BREAK __builtin_trap()
#else
#error Can not define BRO_DEBUG_BREAK. Unknown platform.
#endif

