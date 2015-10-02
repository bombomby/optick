set( CMAKE_CXX_FLAGS  "${CMAKE_CXX_FLAGS} -std=c++11" )
add_executable (ProfilerTest ProfilerTest.cpp TestEngine.cpp)
target_link_libraries (ProfilerTest LINK_PUBLIC ProfilerCore)