// The MIT License(MIT)
//
// Copyright(c) 2019 Vadim Slyusarev
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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
