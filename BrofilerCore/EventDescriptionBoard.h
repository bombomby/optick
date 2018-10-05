#pragma once

#include <mutex>

#include "Common.h"
#include "MemoryPool.h"
#include "Serialization.h"
#include "StringHash.h"

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct EventDescription;
typedef MemoryPool<EventDescription, 4096> EventDescriptionList;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class EventDescriptionBoard
{
	// List of stored Event Descriptions
	EventDescriptionList boardDescriptions;

	// Shared Descriptions
	typedef std::unordered_map<StringHash, EventDescription*> DescriptionMap;
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

	friend OutputDataStream& operator << ( OutputDataStream& stream, const EventDescriptionBoard& ob);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator << ( OutputDataStream& stream, const EventDescriptionBoard& ob);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}


