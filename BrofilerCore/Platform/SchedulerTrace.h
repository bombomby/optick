#pragma once
#include "Core.h"
#include <unordered_set>

// Enable\Disable low-level platform-specific tracing (Switch-contexts, Autosampling, etc.)
#if !defined(BRO_ENABLE_TRACING)
#define BRO_ENABLE_TRACING (USE_BROFILER && BRO_PLATFORM_WINDOWS /*&& 0*/)
#endif

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
