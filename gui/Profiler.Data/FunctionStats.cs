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

        public double AvgTotal
        {
            get { return Samples.Count > 0 ? Samples.Average(s => s.Total) : 0.0; }
        }

        public double AvgWork
        {
            get { return Samples.Count > 0 ? Samples.Average(s => s.Work) : 0.0; }
        }

        public double AvgWait
        {
            get { return Samples.Count > 0 ? Samples.Average(s => s.Wait) : 0.0; }
        }


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
				Group.UpdateDescriptionMask(Description);

				if (Description.Mask != null)
				{
					FrameList frameList = Group.GetFocusThread(Description.Mask.Value);
					if (frameList != null)
					{
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
											sample.Add(e);
										}
									}
								});
							}

							Samples.Add(sample);
						}
					}
					else
					{
						// Fallback to Individual Calls
						Load(Origin.IndividualCalls);
					}
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
								Samples.Add(new Sample(e) { Index = Samples.Count, Name = Description.Name });
							}
						}
					}
				}

				Samples.Sort((a, b) => (a.Entries[0].CompareTo(b.Entries[0])));
			}
		}
	}
}
