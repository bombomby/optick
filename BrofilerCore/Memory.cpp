#include <stdlib.h>
#include <mm_malloc.h>
#include <new>
#include "Memory.h"

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
