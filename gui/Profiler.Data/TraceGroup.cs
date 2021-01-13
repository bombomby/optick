using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Profiler.Data
{
	public interface ITrace
	{
		bool Init(String name, Stream stream);
		FrameGroup MainGroup { get; }
		EventFrame MainFrame { get; }
	}

	public class FTraceGroup : ITrace
	{
		public FrameGroup Group { get; set; }
		public EventDescriptionBoard Board { get; set; }
		public EventFrame Frame { get; set; }

		public FrameGroup MainGroup => Group;
		public EventFrame MainFrame => Frame;

		public FTraceGroup()
		{
			Board = new EventDescriptionBoard() { TimeSettings = new TimeSettings() { Origin = 0, PrecisionCut = 0, TicksToMs = 0.001 } };
			Group = new FrameGroup(Board);
		}

		String FindBetween(String line, String leftString, String rightString)
		{
			int left = line.IndexOf(leftString) + leftString.Length;
			int right = rightString != null ? line.IndexOf(rightString) : line.Length;
			return line.Substring(left, right - left).Trim();
		}

		public bool Init(String name, Stream stream)
		{
			TextReader reader = new StreamReader(stream);

			List<SyncEvent> events = new List<SyncEvent>();
			List<TagsPack> tagPacks = new List<TagsPack>();

			Durable.InitSettings(Board.TimeSettings);

			long minTimestamp = long.MaxValue;
			long maxTimestamp = 0;

			while (true)
			{
				String line = reader.ReadLine();
				if (line == null)
					break;

				if (line.StartsWith("#"))
					continue;

				String function = line.Substring(44);
				function = function.Substring(0, function.IndexOf(':')).Trim();

				if (function == "sched_switch")
				{
					byte core = byte.Parse(line.Substring(24, 3));
					long timestamp = (long)(double.Parse(line.Substring(28, 14).Trim()) * 1000000.0);

					minTimestamp = Math.Min(minTimestamp, timestamp);
					maxTimestamp = Math.Max(maxTimestamp, timestamp);

					String prevThread = FindBetween(line, "prev_comm=", "prev_pid=");
					uint prevPid = uint.Parse(FindBetween(line, "prev_pid=", "prev_prio="));
					uint prevPrio = uint.Parse(FindBetween(line, "prev_prio=", "prev_state="));

					String nextThread = FindBetween(line, "==>", "next_pid=");
					uint nextPid = uint.Parse(FindBetween(line, "next_pid=", "next_prio="));
					uint nextPrio = uint.Parse(FindBetween(line, "next_prio=", null));

					events.Add(new SyncEvent() { CPUID = core, OldThreadID = prevPid, NewThreadID = nextPid, Timestamp = new Tick() { Start = (long)timestamp } });

					ThreadDescription desc = null;
					if (!Board.ThreadDescriptions.TryGetValue(prevPid, out desc))
					{
						desc = new ThreadDescription() { Name = prevThread, ThreadID = prevPid, ProcessID = 0, Origin = ThreadDescription.Source.GameAuto };
						Board.ThreadDescriptions.Add(prevPid, desc);
					}
					else
					{
						desc.Name = prevThread;
					}
				}

				if (function == "tracing_mark_write")
				{
					byte core = byte.Parse(line.Substring(24, 3));
					long timestamp = (long)(double.Parse(line.Substring(28, 14).Trim()) * 1000000.0);
					String text = FindBetween(line, function, null).Substring(2);

					while (tagPacks.Count <= core)
						tagPacks.Add(new TagsPack(null, Group) { CoreIndex = tagPacks.Count });

					Tag tag = new TagString() { Description = new EventDescription("Message"), Time = new Tick() { Start = timestamp }, Value = text };
					tagPacks[core].Tags.Add(tag);
				}
			}

			Board.TimeSlice = new Durable(minTimestamp, maxTimestamp);
			Frame = new EventFrame(new FrameHeader(Board.TimeSlice), new List<Entry> { }, Group);

			List<EventFrame> frames = new List<EventFrame>();
			long step = Durable.MsToTick(1000.0);
			for (long timestamp = minTimestamp; timestamp < maxTimestamp; timestamp += step)
			{
				frames.Add(new EventFrame(new FrameHeader(new Durable(timestamp, timestamp + step)), new List<Entry> { }, Group));
			}
			ThreadDescription ruler = new ThreadDescription() { Name = "Ruler", ThreadIndex = 0 };
			Group.Threads.Add(new ThreadData(ruler) { Events = frames });
			Group.Board.Threads.Add(ruler);
			Group.Board.MainThreadIndex = 0;

			SynchronizationMap syncMap = new SynchronizationMap(events);
			Group.AddSynchronization(syncMap);

			for (int i = 0; i < Math.Min(tagPacks.Count, Group.Cores.Count); ++i)
				Group.Cores[i].TagsPack = tagPacks[i];

			return true;
		}
	}

	public class ChromeTracingGroup : ITrace
	{
		EventDescriptionBoard Board { get; set; }

		public FrameGroup MainGroup { get; private set; }
		public EventFrame MainFrame { get; private set; }

		public ChromeTracingGroup()
		{
			Board = new EventDescriptionBoard() { TimeSettings = new TimeSettings() { Origin = 0, PrecisionCut = 0, TicksToMs = 0.001 } };
			MainGroup = new FrameGroup(Board);
		}

		public class ChromeTrace
		{
			public IList<TraceEvent> traceEvents { get; set; }
			public string displayTimeUnit { get; set; }
			public string systemTraceEvents { get; set; }
			public string powerTraceAsString { get; set; }
			//public IList<TraceStack> stackFrames { get; set; }
			public IList<TraceSample> samples { get; set; }
			public string controllerTraceDataKey { get; set; }
			public JObject otherData { get; set; }
		}

		public class TraceEvent
		{
			/// <summary>
			/// The name of the event.
			/// </summary>
			public string name { get; set; }
			/// <summary>
			/// The event categories. This is a comma separated list of categories for the event. 
			/// </summary>
			public string cat { get; set; }
			/// <summary>
			/// The process ID for the process that output this event.
			/// </summary>
			public UInt64 pid { get; set; }
			/// <summary>
			/// The thread ID for the thread that output this event.
			/// </summary>
			public UInt64 tid { get; set; }
			/// <summary>
			/// The event type. This is a single character which changes depending on the type of event being output. 
			/// </summary>
			public string ph { get; set; }
			/// <summary>
			/// The tracing clock timestamp of the event. The timestamps are provided at microsecond granularity.
			/// </summary>
			public double ts { get; set; }
			/// <summary>
			/// Optional. The thread clock timestamp of the event. The timestamps are provided at microsecond granularity.
			/// </summary>
			public double tts { get; set; }
			/// <summary>
			/// Any arguments provided for the event. Some of the event types have required argument fields, otherwise, you can put any information you wish in here. 
			/// The arguments are displayed in Trace Viewer when you view an event in the analysis section.
			/// </summary>
			public JObject args { get; set; }
			/// <summary>
			///  [Stack Traces] Stack Frame Index.
			/// </summary>
			public int sf { get; set; }
			/// <summary>
			/// [Stack Traces] Raw stacks.
			/// </summary>
			public IList<string> stack { get; set; }
			/// <summary>
			/// [Complete Events] Specifies the thread clock duration of complete events in microseconds.
			/// </summary>
			public double dur { get; set; }
			/// <summary>
			/// [Instant Events] specifies the scope of the event. There are four scopes available global (g), process (p) and thread (t). 
			/// If no scope is provided we default to thread scoped events.
			/// </summary>
			public string s { get; set; }

		}

		public class TraceSample
		{
			public int cpu { get; set; }
			public Int64 tid { get; set; }
			public double ts { get; set; }
			public string name { get; set; }
			public int sf { get; set; }
			public double weight { get; set; }
		}

		// Extracting "detail" field from the json
		// { "detail": "C:\\Program Files (x86)\\Windows Kits\\10\\Include\\10.0.18362.0\\ucrt\\corecrt.h" }
		Regex DetailExtractor = new Regex("{[\\s]*\"detail\":[\\s]*\"(.*)\"[\\s]*}", RegexOptions.Compiled);

		String Extract(String text, Regex regex)
		{
			if (text != null)
			{
				MatchCollection matches = regex.Matches(text);
				if (matches.Count > 0)
				{
					foreach (Match match in matches)
					{
						if (match.Groups.Count > 0)
							return match.Groups[match.Groups.Count - 1].Value;
					}
				}
			}

			return null;
		}

		String GenerateShortContext(String args)
		{
			String context = Extract(args, DetailExtractor);

			if (context != null)
			{
				return context;
			}

			return args;
		}

		String GenerateShortName(String name)
		{
			try
			{
				return Path.GetFileName(name);
			}
			catch (Exception /*ex*/) 
			{
				return name;
			}
		}

		const float RandomColorBrightnessVariance = 0.10f;

		public bool Init(String name, Stream stream)
		{
			Durable.InitSettings(Board.TimeSettings);

			String text = new StreamReader(stream).ReadToEnd();

			ChromeTrace trace = JsonConvert.DeserializeObject<ChromeTrace>(text);

			Dictionary<ulong, List<TraceEvent>> threads = new Dictionary<ulong, List<TraceEvent>>();

			foreach (TraceEvent ev in trace.traceEvents)
			{
				// Complete Event
				if (ev.ph == "X")
				{
					List<TraceEvent> events = null;
					if (!threads.TryGetValue(ev.tid, out events))
					{
						events = new List<TraceEvent>();
						threads.Add(ev.tid, events);
					}
					events.Add(ev);
				}
			}

			Durable range = new Durable(long.MaxValue, long.MinValue);

			Dictionary<string, EventDescription> descriptions = new Dictionary<string, EventDescription>();

			foreach (KeyValuePair<ulong, List<TraceEvent>> pair in threads)
			{
				ulong tid = pair.Key;
				List<TraceEvent> events = pair.Value;
				
				List<Entry> entries = new List<Entry>(events.Count);
				List<Tag> tags = new List<Tag>(events.Count);

				foreach (TraceEvent ev in events)
				{
					String args = ev.args != null ? ev.args.ToString().Replace("\n", "").Replace("\r", "") : null;

					String context = GenerateShortContext(args);
					String extraName = GenerateShortName(context);

					String fullName = ev.name;
					if (!String.IsNullOrWhiteSpace(extraName))
						fullName = String.Format("{0} - {1}", ev.name, extraName);

					EventDescription desc = null;
					if (!descriptions.TryGetValue(fullName, out desc))
					{
						desc = new EventDescription(fullName) { Color = EventDescription.GenerateRandomColor(ev.name, RandomColorBrightnessVariance) };
						descriptions.Add(fullName, desc);
					}
					
					entries.Add(new Entry(desc, (long)ev.ts, (long)(ev.ts + ev.dur)));

					if (!String.IsNullOrWhiteSpace(context))
						tags.Add(new Tag() { Description = new EventDescription(context), Time = new Tick() { Start = (long)ev.ts } });
				}

				entries.Sort();
				tags.Sort();

				ThreadData threadData = MainGroup.AddThread(new ThreadDescription() { Name = String.Format("Thread #{0}", tid), ThreadID = tid, ProcessID = 0, Origin = ThreadDescription.Source.Game });
				threadData.TagsPack = new TagsPack(tags);

				EventFrame frame = new EventFrame(new FrameHeader(new Durable(entries.Min(e => e.Start), entries.Max(e=>e.Finish)), threadData.Description.ThreadIndex), entries, MainGroup);
				entries.ForEach(e => e.Frame = frame);
				
				range.Start = Math.Min(range.Start, frame.Start);
				range.Finish = Math.Max(range.Finish, frame.Finish);

				threadData.Events.Add(frame);
			}

			Board.TimeSlice = range;
			Board.MainThreadIndex = 0;
			MainFrame = new EventFrame(new FrameHeader(Board.TimeSlice), new List<Entry> { }, MainGroup);

			return true;
		}
	}
}
