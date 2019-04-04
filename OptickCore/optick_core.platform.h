#pragma once
#include "Common.h"
//////////////////////////////////////////////////////////////////////////
// Platform-specific stuff
//////////////////////////////////////////////////////////////////////////
namespace Optick
{
	struct Trace;

	// Platform API
	struct Platform
	{
		// Platform Name
		static OPTICK_INLINE const char* GetName();
		// Thread ID (system thread id)
		static OPTICK_INLINE ThreadID GetThreadID();
		// Process ID
		static OPTICK_INLINE ProcessID GetProcessID();
		// CPU Frequency
		static OPTICK_INLINE int64 GetFrequency();
		// CPU Time (Ticks)
		static OPTICK_INLINE int64 GetTime();
		// System Tracer
		static OPTICK_INLINE Trace* GetTrace();
	};

	// Tracing API
	struct Trace
	{
		enum Mode
		{
			// Collect switch-contexts
			SWITCH_CONTEXTS = 1 << 0,
			// Collect callstacks
			STACK_WALK = 1 << 1,
			// Collect SysCalls 
			SYS_CALLS = 1 << 2,
			// Collect Process Stats
			PROCESSES = 1 << 3,
			// Collect Everything
			ALL = 0xFFFFFFFF
		};
		virtual void SetPassword(const char* /*pwd*/) {};
		virtual CaptureStatus::Type Start(Trace::Mode mode, const ThreadList& threads) = 0;
		virtual bool Stop() = 0;
		virtual ~Trace() {};
	};
}
//////////////////////////////////////////////////////////////////////////