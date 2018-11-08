#include "GPUProfiler.h"

#if BRO_ENABLE_GPU

namespace Brofiler
{
	static_assert((1ULL << 32) % GPUProfiler::MAX_QUERIES_COUNT == 0, "(1 << 32) should be a multiple of MAX_QUERIES_COUNT to handle query index overflow!");
}

#endif //BRO_ENABLE_GPU
