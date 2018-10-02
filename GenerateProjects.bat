Tools\genie.exe vs2010 --file=genie.lua
Tools\genie.exe vs2012 --file=genie.lua
Tools\genie.exe vs2015 --file=genie.lua
Tools\genie.exe vs2017 --file=genie.lua

rem COPY /Y BrofilerDurangoTest\BrofilerDurangoTest.vcxproj BrofilerDurangoTest\BrofilerDurangoTest_backup.vcxproj
rem COPY /Y BrofilerDurangoTest\BrofilerDurangoTest.vcxproj.filters BrofilerDurangoTest\BrofilerDurangoTest_backup.vcxproj.filters
rem genie.exe --UWP vs2015 genie.lua
rem COPY /Y BrofilerDurangoTest\BrofilerDurangoTest_backup.vcxproj BrofilerDurangoTest\BrofilerDurangoTest.vcxproj 
rem COPY /Y BrofilerDurangoTest\BrofilerDurangoTest_backup.vcxproj.filters BrofilerDurangoTest\BrofilerDurangoTest.vcxproj.filters 

PAUSE