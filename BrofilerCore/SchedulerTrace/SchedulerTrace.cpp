#include "SchedulerTrace.h"

namespace Brofiler
{
//////////////////////////////////////////////////////////////////////////
CaptureStatus::Type SchedulerTrace::Start(int mode, const ThreadList& threads)
{
	BRO_UNUSED(mode);
	activeThreadsIDs.clear();
	for each (const ThreadEntry* entry in threads)
		if (entry->isAlive)
			activeThreadsIDs.insert(entry->description.threadID.AsUInt64());

	return CaptureStatus::OK;
}
//////////////////////////////////////////////////////////////////////////
bool SchedulerTrace::Stop()
{
	activeThreadsIDs.clear();
	return true;
}
//////////////////////////////////////////////////////////////////////////
}