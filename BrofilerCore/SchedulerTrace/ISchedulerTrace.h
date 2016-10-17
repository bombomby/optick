#pragma once

namespace Brofiler
{

	namespace SchedulerTraceStatus
	{
		enum Type 
		{
			OK = 0,
			ERR_ALREADY_EXISTS = 1,
			ERR_ACCESS_DENIED = 2,
			FAILED = 3,
		};
	}


	struct ISchedulerTracer
	{
		virtual ~ISchedulerTracer() {};

		virtual SchedulerTraceStatus::Type Start() = 0;
		virtual bool Stop() = 0;

		static ISchedulerTracer* Get();
	};

}