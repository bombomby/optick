newoption {
	trigger = "UWP",
	description = "Generates Universal Windows Platform application type",
}

if not _ACTION then
	_ACTION="vs2012"
end

outFolderRoot = "Bin/" .. _ACTION .. "/";

isVisualStudio = false
isUWP = false

if _ACTION == "vs2010" or _ACTION == "vs2012" or _ACTION == "vs2015" then
	if _OPTIONS['platform'] ~= "orbis"  then
		isVisualStudio = true
	end
end


if _OPTIONS["UWP"] then
	isUWP = true
end

if isUWP then
	premake.vstudio.toolset = "v140"
	premake.vstudio.storeapp = "10.0"
end

outputFolder = "Build/" .. _ACTION

if isUWP then
	outputFolder = outputFolder .. "_UWP"
end

solution "Brofiler"
	language "C++"
	startproject "BrofilerTest"

	location ( outputFolder )
	flags { "NoManifest", "ExtraWarnings", "Unicode" }
	optimization_flags = { "OptimizeSpeed" }
	targetdir(outFolderRoot)

	
if isVisualStudio then
	debugdir (outFolderRoot)
end

if isUWP then
	defines { "BRO_UWP=1" }
end

	defines { "USE_BROFILER=1"}
	defines { "BRO_FIBERS=1"}

	local config_list = {
		"Release",
		"Debug",
	}

	local platform_list = {
		"x32",
		"x64",
		"Native",
	}

	configurations(config_list)
	platforms(platform_list)


-- CONFIGURATIONS

configuration "Release"
	defines { "NDEBUG", "MT_INSTRUMENTED_BUILD" }
	flags { "Symbols", optimization_flags }

configuration "Debug"
	defines { "_DEBUG", "_CRTDBG_MAP_ALLOC", "MT_INSTRUMENTED_BUILD"}
	flags { "Symbols" }

--  give each configuration/platform a unique output directory

for _, config in ipairs(config_list) do
	for _, plat in ipairs(platform_list) do
		configuration { config, plat }
		objdir    ( outputFolder .. "/Temp/" )
		targetdir (outFolderRoot .. plat .. "/" .. config)
	end
end

os.mkdir("./" .. outFolderRoot)

-- SUBPROJECTS

project "BrofilerCore"
	kind "StaticLib"
	uuid "830934D9-6F6C-C37D-18F2-FB3304348F00"
	defines { "_CRT_SECURE_NO_WARNINGS" }

	includedirs
	{
		"ThirdParty/TaskScheduler/Scheduler/Include"
	}

	files {
		"BrofilerCore/**.cpp",
        "BrofilerCore/**.h", 
	}
	vpaths {
		["API"] = { 
			"BrofilerCore/Brofiler.h",
		},
		["Core"] = {
			"BrofilerCore/Core.h",
			"BrofilerCore/Core.cpp",
			"BrofilerCore/ETW.h",
			"BrofilerCore/Event.h",
			"BrofilerCore/Event.cpp",
			"BrofilerCore/EventDescription.h",
			"BrofilerCore/EventDescription.cpp",
			"BrofilerCore/EventDescriptionBoard.h",
			"BrofilerCore/EventDescriptionBoard.cpp",
			"BrofilerCore/Sampler.h",
			"BrofilerCore/Sampler.cpp",
			"BrofilerCore/SymEngine.h",
			"BrofilerCore/SymEngine.cpp",
			"BrofilerCore/Tracer.h",
			"BrofilerCore/Tracer.cpp",
		},
		["Network"] = {
			"BrofilerCore/Message.h", 
			"BrofilerCore/Message.cpp",
			"BrofilerCore/ProfilerServer.h", 
			"BrofilerCore/ProfilerServer.cpp", 
			"BrofilerCore/Socket.h", 
			"BrofilerCore/Serialization.h", 
			"BrofilerCore/Serialization.cpp", 
		},
		["System"] = {
			"BrofilerCore/Common.h",
			"BrofilerCore/Concurrency.h",
			"BrofilerCore/HPTimer.h",
			"BrofilerCore/HPTimer.cpp",
			"BrofilerCore/MemoryPool.h",
			"BrofilerCore/Thread.h",
			"BrofilerCore/Types.h",
		},
	}
	
project "TaskScheduler"
    kind "StaticLib"
 	flags {"NoPCH"}
	defines {"USE_BROFILER=1"}
 	files {
 		"ThirdParty/TaskScheduler/Scheduler/**.*", 
 	}

	includedirs
	{
		"ThirdParty/TaskScheduler/Scheduler/Include",
		"BrofilerCore"
	}

	excludes { "Src/Platform/Posix/**.*" }
	
	vpaths { 
		["*"] = "TaskScheduler" 
	}
	
	links {
		"BrofilerCore",
	}

project "BrofilerTest"
 	flags {"NoPCH"}
 	kind "StaticLib"
	uuid "9A313DD9-8694-CC7D-2F1A-05341B5C9800"
 	files {
		"BrofilerTest/**.*", 
 	}

	includedirs
	{
		"BrofilerCore",
		"ThirdParty/TaskScheduler/Scheduler/Include"
	}

	links {
		"BrofilerCore",
		"TaskScheduler",
		"BrofilerCore"
	}
	
if isUWP then
-- Genie can't generate proper UWP application
-- It's a dummy project to match existing project file
project "BrofilerDurangoTest"
	location( "BrofilerDurangoTest" )
 	kind "WindowedApp"
	uuid "5CA6AF66-C2CB-412E-B335-B34357F2FBB6"
	files {
		"BrofilerDurangoTest/**.*", 
 	}
else
project "BrofilerWindowsTest"
 	flags {"NoPCH"}
 	kind "ConsoleApp"
	uuid "C50A1240-316C-EF4D-BAD9-3500263A260D"
 	files {
		"BrofilerWindowsTest/**.*", 
 	}
	
	vpaths { 
		["*"] = "BrofilerWindowsTest" 
	}

	includedirs {
		"BrofilerCore",
		"BrofilerTest",
		"ThirdParty/TaskScheduler/Scheduler/Include"
	}
	
	links {
		"BrofilerCore",
		"BrofilerTest",
		"TaskScheduler"
	}
end

