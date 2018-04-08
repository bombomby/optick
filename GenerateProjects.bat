genie.exe vs2010 genie.lua
genie.exe vs2012 genie.lua
genie.exe vs2015 genie.lua
genie.exe vs2017 genie.lua

COPY /Y BrofilerDurangoTest\BrofilerDurangoTest.vcxproj BrofilerDurangoTest\BrofilerDurangoTest_backup.vcxproj
COPY /Y BrofilerDurangoTest\BrofilerDurangoTest.vcxproj.filters BrofilerDurangoTest\BrofilerDurangoTest_backup.vcxproj.filters
genie.exe --UWP vs2015 genie.lua
COPY /Y BrofilerDurangoTest\BrofilerDurangoTest_backup.vcxproj BrofilerDurangoTest\BrofilerDurangoTest.vcxproj 
COPY /Y BrofilerDurangoTest\BrofilerDurangoTest_backup.vcxproj.filters BrofilerDurangoTest\BrofilerDurangoTest.vcxproj.filters 

PAUSE