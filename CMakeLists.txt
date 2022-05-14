cmake_minimum_required(VERSION 3.2)
project(Optick LANGUAGES CXX VERSION 1.3.0)


set_property(GLOBAL PROPERTY USE_FOLDERS ON)
set(CMAKE_CXX_STANDARD 11)


# hacks for standalone builds with visual studio
if(MSVC AND CMAKE_SOURCE_DIR STREQUAL CMAKE_CURRENT_LIST_DIR)
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
option(OPTICK_INSTALL_TARGETS "Should optick be installed? Set to OFF if you use add_subdirectory to include Optick." ON)

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
target_include_directories(OptickCore
	PUBLIC
		$<BUILD_INTERFACE:${CMAKE_CURRENT_LIST_DIR}/src>
)
set_target_properties(OptickCore 
	PROPERTIES 
		PUBLIC_HEADER "${CMAKE_CURRENT_LIST_DIR}/src/optick.h;${CMAKE_CURRENT_LIST_DIR}/src/optick.config.h"
		DEBUG_POSTFIX d # So that we can install debug and release side by side
)
target_compile_definitions(OptickCore PRIVATE OPTICK_EXPORTS=1)
if(OPTICK_USE_VULKAN)
	message(STATUS "Optick uses Vulkan")
	find_package(Vulkan REQUIRED)
	target_link_libraries(OptickCore PRIVATE Vulkan::Vulkan)
else()
	target_compile_definitions(OptickCore PRIVATE OPTICK_ENABLE_GPU_VULKAN=0)
endif()
if(OPTICK_USE_D3D12)
	message(STATUS "Optick uses DirectX 12")
	target_link_libraries(OptickCore PRIVATE "d3d12.lib" "dxgi.lib")
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


###############
## Packaging ##
###############
if(OPTICK_INSTALL_TARGETS)
	include(CMakePackageConfigHelpers)
	include(GNUInstallDirs)

	set(${PROJECT_NAME}_INSTALL_CMAKEDIR "${CMAKE_INSTALL_LIBDIR}/cmake/${PROJECT_NAME}" CACHE STRING "Path to smartcrop cmake files")

	# Use version checking boilerplate
	write_basic_package_version_file(
		${PROJECT_NAME}ConfigVersion.cmake
		COMPATIBILITY SameMajorVersion
	)

	configure_package_config_file(
		${PROJECT_SOURCE_DIR}/tools/${PROJECT_NAME}Config.cmake.in
		${PROJECT_BINARY_DIR}/${PROJECT_NAME}Config.cmake
		INSTALL_DESTINATION ${${PROJECT_NAME}_INSTALL_CMAKEDIR} 
		# Imported targets do not require the following macros
		NO_SET_AND_CHECK_MACRO
		NO_CHECK_REQUIRED_COMPONENTS_MACRO 
	)

	install(TARGETS OptickCore
		EXPORT ${PROJECT_NAME}_Targets
		RUNTIME  DESTINATION ${CMAKE_INSTALL_BINDIR}
		LIBRARY  DESTINATION ${CMAKE_INSTALL_LIBDIR}
		ARCHIVE  DESTINATION ${CMAKE_INSTALL_LIBDIR}
		INCLUDES DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}
		PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}

	)

	install(
		EXPORT ${PROJECT_NAME}_Targets 
		NAMESPACE ${PROJECT_NAME}::
		FILE ${PROJECT_NAME}Targets.cmake
		DESTINATION ${${PROJECT_NAME}_INSTALL_CMAKEDIR}
	)


	install(FILES 
		${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME}Config.cmake
		${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME}ConfigVersion.cmake
		DESTINATION
			${${PROJECT_NAME}_INSTALL_CMAKEDIR}
	)
endif()



