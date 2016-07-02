if not _ACTION then
	_ACTION="vs2012"
end

isPosix = false
isVisualStudio = false
isOSX = false

if _ACTION == "vs2002" or _ACTION == "vs2003" or _ACTION == "vs2005" or _ACTION == "vs2008" or _ACTION == "vs2010" or _ACTION == "vs2012" then
	isVisualStudio = true
end

if _ACTION == "codeblocks" or _ACTION == "gmake"
then
	isPosix = true
end

if _ACTION == "xcode3" or os.is("macosx")
then
	isOSX = true
end

	
solution "Brofiler"

	language "C++"

	location ( "Build/" .. _ACTION )
	flags {"NoManifest", "ExtraWarnings", "StaticRuntime", "NoMinimalRebuild", "FloatFast", "EnableSSE2" }
	optimization_flags = { "OptimizeSpeed" }
	targetdir("Bin")

if isPosix or isOSX then
	defines { "_XOPEN_SOURCE=600" }
end

if isVisualStudio then
	debugdir ("Bin")
end


	local config_list = {
		"Release",
		"Debug",
	}
	local platform_list = {
		"x32",
		"x64"
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

configuration "x32"
if isVisualStudio then
-- Compiler Warning (level 4) C4127. Conditional expression is constant
-- Compiler Warning 		  C4250. inherits 'std::basic_ostream'
        buildoptions { "/wd4127", "/wd4250" }
else
	buildoptions { "-std=c++11" }
  if isPosix then
  	linkoptions { "-rdynamic" }
  	if isOSX then
		buildoptions { "-Wno-invalid-offsetof -Wno-deprecated-declarations -fsanitize=address -fno-omit-frame-pointer" }
		linkoptions { "-fsanitize=address" }
	else
		buildoptions { "-Wno-invalid-offsetof -fsanitize=undefined -fPIE -g -fno-omit-frame-pointer" }
  		linkoptions { "-fsanitize=undefined -pie" }
  	end
  end
end

configuration "x64"
if isVisualStudio then
-- Compiler Warning (level 4) C4127. Conditional expression is constant
        buildoptions { "/wd4127"  }
else
	buildoptions { "-std=c++11" }
  if isPosix then
  	linkoptions { "-rdynamic" }
  	if isOSX then
		buildoptions { "-Wno-invalid-offsetof -Wno-deprecated-declarations -fsanitize=address -fno-omit-frame-pointer" }
		linkoptions { "-fsanitize=address" }
	else
		buildoptions { "-Wno-invalid-offsetof -fsanitize=undefined -fPIE -g -fno-omit-frame-pointer" }
  		linkoptions { "-fsanitize=undefined -pie" }
  	end
  end
end


--  give each configuration/platform a unique output directory

for _, config in ipairs(config_list) do
	for _, plat in ipairs(platform_list) do
		configuration { config, plat }
		objdir    ( "Build/" .. _ACTION .. "/Temp/" )
	end
end

os.mkdir("./Bin")

-- SUBPROJECTS

project "BrofilerCore"
	kind "StaticLib"
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
			"BrofilerCore/ETW.cpp",
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
 	kind "ConsoleApp"
 	files {
		"BrofilerTest/**.h", 
 		"BrofilerTest/**.cpp", 
 	}

	includedirs
	{
		"BrofilerCore"
	}
	
	links {
		"BrofilerCore"
	}

