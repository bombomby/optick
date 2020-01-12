#pragma once

#include <array>
#include <thread>

// Inject brofiler code into the task scope
#define MT_SCHEDULER_PROFILER_TASK_SCOPE_CODE_INJECTION( TYPE, DEBUG_COLOR, SRC_FILE, SRC_LINE) OPTICK_CATEGORY( MT_TEXT( #TYPE ), OPTICK_MAKE_CATEGORY(0, DEBUG_COLOR) );

#if !defined(OPTICK_ENABLE_FIBERS)
#define OPTICK_ENABLE_FIBERS (0)
#endif

#if OPTICK_ENABLE_FIBERS
#include <MTScheduler.h>
#endif //OPTICK_ENABLE_FILBERS

namespace Test
{
	// Test engine: emulates some hard CPU work.
	class Engine
	{
#if OPTICK_ENABLE_FIBERS
		MT::TaskScheduler scheduler;
#endif //OPTICK_ENABLE_FILBERS
        
		static const size_t WORKER_THREAD_COUNT = 2;
        std::array<std::thread, WORKER_THREAD_COUNT> workers;
		bool isAlive;

		void UpdateInput();
		void UpdateMessages();
		void UpdateLogic();
		void UpdateTasks();
		void UpdateScene();
		void Draw();
	public:
		Engine();
		~Engine();

		// Updates engine, should be called once per frame.
		// Returns false if it doesn't want to update any more.
		bool Update();

		void UpdatePhysics();
		void UpdateRecursive();

		bool IsAlive() const { return isAlive; }
	};

	bool OnOptickStateChanged(Optick::State::Type state);
	void SpinSleep(uint32_t milliseconds);
}
