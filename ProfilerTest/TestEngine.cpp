#include "TestEngine.h"
#include "../ProfilerCore/Brofiler.h"
#include <math.h>
#include <vector>

#if defined(WINDOWS)
#include <windows.h>
#elif !defined(LINUX64)
#error "Wrong OS type"
#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Test
{

namespace
{
void ThreadSleep(int milliseconds)
{
#if defined(WINDOWS)
	Sleep(milliseconds);
#elif defined(LINUX64)
	Profiler::ThreadSleep(5);
#else
#error "Wrong OS type"
#endif
}


void ThreadTerminate( ThreadID& threadId )
{
#if defined(WINDOWS)
	::TerminateThread(threadId, 0);
	DWORD resultCode = WaitForSingleObject((HANDLE)threadId, INFINITE);
	CloseHandle((HANDLE)threadId);
#elif defined(LINUX64)
	threadId.Terminate();
#else
#error "Wrong OS type"
#endif
}

}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
DWORD WINAPI WorkerThread(PVOID params)
{
	BROFILER_THREAD("Worker")
	Engine* engine = (Engine*)params;

	while (engine->IsAlive())
	{
		// Emulate "wait for events" message
		ThreadSleep(5); 
		engine->UpdatePhysics();
	}

	return 0;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
static const uint REPEAT_COUNT = 256 * 1024;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<uint N>
void SlowFunction()
{ PROFILE
	// Make it static to fool compiler and prevent it from skipping
	static float value = 0.0f;
	
	for (uint i = 0; i < N; ++i)
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
bool Engine::Update()
{ BROFILER_FRAME("MainThread")
	UpdateInput();

	UpdateMessages();

	UpdateLogic();

	UpdateScene();

	UpdatePhysics();

	Draw();

	return true;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateInput()
{ BROFILER_CATEGORY( "UpdateInput", Profiler::Color::SteelBlue )
	SlowFunction2();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateMessages()
{ BROFILER_CATEGORY( "UpdateMessages", Profiler::Color::Orange )
	SlowFunction<REPEAT_COUNT>();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateLogic()
{ BROFILER_CATEGORY( "UpdateLogic", Profiler::Color::Orchid )
	SlowFunction<REPEAT_COUNT>();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdateScene()
{ BROFILER_CATEGORY( "UpdateScene", Profiler::Color::SkyBlue )
	SlowFunction<REPEAT_COUNT>();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::Draw()
{ BROFILER_CATEGORY( "Draw", Profiler::Color::Salmon )
	SlowFunction<REPEAT_COUNT>();
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void Engine::UpdatePhysics()
{ BROFILER_CATEGORY( "UpdatePhysics", Profiler::Color::Wheat )
	int64 time = Profiler::GetTimeMicroSeconds();
	while (Profiler::GetTimeMicroSeconds() - time < 20 * 1000) {}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const size_t WORKER_THREAD_COUNT = 2;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Engine::Engine() : isAlive(true)
{
	for (size_t i = 0; i < WORKER_THREAD_COUNT; ++i)
	{
#if defined(WINDOWS)
		workers.push_back( CreateThread(NULL, 0, WorkerThread, this, 0, NULL) );
#elif defined(LINUX64)
		workers.push_back(Profiler::SystemThread());
		workers.back().Create( WorkerThread, this );
#else
#error "Wrong OS type"
#endif
		
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Engine::~Engine()
{
	isAlive = false;

	for (size_t i = 0; i < workers.size(); ++i)
	{
		ThreadTerminate( workers[i] );
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}