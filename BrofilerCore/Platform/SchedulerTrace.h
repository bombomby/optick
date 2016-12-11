#pragma once
#include "Core.h"
#include <unordered_map>
#include "ThreadsEnumerator.h"


namespace Brofiler
{
	struct SchedulerTrace
	{
		enum Mode
		{
			SWITCH_CONTEXTS = 0x01,
			STACK_WALK = 0x02,
			ALL = 0xFFFFFFFF
		};

		std::vector<Brofiler::ThreadInfo> allProcessThreads;
		std::unordered_map<uint64, ThreadEntry*> activeThreadsIDs;

		virtual CaptureStatus::Type Start(int mode, const ThreadList& threads, bool autoAddUnknownThreads);
		virtual bool Stop();

		virtual ~SchedulerTrace() {};
		static SchedulerTrace* Get();
	};
}