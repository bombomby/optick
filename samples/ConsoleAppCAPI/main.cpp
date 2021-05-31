#include <stdio.h>
#include "optick.h"
#include "optick_capi.h"
#include "TestEngine.h"

#if OPTICK_MSVC
#pragma warning(push)

//C4250. inherits 'std::basic_ostream'
#pragma warning(disable : 4250)

//C4127. Conditional expression is constant
#pragma warning(disable : 4127)
#endif

// Testing OPTICK_APP macro for startup performance analysis
void TestOptickApp(Test::Engine *engine)
{
	OptickAPI_StartCapture();
	for (int i = 0; i < 3; ++i)
		engine->Update();
	OptickAPI_StopCapture(NULL, 0);
}

// Testing OPTICK_START_CAPTURE/OPTICK_STOP_CAPTURE/OPTICK_SAVE_CAPTURE for performance automation
void TestAutomation(Test::Engine *engine)
{
	OptickAPI_StartCapture();
	for (int i = 0; i < 3; ++i)
		engine->Update();
	const char outFileName[] = "ConsoleApp";
	OptickAPI_StopCapture(outFileName, sizeof(outFileName));
}

#if OPTICK_MSVC
int wmain()
#else
int main()
#endif
{
	printf("Starting profiler test.\n");

	Test::Engine engine;
	printf("Engine successfully created.\n");

	printf("Starting main loop update.\n");

	// Setting memory allocators
	OptickAPI_SetAllocator(
		[](size_t size) -> void * { return malloc(size); },
		[](void *p)
		{ free(p); },
		[]() { /* Do some TLS initialization here if needed */ });

	//TestOptickApp(&engine);
	//TestAutomation(&engine);

	/*
	Unlike C++ API, you must call StartCapture explicitly
	Otherwise while capture files may be generated correctly the
	OptickApp will fail to generate a preview when live captures are stopped
	*/
	OptickAPI_StartCapture();

	bool needExit = false;
	while (!needExit)
	{
		OptickAPI_NextFrame();

		if (!engine.Update())
			break;

		/*
			Example of how to add a profiling scope in C
		*/
		static uint64_t eventDesc = OptickAPI_CreateEventDescCStr2("Print stdout");
		uint64_t event = OptickAPI_PushEvent(eventDesc);
		{
			printf(".");
			fflush(stdout);
		}
		OptickAPI_PopEvent(event);
	}

	OptickAPI_Shutdown();

	return 0;
}

#if OPTICK_MSVC
#pragma warning(pop)
#endif
