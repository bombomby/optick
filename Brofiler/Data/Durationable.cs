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

    public struct Tick : ITick
    {
        public long Start { get; set; }
    }

    public class TimeSettings
    {
        public Double TicksToMs { get; set; }
        public Int64 Origin { get; set; }
        public Int32 PrecisionCut { get; set; }
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

		private static TimeSettings settings = null;
		public static void InitSettings(TimeSettings s)
		{
            settings = s;
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
			return settings.TicksToMs * duration;
		}

        internal bool Intersect(long value)
        {
            return Start <= value && value <= Finish;
        }

        internal bool Intersect(IDurable other)
        {
            return Start <= other.Finish && Finish >= other.Start;
        }

        public static long MsToTick(double ms)
		{
			return (long)(ms / settings.TicksToMs);
		}

        public static Int64 ReadTime(BinaryReader reader)
        {
            return settings.Origin > 0 ? (((Int64)reader.ReadUInt32() << settings.PrecisionCut) + settings.Origin) : reader.ReadInt64();
        }

		public void ReadDurable(BinaryReader reader)
		{
            Start = ReadTime(reader);
			Finish = ReadTime(reader);
		}

        public Durable Normalize()
        {
            return (Start > Finish) ? new Durable(Finish, Start) : new Durable(Start, Finish);
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
