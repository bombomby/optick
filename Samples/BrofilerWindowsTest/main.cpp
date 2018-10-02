#include <iostream>
#include "Brofiler.h"
#include "TestEngine.h"

#if MT_MSVC_COMPILER_FAMILY
#pragma warning( push )

//C4250. inherits 'std::basic_ostream'
#pragma warning( disable : 4250 )

//C4127. Conditional expression is constant
#pragma warning( disable : 4127 )
#endif

using namespace std;

#if MT_PLATFORM_WINDOWS
int wmain()
#else
int main()
#endif
{
	cout << "Starting profiler test." << endl;

	Test::Engine engine;
	cout << "Engine successfully created." << endl;

	cout << "Starting main loop update." << endl;

	while( true ) 
	{
		BROFILER_FRAME("MainThread");
		
		if (!engine.Update())
			break;

		cout<<'.'; 
	}

	return 0;
}

#if MT_MSVC_COMPILER_FAMILY
#pragma warning( pop )
#endif
