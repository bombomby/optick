#pragma once
#include <windows.h>
#include <vector>

namespace Test
{
	// Test engine: emulates some hard CPU work.
	class Engine
	{
		std::vector<HANDLE> workers;
		bool isAlive;

		void UpdateInput();
		void UpdateMessages();
		void UpdateLogic();
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