file(GLOB Common_SRC
    "*.h"
    "*.cpp"
)

add_library (ProfilerCore ${Common_SRC})
target_include_directories (ProfilerCore PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})