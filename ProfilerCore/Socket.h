#pragma once

#if defined(LINUX64) || defined(ANDROID)
#include "Linux/SocketLinux.h"
#else
#include "Windows/SocketWindows.h"
#endif