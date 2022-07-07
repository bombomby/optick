// The MIT License(MIT)
//
// Copyright(c) 2019 Vadim Slyusarev
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#pragma once
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Config
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#include "optick.config.h"
#include <stdint.h>

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// EXPORTS 
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if defined(OPTICK_EXPORTS) && defined(_MSC_VER)
#define OPTICK_API __declspec(dllexport)
#else
#define OPTICK_API 
#endif


#ifdef __cplusplus
extern "C" {
#endif

#if USE_OPTICK
	OPTICK_API void OptickAPI_RegisterThread(const char* inThreadName, uint16_t inThreadNameLength);

	OPTICK_API uint64_t OptickAPI_CreateEventDescription(const char* inFunctionName, uint16_t inFunctionLength, const char* inFileName, uint16_t inFileNameLenght, uint32_t inFileLine);
	OPTICK_API uint64_t OptickAPI_PushEvent(uint64_t inEventDescription);
	OPTICK_API void OptickAPI_PopEvent(uint64_t inEventData);
	
	OPTICK_API void OptickAPI_NextFrame();

	OPTICK_API void OptickAPI_StartCapture();
	OPTICK_API void OptickAPI_StopCapture(const char* inFileName, uint16_t inFileNameLength);

	OPTICK_API void OptickAPI_AttachTag_String(uint64_t inEventDescription, const char* inValue, uint16_t intValueLength);
	OPTICK_API void OptickAPI_AttachTag_Int32(uint64_t inEventDescription, int inValue);
	OPTICK_API void OptickAPI_AttachTag_Float(uint64_t inEventDescription, float inValue);
	OPTICK_API void OptickAPI_AttachTag_UInt32(uint64_t inEventDescription, uint32_t inValue);
	OPTICK_API void OptickAPI_AttachTag_UInt64(uint64_t inEventDescription, uint64_t inValue);
	OPTICK_API void OptickAPI_AttachTag_Point(uint64_t inEventDescription, float x, float y, float z);
#else

#define OPTICK_CAPI_UNUSED(x) (void)(x)
	inline void OptickAPI_RegisterThread(const char* inThreadName, uint16_t inThreadNameLength) { OPTICK_CAPI_UNUSED(inThreadName); OPTICK_CAPI_UNUSED(inThreadNameLength); }
	inline uint64_t OptickAPI_CreateEventDescription(const char* inFunctionName, uint16_t inFunctionLength, const char* inFileName, uint16_t inFileNameLenght, uint32_t inFileLine) { OPTICK_CAPI_UNUSED(inFunctionName); OPTICK_CAPI_UNUSED(inFunctionLength); OPTICK_CAPI_UNUSED(inFileName); OPTICK_CAPI_UNUSED(inFileNameLenght); OPTICK_CAPI_UNUSED(inFileLine); return 0; }
	inline uint64_t OptickAPI_PushEvent(uint64_t inEventDescription) { OPTICK_CAPI_UNUSED(inEventDescription); return 0; }
	inline void OptickAPI_PopEvent(uint64_t inEventData) { OPTICK_CAPI_UNUSED(inEventData); }
	inline void OptickAPI_NextFrame() {}
	inline void OptickAPI_StartCapture() {}
	inline void OptickAPI_StopCapture(const char* inFileName, uint16_t inFileNameLength) { OPTICK_CAPI_UNUSED(inFileName); OPTICK_CAPI_UNUSED(inFileNameLength); }
	inline void OptickAPI_AttachTag_String(uint64_t inEventDescription, const char* inValue, uint16_t intValueLength) { OPTICK_CAPI_UNUSED(inEventDescription); OPTICK_CAPI_UNUSED(inValue); OPTICK_CAPI_UNUSED(intValueLength); }
	inline void OptickAPI_AttachTag_Int(uint64_t inEventDescription, int inValue) { OPTICK_CAPI_UNUSED(inEventDescription); OPTICK_CAPI_UNUSED(inValue); }
	inline void OptickAPI_AttachTag_Float(uint64_t inEventDescription, float inValue) { OPTICK_CAPI_UNUSED(inEventDescription); OPTICK_CAPI_UNUSED(inValue); }
	inline void OptickAPI_AttachTag_Int32(uint64_t inEventDescription, uint32_t inValue) { OPTICK_CAPI_UNUSED(inEventDescription); OPTICK_CAPI_UNUSED(inValue); }
	inline void OptickAPI_AttachTag_UInt64(uint64_t inEventDescription, uint64_t inValue) { OPTICK_CAPI_UNUSED(inEventDescription); OPTICK_CAPI_UNUSED(inValue); }
	inline void OptickAPI_AttachTag_Point(uint64_t inEventDescription, float x, float y, float z) { OPTICK_CAPI_UNUSED(inEventDescription); OPTICK_CAPI_UNUSED(x); OPTICK_CAPI_UNUSED(y); OPTICK_CAPI_UNUSED(z); }
#endif

#ifdef __cplusplus
} // extern "C"
#endif