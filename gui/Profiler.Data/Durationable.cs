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
		double Duration { get; }
	}

	public struct Tick : ITick
	{
		public static long INVALID_TIMESTAMP = (Int64)(-1);
		public long Start { get; set; }
	}

	public class TimeSettings
	{
		public Double TicksToMs { get; set; }
		public Int64 Origin { get; set; }
		public Int32 PrecisionCut { get; set; }
	}

	public class Durable : IDurable
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

		public bool Intersect(long value)
		{
			return Start <= value && value <= Finish;
		}

		public bool Intersect(IDurable other)
		{
			return Start <= other.Finish && Finish >= other.Start;
		}

		public bool Contains(IDurable other)
		{
			return Start <= other.Start && Finish >= other.Finish;
		}

		public bool Within(IDurable other)
		{
			return Start >= other.Start && Finish <= other.Finish;
		}

		public double Overlap(Durable entry)
		{
			long from = Math.Max(entry.Start, Start);
			long to = Math.Min(entry.Finish, Finish);
			return from < to ? TicksToMs(to - from) : 0.0;
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

		public bool IsValid
		{
			get
			{
				return (Finish >= Start) && (Finish != Tick.INVALID_TIMESTAMP) && (Start != Tick.INVALID_TIMESTAMP);
			}
		}

		public bool IsNonZero
		{
			get
			{
				return (Finish > Start) && (Finish != Tick.INVALID_TIMESTAMP) && (Start != Tick.INVALID_TIMESTAMP);
			}
		}

		public Durable(long s, long f)
		{
			this.Start = s;
			this.Finish = f;
		}

		public Durable() { }
	}
}
