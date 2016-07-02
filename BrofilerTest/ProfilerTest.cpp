#include <iostream>
#include "TestEngine.h"

using namespace std;

int main(int, char **)
{
	cout << "Starting profiler test." << endl;

	Test::Engine engine;
	cout << "Engine successfully created." << endl;

	cout << "Starting main loop update." << endl;
	while( engine.Update() ) { cout<<'.'; }
	
	return 0;
}