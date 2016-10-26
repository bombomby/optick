#ifdef _WIN32
#include "Common.h"


#include "Common.h"
#include "Event.h"
#include "Core.h"
#include "Serialization.h"
#include "Sampler.h"
#include <DbgHelp.h>
#include <unordered_set>

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Sampler::StopSampling()
{
	if (!IsActive())
		return false;

	SetEvent(finishEvent);

	DWORD result = WaitForSingleObject(workerThread, INFINITE);
	BRO_UNUSED(result);
	BRO_ASSERT(result == WAIT_OBJECT_0, "Can't stop sampling thread!");

	CloseHandle(workerThread);
	workerThread = nullptr;

	CloseHandle(finishEvent);
	finishEvent = nullptr;

	targetThreads.clear();

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Sampler::Sampler() : workerThread(nullptr), finishEvent(nullptr), intervalMicroSeconds(300)
{
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Sampler::~Sampler()
{
	StopSampling();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Sampler::StartSampling(const std::vector<ThreadEntry*>& threads, uint32 samplingInterval)
{
	intervalMicroSeconds = samplingInterval;
	targetThreads = threads;

	if (IsActive())
		StopSampling();

	callstacks.clear();

	BRO_VERIFY(finishEvent == nullptr && workerThread == nullptr, "Can't start sampling!", return);

	finishEvent = CreateEvent(NULL, false, false, 0);
	workerThread = CreateThread(NULL, 0, &Sampler::AsyncUpdate, this, 0, NULL);

	BRO_ASSERT(finishEvent && workerThread, "Sampling was not started!")
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Sampler::IsActive() const
{
	return workerThread != nullptr || finishEvent != nullptr;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void ClearStackContext(CONTEXT& context)
{
	memset(&context, 0, sizeof(context));
	context.ContextFlags = CONTEXT_FULL;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
DWORD WINAPI Sampler::AsyncUpdate(LPVOID lpParam)
{
	Sampler& sampler = *(Sampler*)(lpParam);

	std::vector<std::pair<HANDLE, ThreadEntry*>> openThreads;
	openThreads.reserve(sampler.targetThreads.size());

	for each (ThreadEntry* entry in sampler.targetThreads)
	{
		BRO_VERIFY(!entry->description.threadID.IsEqual(MT::ThreadId::Self()), "It's a bad idea to sample specified thread! Deadlock will occur!", continue);

		HANDLE hThread = OpenThread(THREAD_ALL_ACCESS, FALSE, (DWORD)entry->description.threadID.AsUInt64());
		if (hThread == NULL)
			continue;

		openThreads.push_back(std::make_pair(hThread, entry));
	}

	if (openThreads.empty())
		return 0;

	CallStackBuffer buffer;

	CONTEXT context;

	ClearStackContext(context);

	while (WaitForSingleObject(sampler.finishEvent, 0) == WAIT_TIMEOUT)
	{
		MT::SpinSleepMicroSeconds(sampler.intervalMicroSeconds);

		// Check whether we are inside sampling scope
		for each (const auto& entry in openThreads)
		{
			HANDLE handle = entry.first;
			const ThreadEntry* thread = entry.second;

			// Get storage from TLS slot (it can be replaced by Fibers)
			EventStorage* storage = *thread->threadTLS;
			if (!storage || !storage->isSampling.Load())
				continue;

			uint32 count = 0;

			DWORD suspendedStatus = SuspendThread(handle);

			if (suspendedStatus != (DWORD)-1)
			{
				// Check scope again because it is possible to leave sampling scope while trying to suspend main thread
				if (storage->isSampling.Load() && GetThreadContext(handle, &context))
				{
					count = GetCallstack(handle, context, buffer);
				}

				ClearStackContext(context);
				ResumeThread(handle);
			}

			if (count > 0)
			{
				sampler.callstacks.push_back(CallStack(buffer.begin(), buffer.begin() + count));
			}

			ClearStackContext(context);
		}
	}

	for each (const auto& entry in openThreads)
		CloseHandle(entry.first);

	return 0;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

uint32 Sampler::GetCallstack(HANDLE hThread, CONTEXT& context, CallStackBuffer& callstack) 
{
	// We can't initialize dbghelp.dll here => http://microsoft.public.windbg.narkive.com/G2WkSt2k/stackwalk64-performance-problems
	// Otherwise it will be 5x times slower
	// Init();

	STACKFRAME64 stackFrame;
	memset(&stackFrame, 0, sizeof(STACKFRAME64));
	DWORD machineType;

	stackFrame.AddrPC.Mode = AddrModeFlat;
	stackFrame.AddrFrame.Mode = AddrModeFlat;
	stackFrame.AddrStack.Mode = AddrModeFlat;

#ifdef _M_IX86
	machineType = IMAGE_FILE_MACHINE_I386;
	stackFrame.AddrPC.Offset = context.Eip;
	stackFrame.AddrFrame.Offset = context.Ebp;
	stackFrame.AddrStack.Offset = context.Esp;
#elif _M_X64
	machineType = IMAGE_FILE_MACHINE_AMD64;
	stackFrame.AddrPC.Offset = context.Rip;
	stackFrame.AddrFrame.Offset = context.Rsp;
	stackFrame.AddrStack.Offset = context.Rsp;
#elif _M_IA64
	machineType = IMAGE_FILE_MACHINE_IA64;
	stackFrame.AddrPC.Offset = context.StIIP;
	stackFrame.AddrFrame.Offset = context.IntSp;
	stackFrame.AddrStack.Offset = context.IntSp;
	stackFrame.AddrBStore.Offset = context.RsBSP;
	stackFrame.AddrBStore.Mode = AddrModeFlat;
#else
#error "Platform not supported!"
#endif

	uint32 index = 0;
	while (	StackWalk64(machineType, GetCurrentProcess(), hThread, &stackFrame, &context, nullptr, &SymFunctionTableAccess64, &SymGetModuleBase64, nullptr) )
	{
		DWORD64 dwAddress = stackFrame.AddrPC.Offset;
		if (!dwAddress)
			break;

		if (index == callstack.size())
			return 0; // Too long callstack - possible error, let's skip it

		if (index > 0 && callstack[index - 1] == dwAddress)
			continue;

		callstack[index] = static_cast<uintptr_t>(dwAddress);
		++index;
	}

	return index;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


bool Sampler::IsSamplingScope() const
{
	for each (const ThreadEntry* entry in targetThreads)
	{
		if (const EventStorage* storage = *entry->threadTLS)
		{
			if (storage->isSampling.Load())
			{
				return true;
			}
		}
	}

	return false;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
size_t Sampler::GetCollectedCount() const
{
	return callstacks.size();
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
SamplingProfiler* SamplingProfiler::Get()
{
	static Sampler winSamplingProfiler;
	return &winSamplingProfiler;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}



#endif

