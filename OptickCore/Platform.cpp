#include "Platform.h"

namespace Optick
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
ThreadID GetThreadID()
{
#if defined(OPTICK_MSVC)
	return GetCurrentThreadId();
#elif defined(OPTICK_OSX)
	uint64_t tid;
	pthread_threadid_np(pthread_self(), &tid);
	return tid;
#elif defined(OPTICK_LINUX)
	return syscall(SYS_gettid);
#else
#error Platform is not supported!
#endif
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
ProcessID GetProcessID()
{
#if defined(OPTICK_MSVC)
	return GetCurrentProcessId();
#elif defined(OPTICK_GCC)
	return (ProcessID)getpid();
#else
#error Platform is not supported!
#endif
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Platform::ID Platform::Get()
{
#if defined(OPTICK_LINUX)
	return Platform::Linux;
#elif defined(OPTICK_OSX)
	return Platform::MacOS;
#elif defined(OPTICK_XBOX)
	return Platform::XBox;
#elif defined(OPTICK_PS)
	return Platform::Playstation;
#elif defined(OPTICK_PC)
	return Platform::Windows;
#else
	return Platform::Unknown;
#endif
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const char * Platform::GetName()
{
	Platform::ID id = Get();
	const char* names[] = {
			"Unknown",
			"Windows",
			"Linux",
			"MacOS",
			"XBox",
			"Playstation",
	};
	static_assert(OPTICK_ARRAY_SIZE(names) == Platform::Count, "Size mismatch");
	return names[id];
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
