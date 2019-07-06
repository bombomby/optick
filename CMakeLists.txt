cmake_minimum_required(VERSION 3.2)
project(Optick LANGUAGES CXX C)

# Options
option(OPTICK_USE_VULKAN "Built-in support for Vulkan" OFF)
option(OPTICK_USE_D3D12 "Built-in support for DicrectX 12" OFF)
option(OPTICK_BUILD_GUI_APP "Build Optick Gui viewer app" ON)
option(OPTICK_BUILD_CONSOLE_APP "Build Optick Console sample app" OFF)


set_property(GLOBAL PROPERTY USE_FOLDERS ON)
set(CMAKE_CXX_STANDARD 11)
list(APPEND CMAKE_MODULE_PATH "${CMAKE_SOURCE_DIR}/Build")


# OptickCore
file(GLOB OPTICK_SRC ${PROJECT_SOURCE_DIR}/src/*.*)
source_group("OptickCore" FILES ${OPTICK_SRC})
add_library(OptickCore SHARED ${OPTICK_SRC})
target_include_directories(OptickCore PUBLIC "${PROJECT_SOURCE_DIR}/src")
target_compile_definitions(OptickCore PUBLIC OPTICK_EXPORTS=1)
if(OPTICK_USE_VULKAN)
	target_include_directories(OptickCore PRIVATE "$(VULKAN_SDK)/Include")
else()
	target_compile_definitions(OptickCore PRIVATE OPTICK_ENABLE_GPU_VULKAN=0)
endif()
if(!OPTICK_USE_D3D12)
	target_compile_definitions(OptickCore PRIVATE OPTICK_ENABLE_GPU_D3D12=0)
endif()
if(OPTICK_USE_D3D12 OR OPTICK_USE_VULKAN)
	target_compile_definitions(OptickCore PRIVATE OPTICK_ENABLE_GPU=1)
else()
	target_compile_definitions(OptickCore PRIVATE OPTICK_ENABLE_GPU=0)
endif()

set(EXTRA_LIBS ${EXTRA_LIBS} OptickCore pthread)


# GuiApp
if(MSVC AND OPTICK_BUILD_GUI_APP)
	add_subdirectory(gui)
endif()


# ConsoleApp
if(OPTICK_BUILD_CONSOLE_APP)
	file(GLOB TEST_ENGINE_SRC ${PROJECT_SOURCE_DIR}/samples/Common/TestEngine/*.*)
	source_group("TestEngine" FILES ${TEST_ENGINE_SRC})
	add_executable(ConsoleApp ${PROJECT_SOURCE_DIR}/samples/ConsoleApp/main.cpp ${TEST_ENGINE_SRC})
	target_include_directories(ConsoleApp PRIVATE "${PROJECT_SOURCE_DIR}/samples/Common/TestEngine")
	target_link_libraries(ConsoleApp ${EXTRA_LIBS})
	set_target_properties(ConsoleApp PROPERTIES FOLDER Samples)
endif()

