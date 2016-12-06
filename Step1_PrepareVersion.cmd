@echo off

call PrepareVersionExt.cmd

xcopy /Y .\Bin\Release\x64\*.* .\Publish\

xcopy /Y .\BrofilerCore\Brofiler.h .\Publish\Include\

xcopy /Y .\Bin\vs2010\x32\Debug\brofilercore.* .\Publish\Lib\vs2010\x86\debug\
xcopy /Y .\Bin\vs2010\x32\Release\brofilercore.* .\Publish\Lib\vs2010\x86\release\
xcopy /Y .\Bin\vs2010\x64\Debug\brofilercore.* .\Publish\Lib\vs2010\x64\debug\
xcopy /Y .\Bin\vs2010\x64\Release\brofilercore.* .\Publish\Lib\vs2010\x64\release\

xcopy /Y .\Bin\vs2012\Native\Debug\libBrofilerCore.* .\Publish\Lib\vs2012\orbis\debug\
xcopy /Y .\Bin\vs2012\Native\Release\libBrofilerCore.* .\Publish\Lib\vs2012\orbis\release\
xcopy /Y .\Bin\vs2012\x32\Debug\brofilercore.* .\Publish\Lib\vs2012\x86\debug\
xcopy /Y .\Bin\vs2012\x32\Release\brofilercore.* .\Publish\Lib\vs2012\x86\release\
xcopy /Y .\Bin\vs2012\x64\Debug\brofilercore.* .\Publish\Lib\vs2012\x64\debug\
xcopy /Y .\Bin\vs2012\x64\Release\brofilercore.* .\Publish\Lib\vs2012\x64\release\

xcopy /Y .\Bin\vs2015\Native\Debug\libBrofilerCore.* .\Publish\Lib\vs2015\orbis\debug\
xcopy /Y .\Bin\vs2015\Native\Release\libBrofilerCore.* .\Publish\Lib\vs2015\orbis\release\
xcopy /Y .\Bin\vs2015\x32\Debug\brofilercore.* .\Publish\Lib\vs2015\x86\debug\
xcopy /Y .\Bin\vs2015\x32\Release\brofilercore.* .\Publish\Lib\vs2015\x86\release\
xcopy /Y .\Bin\vs2015\x64\Debug\brofilercore.* .\Publish\Lib\vs2015\x64\debug\
xcopy /Y .\Bin\vs2015\x64\Release\brofilercore.* .\Publish\Lib\vs2015\x64\release\


