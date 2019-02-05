project (Brofiler)
cmake_minimum_required (VERSION 3.0)

# Options: GAPI
option(BUILD_VULKAN "Built-in support for Vulkan" OFF)
option(BUILD_D3D12 "Built-in support for DicrectX 12" OFF)
# Options: Other
option(BUILD_FIBERS "Built-in support for Fibers" OFF)

set_property(GLOBAL PROPERTY PREDEFINED_TARGETS_FOLDER "CMake")
set_property(GLOBAL PROPERTY USE_FOLDERS ON)

set (CMAKE_CXX_STANDARD 11)
set (CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/Build)

set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)

#BrofilerCore
file(GLOB BROFILER_SOURCE ${PROJECT_SOURCE_DIR}/BrofilerCore/*.*)
SET(BROFILER_SRC ${BROFILER_SOURCE})  
source_group("BrofilerCore" FILES ${BROFILER_SOURCE})
include_directories ("${PROJECT_SOURCE_DIR}/BrofilerCore")

if(BUILD_VULKAN)
	include_directories("$(VULKAN_SDK)/Include")
else()
	add_definitions(-DBRO_ENABLE_GPU_VULKAN=0)
endif()	


if(!BUILD_D3D12)
	add_definitions(-DBRO_ENABLE_GPU_D3D12=0)
endif()	

add_library(BrofilerCore ${BROFILER_SRC})

set (EXTRA_LIBS ${EXTRA_LIBS} BrofilerCore pthread)

#ConsoleApp
file(GLOB TEST_ENGINE_SOURCE ${PROJECT_SOURCE_DIR}/Samples/Common/TestEngine/*.*)
SET(TEST_ENGINE_SRC ${TEST_ENGINE_SOURCE})  
source_group("TestEngine" FILES ${TEST_ENGINE_SRC})
include_directories("${PROJECT_SOURCE_DIR}/Samples/Common/TestEngine")
add_executable(ConsoleApp ${PROJECT_SOURCE_DIR}/Samples/ConsoleApp/main.cpp ${TEST_ENGINE_SRC})
target_link_libraries (ConsoleApp ${EXTRA_LIBS})
set_target_properties (ConsoleApp PROPERTIES
    FOLDER Samples
)