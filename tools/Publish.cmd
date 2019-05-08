@echo off

cd ..

call generate_projects.gpu.bat

MsBuild build/vs2017/Optick.sln /t:OptickCore:Rebuild /p:Configuration=Debug /p:Platform=x64
MsBuild build/vs2017/Optick.sln /t:Samples\ConsoleApp:Rebuild /p:Configuration=Release /p:Platform=x64
MsBuild gui/OptickApp_vs2017.sln /t:Rebuild /p:Configuration=Release /p:Platform=x64

for /f %%i in ('powershell "(Get-Item -path Bin\Release\x64\Optick.exe).VersionInfo.ProductVersion"') do set VERSION=%%i

rem GUI
xcopy /Y Bin\Release\x64\Optick.exe publish\Optick_%VERSION%\*

rem Samples
xcopy /Y Bin\vs2017\x64\Release\ConsoleApp.exe publish\Optick_%VERSION%\samples\*
xcopy /Y Bin\vs2017\x64\Release\OptickCore.dll publish\Optick_%VERSION%\samples\*

rem Include
xcopy /Y src\optick.* publish\Optick_%VERSION%\include\*

rem Lib
xcopy /Y Bin\vs2017\x64\Debug\OptickCore.dll publish\Optick_%VERSION%\lib\x64\debug\*
xcopy /Y Bin\vs2017\x64\Debug\OptickCore.lib publish\Optick_%VERSION%\lib\x64\debug\*
xcopy /Y Bin\vs2017\x64\Debug\OptickCore.pdb publish\Optick_%VERSION%\lib\x64\debug\*
xcopy /Y Bin\vs2017\x64\Release\OptickCore.dll publish\Optick_%VERSION%\lib\x64\release\*
xcopy /Y Bin\vs2017\x64\Release\OptickCore.lib publish\Optick_%VERSION%\lib\x64\release\*
xcopy /Y Bin\vs2017\x64\Release\OptickCore.pdb publish\Optick_%VERSION%\lib\x64\release\*

rem Src
xcopy /Y src\*.* publish\Optick_%VERSION%\src\*

rem License
xcopy /Y LICENSE publish\Optick_%VERSION%\*

rem powershell "Compress-Archive -Path .\publish\Optick_%VERSION%\* -DestinationPath .\publish\Optick_%VERSION%.zip"