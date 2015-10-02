#pragma once

#ifdef LINUX64
#include "Linux/SocketLinux.h"
#else
#include "Windows/SocketWindows.h"
#endif