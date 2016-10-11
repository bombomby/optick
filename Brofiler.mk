include $(CLEAR_VARS)
LOCAL_MODULE := brofiler
LOCAL_C_INCLUDES := $(LOCAL_PATH_BROFILER)/ProfilerCore
EXPORT_C_INCLUDES := $(LOCAL_PATH_BROFILER)/ProfilerCore
FILE_LIST := $(wildcard $(LOCAL_PATH_BROFILER)/ProfilerCore/*.cpp)
FILE_LIST += $(wildcard $(LOCAL_PATH_BROFILER)/ProfilerCore/Linux/*.cpp)
LOCAL_SRC_FILES := $(FILE_LIST:$(LOCAL_PATH)/%=%)
LOCAL_CPPFLAGS := -std=c++11 -Wno-unused-variable -DANDROID -DUSE_PROFILER -funwind-tables -latomic -pthread
include $(BUILD_SHARED_LIBRARY)

#restore the LOCAL_PATH
LOCAL_PATH:=$(USER_LOCAL_PATH)