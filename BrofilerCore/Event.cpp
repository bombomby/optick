#include <cstring>
#include <mutex>
#include <thread>

#include "Event.h"
#include "Core.h"
#include "EventDescriptionBoard.h"

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription* EventDescription::Create(const char* eventName, const char* fileName, const unsigned long fileLine, const unsigned long eventColor /*= Color::Null*/, const unsigned long filter /*= 0*/, float budget /*= 0.0f*/)
{
	static std::mutex creationLock;
	std::lock_guard<std::mutex> lock(creationLock);

	EventDescription* result = EventDescriptionBoard::Get().CreateDescription();
	result->name = eventName;
	result->file = fileName;
	result->line = fileLine;
	result->color = eventColor;
	result->budget = budget;
	result->filter = filter;
	return result;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription::EventDescription() : name(""), file(""), line(0), color(0)
{
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription& EventDescription::operator=(const EventDescription&)
{
	BRO_FAILED("It is pointless to copy EventDescription. Please, check you logic!"); return *this; 
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventData* Event::Start(const EventDescription& description)
{
	EventData* result = nullptr;

	if (EventStorage* storage = Core::storage)
	{
		result = &storage->NextEvent();
		result->description = &description;
		result->Start();
	}
	return result;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Event::Stop(EventData& data)
{
	data.Stop();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void FiberSyncData::AttachToThread(EventStorage* storage, uint64_t threadId)
{
	if (storage)
	{
		FiberSyncData& data = storage->fiberSyncBuffer.Add();
		data.Start();
		data.finish = LLONG_MAX;
		data.threadId = threadId;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void FiberSyncData::DetachFromThread(EventStorage* storage)
{
	if (storage)
	{
		if (FiberSyncData* syncData = storage->fiberSyncBuffer.Back())
		{
			syncData->Stop();
		}
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, float val)
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagFloatBuffer.Push(TagFloat(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, int32_t val)
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagS32Buffer.Push(TagS32(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, uint64_t val)
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagU64Buffer.Push(TagU64(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, float val[3])
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagPointBuffer.Push(TagPoint(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, const char* val)
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagStringBuffer.Push(TagString(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream & operator<<(OutputDataStream &stream, const EventDescription &ob)
{
	byte flags = 0;
	return stream << ob.name << ob.file << ob.line << ob.filter << ob.color << ob.budget << flags;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const EventTime& ob)
{
	return stream << ob.start << ob.finish;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const EventData& ob)
{
	return stream << (EventTime)(ob) << ob.description->index;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const SyncData& ob)
{
	return stream << (EventTime)(ob) << ob.core << ob.reason << ob.newThreadId;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& stream, const FiberSyncData& ob)
{
	return stream << (EventTime)(ob) << ob.threadId;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Category::Category(const EventDescription& description) : Event(description)
{
	if (data)
	{
		if (EventStorage* storage = Core::storage)
		{
			storage->RegisterCategory(*data);
		}
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
