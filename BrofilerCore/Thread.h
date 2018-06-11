#pragma once
#include "Common.h"

namespace Brofiler
{
#if MT_PLATFORM_WINDOWS

#if MT_PTR64
#define ReadTeb(offset) __readgsqword(offset);
#else
#define ReadTeb(offset) __readfsdword(offset);
#endif

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Returns current Thread Environment Block (Extremely fast approach to get Thread Unique ID)
	BRO_INLINE const void* GetThreadUniqueID()
	{
		return (void*)ReadTeb(MW_STACK_BASE_OFFSET);
	}
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#undef ReadTeb

#else

#error implement GetThreadUniqueID

#endif
}
