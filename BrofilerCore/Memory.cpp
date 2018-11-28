#include <stdlib.h>
#include <new>
#include "Memory.h"
#include "Platform.h"

#include <xmmintrin.h>

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
