#pragma once

#include "Brofiler.h"

#include <cstdio>
#include <stdarg.h>
#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>

#if BRO_MSVC
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#define NOMINMAX
#include <windows.h>
#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Types
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef signed char int8;
typedef unsigned char uint8;
typedef unsigned char byte;
typedef short int16;
typedef unsigned short uint16;
typedef int int32;
typedef unsigned int uint32;
#if defined(BRO_MSVC)
typedef __int64 int64;
typedef unsigned __int64 uint64;
#elif defined(BRO_GCC)
typedef int64_t int64;
typedef uint64_t uint64;
#else
#error Compiler is not supported
#endif
static_assert(sizeof(int8) == 1, "Invalid type size, int8");
static_assert(sizeof(uint8) == 1, "Invalid type size, uint8");
static_assert(sizeof(byte) == 1, "Invalid type size, byte");
static_assert(sizeof(int16) == 2, "Invalid type size, int16");
static_assert(sizeof(uint16) == 2, "Invalid type size, uint16");
static_assert(sizeof(int32) == 4, "Invalid type size, int32");
static_assert(sizeof(uint32) == 4, "Invalid type size, uint32");
static_assert(sizeof(int64) == 8, "Invalid type size, int64");
static_assert(sizeof(uint64) == 8, "Invalid type size, uint64");
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Memory
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if defined(BRO_MSVC)
#define BRO_ALIGN(N) __declspec( align( N ) )
#elif defined(BRO_GCC)
#define BRO_ALIGN(N) __attribute__((aligned(N)))
#else
#error Can not define BRO_ALIGN. Unknown platform.
#endif
#define BRO_CACHE_LINE_SIZE 64
#define BRO_ALIGN_CACHE BRO_ALIGN(BRO_CACHE_LINE_SIZE)
#define BRO_ARRAY_SIZE(ARR) (sizeof(ARR)/sizeof((ARR)[0]))
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Warnings
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if _MSC_VER >= 1600 // >= VS 2010 (VC10)
#pragma warning (disable: 4481) //http://msdn.microsoft.com/en-us/library/ms173703.aspx
#else
#define override
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Asserts
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if defined(BRO_MSVC)
#define BRO_DEBUG_BREAK __debugbreak()
#elif defined(BRO_GCC)
#define BRO_DEBUG_BREAK __builtin_trap()
#else
	#error Can not define BRO_DEBUG_BREAK. Unknown platform.
#endif
#define BRO_UNUSED(x) (void)(x)
#ifdef _DEBUG
	#define BRO_ASSERT(arg, description) if (!(arg)) { BRO_DEBUG_BREAK; }
	#define BRO_VERIFY(arg, description, operation) if (!(arg)) { BRO_DEBUG_BREAK; operation; }
	#define BRO_FAILED(description) { BRO_DEBUG_BREAK; }
#else
	#define BRO_ASSERT(arg, description)
	#define BRO_VERIFY(arg, description, operation)
	#define BRO_FAILED(description)
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Safe functions
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if defined(BRO_GCC)
template<size_t sizeOfBuffer>
inline int sprintf_s(char(&buffer)[sizeOfBuffer], const char* format, ...)
{
	va_list ap;
	va_start(ap, format);
	int result = vsnprintf(buffer, sizeOfBuffer, format, ap);
	va_end(ap);
	return result;
}

template<size_t sizeOfBuffer>
inline int wcstombs_s(char(&buffer)[sizeOfBuffer], const wchar_t* src, size_t maxCount)
{
	return wcstombs(buffer, src, maxCount);
}
#endif

#if BRO_MSVC
template<size_t sizeOfBuffer>
inline int wcstombs_s(char(&buffer)[sizeOfBuffer], const wchar_t* src, size_t maxCount)
{
	size_t converted = 0;
	return wcstombs_s(&converted, buffer, src, maxCount);
}
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
