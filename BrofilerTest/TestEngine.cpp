#include "Brofiler.h"
#include "TestEngine.h"
#include <math.h>
#include <vector>
#include <MTProfilerEventListener.h>

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
		MT::Thread::Sleep(5); 
		engine->UpdatePhysics();
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
	MT_DECLARE_TASK(SimpleTask, MT::StackRequirements::STANDARD, MT::TaskPriority::NORMAL, MT::Color::Blue);

	float value;

	SimpleTask() : value(0.0f) {}

	void Do(MT::FiberContext&)
	{
		for (unsigned long i = 0; i < N; ++i)
			value = (value + sin((float)i)) * 0.5f;
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
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Engine::Update()
{ 
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
{ BROFILER_CATEGORY( "UpdateInput", Brofiler::Color::SteelBlue )
	SlowFunction2();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateMessages()
{ BROFILER_CATEGORY( "UpdateMessages", Brofiler::Color::Orange )
	SlowFunction<REPEAT_COUNT>();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateLogic()
{ BROFILER_CATEGORY( "UpdateLogic", Brofiler::Color::Orchid )
	SlowFunction<REPEAT_COUNT>();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateTasks()
{ BROFILER_CATEGORY( "UpdateTasks", Brofiler::Color::SkyBlue )
	RootTask<4> task;
	scheduler.RunAsync(MT::TaskGroup::Default(), &task, 1);
	scheduler.WaitAll(100000);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateScene()
{ BROFILER_CATEGORY( "UpdateScene", Brofiler::Color::SkyBlue )
	SlowFunction<REPEAT_COUNT>();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::Draw()
{ BROFILER_CATEGORY( "Draw", Brofiler::Color::Salmon )
	SlowFunction<REPEAT_COUNT>();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdatePhysics()
{ BROFILER_CATEGORY( "UpdatePhysics", Brofiler::Color::Wheat )

	MT::SpinSleepMilliSeconds(20);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class Profiler : public MT::IProfilerEventListener
{
	Brofiler::EventStorage* eventStorages[MT::MT_MAX_STANDART_FIBERS_COUNT + MT::MT_MAX_EXTENDED_FIBERS_COUNT];
	Brofiler::EventStorage** eventStorageSlots[MT::MT_MAX_THREAD_COUNT];
	MT::ThreadId threadIds[MT::MT_MAX_THREAD_COUNT];

	static mt_thread_local Brofiler::EventStorage* restoreStorage;

public:
	virtual void OnFibersCreated(uint32 fibersCount) override
	{
		MT_ASSERT(fibersCount <= MT_ARRAY_SIZE(eventStorages), "Too many fibers!");
		for(uint32 i = 0; i < fibersCount; i++)
		{
			Brofiler::RegisterFiber("Fiber", &eventStorages[i]);
		}
	}

	virtual void OnThreadsCreated(uint32 threadsCount) override
	{
		MT_ASSERT(threadsCount <= MT_ARRAY_SIZE(eventStorageSlots), "Too many threads!");

		for(uint32 i = 0; i < threadsCount; i++)
		{
			eventStorageSlots[i] = nullptr;
		}
	}

	virtual void OnFiberAssignedToThread(uint32 fiberIndex, uint32 threadIndex) override
	{
		// from current thread
		if (threadIndex == 0xFFFFFFFF)
		{
			Brofiler::EventStorage** threadStorageSlot = Brofiler::GetEventStorageSlot();

			// Save storage for the first time
			if (!restoreStorage)
				restoreStorage = *threadStorageSlot;

			*threadStorageSlot = eventStorages[fiberIndex];
			Brofiler::SyncData::StartWork(*threadStorageSlot, (uint32)MT::ThreadId::Self().AsUInt64() );
		}
		else
		{
			Brofiler::EventStorage** & eventStorageSlot = eventStorageSlots[threadIndex];
			MT::ThreadId & threadId = threadIds[threadIndex];
			Brofiler::EventStorage* & eventStorage = eventStorages[fiberIndex];

			Brofiler::EventStorage* previousStorage = *eventStorageSlot;
			Brofiler::SyncData::StopWork(previousStorage);

			// If we have an active storage - put current storage into the slot
			*eventStorageSlot = eventStorage;

			Brofiler::SyncData::StartWork(eventStorage, (uint32)threadId.AsUInt64() );
		}
	}


	virtual void OnThreadAssignedToFiber(uint32 threadIndex, uint32 fiberIndex) override
	{
		MT_UNUSED(fiberIndex);
		MT_ASSERT(threadIndex == 0xFFFFFFFF, "Can't make assignment from another thread!");

		if (restoreStorage)
		{
			// Restore original storage
			Brofiler::EventStorage** threadStorageSlot = Brofiler::GetEventStorageSlot();
			*threadStorageSlot = restoreStorage;
			restoreStorage = nullptr;
		}
	}

	virtual void OnThreadCreated(uint32 workerIndex) override 
	{
		BROFILER_THREAD("Scheduler(Worker)");
		eventStorageSlots[workerIndex] = Brofiler::GetEventStorageSlot();
		threadIds[workerIndex] = MT::ThreadId::Self();
	}

	virtual void OnThreadStarted(uint32 workerIndex) override
	{
		MT_UNUSED(workerIndex);
	}

	virtual void OnThreadStoped(uint32 workerIndex) override
	{
		MT_UNUSED(workerIndex);
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

	virtual void OnTaskExecuteStateChanged(MT::Color::Type debugColor, const mt_char* debugID, MT::TaskExecuteState::Type type) override 
	{
		MT_UNUSED(debugColor);
		MT_UNUSED(debugID);
		MT_UNUSED(type);
	}

};

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
mt_thread_local Brofiler::EventStorage* Profiler::restoreStorage = nullptr;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

MT::IProfilerEventListener* GetProfiler()
{
	static Profiler profiler;
	return &profiler;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Engine::Engine() : scheduler(0, nullptr, GetProfiler()), isAlive(true)
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