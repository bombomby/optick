#pragma once
#include <windows.h>

namespace Test
{
	// Test engine: emulates some hard CPU work.
	class Engine
	{
		void UpdateInput();
		void UpdateMessages();
		void UpdateLogic();
		void UpdateScene();
		void Draw();
	public:
		// Updates engine, should be called once per frame.
		// Returns false if it doesn't want to update any more.
		bool Update();

		void UpdatePhysics();
	};
}