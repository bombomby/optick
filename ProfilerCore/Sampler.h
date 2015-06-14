#pragma once
#include "SymEngine.h"
#include <windows.h>
#include <array>
#include <vector>

namespace Profiler
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

	// Called from worker thread
	static DWORD WINAPI AsyncUpdate( LPVOID lpParam );
public:
	Sampler();
	~Sampler();

	bool IsSamplingScope() const;

	bool IsActive() const;

	void StartSampling(const std::vector<ThreadEntry*>& threads, uint samplingInterval = 300);
	bool StopSampling();

	bool SetupHook(uint64 address, bool isHooked);

	size_t GetCollectedCount() const;
	OutputDataStream& Serialize(OutputDataStream& stream);

	static uint GetCallstack(HANDLE hThread, CONTEXT& context, CallStackBuffer& callstack);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}