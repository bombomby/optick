#pragma once
#include "Core.h"
#include <unordered_set>

// Enable\Disable low-level platform-specific tracing (Switch-contexts, Autosampling, etc.)
#if !defined(OPTICK_ENABLE_TRACING)
#define OPTICK_ENABLE_TRACING (USE_OPTICK && (OPTICK_MSVC || OPTICK_LINUX || OPTICK_OSX) /*&& 0*/)
#endif

namespace Optick
{
	struct Trace
	{
		enum Mode
		{
			// Collect switch-contexts
			SWITCH_CONTEXTS =	1 << 0,
			// Collect callstacks
			STACK_WALK =		1 << 1,
			// Collect SysCalls 
			SYS_CALLS =			1 << 2,
			// Collect Process Stats
			PROCESSES =			1 << 3,
			ALL =				0xFFFFFFFF
		};

		std::unordered_set<uint64> activeThreadsIDs;
        std::string password;

		virtual CaptureStatus::Type Start(int mode, const ThreadList& threads);
		virtual bool Stop();

		virtual ~Trace() {};
		static Trace* Get();
	};
}
