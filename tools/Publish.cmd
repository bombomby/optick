@echo off

for /f %%i in ('powershell "(Get-Item -path ..\Bin\Release\x64\Optick.exe).VersionInfo.ProductVersion"') do set VERSION=%%i

xcopy /Y ..\Bin\Release\x64\Optick.exe .\Publish\Optick_%VERSION%\bin\*
xcopy /Y ..\Bin\vs2017\x64\Release\ConsoleApp.exe .\Publish\Optick_%VERSION%\samples\*
xcopy /Y ..\src\*.* .\Publish\Optick_%VERSION%\src\*
xcopy /Y ..\LICENSE .\Publish\Optick_%VERSION%\*

powershell "Compress-Archive -Path .\Publish\Optick_%VERSION%\* -DestinationPath .\Publish\Optick_%VERSION%.zip"