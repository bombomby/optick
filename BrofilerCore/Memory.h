#pragma once

namespace Brofiler
{
	class Memory
	{
	public:
		static void* Alloc(size_t size, size_t align = 16);
		static void Free(void* p);

		template<class T>
		static T* New()
		{
			return new (Memory::Alloc(sizeof(T))) T();
		}
	};
}
