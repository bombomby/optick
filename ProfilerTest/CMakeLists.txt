add_executable (ProfilerTest ProfilerTest.cpp TestEngine.cpp)
target_link_libraries (ProfilerTest LINK_PUBLIC ProfilerCore)