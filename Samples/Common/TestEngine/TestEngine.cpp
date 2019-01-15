#include "Brofiler.h"
#include "TestEngine.h"
#include "TestImage.h"
#include <chrono>
#include <math.h>
#include <vector>
#include <cstring>

#if BRO_ENABLE_FIBERS
#include <MTProfilerEventListener.h>
#endif //BRO_ENABLE_FIBERS

static const size_t SCHEDULER_WORKERS_COUNT = 0;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Test
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
inline void SpinSleep(uint32_t milliseconds)
{
    auto start = std::chrono::system_clock::now();
    auto finish = start + std::chrono::milliseconds(milliseconds);
    while (std::chrono::system_clock::now() < finish) {}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool OnBrofilerStateChanged(Brofiler::BroState state)
{
	if (state == Brofiler::BRO_DUMP_CAPTURE)
	{
		Brofiler::AttachSummary("Version", "v2.0");
		Brofiler::AttachSummary("Build", __DATE__ " " __TIME__);

		// Attach screenshot
		Brofiler::AttachFile(Brofiler::BroFile::BRO_IMAGE, Brofiler::TestImage::Name, Brofiler::TestImage::Data, Brofiler::TestImage::Size);

		// Attach text file
		const char* textFile = "You could attach custom text files!\nFor example you could add dxdiag.txt or current game settings.";
		Brofiler::AttachFile(Brofiler::BroFile::BRO_TEXT, "Test.txt", (uint8_t*)textFile, (uint32_t)strlen(textFile));
	}
	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
float randf()
{
	return ((float)rand()) / (float)RAND_MAX;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WorkerThread(Engine* engine)
{
	BROFILER_THREAD("Worker")
	
	while (engine->IsAlive())
	{
		// Emulate "wait for events" message
        std::this_thread::sleep_for(std::chrono::milliseconds(10));
		engine->UpdatePhysics();
		engine->UpdateRecursive();
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
static const unsigned long REPEAT_COUNT = 128 * 1024;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<unsigned long N>
void SlowFunction()
{ 
	BROFILE;
	// Make it static to fool compiler and prevent it from skipping
	static float value = 0.0f;
	
	BROFILER_TAG("Before", value);

	for (unsigned long i = 0; i < N; ++i)
		value = (value + sin((float)i)) * 0.5f;

	BROFILER_TAG("After", value);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void SlowFunction2()
{ 
	BROFILE;
	// Make it static to fool compiler and prevent it from skipping
	static const size_t NUM_VALUES = 1024 * 1024;
	static float values[NUM_VALUES] = { 0 };

	for (size_t i = 1; i < NUM_VALUES; ++i)
	{
		values[i] += i;
		values[i] *= i;
		values[i] /= i;
		values[i] -= i;
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if BRO_ENABLE_FIBERS
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
		SpinSleep(1);

		SimpleTask<REPEAT_COUNT> children[CHILDREN_COUNT];
		context.RunSubtasksAndYield(MT::TaskGroup::Default(), children, CHILDREN_COUNT);

		SpinSleep(1);
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
#endif // BRO_ENABLE_FIBERS
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool Engine::Update()
{ 
	BROFILE;

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

	static const char* name[3] = { "Alice", "Bob", "Craig" };

	int index = rand() % 3;

	BROFILER_TAG("PlayerName", name[index]);
	BROFILER_TAG("Position", 123.0f, 456.0f, 789.0f);
	BROFILER_TAG("Health", 100);
	BROFILER_TAG("Score", 0x80000000u);
	BROFILER_TAG("Height(cm)", 176.3f);
	BROFILER_TAG("Address", (uint64_t)&name[index]);

	SlowFunction<REPEAT_COUNT>();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateTasks()
{
	BROFILER_CATEGORY("UpdateTasks", Brofiler::Color::SkyBlue);
    
#if BRO_ENABLE_FIBERS
	RootTask<16> task;
	scheduler.RunAsync(MT::TaskGroup::Default(), &task, 1);

	SpinSleep(1);

	PriorityTask priorityTasks[128];
	scheduler.RunAsync(MT::TaskGroup::Default(), &priorityTasks[0], MT_ARRAY_SIZE(priorityTasks));

	scheduler.WaitAll(100000);
#endif //BRO_ENABLE_FIBERS
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

	int64_t cpuTimestampStart = Brofiler::GetHighPrecisionTime();
	SlowFunction<REPEAT_COUNT>();
	int64_t cpuTimestampFinish = Brofiler::GetHighPrecisionTime();

	// Registering a storage - could be done in any place
	static Brofiler::EventStorage* GPUStorage = Brofiler::RegisterStorage("GPU");

	// Creating a shared event-description
	static Brofiler::EventDescription* GPUFrame = Brofiler::EventDescription::CreateShared("GPU Frame");

	// Adding GPUFrame event to the GPUStorage with specified start\stop timestamps
	BROFILER_STORAGE_EVENT(GPUStorage, GPUFrame, cpuTimestampStart, cpuTimestampFinish);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdatePhysics()
{ 
	BROFILER_CATEGORY("UpdatePhysics", Brofiler::Color::Wheat);
	BROFILER_TAG("Position", 123.0f, 456.0f, 789.0f);
	SpinSleep(20);
}

#if MT_MSVC_COMPILER_FAMILY
#pragma warning( push )
//warning C4996 : 'sprintf' : This function or variable may be unsafe.Consider using sprintf_s instead.
#pragma warning( disable : 4996 )
#endif

template<int N>
void RecursiveUpdate(int sleep)
{
	const char* scenes[4] = { "Earth", "Mars", "Moon", "Pluto" };
	char label[64] = { 0 };
	sprintf(label,  "UpdateScene - %s", scenes[rand() % 4]);

	BROFILER_PUSH_DYNAMIC(label);
	
	SpinSleep(sleep);
	RecursiveUpdate<N - 1>(sleep);
	RecursiveUpdate<N - 1>(sleep);

	BROFILER_POP();
}

#if MT_MSVC_COMPILER_FAMILY
#pragma warning( pop )
#endif

template<>
void RecursiveUpdate<0>(int) {}

void Engine::UpdateRecursive()
{
	BROFILE;
	RecursiveUpdate<4>(1);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if MT_MSVC_COMPILER_FAMILY
#pragma warning( push )

//C4481. nonstandard extension used: override specifier 'override'
#pragma warning( disable : 4481 )

#endif

#if BRO_ENABLE_FIBERS
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
		BROFILER_START_THREAD("FiberWorker");
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

		MT_UNUSED(debugColor);
		MT_UNUSED(debugID);
		MT_UNUSED(type);
	}
};
    
MT::IProfilerEventListener* GetProfiler()
{
    static Profiler profiler;
    return &profiler;
}
    
mt_thread_local Brofiler::EventStorage* Profiler::originalThreadStorage = nullptr;
mt_thread_local Brofiler::EventStorage* Profiler::activeThreadStorage = 0;
#endif //BRO_ENABLE_FIBERS

#if MT_MSVC_COMPILER_FAMILY
#pragma warning( pop )
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Engine::Engine() : isAlive(true)
#if BRO_ENABLE_FIBERS
    , scheduler(SCHEDULER_WORKERS_COUNT, nullptr, GetProfiler())
#endif
{
	BROFILER_SET_STATE_CHANGED_CALLBACK(OnBrofilerStateChanged);

	for (size_t i = 0; i < WORKER_THREAD_COUNT; ++i)
	{
        workers[i] = std::thread(WorkerThread, this);
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Engine::~Engine()
{
	isAlive = false;

	for (size_t i = 0; i < workers.size(); ++i)
		workers[i].join();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
