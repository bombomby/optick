#pragma once

#include <array>
#include <string>
#include <unordered_map>

#include "Common.h"
#include "MemoryPool.h"


namespace Brofiler
{
	// We expect to have 1k unique strings going through Brofiler at once
	// Collision chances are 1 in 10 trillion (odds of a meteor landing on your house)
	// We should be quite safe here :)
	// https://preshing.com/20110504/hash-collision-probabilities/
	// If your house is hit by a meteor - feel free to add salt to the hashing function and wait for another strike
	// E.g. size_t operator()(const Brofiler::StringHash& x) const { return x.hash ^ std::hash<size_t>()(42); }
	struct StringHash
	{
		size_t hash;

		StringHash(size_t h) : hash(h) {}
		StringHash(const char* str) : hash(std::hash<const char*>()(str)) {}

		bool operator==(const StringHash& other) const { return hash == other.hash; }
		bool operator<(const StringHash& other) const { return hash < other.hash; }
	};

	//class StringTable
	//{
	//public:
	//	typedef uint32 ID;
	//	typedef uint32 StringHash;

	//private:
	//	// Stores copies for all the unique strings
	//	MemoryBuffer stringBuffer;

	//	// Hash => ID mapping
	//	typedef std::unordered_map<StringHash, ID> StringMap;
	//	StringMap stringMap;

	//	// Number of entries in the table
	//	uint32 count;

	//public:

	//	ID Add(const char* str)
	//	{
	//		StringHash hash = std::hash<const char*>()(str);
	//		
	//		std::pair<StringMap::iterator, bool> result = stringMap.insert(hash);
	//		if (!result.second)
	//			return result.first->second;

	//		 size = std::min(strlen(str), 255);

	//		// Copy string
	//		stringBuffer.Push((uint8)size);
	//		stringBuffer.Push((uint8*)str, size);

	//		// Assign a new ID
	//		result.first->second = count++;

	//		return count;
	//	}
	//};
}

// Overriding default hash function
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