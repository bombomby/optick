cmake_minimum_required(VERSION 3.2)
project(Optick LANGUAGES CXX)


set_property(GLOBAL PROPERTY USE_FOLDERS ON)
set(CMAKE_CXX_STANDARD 11)


# hacks for standalone builds with visual studio
if(MSVC AND CMAKE_SOURCE_DIR STREQUAL CMAKE_CURRENT_SOURCE_DIR)
	message(STATUS "Standalone build")
	list(APPEND CMAKE_MODULE_PATH "${CMAKE_SOURCE_DIR}/Build")
	set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)
	set(standalone ON)
else()
	set(standalone OFF)
endif()


# Sources
file(GLOB OPTICK_SRC "src/*.*")
source_group("OptickCore" FILES ${OPTICK_SRC})


# Enabled
option(OPTICK_ENABLED "Enable profiling with Optick" ON)
if(NOT OPTICK_ENABLED)
	message(STATUS "Optick is disabled")
	# add dummy target as a replacement
	add_library(OptickCore STATIC ${OPTICK_SRC})
	target_include_directories(OptickCore PUBLIC "src")
	target_compile_definitions(OptickCore PUBLIC USE_OPTICK=0)
	return()
endif()


# Options
option(OPTICK_USE_VULKAN "Built-in support for Vulkan" OFF)
option(OPTICK_USE_D3D12 "Built-in support for DirectX 12" OFF)
option(OPTICK_BUILD_GUI_APP "Build Optick gui viewer app" OFF)
option(OPTICK_BUILD_CONSOLE_SAMPLE "Build Optick console sample app" ${standalone})

# OptickCore
add_library(OptickCore SHARED ${OPTICK_SRC})
target_include_directories(OptickCore PUBLIC "src")
target_compile_definitions(OptickCore PUBLIC OPTICK_EXPORTS=1)
if(OPTICK_USE_VULKAN)
	message(STATUS "Optick uses Vulkan")
	find_package(Vulkan REQUIRED)
	target_link_libraries(OptickCore PRIVATE Vulkan::Vulkan)
else()
	target_compile_definitions(OptickCore PRIVATE OPTICK_ENABLE_GPU_VULKAN=0)
endif()
if(OPTICK_USE_D3D12)
	message(STATUS "Optick uses DirectX 12")
else()
	target_compile_definitions(OptickCore PRIVATE OPTICK_ENABLE_GPU_D3D12=0)
endif()
if(OPTICK_USE_D3D12 OR OPTICK_USE_VULKAN)
	target_compile_definitions(OptickCore PRIVATE OPTICK_ENABLE_GPU=1)
else()
	target_compile_definitions(OptickCore PRIVATE OPTICK_ENABLE_GPU=0)
endif()

if(MSVC)
	# temporary solution to unblock C++17 users
	target_compile_definitions(OptickCore PRIVATE _SILENCE_ALL_CXX17_DEPRECATION_WARNINGS)
endif()

set(EXTRA_LIBS ${EXTRA_LIBS} OptickCore)
if(NOT MSVC)
	set(EXTRA_LIBS ${EXTRA_LIBS} pthread)
endif()


# Gui App
if(MSVC AND OPTICK_BUILD_GUI_APP)
	add_subdirectory(gui)
endif()


# Console App
if(OPTICK_BUILD_CONSOLE_SAMPLE)
	file(GLOB TEST_ENGINE_SRC "samples/Common/TestEngine/*.*")
	source_group("TestEngine" FILES ${TEST_ENGINE_SRC})
	add_executable(ConsoleApp "samples/ConsoleApp/main.cpp" ${TEST_ENGINE_SRC})
	target_include_directories(ConsoleApp PRIVATE "samples/Common/TestEngine")
	target_link_libraries(ConsoleApp ${EXTRA_LIBS})
	set_target_properties(ConsoleApp PROPERTIES FOLDER Samples)
endif()

