// Copyright 1998-2018 Epic Games, Inc. All Rights Reserved.

namespace UnrealBuildTool.Rules
{
	public class OptickPlugin : ModuleRules
	{
		public OptickPlugin(ReadOnlyTargetRules Target) : base(Target)
		{
			PublicIncludePaths.AddRange(
				new string[] {
					// ... add public include paths required here ...
				}
				);

			PrivateIncludePaths.AddRange(
				new string[] {
					"Private",
					"Private/OptickCore",
					// ... add other private include paths required here ...
				}
				);

			PublicDependencyModuleNames.AddRange(
				new string[]
				{
					"Core",
					"CoreUObject",
					"Engine",
					"EngineSettings",
					"DesktopPlatform",
					// ... add other public dependencies that you statically link with here ...
				}
				);

			PrivateDependencyModuleNames.AddRange(
				new string[]
				{
					// ... add private dependencies that you statically link with here ...
				}
				);

			DynamicallyLoadedModuleNames.AddRange(
				new string[]
				{
					// ... add any modules that your module loads dynamically here ...
				}
				);

			PublicDefinitions.AddRange(
				new string[]
				{
					"OPTICK_ENABLE_GPU_VULKAN=0",
					"OPTICK_ENABLE_GPU_D3D12=0",
					"OPTICK_UE4=1",
				}
				);
				
				
			if (Target.bBuildEditor == true)
			{
				PublicDependencyModuleNames.AddRange(
					new string[]
					{
						"Slate",
						"SlateCore",
						"EditorStyle",
						"UnrealEd",
						"MainFrame",
						"GameProjectGeneration",
						"Projects",
						"InputCore",
						"LevelEditor",
					}
				);
			}
			//PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;
		}
	}
}
