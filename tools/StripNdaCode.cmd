rd /Q /S "./Bin"
rd /Q /S "./ThirdParty/DevkitsInterop"
rd /Q /S "./ThirdParty/TaskScheduler/Scheduler/Include/Platform/Orbis"
rd /Q /S "./BrofilerCore/Platform/PS4"

rd /Q /S "./Build"
sunifdef.exe --replace --recurse --filter cs --discard drop --undef NDA_CODE_SECTION ./Brofiler
sunifdef.exe --replace --recurse --filter cpp,h,inl --discard drop --undef MT_PLATFORM_DURANGO --undef MT_PLATFORM_ORBIS --undef _XBOX_ONE --undef __ORBIS__ ./ThirdParty/TaskScheduler
sunifdef.exe --replace --recurse --filter cpp,h,inl --discard drop --undef MT_PLATFORM_DURANGO --undef MT_PLATFORM_ORBIS --undef _XBOX_ONE --undef __ORBIS__ ./BrofilerCore
sunifdef.exe --replace --recurse --filter cpp,h,inl --discard drop --undef MT_PLATFORM_DURANGO --undef MT_PLATFORM_ORBIS --undef _XBOX_ONE --undef __ORBIS__ ./BrofilerTest
sunifdef.exe --replace --recurse --filter cpp,h,inl --discard drop --undef MT_PLATFORM_DURANGO --undef MT_PLATFORM_ORBIS --undef _XBOX_ONE --undef __ORBIS__ ./BrofilerWindowsTest

RemoveXML.exe ./Brofiler/Brofiler.csproj

del Step2_Publish.cmd
del PrepareVersionExt.cmd

pushd
cd "./Publish/Include/"
del /F OrbisDbgHelp.h
popd

