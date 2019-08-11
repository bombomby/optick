#include <iostream>
#include "optick.h"
#include "TestEngine.h"

#if OPTICK_MSVC
#pragma warning( push )

//C4250. inherits 'std::basic_ostream'
#pragma warning( disable : 4250 )

//C4127. Conditional expression is constant
#pragma warning( disable : 4127 )
#endif

using namespace std;

// Testing OPTICK_APP macro for startup performance analysis
void TestOptickApp(Test::Engine& engine)
{
	OPTICK_APP("ConsoleApp");
	for (int i = 0; i < 3; ++i)
		engine.Update();
}


// Testing OPTICK_START_CAPTURE/OPTICK_STOP_CAPTURE/OPTICK_SAVE_CAPTURE for performance automation
void TestAutomation(Test::Engine& engine)
{
	OPTICK_START_CAPTURE();
	for (int i = 0; i < 3; ++i)
		engine.Update();
	OPTICK_STOP_CAPTURE();
	OPTICK_SAVE_CAPTURE("ConsoleApp");
}

#if OPTICK_MSVC
int wmain()
#else
int main()
#endif
{
	cout << "Starting profiler test." << endl;

	Test::Engine engine;
	cout << "Engine successfully created." << endl;

	cout << "Starting main loop update." << endl;

	// Setting memory allocators
	OPTICK_SET_MEMORY_ALLOCATOR(
		[](size_t size) -> void* { return operator new(size); }, 
		[](void* p) { operator delete(p); }, 
		[]() { /* Do some TLS initialization here if needed */ }
	);


	//TestOptickApp(engine);
	//TestAutomation(engine);

	bool needExit = false;
	while( !needExit ) 
	{
		OPTICK_FRAME("MainThread");
		
		if (!engine.Update())
			break;

		cout<<'.'<<flush; 
	}

	OPTICK_SHUTDOWN();

	return 0;
}

#if OPTICK_MSVC
#pragma warning( pop )
#endif
