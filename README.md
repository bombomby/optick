# [Optick: C++ Profiler For Games](https://optick.dev)
Optick is a super-lightweight C++ profiler for Games.<br/>
It provides access for all the necessary tools required for efficient performance analysis and optimization:<br/>
instrumentation, switch-contexts, sampling, GPU counters.<br/>
This tool will be useful for any type of the game: from Indie to AAA.<br/>

## Build Status
| Windows | Linux | MacOS |
| ------- | ----- | ----- |
| [![Windows Build status](https://ci.appveyor.com/api/projects/status/bu5smbuh1d2lcsf6?svg=true)](https://ci.appveyor.com/project/bombomby/brofiler) | [![Linux+MacOS Build Status](https://travis-ci.org/bombomby/brofiler.svg?branch=v2.0)](https://travis-ci.org/bombomby/brofiler) | [![Linux+MacOS Build Status](https://travis-ci.org/bombomby/brofiler.svg?branch=v2.0)](https://travis-ci.org/bombomby/brofiler) |

![](https://optick.dev/images/screenshots/optick/Optick.png)

| Features | Windows | Linux | MacOS | XBox | PS4 |
| -------- | ------- | ----- | ----- | ---- | --- |
| Instrumentation | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :grey_question: |
| Switch Contexts | :heavy_check_mark: ETW | :heavy_check_mark: FTrace | :heavy_check_mark: DTrace | :heavy_check_mark: | :grey_question: |
| Sampling | :heavy_check_mark: ETW | :hourglass_flowing_sand: | :hourglass_flowing_sand: | :grey_question: | :grey_question: |
| GPU | :heavy_check_mark: D3D12, Vulkan | :heavy_check_mark: Vulkan | :heavy_check_mark: Vulkan | | |

:heavy_check_mark: - available now, :hourglass_flowing_sand: - in progress, :grey_question: - will be available soon for the certified developers

## Integration Time - 1 minute
1) Copy 'src' folder from the repository to your game project
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
