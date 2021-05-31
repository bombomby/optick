# [Optick: C++ Profiler For Games](https://optick.dev)
![GitHub](https://img.shields.io/github/license/bombomby/optick.svg) ![GitHub release](https://img.shields.io/github/release/bombomby/optick.svg) <br/>
![](https://optick.dev/images/screenshots/optick/Optick.png)
Optick is a super-lightweight C++ profiler for Games.<br/>
It provides access for all the necessary tools required for efficient performance analysis and optimization:<br/>
instrumentation, switch-contexts, sampling, GPU counters.<br/>
> Looking for 'Brofiler'? It has been renamed to 'Optick', so you are in the right place.
## Build Status
| Windows (x64: msvc) | Linux (x64: clang, gcc) | MacOS (x64: clang, gcc) | Static Code Analysis |
| ------- | ----- | ----- | --------------------- |
| [![Windows Build status](https://ci.appveyor.com/api/projects/status/bu5smbuh1d2lcsf6?svg=true)](https://ci.appveyor.com/project/bombomby/optick) | [![Linux+MacOS Build Status](https://travis-ci.org/bombomby/optick.svg)](https://travis-ci.org/bombomby/optick) | [![Linux+MacOS Build Status](https://travis-ci.org/bombomby/optick.svg)](https://travis-ci.org/bombomby/optick) | [![Total alerts](https://img.shields.io/lgtm/alerts/g/bombomby/optick.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/bombomby/optick/alerts/) [![Codacy Badge](https://api.codacy.com/project/badge/Grade/3195c1fa7d554dc1bb9d45dd30454b48)](https://www.codacy.com/app/bombomby/optick?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=bombomby/optick&amp;utm_campaign=Badge_Grade) |

| Features | Windows | Linux | MacOS | XBox | PS4 | UE4 |
| -------- | ------- | ----- | ----- | ---- | --- | --- |
| Instrumentation | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :grey_question: | :heavy_check_mark: |
| Switch Contexts | :heavy_check_mark: ETW | :heavy_check_mark: FTrace | :heavy_check_mark: DTrace | :heavy_check_mark: | :grey_question: | :heavy_check_mark: Win |
| Sampling | :heavy_check_mark: ETW | | | :grey_question: | :grey_question: | :heavy_check_mark: Win |
| GPU | :heavy_check_mark: D3D12, Vulkan | :heavy_check_mark: Vulkan | :heavy_check_mark: Vulkan | | | :hourglass_flowing_sand: |

:heavy_check_mark: - works out of the box, :hourglass_flowing_sand: - in progress, :grey_question: - coming soon for the certified developers

## List of Games and Studios using Optick(Brofiler)
![Allods Team](https://optick.dev/images/studios/AllodsTeam_thumb2.png "Allods Team") ![4A Games](https://optick.dev/images/studios/4A_Games_thumb2.png "4A Gaemes") ![CryEngine](https://optick.dev/images/studios/CryEngine_thumb.png "CryEngine") ![Larian Studios](https://optick.dev/images/studios/Larian_png.png "Larian Studios")
![Skyforge](https://optick.dev/images/studios/Skyforge_thumb.jpg "Skyforge") ![Metro Exodus](https://optick.dev/images/studios/Metro_thumb.jpg "Metro Exodus")  ![Warface](https://optick.dev/images/studios/Warface_thumb.jpg "Metro Exodus") ![Armored Warfare](https://optick.dev/images/studios/ArmoredWarfare_thumb.jpg "Metro Exodus")

## Video Tutorial
[![Optick Video Tutorial](https://github.com/bombomby/brofiler/blob/gh-pages/images/VideoThumbnail.jpg)](https://www.youtube.com/watch?v=p57TV5342fo)

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
4) Add `OPTICK_THREAD("Name");` macro to declare a new thread with Optick
```c++
void WorkerThread(...)
{
	OPTICK_THREAD("Worker");
	while (isRunning)
	{
		...
	}
}
```
5) Edit `optick.config.h` to enable/disable some of the features in specific configs or platforms.<br/>(e.g. disabling Optick in final builds)

> :warning: If your Game uses **dynamic linking** and you are planning to **use Optick from multiple dlls** within the same executable - please make sure that Optick's code is added to the common **Dynamic Library** and this library is compiled with **OPTICK_EXPORT** define (Static Library won't work).<br/>
> You could also use precompiled **OptickCore.dll** which is packaged with every release:
> - Add `include` folder to the extra include dirs of your project
> - Add `lib/x64/debug` and `lib/x64/release` to the extra library dirs of your project
> - Copy `lib/x64/debug/OptickCore.dll` and `lib/x64/release/OptickCore.dll` to the debug and release output folders of your project respectively

## API
All the available API calls are documented here:<br/>
https://github.com/bombomby/optick/wiki/Optick-API

## Unreal Engine
Optick provides a special plugin for UE4. Check more detailed documentation here: 
https://github.com/bombomby/optick/wiki/UE4-Optick-Plugin <br/>
![](https://github.com/bombomby/brofiler/blob/gh-pages/images/UE4_Optick_1.png)

## Samples
Run [GenerateProjects_gpu.bat](https://github.com/bombomby/optick/blob/master/tools/GenerateProjects_gpu.bat) to generate project files. To compile the samples you'll need to install VulkanSDK. Alternatively you could use [GenerateProjects.bat](https://github.com/bombomby/optick/blob/master/tools/GenerateProjects.bat) to generate only minimal solution with ConsoleApp sample.<br/>
Open solution `build\vs2017\Optick.sln` with generated samples.

| [WindowsD3D12](https://github.com/bombomby/optick/tree/master/samples/WindowsD3D12) | [WindowsVulkan](https://github.com/bombomby/optick/tree/master/samples/WindowsVulkan) | [ConsoleApp](https://github.com/bombomby/optick/tree/master/samples/ConsoleApp) |
| ---------- | ------------ | ------------- |
| ![WindowsD3D12](https://optick.dev/images/screenshots/optick/WindowsD3D12.png) | ![WindowsVulkan](https://optick.dev/images/screenshots/optick/WindowsVulkan.png) | ![ConsoleApp](https://optick.dev/images/screenshots/optick/ConsoleApp2.png) |
| DirectX12 multithreading sample with Optick integration | SaschaWillems's vulkan multithreading sample with Optick integration | Basic ConsoleApp with Optick integration  (Windows, Linux, MacOS) |

## Brofiler
Brofiler has been renamed into Optick starting from v1.2.0.<br/>
All the future development is going under the new name.<br/>
Cheatsheet for upgrading to the new version:
* `BROFILER_FRAME("MainThread");` => `OPTICK_FRAME("MainThread");`
* `BROFILER_THREAD("WorkerThread");` => `OPTICK_THREAD("WorkerThread");`
* `BROFILER_CATEGORY("Physics", Brofiler::Color::Green);` => `OPTICK_CATEGORY("Physics", Optick::Category::Physics);`
* `BROFILER_EVENT(NAME);` => `OPTICK_EVENT(NAME);`
* `PROFILE;` => `OPTICK_EVENT();`

## How To Start?
You can find a short instruction here:<br/>
https://github.com/bombomby/optick/wiki/How-to-start%3F-(Programmers-Setup)

