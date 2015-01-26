#pragma once
#include "SymEngine.h"
#include <windows.h>
#include <array>

namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class Sampler
{
	SymEngine symEngine;

	std::list<CallStack> callstacks;

	DWORD targetThreadID;

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

	void StartSampling(DWORD threadID, uint samplingInterval = 300);
	bool StopSampling();

	bool SetupHook(uint64 address, bool isHooked);

	size_t GetCollectedCount() const;
	OutputDataStream& Serialize(OutputDataStream& stream);

	static uint GetCallstack(HANDLE hThread, CONTEXT& context, CallStackBuffer& callstack);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}