Echo Building shaders
Echo Launch dir: "%~dp0"
Echo Current dir: "%CD%"
cd "%~dp0"
PATH="c:\Program Files (x86)\Microsoft DirectX SDK (June 2010)\Utilities\bin\x64\"
fxc.exe /T vs_4_0 /O3 /E VS /Fo Basic_vs.fxo Basic.fx
fxc.exe /T ps_4_0 /O3 /E PS /Fo Basic_ps.fxo Basic.fx
fxc.exe /T vs_4_0 /O3 /E VS /Fo Text_vs.fxo Text.fx
fxc.exe /T ps_4_0 /O3 /E PS /Fo Text_ps.fxo Text.fx
