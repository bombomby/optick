#pragma once

#define MT_CPUCORE_ANY (0xffffffff)

#include <Platform/Common/MTAtomic.h>

namespace MT
{
	namespace ThreadPriority
	{
		enum Type
		{
			HIGH = 0,
			DEFAULT = 1,
			LOW = 2
		};
	}


	class ThreadBase
	{
	protected:
		void * funcData;
		TThreadEntryPoint func;
	public:

		MT_NOCOPYABLE(ThreadBase);

		ThreadBase()
			: funcData(nullptr)
			, func(nullptr)
		{
		}

		static void SpinSleepMicroSeconds(uint32 microseconds)
		{
			int64 desiredTime = GetTimeMicroSeconds() + microseconds;
			for(;;)
			{
				int64 timeNow = GetTimeMicroSeconds();
				if (timeNow > desiredTime)
				{
					break;
				}
				YieldCpu();
			}
		}

		static void SpinSleepMilliSeconds(uint32 milliseconds)
		{
			int64 desiredTime = GetTimeMilliSeconds() + milliseconds;
			while(GetTimeMilliSeconds() <= desiredTime) {}
		}

	};
}


