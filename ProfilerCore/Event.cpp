#include "Event.hpp"
#include <cstring>
#include <windows.h>
#include "Core.h"
#include "Thread.h"
#include "EventDescriptionBoard.h"

namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription::EventDescription(const char* eventName, const char* fileName, const uint32 fileLine, const uint32 eventColor /*= Color::Null */) 
	: name(eventName), file(fileName), line(fileLine), color(eventColor), isSampling(false)
{
	index = EventDescriptionBoard::Get().Register(*this);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription::EventDescription()
	: name(""), file(""), line(0), color(0), isSampling(false)
{
	index = EventDescriptionBoard::Get().Register(*this);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription& EventDescription::operator=(const EventDescription&)
{
	BRO_FAILED("It is pointless to copy EventDescription. Please, check you logic!"); return *this; 
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Event::Event(const EventDescription& description)
{
	if (GetThreadUniqueID() == Core::frame.threadUniqueID)
	{
		data = &Core::frame.NextEvent();
		data->description = &description;
		data->Start();

		if (description.isSampling)
		{
			InterlockedIncrement(&Core::frame.isSampling);
		}
	} 
	else
	{
		data = 0;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Event::~Event()
{
	if (data)
	{
		data->Stop();

		if (data->description->isSampling)
		{
			InterlockedDecrement(&Core::frame.isSampling);
		}
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream & operator<<(OutputDataStream &stream, const EventDescription &ob)
{
	byte flags = (ob.isSampling ? 0x1 : 0);
	return stream << ob.name << ob.file << ob.line << ob.color << flags;
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
Category::Category(const EventDescription& description) : Event(description)
{
	if (data)
	{
		Core::frame.RegisterCategory(*data);
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}