using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Data
{
	public enum Mode
	{
		OFF = 0x0,
		INSTRUMENTATION_CATEGORIES = (1 << 0),
		INSTRUMENTATION_EVENTS = (1 << 1),
		SAMPLING = (1 << 2),
		TAGS = (1 << 3),
		AUTOSAMPLING = (1 << 4),
		SWITCH_CONTEXT = (1 << 5),
		IO = (1 << 6),
		GPU = (1 << 7),
		END_SCREENSHOT = (1 << 8),
		RESERVED_0 = (1 << 9),
		RESERVED_1 = (1 << 10),
		HW_COUNTERS = (1 << 11),
		LIVE = (1 << 12),
		RESERVED_2 = (1 << 13),
		RESERVED_3 = (1 << 14),
		RESERVED_4 = (1 << 15),
		SYS_CALLS = (1 << 16),
		OTHER_PROCESSES = (1 << 17),
	}
}
