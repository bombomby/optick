# [Optick: C++ Profiler For Games](https://optick.dev)
Optick is a super-lightweight C++ profiler for Games.<br/>
It provides access for all the necessary tools required for efficient performance analysis and optimization:<br/>
instrumentation, switch-contexts, sampling, GPU counters.<br/>

## Build Status
| Windows | Linux | MacOS |
| ------- | ----- | ----- |
| [![Windows Build status](https://ci.appveyor.com/api/projects/status/bu5smbuh1d2lcsf6?svg=true)](https://ci.appveyor.com/project/bombomby/brofiler) | [![Linux+MacOS Build Status](https://travis-ci.org/bombomby/optick.svg)](https://travis-ci.org/bombomby/optick) | [![Linux+MacOS Build Status](https://travis-ci.org/bombomby/optick.svg)](https://travis-ci.org/bombomby/optick) |

![](https://optick.dev/images/screenshots/optick/Optick.png)

| Features | Windows | Linux | MacOS | XBox | PS4 |
| -------- | ------- | ----- | ----- | ---- | --- |
| Instrumentation | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :grey_question: |
| Switch Contexts | :heavy_check_mark: ETW | :heavy_check_mark: FTrace | :heavy_check_mark: DTrace | :heavy_check_mark: | :grey_question: |
| Sampling | :heavy_check_mark: ETW | :hourglass_flowing_sand: | :hourglass_flowing_sand: | :grey_question: | :grey_question: |
| GPU | :heavy_check_mark: D3D12, Vulkan | :heavy_check_mark: Vulkan | :heavy_check_mark: Vulkan | | |

:heavy_check_mark: - works out of the box, :hourglass_flowing_sand: - in progress, :grey_question: - will be available soon for the certified developers

## Basic Integration (one line of code)
1) Copy 'src' folder from the repository or latest release to your game project
2) Add `OPTICK_FRAME("MainThread");` macro to the main loop of your game and `#include "optick.h"` header
```c++
#include "optick.h"
...
while( true ) 
{
	OPTICK_FRAME("MainThread");
	engine.Update();
}
```
3) Use `OPTICK_EVENT();` macro to instrument a function
```c++
void SlowFunction()
{ 
	OPTICK_EVENT();
	...
}
```
	

## Samples
Run [generate_projects.gpu.bat](https://github.com/bombomby/optick/blob/master/generate_projects.gpu.bat) from the root folder to generate project files.<br/>
Open solution `build\vs2017\Optick.sln` with samples.

| [WindowsD3D12](https://github.com/bombomby/optick/tree/master/samples/WindowsD3D12) | [WindowsVulkan](https://github.com/bombomby/optick/tree/master/samples/WindowsVulkan) | [ConsoleApp](https://github.com/bombomby/optick/tree/master/samples/ConsoleApp) |
| ---------- | ------------ | ------------- |
| ![WindowsD3D12](https://optick.dev/images/screenshots/optick/WindowsD3D12.png) | ![WindowsVulkan](https://optick.dev/images/screenshots/optick/WindowsVulkan.png) | ![ConsoleApp](https://optick.dev/images/screenshots/optick/ConsoleApp2.png) |
| DirectX12 multithreading sample with Optick integration | SaschaWillems's vulkan multithreading sample with Optick integration | Basic ConsoleApp with Optick integration  (Windows, Linux, MacOS) |

## API
#### ```OPTICK_EVENT();```
Basic scoped performance counter. Use this counter 99% of the time.<br/>
It automatically extracts the name of the current function.
```c++
void Function()
{ 
	OPTICK_EVENT();
	...
}
```
You could also pass an optional name for this macro to override the name - `OPTICK_EVENT("ScopeName");`.<br/>
Useful for marking multiple scopes within one function.
#### ```OPTICK_CATEGORY("UpdateLogic", Optick::Category::GameLogic);```
Scoped performance counter with dedicated category.<br/>
Categories always go with predefined set of colors.<br/>
Use categories for high-level overview of the code.
#### ```OPTICK_THREAD("ThreadName");```
A macro for declaring a new thread.<br/>
Required for collecting events from the current thread.
#### ```OPTICK_TAG("ModelName", m_Name);```
A macro for attaching any custom data to the current scope.<br/>
Supported types: float, int32_t, uint32_t, uint64_t, float[3], const char*.
#### ```OPTICK_SET_STATE_CHANGED_CALLBACK(CALLBACK_FUNCTION);```
A macro for subscribing on state change event.<br/>
Useful for attaching screenshots or any custom files and data to the capture.<br/>
```c++
bool OnOptickStateChanged(Optick::State::Type state)
{
	if (state == Optick::State::STOP_CAPTURE)
	{
		// Starting to save screenshot
		g_TakingScreenshot = true;
	}

	if (state == Optick::State::DUMP_CAPTURE)
	{
		// Wait for screenshot to be ready
		// Returning false from this function will force Optick to retry again the next frame
		if (g_TakingScreenshot)
			return false;

		// Attach screenshot
		Optick::AttachFile(Optick::File::OPTICK_IMAGE, "Screenshot.bmp", g_ScreenshotRequest.c_str());
		
		// Attach text file
		const char* textFile = "You could attach custom text files!";
		Optick::AttachFile(Optick::File::OPTICK_TEXT, "Test.txt", (uint8_t*)textFile, (uint32_t)strlen(textFile));
		
		// Attaching some custom data
		Optick::AttachSummary("Build", __DATE__ " " __TIME__);
	}
	return true;
}
```
## GPU API
#### ```OPTICK_GPU_INIT_D3D12(DEVICE, CMD_QUEUES, NUM_CMD_QUEUS);```
#### ```OPTICK_GPU_INIT_VULKAN(DEVICES, PHYSICAL_DEVICES, CMD_QUEUES, CMD_QUEUES_FAMILY, NUM_CMD_QUEUS);```
#### ```OPTICK_GPU_CONTEXT(...);```
#### ```OPTICK_GPU_EVENT(NAME);```
#### ```OPTICK_GPU_FLIP(SWAP_CHAIN);```
