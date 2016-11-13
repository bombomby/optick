#pragma once
#include "Core.h"
#include <unordered_set>

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

		std::unordered_set<uint64> activeThreadsIDs;

		virtual CaptureStatus::Type Start(int mode, const ThreadList& threads);
		virtual bool Stop();

		virtual ~SchedulerTrace() {};
		static SchedulerTrace* Get();
	};

}