#include <iostream>
#include "TestEngine.h"
#include "Brofiler.h"

using namespace std;

int wmain()
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