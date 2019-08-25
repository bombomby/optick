rem @echo off

cd ..

rem call generate_projects.gpu.bat

rem MsBuild build/vs2017/Optick.sln /t:OptickCore:Rebuild /p:Configuration=Debug /p:Platform=x64
rem MsBuild build/vs2017/Optick.sln /t:Samples\ConsoleApp:Rebuild /p:Configuration=Release /p:Platform=x64
rem MsBuild gui/OptickApp_vs2017.sln /t:Rebuild /p:Configuration=Release /p:Platform=x64

for /f %%i in ('powershell "(Get-Item -path Bin\Release\x64\Optick.exe).VersionInfo.ProductVersion"') do set VERSION=%%i

set UNREAL_VERSION=4.22
set VERSION_NAME=%VERSION:~0,-2%(UE4Plugin_%UNREAL_VERSION%)

xcopy /Y Bin\Release\x64\Optick.exe samples\UnrealEnginePlugin\GUI\*

call "C:\Program Files\Epic Games\UE_%UNREAL_VERSION%\Engine\Build\BatchFiles\RunUAT.bat" BuildPlugin -Plugin="%CD%\samples\UnrealEnginePlugin\OptickPlugin.uplugin" -Package="%CD%\publish\Optick_%VERSION_NAME%\OptickPlugin" -Rocket

powershell "Compress-Archive -Path \".\publish\Optick_%VERSION_NAME%\*\" -DestinationPath \".\publish\Optick_%VERSION_NAME%.zip\""

cd tools