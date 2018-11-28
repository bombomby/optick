newoption {
	trigger = "UWP",
	description = "Generates Universal Windows Platform application type",
}

newoption {
	trigger = "DX12",
	description = "Generates a sample for DirectX12",
}

newoption {
	trigger = "Vulkan",
	description = "Generates a sample for Vulkan",
}

newoption {
    trigger = "Fibers",
    description = "Enables fibers support",
}

if not _ACTION then
	_ACTION="vs2017"
end

outFolderRoot = "Bin/" .. _ACTION .. "/";

isVisualStudio = false
isUWP = false
isDX12 = false
isVulkan = false

if _ACTION == "vs2010" or _ACTION == "vs2012" or _ACTION == "vs2015" or _ACTION == "vs2017" then
	isVisualStudio = true
end

if _OPTIONS["UWP"] then
	isUWP = true
end

if _OPTIONS["DX12"] then
	isDX12 = true
end

if _OPTIONS["Vulkan"] then
	isVulkan = true
end

if _OPTIONS["Fibers"] then
    isFibersEnabled = true
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
if _ACTION == "vs2017" then
	-- windowstargetplatformversion "10.0.17134.0"
	systemversion "latest"
end
	startproject "ConsoleApp"
    cppdialect "C++11"
	location ( outputFolder )
	flags { "NoManifest" }
    symbols "On"
	optimization_flags = {}

if isVisualStudio then
	debugdir (outFolderRoot)
	buildoptions { 
		"/wd4127", -- Conditional expression is constant
		"/wd4091"  -- 'typedef ': ignored on left of '' when no variable is declared
	}
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

if _ACTION == "vs2010" then
defines { "_DISABLE_DEPRECATE_STATIC_CPPLIB", "_STATIC_CPPLIB"}
end

configuration "Release"
	targetdir(outFolderRoot .. "/Native/Release")
	defines { "NDEBUG", "MT_INSTRUMENTED_BUILD" }
	flags { optimization_flags }
    optimize "Speed"

configuration "Debug"
	targetdir(outFolderRoot .. "/Native/Debug")
	defines { "_DEBUG", "_CRTDBG_MAP_ALLOC", "MT_INSTRUMENTED_BUILD" }

--  give each configuration/platform a unique output directory

for _, config in ipairs(config_list) do
	for _, plat in ipairs(platform_list) do
		configuration { config, plat }
			objdir    ( outputFolder .. "/Temp/" )
			targetdir ( outFolderRoot .. plat .. "/" .. config )
	end
end

os.mkdir("./" .. outFolderRoot)




-- SUBPROJECTS

project "BrofilerCore"
	uuid "830934D9-6F6C-C37D-18F2-FB3304348F00"
	defines { "_CRT_SECURE_NO_WARNINGS", "BROFILER_LIB=1" }
	systemversion "10.0.15063.0"

-- if _OPTIONS['platform'] ~= "orbis" then
-- 	kind "SharedLib"
-- 	defines { "BROFILER_EXPORTS" }
-- else
	kind "StaticLib"
-- end

	includedirs
	{
		"BrofilerCore"
	}
	
	if isDX12 then
	--	includedirs
	--	{
	--		"$(DXSDK_DIR)Include",
	--	}
	--	links { 
	--		"d3d12", 
	--		"dxgi",
	--	}
	else
		defines { "BRO_ENABLE_GPU_D3D12=0" }
	end
	
	if isVulkan then
		includedirs
		{
			"$(VULKAN_SDK)/Include",
		}
	else
		defines { "BRO_ENABLE_GPU_VULKAN=0" }
	end
	
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
			"BrofilerCore/CityHash.h",
			"BrofilerCore/CityHash.cpp",
			"BrofilerCore/HPTimer.h",
			"BrofilerCore/HPTimer.cpp",
			"BrofilerCore/Memory.h",
			"BrofilerCore/Memory.cpp",
			"BrofilerCore/MemoryPool.h",
			"BrofilerCore/StringHash.h",
			"BrofilerCore/Platform.h",
			"BrofilerCore/Timer.h",
			"BrofilerCore/ThreadID.h",
			"BrofilerCore/Types.h",
		},
	}
	
group "Samples"
if isFibersEnabled then
	project "TaskScheduler"
		excludes { "ThirdParty/TaskScheduler/Scheduler/Source/MTDefaultAppInterop.cpp", }
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
		
		links {
			"BrofilerCore",
		}
end

if isUWP then
	-- Genie can't generate proper UWP application
	-- It's a dummy project to match existing project file
	project "DurangoUWP"
		location( "Samples/DurangoUWP" )
		kind "WindowedApp"
		uuid "5CA6AF66-C2CB-412E-B335-B34357F2FBB6"
		files {
			"Samples/DurangoUWP/**.*", 
		}
else
	project "ConsoleApp"
		flags {"NoPCH"}
		kind "ConsoleApp"
		uuid "C50A1240-316C-EF4D-BAD9-3500263A260D"
		files {
			"Samples/ConsoleApp/**.*", 
			"Samples/Common/TestEngine/**.*",
		}
		
		includedirs {
			"BrofilerCore",
			"Samples/Common/TestEngine",
			"ThirdParty/TaskScheduler/Scheduler/Include"
		}
		
		links {
			"BrofilerCore"
		}

		vpaths { 
			["*"] = "Samples/ConsoleApp"
		}

        if isFibersEnabled then
        defines {
			"BRO_ENABLE_FIBERS=1"
		}
		files {
            "ThirdParty/TaskScheduler/Scheduler/Source/MTDefaultAppInterop.cpp",
        }
        links {
            "TaskScheduler"
        }
        end
		
end

if isDX12 then
	project "WindowsD3D12"
		entrypoint "WinMainCRTStartup"
		flags {"NoPCH"}
		kind "WindowedApp"
		uuid "D055326C-F1F3-4695-B7E2-A683077BE4DF"

		buildoptions { 
		"/wd4324", -- structure was padded due to alignment specifier
		"/wd4238"  -- nonstandard extension used: class rvalue used as lvalue
		}
		
		links { 
			"d3d12", 
			"dxgi",
			"d3dcompiler"
		}
		
		files {
			"Samples/WindowsD3D12/**.h", 
			"Samples/WindowsD3D12/**.cpp", 
		}
		
		includedirs {
			"BrofilerCore",
		}
		
		links {
			"BrofilerCore",
		}
		
		vpaths { 
			["*"] = "Samples/WindowsD3D12" 
		}
end

if isVulkan then
	project "WindowsVulkan"
		entrypoint "WinMainCRTStartup"
		flags {"NoPCH"}
		kind "WindowedApp"
		uuid "07A250C4-4432-45FE-9E63-BB7F71B7C14C"

		defines {
			"VK_USE_PLATFORM_WIN32_KHR", 
			"NOMINMAX", 
			"_USE_MATH_DEFINES",
			"VK_EXAMPLE_DATA_DIR=\"" .. os.getcwd() .. "/Samples/WindowsVulkan/data/\"",
		}
		
		buildoptions { 
			"/wd4201", -- nonstandard extension used: class rvalue used as lvalue
			"/wd4458", -- declaration of '***' hides class member
			"/wd4018", -- '<': signed/unsigned mismatch
			"/wd4267", -- 'argument': conversion from 'size_t' to 'uint32_t'
			"/wd4244", -- 'initializing': conversion from 'double' to 'float', possible loss of data
			"/wd4189", -- local variable is initialized but not referenced
			"/wd4100", -- unreferenced formal parameter
			"/wd4189", -- local variable is initialized but not referenced
			"/wd4456", -- declaration of '***' hides previous local declaration
			"/wd4700", -- uninitialized local variable '***' used
			"/wd4702", -- unreachable code
		}
	
		files {
			"Samples/WindowsVulkan/**.*", 
		}
		
		includedirs {
			"$(VULKAN_SDK)/Include",
			"Samples/WindowsVulkan",
			"Samples/WindowsVulkan/base",
			"BrofilerCore",
		}
		
		libdirs {
			"$(VULKAN_SDK)/Lib",
			"Samples/WindowsVulkan/libs/assimp",
		}

		links { 
			"vulkan-1",
			"assimp",
		}
		
		links {
			"BrofilerCore",
		}
		
		vpaths { 
			["*"] = "Samples/WindowsVulkan" 
		}
		
		if isVisualStudio then
			fullPath = os.getcwd() .. "\\Samples\\WindowsVulkan\\"
			postbuildcommands { "copy \"" .. fullPath .. "dll\\assimp-vc140-mt.dll\" \"" .. fullPath .. "$(OutputPath)\\assimp-vc140-mt.dll\" /Y" }
		end
end
