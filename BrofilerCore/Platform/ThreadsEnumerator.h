#pragma once

#include "Core.h"
#include <vector>
#include <string>

namespace Brofiler
{
	struct ThreadIdExt : public MT::ThreadId
	{

		ThreadIdExt(uint32 _id)
		{
			id = _id;
			isInitialized.Store(1);
		}

	};

	struct ThreadInfo
	{
		std::string name;
		ThreadIdExt id;
		bool fromOtherProcess;

		ThreadInfo()
			: id(0)
			, fromOtherProcess(false)
		{
		}

		ThreadInfo(uint32 _id, const char* _name, bool _fromOtherProcess)
			: name(_name)
			, id(_id)
			, fromOtherProcess(_fromOtherProcess)
		{
		}

	};


	extern bool EnumerateAllThreads(std::vector<ThreadInfo> & threads);

}