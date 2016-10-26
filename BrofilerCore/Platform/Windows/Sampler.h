#pragma once

#include "SymEngine.h"
#include <array>
#include <vector>
#include <stdint.h>
#include "../SamplingProfiler.h"


namespace Brofiler
{

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct ThreadEntry;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class Sampler : public SamplingProfiler
{
	std::vector<ThreadEntry*> targetThreads;

	MW_HANDLE workerThread;
	MW_HANDLE finishEvent;

	uint32 intervalMicroSeconds;

	// Called from worker thread
	static MW_DWORD MW_WINAPI AsyncUpdate( void* lpParam );

public:
	Sampler();
	~Sampler();

	virtual bool IsSamplingScope() const override;

	virtual bool IsActive() const override;

	virtual void StartSampling(const std::vector<ThreadEntry*>& threads, uint32 samplingInterval) override;
	virtual bool StopSampling() override;

	virtual size_t GetCollectedCount() const override;

	static uint32 GetCallstack(MW_HANDLE hThread, MW_CONTEXT& context, CallStackBuffer& callstack);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

