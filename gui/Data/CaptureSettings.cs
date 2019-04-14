using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Data
{
    public class CaptureSettings
    {
		public UInt32 Mode { get; set; }
		public UInt32 CategoryMask { get; set; } = UInt32.MaxValue;
		public UInt32 SamplingFrequencyHz { get; set; } = 1000;
		public UInt32 FrameLimit { get; set; } = 0;
		public UInt32 TimeLimitUs { get; set; } = 0;
		public UInt32 MaxSpikeLimitUs { get; set; } = 0;
		public UInt64 MemoryLimitMb { get; set; } = 0;
	}
}
