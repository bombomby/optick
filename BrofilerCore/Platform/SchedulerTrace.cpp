#include "SchedulerTrace.h"

namespace Brofiler
{
	//////////////////////////////////////////////////////////////////////////
	CaptureStatus::Type SchedulerTrace::Start(int mode, const ThreadList& threads, bool autoAddUnknownThreads)
	{
		if ((mode & STACK_WALK) != 0 && autoAddUnknownThreads != false)
		{
			Core::Get().DumpProgress("Enumerate threads");

			if (EnumerateAllThreads(allProcessThreads))
			{
				for (auto it = allProcessThreads.begin(); it != allProcessThreads.end(); ++it)
				{
					const Brofiler::ThreadInfo& threadInfo = *it;
					if (Core::Get().IsRegistredThread(threadInfo.id))
					{
						continue;
					}

					// TODO: threadInfo.name.c_str() - is pointer to temporary memory!!!!
					const char* threadName = threadInfo.name.c_str();

					if (threadInfo.name.empty() || threadInfo.name.length() < 1)
					{
						threadName = "Unknown";
					}

					ThreadDescription threadDesc(threadName, threadInfo.id, threadInfo.fromOtherProcess);
					Core::Get().RegisterThread(threadDesc, nullptr);
				}
			}

			Core::Get().DumpProgress("Starting");
		}

		BRO_UNUSED(mode);
		activeThreadsIDs.clear();
		for (auto it = threads.begin(); it != threads.end(); ++it)
		{
			ThreadEntry* entry = *it;
			if (entry->isAlive && !entry->description.fromOtherProcess)
			{
				activeThreadsIDs.insert(std::make_pair(entry->description.threadID.AsUInt64(), entry));
			}
		}

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