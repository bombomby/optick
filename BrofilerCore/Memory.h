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

		template<class T, class P1>
		static T* New(P1 p1)
		{
			return new (Memory::Alloc(sizeof(T))) T(p1);
		}

		template<class T, class P1, class P2>
		static T* New(P1 p1, P2 p2)
		{
			return new (Memory::Alloc(sizeof(T))) T(p1, p2);
		}

		template<class T>
		static void Delete(T* p)
		{
			if (p)
			{
				p->~T();
				Free(p);
			}
		}
	};
}
