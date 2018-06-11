#pragma once
#include "Common.h"
#include <new>

namespace Brofiler
{
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	template<class T, uint32 SIZE>
	struct MemoryChunk
	{
		BRO_ALIGN_CACHE T data[SIZE];
		MemoryChunk* next;
		MemoryChunk* prev;

		MemoryChunk() : next(0), prev(0) {}

		~MemoryChunk()
		{
			if (next)
			{
				next->~MemoryChunk();
				MT::Memory::Free(next);
				next = 0;
				prev = 0;
			}
		}
	};
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	template<class T, uint32 SIZE = 16>
	class MemoryPool
	{
		typedef MemoryChunk<T, SIZE> Chunk;
		Chunk root;

		Chunk* chunk;
		uint32 index;

		uint32 chunkCount;

		BRO_INLINE void AddChunk()
		{
			index = 0;
			if (!chunk->next)
			{
				void* ptr = MT::Memory::Alloc(sizeof(Chunk), BRO_CACHE_LINE_SIZE);
				chunk->next = new (ptr) Chunk();
				chunk->next->prev = chunk;
			}
			chunk = chunk->next;

			++chunkCount;
		}
	public:
		MemoryPool() : chunk(&root), index(0), chunkCount(1) {}

		BRO_INLINE T& Add()
		{
			if (index >= SIZE)
				AddChunk();

			return chunk->data[index++];
		}

		BRO_INLINE T* TryAdd(int count)
		{
			if (index + count <= SIZE)
			{
				T* res = &chunk->data[index];
				index += count;
				return res;
			}

			return nullptr;
		}

		BRO_INLINE T* Back()
		{
			if (index > 0)
				return &chunk->data[index - 1];

			if (chunk->prev != nullptr)
				return &chunk->prev->data[SIZE - 1];

			return nullptr;
		}

		BRO_INLINE size_t Size() const
		{
			size_t count = 0;

			for (const Chunk* it = &root; it != chunk; it = it->next)
				count += SIZE;

			return count + index;
		}

		BRO_INLINE bool IsEmpty() const
		{
			return chunk == &root && index == 0;
		}

		BRO_INLINE void Clear(bool preserveMemory = true)
		{
			if (!preserveMemory)
			{
				if (root.next)
				{
					root.next->~MemoryChunk();
					MT::Memory::Free(root.next);
					root.next = 0;
				}
			}

			index = 0;
			chunk = &root;
		}

		class const_iterator
		{
			void advance()
			{
				if (chunkIndex < SIZE - 1)
				{
					++chunkIndex;
				}
				else
				{
					chunkPtr = chunkPtr->next;
					chunkIndex = 0;
				}
			}
		public:
			typedef const_iterator self_type;
			typedef T value_type;
			typedef T& reference;
			typedef T* pointer;
			typedef int difference_type;
			const_iterator(const Chunk* ptr, size_t index) : chunkPtr(ptr), chunkIndex(index) { }
			self_type operator++()
			{
				self_type i = *this;
				advance();
				return i;
			}
			self_type operator++(int junk)
			{
				advance();
				return *this;
			}
			reference operator*() { return (reference)chunkPtr->data[chunkIndex]; }
			const pointer operator->() { return &chunkPtr->data[chunkIndex]; }
			bool operator==(const self_type& rhs) { return (chunkPtr == rhs.chunkPtr) && (chunkIndex == rhs.chunkIndex); }
			bool operator!=(const self_type& rhs) { return (chunkPtr != rhs.chunkPtr) || (chunkIndex != rhs.chunkIndex); }
		private:
			const Chunk* chunkPtr;
			size_t chunkIndex;
		};

		const_iterator begin() const
		{
			return const_iterator(&root, 0);
		}

		const_iterator end() const
		{
			return const_iterator(chunk, index);
		}

		template<class Func>
		void ForEach(Func func) const
		{
			for (const Chunk* it = &root; it != chunk; it = it->next)
				for (uint32 i = 0; i < SIZE; ++i)
					func(it->data[i]);

			for (uint32 i = 0; i < index; ++i)
				func(chunk->data[i]);
		}

		template<class Func>
		void ForEach(Func func)
		{
			for (Chunk* it = &root; it != chunk; it = it->next)
				for (uint32 i = 0; i < SIZE; ++i)
					func(it->data[i]);

			for (uint32 i = 0; i < index; ++i)
				func(chunk->data[i]);
		}

		template<class Func>
		void ForEachChunk(Func func) const
		{
			for (const Chunk* it = &root; it != chunk; it = it->next)
				for (uint32 i = 0; i < SIZE; ++i)
					func(it->data, SIZE);

			for (uint32 i = 0; i < index; ++i)
				func(chunk->data, index);
		}

		void ToArray(T* destination) const
		{
			uint32 curIndex = 0;

			for (const Chunk* it = &root; it != chunk; it = it->next)
			{
				memcpy(&destination[curIndex], it->data, sizeof(T) * SIZE);
				curIndex += SIZE;
			}

			if (index > 0)
			{
				memcpy(&destination[curIndex], chunk->data, sizeof(T) * index);
			}
		}
	};
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
