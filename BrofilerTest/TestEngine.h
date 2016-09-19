#pragma once
#include <windows.h>
#include <vector>
#include <thread>

#if BRO_FIBERS
#include <MTScheduler.h>
#endif

namespace Test
{
	// Test engine: emulates some hard CPU work.
	class Engine
	{
#if BRO_FIBERS
		MT::TaskScheduler scheduler;
#endif

		std::vector<std::thread> workers;
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

		bool IsAlive() const { return isAlive; }
	};
}