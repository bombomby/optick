#include <new>
#include "Memory.h"

namespace Brofiler
{
	void* Memory::Alloc(size_t size, size_t align)
	{
		return _aligned_malloc(size, align);
	}

	void Memory::Free(void* p)
	{
		_aligned_free(p);
	}
}
