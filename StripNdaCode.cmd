rd /Q /S "./Bin"
rd /Q /S "./ThirdParty/DevkitsInterop"
rd /Q /S "./ThirdParty/TaskScheduler/Scheduler/Include/Platform/Orbis"
rd /Q /S "./BrofilerCore/SchedulerTrace/PS4"

rd /Q /S "./Build"
sunifdef.exe --replace --recurse --filter cs --discard drop --undef NDA_CODE_SECTION ./Brofiler
sunifdef.exe --replace --recurse --filter cpp,h,inl --discard drop --undef MT_PLATFORM_DURANGO --undef MT_PLATFORM_ORBIS --undef _XBOX_ONE --undef __ORBIS__ ./ThirdParty/Scheduler
sunifdef.exe --replace --recurse --filter cpp,h,inl --discard drop --undef MT_PLATFORM_DURANGO --undef MT_PLATFORM_ORBIS --undef _XBOX_ONE --undef __ORBIS__ ./BrofilerCore
sunifdef.exe --replace --recurse --filter cpp,h,inl --discard drop --undef MT_PLATFORM_DURANGO --undef MT_PLATFORM_ORBIS --undef _XBOX_ONE --undef __ORBIS__ ./BrofilerTest
sunifdef.exe --replace --recurse --filter cpp,h,inl --discard drop --undef MT_PLATFORM_DURANGO --undef MT_PLATFORM_ORBIS --undef _XBOX_ONE --undef __ORBIS__ ./BrofilerWindowsTest

RemoveXML.exe ./Brofiler/Brofiler_vs2010.csproj
RemoveXML.exe ./Brofiler/Brofiler_vs2012.csproj
RemoveXML.exe ./Brofiler/Brofiler_vs2015.csproj

del Publish.cmd