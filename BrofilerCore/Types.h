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

#include <stddef.h>
#include <stdint.h>

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef signed char int8;
typedef unsigned char uint8;
typedef unsigned char byte;
typedef short int16;
typedef unsigned short uint16;
typedef int int32;
typedef unsigned int uint32;

#if BRO_MSVC_COMPILER_FAMILY
typedef __int64 int64;
typedef unsigned __int64 uint64;
#elif BRO_GCC_COMPILER_FAMILY
typedef int64_t int64;
typedef uint64_t uint64;
#else
#error Compiler is not supported
#endif

static_assert( sizeof(int8) == 1, "Invalid type size, int8" );
static_assert( sizeof(uint8) == 1, "Invalid type size, uint8" );
static_assert( sizeof(byte) == 1, "Invalid type size, byte" );
static_assert( sizeof(int16) == 2, "Invalid type size, int16" );
static_assert( sizeof(uint16) == 2, "Invalid type size, uint16" );
static_assert( sizeof(int32) == 4, "Invalid type size, int32" );
static_assert( sizeof(uint32) == 4, "Invalid type size, uint32" );
static_assert( sizeof(int64) == 8, "Invalid type size, int64" );
static_assert( sizeof(uint64) == 8, "Invalid type size, uint64" );
