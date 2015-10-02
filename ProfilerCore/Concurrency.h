#pragma once

#ifdef LINUX64
#include "Linux/ConcurrencyLinux.h"
#else
#include "Windows/ConcurrencyWindows.h"
#endif