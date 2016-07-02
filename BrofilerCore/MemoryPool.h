#pragma once
#include "Common.h"
#include <new>

namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<class T, uint SIZE>
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
			_aligned_free(next);
			next = 0;
			prev = 0;
		}
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<class T, uint SIZE = 16>
class MemoryPool
{
	typedef MemoryChunk<T, SIZE> Chunk;
	Chunk root;

	Chunk* chunk;
	uint index;

	uint chunkCount;

	BRO_INLINE void AddChunk()
	{
		index = 0;
		if (!chunk->next)
		{
			void* ptr = _aligned_malloc(sizeof(Chunk), BRO_CACHE_LINE_SIZE);
			chunk->next = new (ptr) Chunk();
			chunk->next->prev = chunk;
		}
		chunk = chunk->next;

		++chunkCount;
	}
public:
	MemoryPool() : index(0), chunk(&root), chunkCount(1)	{}

	BRO_INLINE T& Add()
	{
		if (index >= SIZE)
			AddChunk();

		return chunk->data[index++];
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
				_aligned_free(root.next);
				root.next = 0;
			}
		}

		index = 0;
		chunk = &root;
	}

	template<class Func>
	void ForEach(Func func) const
	{
		for (const Chunk* it = &root; it != chunk; it = it->next)
			for (uint i = 0; i < SIZE; ++i)
				func(it->data[i]);

		for (uint i = 0; i < index; ++i)
			func(chunk->data[i]);
	}

	template<class Func>
	void ForEach(Func func)
	{
		for (Chunk* it = &root; it != chunk; it = it->next)
			for (uint i = 0; i < SIZE; ++i)
				func(it->data[i]);

		for (uint i = 0; i < index; ++i)
			func(chunk->data[i]);
	}

	template<class Func>
	void ForEachChunk(Func func) const
	{
		for (const Chunk* it = &root; it != chunk; it = it->next)
			for (uint i = 0; i < SIZE; ++i)
				func(it->data, SIZE);

		for (uint i = 0; i < index; ++i)
			func(chunk->data, index);
	}

	void ToArray(T* destination) const
	{
		uint curIndex = 0;

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