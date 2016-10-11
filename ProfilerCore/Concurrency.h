#pragma once

#if defined(LINUX64) || defined(ANDROID)
#include "Linux/ConcurrencyLinux.h"
#else
#include "Windows/ConcurrencyWindows.h"
#endif

namespace Profiler
{

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class CriticalSectionScope
{
	CriticalSection &section;
private:
	CriticalSectionScope &operator=( CriticalSectionScope& ) {}
public:
	CriticalSectionScope( CriticalSection& _lock ) : section(_lock) 
	{
		section.Enter(); 
	}

	~CriticalSectionScope() 
	{ 
		section.Leave(); 
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#define CRITICAL_SECTION(criticalSection) CriticalSectionScope generatedCriticalSectionScope##__LINE__(criticalSection); 
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}