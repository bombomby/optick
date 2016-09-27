#include <xmmintrin.h>

namespace MT
{
	struct Memory
	{
		static void* Alloc(size_t size, size_t align);
		static void Free(void* p);
	};

	struct Diagnostic
	{
		static void ReportAssert(const char* condition, const char* description, const char* sourceFile, int sourceLine);
	};


	void* Memory::Alloc(size_t size, size_t align)
	{
		return _mm_malloc(size, align);
	}

	void Memory::Free(void* p)
	{
		_mm_free(p);
	}

	void Diagnostic::ReportAssert(const char*, const char*, const char*, int)
	{
		__debugbreak();
	}
}
