#include <cstring>
#include <mutex>
#include <thread>

#include "Event.h"
#include "Core.h"
#include "EventDescriptionBoard.h"

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription* EventDescription::Create(const char* eventName, const char* fileName, const unsigned long fileLine, const unsigned long eventColor /*= Color::Null*/, const unsigned long filter /*= 0*/)
{
	return EventDescriptionBoard::Get().CreateDescription(eventName, fileName, fileLine, eventColor, filter);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription* EventDescription::CreateShared(const char* eventName, const char* fileName, const unsigned long fileLine, const unsigned long eventColor /*= Color::Null*/, const unsigned long filter /*= 0*/)
{
	return EventDescriptionBoard::Get().CreateSharedDescription(eventName, fileName, fileLine, eventColor, filter);
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
void Event::Push(const char* name)
{
	if (EventStorage* storage = Core::storage)
	{
		EventDescription* desc = EventDescription::CreateShared(name);
		Push(*desc);
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Event::Push(const EventDescription& description)
{
	if (EventStorage* storage = Core::storage)
	{
		EventData& result = storage->NextEvent();
		result.description = &description;
		result.Start();
		storage->pushPopEventStack[storage->pushPopEventStackIndex++] = &result;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Event::Pop()
{
	if (EventStorage* storage = Core::storage)
		if (storage->pushPopEventStackIndex > 0)
			storage->pushPopEventStack[--storage->pushPopEventStackIndex]->Stop();
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
		storage->tagFloatBuffer.Add(TagFloat(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, int32_t val)
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagS32Buffer.Add(TagS32(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, uint32_t val)
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagU32Buffer.Add(TagU32(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, uint64_t val)
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagU64Buffer.Add(TagU64(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, float val[3])
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagPointBuffer.Add(TagPoint(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Tag::Attach(const EventDescription& description, const char* val)
{
	if (EventStorage* storage = Core::storage)
	{
		storage->tagStringBuffer.Add(TagString(description, val));
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream & operator<<(OutputDataStream &stream, const EventDescription &ob)
{
	byte flags = 0;
	return stream << ob.name << ob.file << ob.line << ob.filter << ob.color << (float)0.0f << flags;
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
