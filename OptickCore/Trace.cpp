#include "Trace.h"

namespace Optick
{
//////////////////////////////////////////////////////////////////////////
CaptureStatus::Type Trace::Start(int mode, const ThreadList& threads)
{
	Core::Get().DumpProgress("Starting");

	OPTICK_UNUSED(mode);
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
#if !OPTICK_ENABLE_TRACING
Trace* Trace::Get()
{
    return nullptr;
}
#endif //!OPTICK_ENABLE_TRACING
}
