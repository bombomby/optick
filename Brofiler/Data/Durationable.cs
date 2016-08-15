using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Profiler.Data
{
    public interface ITick
    {
        long Start { get; }
    }

    public interface IDurable : ITick
    {
        long Finish { get; }
    }

    public class Tick : ITick
    {
        public long Start { get; set; }
    }

    public class Durable :  IDurable
    {
        public long Start { get; set; }
        public long Finish { get; set; }
        public double StartMS
		{
			get { return TicksToMs(Start); }
		}

		public double FinishMS
		{
			get { return TicksToMs(Finish); }
		}

		private static double freq = 1;
		public static void InitFrequency(long frequency)
		{
			freq = 1000.0 / (double)frequency;
		}

		public double Duration
		{
			get { return TicksToMs(Finish - Start); }
		}

        public String DurationF1
        {
            get { return Duration.ToString("F1", System.Globalization.CultureInfo.InvariantCulture); }
        }

        public String DurationF3
        {
            get { return Duration.ToString("F3", System.Globalization.CultureInfo.InvariantCulture); }
        }


        public static double TicksToMs(long duration)
		{
			return freq * duration;
		}

        internal bool Intersect(long value)
        {
            return Start <= value && value <= Finish;
        }

        public static long MsToTick(double ms)
		{
			return (long)(ms / freq);
		}

		public void ReadDurable(BinaryReader reader)
		{
			Start = reader.ReadInt64();
			Finish = reader.ReadInt64();
		}

		public Durable(long s, long f)
		{
			this.Start = s;
			this.Finish = f;
		}

		public Durable() { }
	}

  public struct Timestamp
  {
    long time;
    public long Time
    {
      get { return time; }
      set { time = value; }
    }

    public double TimeMS
    {
      get { return Durable.TicksToMs(time); }
    }
  }
}
