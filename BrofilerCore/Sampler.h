#pragma once
#include "SymEngine.h"
#include <windows.h>
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

	HANDLE workerThread;
	HANDLE finishEvent;

	uint intervalMicroSeconds;

#if USE_BROFILER_SAMPLING
	// Called from worker thread
	static DWORD WINAPI AsyncUpdate( LPVOID lpParam );
#endif
public:
	Sampler();
	~Sampler();

	bool IsSamplingScope() const;

	bool IsActive() const;

	void StartSampling(const std::vector<ThreadEntry*>& threads, uint samplingInterval = 300);
	bool StopSampling();

	size_t GetCollectedCount() const;
	OutputDataStream& Serialize(OutputDataStream& stream);

	static uint GetCallstack(HANDLE hThread, CONTEXT& context, CallStackBuffer& callstack);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}