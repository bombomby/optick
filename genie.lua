newoption {
	trigger = "UWP",
	description = "Generates Universal Windows Platform application type",
}

if not _ACTION then
	_ACTION="vs2012"
end

isVisualStudio = false
isUWP = false

if _ACTION == "vs2010" or _ACTION == "vs2012" or _ACTION == "vs2015" then
	isVisualStudio = true
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
	targetdir("Bin")

if isVisualStudio then
-- Compiler Warning (level 4) C4127. Conditional expression is constant
-- Compiler Warning 		  C4250. inherits 'std::basic_ostream'
    buildoptions { "/wd4127", "/wd4250" }
end
	
if isVisualStudio then
	debugdir ("Bin")
end

if isUWP then
	defines { "BRO_UWP=1" }
end

	local config_list = {
		"Release",
		"Debug",
	}

	local platform_list = {
		"x32",
		"x64",
	}

	configurations(config_list)
	platforms(platform_list)


-- CONFIGURATIONS

configuration "Release"
	defines { "NDEBUG" }
	flags { "Symbols", optimization_flags }

configuration "Debug"
	defines { "_DEBUG", "_CRTDBG_MAP_ALLOC"}
	flags { "Symbols" }

--  give each configuration/platform a unique output directory

for _, config in ipairs(config_list) do
	for _, plat in ipairs(platform_list) do
		configuration { config, plat }
		objdir    ( outputFolder .. "/Temp/" )
		targetdir ("Bin/" .. plat .. "/" .. config)
	end
end

os.mkdir("./Bin")

-- SUBPROJECTS

project "BrofilerCore"
	kind "StaticLib"
	uuid "830934D9-6F6C-C37D-18F2-FB3304348F00"
	defines { "_CRT_SECURE_NO_WARNINGS" }
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

project "BrofilerTest"
 	flags {"NoPCH"}
 	kind "StaticLib"
	uuid "9A313DD9-8694-CC7D-2F1A-05341B5C9800"
 	files {
		"BrofilerTest/**.*", 
 	}
	
	vpaths { 
		["*"] = "BrofilerTest" 
	}

	includedirs
	{
		"BrofilerCore"
	}
	
	links {
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

	includedirs
	{
		"BrofilerCore",
		"BrofilerTest"
	}
	
	links {
		"BrofilerCore",
		"BrofilerTest"
	}
end

