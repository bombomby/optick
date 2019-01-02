#pragma once

#include "CityHash.h"
#include "Serialization.h"

#include <string>
#include <unordered_map>


// We expect to have 1k unique strings going through Brofiler at once
// The chances to hit a collision are 1 in 10 trillion (odds of a meteor landing on your house)
// We should be quite safe here :)
// https://preshing.com/20110504/hash-collision-probabilities/
// Feel free to add a seed and wait for another strike if armageddon starts
struct BroStringHash
{
	uint64 hash;

	BroStringHash(size_t h) : hash(h) {}
	BroStringHash(const char* str) : hash(CityHash64(str, (int)strlen(str))) {}

	bool operator==(const BroStringHash& other) const { return hash == other.hash; }
	bool operator<(const BroStringHash& other) const { return hash < other.hash; }
};

// Overriding default hash function to return hash value directly
namespace std
{
	template<>
	struct hash<BroStringHash>
	{
		size_t operator()(const BroStringHash& x) const
		{
			return x.hash;
		}
	};
}

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<class T>
struct TagData
{
	const EventDescription* description;
	int64_t timestamp;
	T data;
	TagData() {}
	TagData(const EventDescription& desc, T d) : description(&desc), timestamp(Brofiler::GetHighPrecisionTime()), data(d) {}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream &operator<<(OutputDataStream &stream, const EventDescription &ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const EventTime& ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const EventData& ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const SyncData& ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const FiberSyncData& ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<class T>
OutputDataStream& operator<<(OutputDataStream& stream, const TagData<T>& ob)
{
	return stream << ob.timestamp << ob.description->index << ob.data;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Board
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef MemoryPool<EventDescription, 4096> EventDescriptionList;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class EventDescriptionBoard
{
	// List of stored Event Descriptions
	EventDescriptionList boardDescriptions;

	// Shared Descriptions
	typedef std::unordered_map<BroStringHash, EventDescription*> DescriptionMap;
	DescriptionMap sharedDescriptions;
	MemoryBuffer<64 * 1024> sharedNames;
	std::mutex sharedLock;

	// Singleton instance of the board
	static EventDescriptionBoard instance;
public:
	EventDescription* CreateDescription(const char* name, const char* file = nullptr, uint32_t line = 0, uint32_t color = Color::Null, uint32_t filter = 0);
	EventDescription* CreateSharedDescription(const char* name, const char* file = nullptr, uint32_t line = 0, uint32_t color = Color::Null, uint32_t filter = 0);

	static EventDescriptionBoard& Get();

	const EventDescriptionList& GetEvents() const;

	friend OutputDataStream& operator << (OutputDataStream& stream, const EventDescriptionBoard& ob);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator << (OutputDataStream& stream, const EventDescriptionBoard& ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}