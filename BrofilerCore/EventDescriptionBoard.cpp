#include "EventDescriptionBoard.h"
#include "Event.h"

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
static MT::Mutex& GetBoardLock()
{
	// Initialize as static local variable to prevent problems with static initialization order
	static MT::Mutex lock;
	return lock;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescriptionBoard& EventDescriptionBoard::Get()
{ 
	return instance;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void EventDescriptionBoard::SetSamplingFlag( int index, bool flag )
{ 
	MT::ScopedGuard guard(GetBoardLock());
	BRO_VERIFY(index < (int)board.size(), "Invalid EventDescription index", return);

	if (index < 0)
	{
		for(auto it = board.begin(); it != board.end(); ++it)
		{
			EventDescription* desc = *it;
			desc->isSampling = flag;
		}
	} else
	{
		board[index]->isSampling = flag;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool EventDescriptionBoard::HasSamplingEvents() const
{
	MT::ScopedGuard guard(GetBoardLock());
	for(auto it = board.begin(); it != board.end(); ++it)
	{
		EventDescription* desc = *it;
		if (desc->isSampling)
		{
			return true;
		}
	}

	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const std::vector<EventDescription*>& EventDescriptionBoard::GetEvents() const
{
	return board;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescriptionBoard::~EventDescriptionBoard()
{
	for(auto it = board.begin(); it != board.end(); ++it)
	{
		EventDescription* desc = *it;
		delete desc;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
EventDescription* EventDescriptionBoard::CreateDescription()
{
	MT::ScopedGuard guard(GetBoardLock());
	EventDescription* desc = new EventDescription();
	desc->index = (unsigned long)board.size();
	board.push_back(desc);
	return desc;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void EventDescriptionBoard::DeleteAllDescriptions()
{
	MT::ScopedGuard guard(GetBoardLock());
	for (auto it = board.begin(); it != board.end(); ++it)
	{
		EventDescription* desc = *it;
		delete desc;
	}
	board.clear();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator << ( OutputDataStream& stream, const EventDescriptionBoard& ob)
{
	MT::ScopedGuard guard(GetBoardLock());
	const std::vector<EventDescription*>& events = ob.GetEvents();

	stream << (uint32)events.size();

	for(auto it = events.begin(); it != events.end(); ++it)
	{
		const EventDescription* desc = *it;
		stream << *desc;
	}

	return stream;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Brofiler::EventDescriptionBoard EventDescriptionBoard::instance;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
