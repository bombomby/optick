#include "Brofiler.h"
#include "TestEngine.h"
#include <math.h>
#include <vector>
#include <MTProfilerEventListener.h>


static const size_t SCHEDULER_WORKERS_COUNT = 0;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Test
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WorkerThread(void* _engine)
{
	Engine* engine = (Engine*)_engine;
	BROFILER_THREAD("Worker")
	
	while (engine->IsAlive())
	{
		// Emulate "wait for events" message
		MT::Thread::Sleep(10); 
		engine->UpdatePhysics();
		engine->UpdateRecursive();
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
static const unsigned long REPEAT_COUNT = 128 * 1024;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<unsigned long N>
void SlowFunction()
{ PROFILE
	// Make it static to fool compiler and prevent it from skipping
	static float value = 0.0f;
	
	for (unsigned long i = 0; i < N; ++i)
		value = (value + sin((float)i)) * 0.5f;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void SlowFunction2()
{ PROFILE
	// Make it static to fool compiler and prevent it from skipping
	static std::vector<float> values(1024 * 1024);

	for (size_t i = 1; i < values.size(); ++i)
	{
		values[i] += i;
		values[i] *= i;
		values[i] /= i;
		values[i] -= i;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<unsigned long N>
struct SimpleTask
{
	MT_DECLARE_TASK(SimpleTask, MT::StackRequirements::STANDARD, MT::TaskPriority::NORMAL, MT::Color::LightBlue);

	float value;

	SimpleTask() : value(0.0f) {}

	void Do(MT::FiberContext& ctx)
	{
		{
			BROFILER_CATEGORY("BeforeYield", Brofiler::Color::PaleGreen);

			for (unsigned long i = 0; i < N; ++i)
				value = (value + sin((float)i)) * 0.5f;
		}

		ctx.Yield();

		{
			BROFILER_CATEGORY("AfterYield", Brofiler::Color::SandyBrown);

			for (unsigned long i = 0; i < N; ++i)
				value = (value + cos((float)i)) * 0.5f;
		}

	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<unsigned long CHILDREN_COUNT>
struct RootTask
{
	MT_DECLARE_TASK(RootTask, MT::StackRequirements::STANDARD, MT::TaskPriority::NORMAL, MT::Color::BurlyWood);

	float value;

	RootTask() : value(0.0f) {}

	void Do(MT::FiberContext& context)
	{
		MT::SpinSleepMilliSeconds(1);

		SimpleTask<REPEAT_COUNT> children[CHILDREN_COUNT];
		context.RunSubtasksAndYield(MT::TaskGroup::Default(), children, CHILDREN_COUNT);

		MT::SpinSleepMilliSeconds(1);
	}
};


struct PriorityTask
{
	MT_DECLARE_TASK(PriorityTask, MT::StackRequirements::STANDARD, MT::TaskPriority::HIGH, MT::Color::Orange);

	float value;

	PriorityTask() : value(0.0f) {}

	void Do(MT::FiberContext&)
	{
		for (unsigned long i = 0; i < 8192; ++i)
		{
			value = (value + cos((float)i)) * 0.5f;
		}
	}
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Engine::Update()
{ 
	PROFILE;

	UpdateInput();

	UpdateMessages();

	UpdateLogic();

	UpdateTasks();

	UpdateScene();

	UpdatePhysics();

	Draw();

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateInput()
{
	BROFILER_CATEGORY("UpdateInput", Brofiler::Color::SteelBlue);
	SlowFunction2();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateMessages()
{
	BROFILER_CATEGORY("UpdateMessages", Brofiler::Color::Orange);
	SlowFunction<REPEAT_COUNT>();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateLogic()
{
	BROFILER_CATEGORY("UpdateLogic", Brofiler::Color::Orchid);
	SlowFunction<REPEAT_COUNT>();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateTasks()
{
	BROFILER_CATEGORY("UpdateTasks", Brofiler::Color::SkyBlue);
	RootTask<16> task;
	scheduler.RunAsync(MT::TaskGroup::Default(), &task, 1);

	MT::SpinSleepMilliSeconds(1);

	PriorityTask priorityTasks[128];
	scheduler.RunAsync(MT::TaskGroup::Default(), &priorityTasks[0], MT_ARRAY_SIZE(priorityTasks));

	scheduler.WaitAll(100000);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateScene()
{
	BROFILER_CATEGORY("UpdateScene", Brofiler::Color::SkyBlue);
	SlowFunction<REPEAT_COUNT>();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::Draw()
{
	BROFILER_CATEGORY("Draw", Brofiler::Color::Salmon);
	SlowFunction<REPEAT_COUNT>();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdatePhysics()
{ 
	BROFILER_CATEGORY("UpdatePhysics", Brofiler::Color::Wheat);
	MT::SpinSleepMilliSeconds(20);
}

template<int N>
void RecursiveUpdate(int sleep)
{
	PROFILE;
	MT::SpinSleepMicroSeconds(sleep);
	RecursiveUpdate<N - 1>(sleep);
	RecursiveUpdate<N - 1>(sleep);
}

template<>
void RecursiveUpdate<0>(int) {}

void Engine::UpdateRecursive()
{
	PROFILE;
	RecursiveUpdate<4>(500);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if MT_MSVC_COMPILER_FAMILY
#pragma warning( push )

//C4481. nonstandard extension used: override specifier 'override'
#pragma warning( disable : 4481 )

#endif

class Profiler : public MT::IProfilerEventListener
{
	Brofiler::EventStorage* fiberEventStorages[MT::MT_MAX_STANDART_FIBERS_COUNT + MT::MT_MAX_EXTENDED_FIBERS_COUNT];
	uint32 totalFibersCount;

	static mt_thread_local Brofiler::EventStorage* originalThreadStorage;
	static mt_thread_local Brofiler::EventStorage* activeThreadStorage;

public:

	Profiler()
		: totalFibersCount(0)
	{
	}

	virtual void OnFibersCreated(uint32 fibersCount) override
	{
		totalFibersCount = fibersCount;
		MT_ASSERT(fibersCount <= MT_ARRAY_SIZE(fiberEventStorages), "Too many fibers!");
		for(uint32 fiberIndex = 0; fiberIndex < fibersCount; fiberIndex++)
		{
			Brofiler::RegisterFiber(fiberIndex, &fiberEventStorages[fiberIndex]);
		}
	}

	virtual void OnThreadsCreated(uint32 threadsCount) override
	{
		MT_UNUSED(threadsCount);
	}



	virtual void OnTemporaryWorkerThreadLeave() override
	{
		Brofiler::EventStorage** currentThreadStorageSlot = Brofiler::GetEventStorageSlotForCurrentThread();
		MT_ASSERT(currentThreadStorageSlot, "Sanity check failed");
		Brofiler::EventStorage* storage = *currentThreadStorageSlot;

		// if profile session is not active
		if (storage == nullptr)
		{
			return;
		}

		MT_ASSERT(IsFiberStorage(storage) == false, "Sanity check failed");
	}

	virtual void OnTemporaryWorkerThreadJoin() override
	{
		Brofiler::EventStorage** currentThreadStorageSlot = Brofiler::GetEventStorageSlotForCurrentThread();
		MT_ASSERT(currentThreadStorageSlot, "Sanity check failed");
		Brofiler::EventStorage* storage = *currentThreadStorageSlot;

		// if profile session is not active
		if (storage == nullptr)
		{
			return;
		}

		MT_ASSERT(IsFiberStorage(storage) == false, "Sanity check failed");
	}


	virtual void OnThreadCreated(uint32 workerIndex) override 
	{
		BROFILER_START_THREAD("Scheduler(Worker)");
		MT_UNUSED(workerIndex);
	}

	virtual void OnThreadStarted(uint32 workerIndex) override
	{
		MT_UNUSED(workerIndex);
	}

	virtual void OnThreadStoped(uint32 workerIndex) override
	{
		MT_UNUSED(workerIndex);
		BROFILER_STOP_THREAD();
	}

	virtual void OnThreadIdleStarted(uint32 workerIndex) override
	{
		MT_UNUSED(workerIndex);
	}

	virtual void OnThreadIdleFinished(uint32 workerIndex) override
	{
		MT_UNUSED(workerIndex);
	}

	virtual void OnThreadWaitStarted() override
	{
	}

	virtual void OnThreadWaitFinished() override
	{
	}

	virtual void OnTaskExecuteStateChanged(MT::Color::Type debugColor, const mt_char* debugID, MT::TaskExecuteState::Type type, int32 fiberIndex) override 
	{
		MT_ASSERT(fiberIndex < (int32)totalFibersCount, "Sanity check failed");

		Brofiler::EventStorage** currentThreadStorageSlot = Brofiler::GetEventStorageSlotForCurrentThread();
		MT_ASSERT(currentThreadStorageSlot, "Sanity check failed");

		// if profile session is not active
		if (*currentThreadStorageSlot == nullptr)
		{
			return;
		}

		// if actual fiber is scheduler internal fiber (don't have event storage for internal scheduler fibers)
		if (fiberIndex < 0)
		{
			return;
		}

		switch(type)
		{
		case MT::TaskExecuteState::START:
		case MT::TaskExecuteState::RESUME:
			{
				MT_ASSERT(originalThreadStorage == nullptr, "Sanity check failed");

				originalThreadStorage = *currentThreadStorageSlot;

				MT_ASSERT(IsFiberStorage(originalThreadStorage) == false, "Sanity check failed");

				Brofiler::EventStorage* currentFiberStorage = nullptr;
				if (fiberIndex >= (int32)0)
				{
					currentFiberStorage = fiberEventStorages[fiberIndex];
				} 

				*currentThreadStorageSlot = currentFiberStorage;
				activeThreadStorage = currentFiberStorage;
				Brofiler::FiberSyncData::AttachToThread(currentFiberStorage, MT::ThreadId::Self().AsUInt64());
			}
			break;

		case MT::TaskExecuteState::STOP:
		case MT::TaskExecuteState::SUSPEND:
			{
				Brofiler::EventStorage* currentFiberStorage = *currentThreadStorageSlot;

				//////////////////////////////////////////////////////////////////////////
				Brofiler::EventStorage* checkFiberStorage = nullptr;
				if (fiberIndex >= (int32)0)
				{
					checkFiberStorage = fiberEventStorages[fiberIndex];
				}
				MT_ASSERT(checkFiberStorage == currentFiberStorage, "Sanity check failed");

				MT_ASSERT(activeThreadStorage == currentFiberStorage, "Sanity check failed");

				//////////////////////////////////////////////////////////////////////////

				MT_ASSERT(IsFiberStorage(currentFiberStorage) == true, "Sanity check failed");

				Brofiler::FiberSyncData::DetachFromThread(currentFiberStorage);

				*currentThreadStorageSlot = originalThreadStorage;
				originalThreadStorage = nullptr;
			}
			break;
		}

/*
static ::Brofiler::EventDescription* BRO_CONCAT(autogenerated_description_, __LINE__) = ::Brofiler::EventDescription::Create( NAME, __FILE__, __LINE__, (unsigned long)COLOR ); \
	::Brofiler::Category				  BRO_CONCAT(autogenerated_event_, __LINE__)( *(BRO_CONCAT(autogenerated_description_, __LINE__)) ); \

		
		BROFILER_CATEGORY

*/

		MT_UNUSED(debugColor);
		MT_UNUSED(debugID);
		MT_UNUSED(type);
	}
};


#if MT_MSVC_COMPILER_FAMILY
#pragma warning( pop )
#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
mt_thread_local Brofiler::EventStorage* Profiler::originalThreadStorage = nullptr;
mt_thread_local Brofiler::EventStorage* Profiler::activeThreadStorage = 0;


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

MT::IProfilerEventListener* GetProfiler()
{
	static Profiler profiler;
	return &profiler;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Engine::Engine() : scheduler(SCHEDULER_WORKERS_COUNT, nullptr, GetProfiler()), isAlive(true)
{
	for (size_t i = 0; i < WORKER_THREAD_COUNT; ++i)
	{
		workers[i].Start(1024*1024, WorkerThread, this);
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Engine::~Engine()
{
	isAlive = false;

	for (size_t i = 0; i < workers.size(); ++i)
		workers[i].Join();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}