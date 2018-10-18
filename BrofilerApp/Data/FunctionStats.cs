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
			public double Duration { get; set; }
			public double Work { get; set; }
			public double Wait { get { return Duration - Work; } }
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
				Duration = Duration + e.Duration;
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

		public FunctionStats(FrameGroup group, EventDescription desc)
		{
			Group = group;
			Description = desc;
		}

		public void Load(Origin origin = Origin.MainThread)
		{
			Samples = new List<Sample>();

			if (origin == Origin.MainThread)
			{
				List<EventFrame> frames = Group.MainThread.Events;

				for (int i = 0; i < frames.Count; ++i)
				{
					Sample sample = new Sample();

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
									sample.Add(e);
								}
							}
						});
					}

					Samples.Add(sample);
				}
			}

			if (origin == Origin.IndividualCalls)
			{
				foreach (ThreadData thread in Group.Threads)
				{
					foreach (EventFrame frame in thread.Events)
					{
						List<Entry> shortEntries = null;
						if (frame.ShortBoard.TryGetValue(Description, out shortEntries))
						{
							foreach (Entry e in shortEntries)
							{
								Samples.Add(new Sample(e));
							}
						}
					}
				}

				Samples.Sort((a, b) => (a.Entries[0].CompareTo(b.Entries[0])));
			}
		}
	}
}
