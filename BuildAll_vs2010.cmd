@echo off

set solutionFile=%cd%\Brofiler\Brofiler_vs2010.sln
set coreSolutionFile=%cd%\Build\vs2010\Brofiler.sln

echo Generate project files

genie.exe vs2010 genie.lua

pushd "C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\"

echo Building %solutionFile%

echo "Windows64 - Debug"
devenv.exe %solutionFile% /build "Debug|x64"
if errorlevel 1 ( echo Fail )
echo "Windows64 - Release"
devenv.exe %solutionFile% /build "Release|x64"
if errorlevel 1 ( echo Fail )

echo Building %coreSolutionFile%

echo "Windows64 - Debug"
devenv.exe %coreSolutionFile% /build "Debug|x64"
if errorlevel 1 ( echo Fail )
echo "Windows64 - Release"
devenv.exe %coreSolutionFile% /build "Release|x64"
if errorlevel 1 ( echo Fail )

echo "Windows32 - Debug"
devenv.exe %coreSolutionFile% /build "Debug|Win32"
if errorlevel 1 ( echo Fail )
echo "Windows32 - Release"
devenv.exe %coreSolutionFile% /build "Release|Win32"
if errorlevel 1 ( echo Fail )

popd


