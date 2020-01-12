cd ..
cmake -H"." -B"build\cmake" -G "Visual Studio 15 2017 Win64" -DBUILD_VULKAN=1 -DBUILD_D3D12=1
PAUSE