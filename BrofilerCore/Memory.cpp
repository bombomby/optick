#include <new>
#include "Memory.h"

namespace Brofiler
{
	void* Memory::Alloc(size_t size, size_t /*align*/)
	{
		return operator new (size);
	}

	void Memory::Free(void* p)
	{
		delete p;
	}
}
