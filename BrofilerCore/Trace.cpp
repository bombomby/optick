#include "Trace.h"

namespace Brofiler
{
//////////////////////////////////////////////////////////////////////////
CaptureStatus::Type Trace::Start(int mode, const ThreadList& threads)
{
	Core::Get().DumpProgress("Starting");

	BRO_UNUSED(mode);
	activeThreadsIDs.clear();
	for(auto it = threads.begin(); it != threads.end(); ++it)
	{
		ThreadEntry* entry = *it;
		if (entry->isAlive)
		{
			activeThreadsIDs.insert(entry->description.threadID);
		}
	}

	return CaptureStatus::OK;
}
//////////////////////////////////////////////////////////////////////////
bool Trace::Stop()
{
	activeThreadsIDs.clear();
	return true;
}
//////////////////////////////////////////////////////////////////////////
#if !BRO_ENABLE_TRACING
Trace* Trace::Get()
{
    return nullptr;
}
#endif //!BRO_ENABLE_TRACING
}
