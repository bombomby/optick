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

	OPTICK_SET_MEMORY_ALLOCATOR(
		[](size_t size) -> void* { return operator new(size); }, 
		[](void* p) { operator delete(p); }, 
		[]() { /* Do some TLS initialization here if needed */ }
	);
	
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
