#include <iostream>
#include "Optick.h"
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

	while( true ) 
	{
		OPTICK_FRAME("MainThread");
		
		if (!engine.Update())
			break;

		cout<<'.'<<flush; 
	}

	return 0;
}

#if OPTICK_MSVC
#pragma warning( pop )
#endif
