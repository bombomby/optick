#pragma once

#if USE_BROFILER_SAMPLING

#include "SymEngine.h"
#include <array>
#include <vector>

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct ThreadEntry;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class Sampler
{
	SymEngine symEngine;

	std::list<CallStack> callstacks;
	std::vector<ThreadEntry*> targetThreads;

	MW_HANDLE workerThread;
	MW_HANDLE finishEvent;

	uint32 intervalMicroSeconds;

	// Called from worker thread
	static MW_DWORD MW_WINAPI AsyncUpdate( void* lpParam );

public:
	Sampler();
	~Sampler();

	bool IsSamplingScope() const;

	bool IsActive() const;

	void StartSampling(const std::vector<ThreadEntry*>& threads, uint32 samplingInterval = 300);
	bool StopSampling();

	size_t GetCollectedCount() const;
	OutputDataStream& Serialize(OutputDataStream& stream);

	static uint32 GetCallstack(MW_HANDLE hThread, MW_CONTEXT& context, CallStackBuffer& callstack);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

#endif
