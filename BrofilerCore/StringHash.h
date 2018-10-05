#pragma once

#include <array>
#include <string>
#include <unordered_map>

#include "Common.h"
#include "CityHash.h"
#include "MemoryPool.h"

namespace Brofiler
{
	// We expect to have 1k unique strings going through Brofiler at once
	// The chances to hit a collision are 1 in 10 trillion (odds of a meteor landing on your house)
	// We should be quite safe here :)
	// https://preshing.com/20110504/hash-collision-probabilities/
	// Feel free to add a seed and wait for another strike if armageddon starts
	struct StringHash
	{
		uint64 hash;

		StringHash(size_t h) : hash(h) {}
		StringHash(const char* str) : hash(CityHash64(str, (int)strlen(str))) {}

		bool operator==(const StringHash& other) const { return hash == other.hash; }
		bool operator<(const StringHash& other) const { return hash < other.hash; }
	};
}

// Overriding default hash function to return hash value directly
namespace std
{
	template<>
	struct hash<Brofiler::StringHash>
	{
		size_t operator()(const Brofiler::StringHash& x) const
		{
			return x.hash;
		}
	};
}