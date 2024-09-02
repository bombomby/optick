using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Data
{
	public class FunctionStats
	{
		public class Sample
		{
			public String Name { get; set; }
			public int Index { get; set; }
			public double Total { get; set; }
			public double Work { get; set; }
			public double Wait { get { return Total - Work; } }
			public int Count { get; set; }
			public List<Entry> Entries { get; set; }

			public Sample()
			{
				Entries = new List<Entry>();
			}

			public Sample(Entry e) : this()
			{
				Add(e);
			}

			public void Add(Entry e)
			{
				Total = Total + e.Duration;
				Work = Work + e.CalculateWork();
				Count = Count + 1;
				Entries.Add(e);
			}
		}

		public enum Origin
		{
			MainThread,
			IndividualCalls,
		}

		public List<Sample> Samples { get; set; }
		public FrameGroup Group { get; set; }
		public EventDescription Description { get; set; }

		// Timing stats computed during load

		// Fastest time
		public double MinPerCall { get; set; }
		// Slowest time
		public double MaxPerCall { get; set; }
		// Average function time (averaged over calls, not frames)
		public double AvgTotalPerCall { get; set; }
		// Standard deviation of the individual function times
		public double StdDevPerCall { get; set; }

		// Time spent in this function during an average frame
		public double AvgTotal { get; set; }
		// Time spent working in this function during an average frame
		public double AvgWork { get; set; }
		// Time spent waiting in this function during an average frame
		public double AvgWait { get; set; }

		public FunctionStats(FrameGroup group, EventDescription desc)
		{
			Group = group;
			Description = desc;
		}

		public void Load(Origin origin = Origin.MainThread)
		{
			Samples = new List<Sample>();

			MinPerCall = Double.MaxValue;
			MaxPerCall = 0.0;
			AvgTotalPerCall = 0.0;
			StdDevPerCall = 0.0;
			double sumOfCallTimes = 0.0;
			double sumOfCallTimesSq = 0.0;
			double sumOfFrameTimes = 0.0;
			double sumOfFrameTimesWork = 0.0;
			double sumOfFrameTimesWait = 0.0;
			int numCalls = 0;

			if (origin == Origin.MainThread)
			{
				Group.UpdateDescriptionMask(Description);

				if (Description.Mask == null)
				{
					return;
				}

				FrameList frameList = Group.GetFocusThread(Description.Mask.Value);
				if (frameList == null)
				{
					// Fallback to Individual Calls
					Load(Origin.IndividualCalls);
					return;
				}
					
				List<FrameData> frames = frameList.Events;

				for (int i = 0; i < frames.Count; ++i)
				{
					Sample sample = new Sample() { Name = String.Format("Frame {0:000}", i), Index = i };

					long start = frames[i].Start;
					long finish = i == frames.Count - 1 ? frames[i].Finish : frames[i + 1].Start;

					foreach (ThreadData thread in Group.Threads)
					{
						Utils.ForEachInsideIntervalStrict(thread.Events, start, finish, (frame) =>
						{
							List<Entry> shortEntries = null;
							if (frame.ShortBoard.TryGetValue(Description, out shortEntries))
							{
								foreach (Entry e in shortEntries)
								{
									double dur = e.Duration;
									sumOfCallTimes += dur;
									sumOfCallTimesSq += dur * dur;
									numCalls++;
									MinPerCall = Math.Min(MinPerCall, dur);
									MaxPerCall = Math.Max(MaxPerCall, dur);

									sample.Add(e);
								}
							}
						});
					}

					sumOfFrameTimes += sample.Total;
					sumOfFrameTimesWait += sample.Wait;
					sumOfFrameTimesWork += sample.Work;
					Samples.Add(sample);
				}
			} 
			else if (origin == Origin.IndividualCalls)
			{
				foreach (ThreadData thread in Group.Threads)
				{
					foreach (EventFrame frame in thread.Events)
					{
						List<Entry> shortEntries = null;
						if (!frame.ShortBoard.TryGetValue(Description, out shortEntries))
						{
							continue;
						}

						foreach (Entry e in shortEntries)
						{
							double dur = e.Duration;
							sumOfCallTimes += dur;
							sumOfCallTimesSq += dur * dur;
							numCalls++;
							MinPerCall = Math.Min(MinPerCall, dur);
							MaxPerCall = Math.Max(MaxPerCall, dur);

							Sample sample = new Sample(e) { Index = Samples.Count, Name = Description.Name };

							sumOfFrameTimes += sample.Total;
							sumOfFrameTimesWait += sample.Wait;
							sumOfFrameTimesWork += sample.Work;
							Samples.Add(sample);
						}
					}
				}

				Samples.Sort((a, b) => a.Entries[0].CompareTo(b.Entries[0]));
			}
		
			// compute averages
			double numCallsInv = numCalls > 0 ? 1.0 / numCalls : 0.0;
			double numSamplesInv = Samples.Count > 0 ? 1.0 / Samples.Count : 0.0;

			AvgTotalPerCall = sumOfCallTimes * numCallsInv;
			double varOfTimes = sumOfCallTimesSq * numCallsInv - AvgTotalPerCall * AvgTotalPerCall;
			StdDevPerCall = Math.Sqrt(Math.Max(varOfTimes, 0.0));

			AvgTotal = numSamplesInv * sumOfFrameTimes;
			AvgWait = numSamplesInv * sumOfFrameTimesWait;
			AvgWork = numSamplesInv * sumOfFrameTimesWork;
		}
	}
}
