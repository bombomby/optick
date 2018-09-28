#pragma once
#include "Core.h"
#include <unordered_set>

namespace Brofiler
{
	struct SchedulerTrace
	{
		enum Mode
		{
			SWITCH_CONTEXTS =	1 << 0,
			STACK_WALK =		1 << 1,
			SYS_CALLS =			1 << 2,
			ALL =				0xFFFFFFFF
		};

		std::unordered_set<uint64> activeThreadsIDs;

		virtual CaptureStatus::Type Start(int mode, const ThreadList& threads, bool autoAddUnknownThreads);
		virtual bool Stop();

		virtual ~SchedulerTrace() {};
		static SchedulerTrace* Get();
	};
}