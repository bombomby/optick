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
#include <stddef.h>

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

typedef void* (*OptickAPI_AllocateFn)(size_t);
typedef void  (*OptickAPI_DeallocateFn)(void*);
typedef void  (*OptickAPI_InitThreadCb)(void);

// Source: http://msdn.microsoft.com/en-us/library/system.windows.media.colors(v=vs.110).aspx
// Image:  http://i.msdn.microsoft.com/dynimg/IC24340.png
enum OptickAPI_Color
{
	OptickAPI_Color_Null = 0x00000000,
	OptickAPI_Color_AliceBlue = 0xFFF0F8FF,
	OptickAPI_Color_AntiqueWhite = 0xFFFAEBD7,
	OptickAPI_Color_Aqua = 0xFF00FFFF,
	OptickAPI_Color_Aquamarine = 0xFF7FFFD4,
	OptickAPI_Color_Azure = 0xFFF0FFFF,
	OptickAPI_Color_Beige = 0xFFF5F5DC,
	OptickAPI_Color_Bisque = 0xFFFFE4C4,
	OptickAPI_Color_Black = 0xFF000000,
	OptickAPI_Color_BlanchedAlmond = 0xFFFFEBCD,
	OptickAPI_Color_Blue = 0xFF0000FF,
	OptickAPI_Color_BlueViolet = 0xFF8A2BE2,
	OptickAPI_Color_Brown = 0xFFA52A2A,
	OptickAPI_Color_BurlyWood = 0xFFDEB887,
	OptickAPI_Color_CadetBlue = 0xFF5F9EA0,
	OptickAPI_Color_Chartreuse = 0xFF7FFF00,
	OptickAPI_Color_Chocolate = 0xFFD2691E,
	OptickAPI_Color_Coral = 0xFFFF7F50,
	OptickAPI_Color_CornflowerBlue = 0xFF6495ED,
	OptickAPI_Color_Cornsilk = 0xFFFFF8DC,
	OptickAPI_Color_Crimson = 0xFFDC143C,
	OptickAPI_Color_Cyan = 0xFF00FFFF,
	OptickAPI_Color_DarkBlue = 0xFF00008B,
	OptickAPI_Color_DarkCyan = 0xFF008B8B,
	OptickAPI_Color_DarkGoldenRod = 0xFFB8860B,
	OptickAPI_Color_DarkGray = 0xFFA9A9A9,
	OptickAPI_Color_DarkGreen = 0xFF006400,
	OptickAPI_Color_DarkKhaki = 0xFFBDB76B,
	OptickAPI_Color_DarkMagenta = 0xFF8B008B,
	OptickAPI_Color_DarkOliveGreen = 0xFF556B2F,
	OptickAPI_Color_DarkOrange = 0xFFFF8C00,
	OptickAPI_Color_DarkOrchid = 0xFF9932CC,
	OptickAPI_Color_DarkRed = 0xFF8B0000,
	OptickAPI_Color_DarkSalmon = 0xFFE9967A,
	OptickAPI_Color_DarkSeaGreen = 0xFF8FBC8F,
	OptickAPI_Color_DarkSlateBlue = 0xFF483D8B,
	OptickAPI_Color_DarkSlateGray = 0xFF2F4F4F,
	OptickAPI_Color_DarkTurquoise = 0xFF00CED1,
	OptickAPI_Color_DarkViolet = 0xFF9400D3,
	OptickAPI_Color_DeepPink = 0xFFFF1493,
	OptickAPI_Color_DeepSkyBlue = 0xFF00BFFF,
	OptickAPI_Color_DimGray = 0xFF696969,
	OptickAPI_Color_DodgerBlue = 0xFF1E90FF,
	OptickAPI_Color_FireBrick = 0xFFB22222,
	OptickAPI_Color_FloralWhite = 0xFFFFFAF0,
	OptickAPI_Color_ForestGreen = 0xFF228B22,
	OptickAPI_Color_Fuchsia = 0xFFFF00FF,
	OptickAPI_Color_Gainsboro = 0xFFDCDCDC,
	OptickAPI_Color_GhostWhite = 0xFFF8F8FF,
	OptickAPI_Color_Gold = 0xFFFFD700,
	OptickAPI_Color_GoldenRod = 0xFFDAA520,
	OptickAPI_Color_Gray = 0xFF808080,
	OptickAPI_Color_Green = 0xFF008000,
	OptickAPI_Color_GreenYellow = 0xFFADFF2F,
	OptickAPI_Color_HoneyDew = 0xFFF0FFF0,
	OptickAPI_Color_HotPink = 0xFFFF69B4,
	OptickAPI_Color_IndianRed = 0xFFCD5C5C,
	OptickAPI_Color_Indigo = 0xFF4B0082,
	OptickAPI_Color_Ivory = 0xFFFFFFF0,
	OptickAPI_Color_Khaki = 0xFFF0E68C,
	OptickAPI_Color_Lavender = 0xFFE6E6FA,
	OptickAPI_Color_LavenderBlush = 0xFFFFF0F5,
	OptickAPI_Color_LawnGreen = 0xFF7CFC00,
	OptickAPI_Color_LemonChiffon = 0xFFFFFACD,
	OptickAPI_Color_LightBlue = 0xFFADD8E6,
	OptickAPI_Color_LightCoral = 0xFFF08080,
	OptickAPI_Color_LightCyan = 0xFFE0FFFF,
	OptickAPI_Color_LightGoldenRodYellow = 0xFFFAFAD2,
	OptickAPI_Color_LightGray = 0xFFD3D3D3,
	OptickAPI_Color_LightGreen = 0xFF90EE90,
	OptickAPI_Color_LightPink = 0xFFFFB6C1,
	OptickAPI_Color_LightSalmon = 0xFFFFA07A,
	OptickAPI_Color_LightSeaGreen = 0xFF20B2AA,
	OptickAPI_Color_LightSkyBlue = 0xFF87CEFA,
	OptickAPI_Color_LightSlateGray = 0xFF778899,
	OptickAPI_Color_LightSteelBlue = 0xFFB0C4DE,
	OptickAPI_Color_LightYellow = 0xFFFFFFE0,
	OptickAPI_Color_Lime = 0xFF00FF00,
	OptickAPI_Color_LimeGreen = 0xFF32CD32,
	OptickAPI_Color_Linen = 0xFFFAF0E6,
	OptickAPI_Color_Magenta = 0xFFFF00FF,
	OptickAPI_Color_Maroon = 0xFF800000,
	OptickAPI_Color_MediumAquaMarine = 0xFF66CDAA,
	OptickAPI_Color_MediumBlue = 0xFF0000CD,
	OptickAPI_Color_MediumOrchid = 0xFFBA55D3,
	OptickAPI_Color_MediumPurple = 0xFF9370DB,
	OptickAPI_Color_MediumSeaGreen = 0xFF3CB371,
	OptickAPI_Color_MediumSlateBlue = 0xFF7B68EE,
	OptickAPI_Color_MediumSpringGreen = 0xFF00FA9A,
	OptickAPI_Color_MediumTurquoise = 0xFF48D1CC,
	OptickAPI_Color_MediumVioletRed = 0xFFC71585,
	OptickAPI_Color_MidnightBlue = 0xFF191970,
	OptickAPI_Color_MintCream = 0xFFF5FFFA,
	OptickAPI_Color_MistyRose = 0xFFFFE4E1,
	OptickAPI_Color_Moccasin = 0xFFFFE4B5,
	OptickAPI_Color_NavajoWhite = 0xFFFFDEAD,
	OptickAPI_Color_Navy = 0xFF000080,
	OptickAPI_Color_OldLace = 0xFFFDF5E6,
	OptickAPI_Color_Olive = 0xFF808000,
	OptickAPI_Color_OliveDrab = 0xFF6B8E23,
	OptickAPI_Color_Orange = 0xFFFFA500,
	OptickAPI_Color_OrangeRed = 0xFFFF4500,
	OptickAPI_Color_Orchid = 0xFFDA70D6,
	OptickAPI_Color_PaleGoldenRod = 0xFFEEE8AA,
	OptickAPI_Color_PaleGreen = 0xFF98FB98,
	OptickAPI_Color_PaleTurquoise = 0xFFAFEEEE,
	OptickAPI_Color_PaleVioletRed = 0xFFDB7093,
	OptickAPI_Color_PapayaWhip = 0xFFFFEFD5,
	OptickAPI_Color_PeachPuff = 0xFFFFDAB9,
	OptickAPI_Color_Peru = 0xFFCD853F,
	OptickAPI_Color_Pink = 0xFFFFC0CB,
	OptickAPI_Color_Plum = 0xFFDDA0DD,
	OptickAPI_Color_PowderBlue = 0xFFB0E0E6,
	OptickAPI_Color_Purple = 0xFF800080,
	OptickAPI_Color_Red = 0xFFFF0000,
	OptickAPI_Color_RosyBrown = 0xFFBC8F8F,
	OptickAPI_Color_RoyalBlue = 0xFF4169E1,
	OptickAPI_Color_SaddleBrown = 0xFF8B4513,
	OptickAPI_Color_Salmon = 0xFFFA8072,
	OptickAPI_Color_SandyBrown = 0xFFF4A460,
	OptickAPI_Color_SeaGreen = 0xFF2E8B57,
	OptickAPI_Color_SeaShell = 0xFFFFF5EE,
	OptickAPI_Color_Sienna = 0xFFA0522D,
	OptickAPI_Color_Silver = 0xFFC0C0C0,
	OptickAPI_Color_SkyBlue = 0xFF87CEEB,
	OptickAPI_Color_SlateBlue = 0xFF6A5ACD,
	OptickAPI_Color_SlateGray = 0xFF708090,
	OptickAPI_Color_Snow = 0xFFFFFAFA,
	OptickAPI_Color_SpringGreen = 0xFF00FF7F,
	OptickAPI_Color_SteelBlue = 0xFF4682B4,
	OptickAPI_Color_Tan = 0xFFD2B48C,
	OptickAPI_Color_Teal = 0xFF008080,
	OptickAPI_Color_Thistle = 0xFFD8BFD8,
	OptickAPI_Color_Tomato = 0xFFFF6347,
	OptickAPI_Color_Turquoise = 0xFF40E0D0,
	OptickAPI_Color_Violet = 0xFFEE82EE,
	OptickAPI_Color_Wheat = 0xFFF5DEB3,
	OptickAPI_Color_White = 0xFFFFFFFF,
	OptickAPI_Color_WhiteSmoke = 0xFFF5F5F5,
	OptickAPI_Color_Yellow = 0xFFFFFF00,
	OptickAPI_Color_YellowGreen = 0xFF9ACD32,
};

enum OptickAPI_Filter
{
	OptickAPI_Filter_None,
	
	// CPU
	OptickAPI_Filter_AI,
	OptickAPI_Filter_Animation, 
	OptickAPI_Filter_Audio,
	OptickAPI_Filter_Debug,
	OptickAPI_Filter_Camera,
	OptickAPI_Filter_Cloth,
	OptickAPI_Filter_GameLogic,
	OptickAPI_Filter_Input,
	OptickAPI_Filter_Navigation,
	OptickAPI_Filter_Network,
	OptickAPI_Filter_Physics,
	OptickAPI_Filter_Rendering,
	OptickAPI_Filter_Scene,
	OptickAPI_Filter_Script,
	OptickAPI_Filter_Streaming,
	OptickAPI_Filter_UI,
	OptickAPI_Filter_VFX,
	OptickAPI_Filter_Visibility,
	OptickAPI_Filter_Wait,

	// IO
	OptickAPI_Filter_IO,

	// GPU
	OptickAPI_Filter_GPU_Cloth,
	OptickAPI_Filter_GPU_Lighting,
	OptickAPI_Filter_GPU_PostFX,
	OptickAPI_Filter_GPU_Reflections,
	OptickAPI_Filter_GPU_Scene,
	OptickAPI_Filter_GPU_Shadows,
	OptickAPI_Filter_GPU_UI,
	OptickAPI_Filter_GPU_VFX,
	OptickAPI_Filter_GPU_Water,
};

#define OPTICK_C_MAKE_CATEGORY(filter, color) ((((uint64_t)(1ull) << (filter + 32)) | (uint64_t)color))

typedef uint64_t OptickAPI_Category;

static const OptickAPI_Category OptickAPI_Category_None 			= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_None, OptickAPI_Color_Null);
static const OptickAPI_Category OptickAPI_Category_AI				= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_AI, OptickAPI_Color_Purple);
static const OptickAPI_Category OptickAPI_Category_Animation		= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Animation, OptickAPI_Color_LightSkyBlue);
static const OptickAPI_Category OptickAPI_Category_Audio			= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Audio, OptickAPI_Color_HotPink);
static const OptickAPI_Category OptickAPI_Category_Debug			= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Debug, OptickAPI_Color_Black);
static const OptickAPI_Category OptickAPI_Category_Camera			= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Camera, OptickAPI_Color_Black);
static const OptickAPI_Category OptickAPI_Category_Cloth			= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Cloth, OptickAPI_Color_DarkGreen);
static const OptickAPI_Category OptickAPI_Category_GameLogic		= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_GameLogic, OptickAPI_Color_RoyalBlue);
static const OptickAPI_Category OptickAPI_Category_Input			= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Input, OptickAPI_Color_Ivory);
static const OptickAPI_Category OptickAPI_Category_Navigation		= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Navigation, OptickAPI_Color_Magenta);
static const OptickAPI_Category OptickAPI_Category_Network			= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Network, OptickAPI_Color_Olive);
static const OptickAPI_Category OptickAPI_Category_Physics			= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Physics, OptickAPI_Color_LawnGreen);
static const OptickAPI_Category OptickAPI_Category_Rendering		= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Rendering, OptickAPI_Color_BurlyWood);
static const OptickAPI_Category OptickAPI_Category_Scene			= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Scene, OptickAPI_Color_RoyalBlue);
static const OptickAPI_Category OptickAPI_Category_Script			= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Script, OptickAPI_Color_Plum);
static const OptickAPI_Category OptickAPI_Category_Streaming		= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Streaming, OptickAPI_Color_Gold);
static const OptickAPI_Category OptickAPI_Category_UI				= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_UI, OptickAPI_Color_PaleTurquoise);
static const OptickAPI_Category OptickAPI_Category_VFX				= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_VFX, OptickAPI_Color_SaddleBrown);
static const OptickAPI_Category OptickAPI_Category_Visibility		= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Visibility, OptickAPI_Color_Snow);
static const OptickAPI_Category OptickAPI_Category_Wait				= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Wait, OptickAPI_Color_Tomato);
static const OptickAPI_Category OptickAPI_Category_WaitEmpty		= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_Wait, OptickAPI_Color_White);
// IO
static const OptickAPI_Category OptickAPI_Category_IO				= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_IO, OptickAPI_Color_Khaki);
// GPU
static const OptickAPI_Category OptickAPI_Category_GPU_Cloth		= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_GPU_Cloth, OptickAPI_Color_DarkGreen);
static const OptickAPI_Category OptickAPI_Category_GPU_Lighting		= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_GPU_Lighting, OptickAPI_Color_Khaki);
static const OptickAPI_Category OptickAPI_Category_GPU_PostFX		= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_GPU_PostFX, OptickAPI_Color_Maroon);
static const OptickAPI_Category OptickAPI_Category_GPU_Reflections	= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_GPU_Reflections, OptickAPI_Color_CadetBlue);
static const OptickAPI_Category OptickAPI_Category_GPU_Scene		= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_GPU_Scene, OptickAPI_Color_RoyalBlue);
static const OptickAPI_Category OptickAPI_Category_GPU_Shadows		= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_GPU_Shadows, OptickAPI_Color_LightSlateGray);
static const OptickAPI_Category OptickAPI_Category_GPU_UI			= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_GPU_UI, OptickAPI_Color_PaleTurquoise);
static const OptickAPI_Category OptickAPI_Category_GPU_VFX			= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_GPU_VFX, OptickAPI_Color_SaddleBrown);
static const OptickAPI_Category OptickAPI_Category_GPU_Water		= OPTICK_C_MAKE_CATEGORY(OptickAPI_Filter_GPU_Water, OptickAPI_Color_SteelBlue);


#if USE_OPTICK
	OPTICK_API void OptickAPI_SetAllocator(OptickAPI_AllocateFn allocateFn, OptickAPI_DeallocateFn deallocateFn, OptickAPI_InitThreadCb initThreadCb);
	OPTICK_API void OptickAPI_RegisterThread(const char* inThreadName, uint16_t inThreadNameLength);

	OPTICK_API uint64_t OptickAPI_CreateEventDescription(const char* inFunctionName, const char* inFileName, uint32_t inFileLine, OptickAPI_Category category);
	OPTICK_API uint64_t OptickAPI_PushEvent(uint64_t inEventDescription);
	OPTICK_API void OptickAPI_PopEvent(uint64_t inEventData);
	
	OPTICK_API void OptickAPI_NextFrame();

	OPTICK_API void OptickAPI_StartCapture();
	OPTICK_API void OptickAPI_StopCapture(const char* inFileName, uint16_t inFileNameLength);
	OPTICK_API void OptickAPI_Shutdown();

	OPTICK_API void OptickAPI_AttachTag_String(uint64_t inEventDescription, const char* inValue, uint16_t intValueLength);
	OPTICK_API void OptickAPI_AttachTag_Int32(uint64_t inEventDescription, int inValue);
	OPTICK_API void OptickAPI_AttachTag_Float(uint64_t inEventDescription, float inValue);
	OPTICK_API void OptickAPI_AttachTag_UInt32(uint64_t inEventDescription, uint32_t inValue);
	OPTICK_API void OptickAPI_AttachTag_UInt64(uint64_t inEventDescription, uint64_t inValue);
	OPTICK_API void OptickAPI_AttachTag_Point(uint64_t inEventDescription, float x, float y, float z);
#else
	inline void OptickAPI_SetAllocator(OptickAPI_AllocateFn allocateFn, OptickAPI_DeallocateFn deallocateFn, OptickAPI_InitThreadCb initThreadCb) {}
	inline void OptickAPI_RegisterThread(const char* inThreadName, uint16_t inThreadNameLength) {}
	inline uint64_t OptickAPI_CreateEventDescription(const char* inFunctionName, const char* inFileName, uint32_t inFileLine, OptickAPI_Category category) { return 0; }
	inline uint64_t OptickAPI_PushEvent(uint64_t inEventDescription) { return 0; }
    inline void OptickAPI_PopEvent(uint64_t inEventData) {}
	inline void OptickAPI_NextFrame() {}
	inline void OptickAPI_StartCapture() {}
	inline void OptickAPI_StopCapture(const char* inFileName, uint16_t inFileNameLength) {}
	inline void OptickAPI_Shutdown() {}
	inline void OptickAPI_AttachTag_String(uint64_t inEventDescription, const char* inValue, uint16_t intValueLength) {}
	inline void OptickAPI_AttachTag_Int(uint64_t inEventDescription, int inValue) {}
	inline void OptickAPI_AttachTag_Float(uint64_t inEventDescription, float inValue) {}
	inline void OptickAPI_AttachTag_Int32(uint64_t inEventDescription, uint32_t inValue) {}
	inline void OptickAPI_AttachTag_UInt64(uint64_t inEventDescription, uint64_t inValue) {}
	inline void OptickAPI_AttachTag_Point(uint64_t inEventDescription, float x, float y, float z) {}
#endif

#ifdef __cplusplus
} // extern "C"
#endif