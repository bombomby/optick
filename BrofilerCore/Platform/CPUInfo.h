#pragma once

#include "Common.h"
#include "Platform.h"
#include "Serialization.h"

#include <string>
#include <thread>

#if defined(BRO_PLATFORM_WINDOWS)
#define CPUID(INFO, ID) __cpuid(INFO, ID)
#include <intrin.h> 
#elif defined(BRO_PLATFORM_POSIX) || defined(BRO_PLATFORM_OSX)
#include <cpuid.h>
#define CPUID(INFO, ID) __cpuid(ID, INFO[0], INFO[1], INFO[2], INFO[3])
#else
#error Platform is not supported!
#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
namespace Brofiler
{
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	std::string GetCPUName()
	{
		int cpuInfo[4] = { -1 };
		char cpuBrandString[0x40] = { 0 };
		CPUID(cpuInfo, 0x80000000);
		unsigned nExIds = cpuInfo[0];
		for (unsigned i = 0x80000000; i <= nExIds; ++i)
		{
			CPUID(cpuInfo, i);
			if (i == 0x80000002)
				memcpy(cpuBrandString, cpuInfo, sizeof(cpuInfo));
			else if (i == 0x80000003)
				memcpy(cpuBrandString + 16, cpuInfo, sizeof(cpuInfo));
			else if (i == 0x80000004)
				memcpy(cpuBrandString + 32, cpuInfo, sizeof(cpuInfo));
		}
		return std::string(cpuBrandString);
	}
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
