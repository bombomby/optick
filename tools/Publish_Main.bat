@echo off

cd ..

rem call tools/GenerateProjects.bat

MsBuild build/vs2019/Optick.sln /t:OptickCore:Rebuild /p:Configuration=Debug /p:Platform=x64
MsBuild build/vs2019/Optick.sln /t:Samples\ConsoleApp:Rebuild /p:Configuration=Release /p:Platform=x64
MsBuild gui/OptickApp_vs2022.sln /t:Rebuild /p:Configuration=Release /p:Platform=x64

for /f %%i in ('powershell "(Get-Item -path gui\Bin\Release\x64\Optick.exe).VersionInfo.ProductVersion"') do set VERSION=%%i

set VERSION_NAME=%VERSION:~0,-2%

rem GUI
xcopy /Y gui\Bin\Release\x64\Optick.exe publish\Optick_%VERSION_NAME%\*

rem Samples
xcopy /Y Bin\vs2019\x64\Release\ConsoleApp.exe publish\Optick_%VERSION_NAME%\samples\*
xcopy /Y Bin\vs2019\x64\Release\OptickCore.dll publish\Optick_%VERSION_NAME%\samples\*

rem Include
xcopy /Y src\optick.* publish\Optick_%VERSION_NAME%\include\*

rem Lib
xcopy /Y Bin\vs2019\x64\Debug\OptickCore.dll publish\Optick_%VERSION_NAME%\lib\x64\debug\*
xcopy /Y Bin\vs2019\x64\Debug\OptickCore.lib publish\Optick_%VERSION_NAME%\lib\x64\debug\*
xcopy /Y Bin\vs2019\x64\Debug\OptickCore.pdb publish\Optick_%VERSION_NAME%\lib\x64\debug\*
xcopy /Y Bin\vs2019\x64\Release\OptickCore.dll publish\Optick_%VERSION_NAME%\lib\x64\release\*
xcopy /Y Bin\vs2019\x64\Release\OptickCore.lib publish\Optick_%VERSION_NAME%\lib\x64\release\*
xcopy /Y Bin\vs2019\x64\Release\OptickCore.pdb publish\Optick_%VERSION_NAME%\lib\x64\release\*

rem Src
xcopy /Y src\*.* publish\Optick_%VERSION_NAME%\src\*

rem License
xcopy /Y LICENSE publish\Optick_%VERSION_NAME%\*

powershell "Compress-Archive -Path .\publish\Optick_%VERSION_NAME%\* -DestinationPath .\publish\Optick_%VERSION_NAME%.zip"

cd tools