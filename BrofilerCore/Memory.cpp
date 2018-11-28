#include <stdlib.h>
#include <new>
#include "Memory.h"
#include "Platform.h"

#if defined(BRO_PLATFORM_OSX)
#include <mm_malloc.h>
#endif //BRO_PLATFORM_OSX

namespace Brofiler
{
	void* Memory::Alloc(size_t size, size_t align)
	{
		return _mm_malloc(size, align);
	}

	void Memory::Free(void* p)
	{
		_mm_free(p);
	}
}
