using Profiler.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Profiler
{
	/// <summary>
	/// Interaction logic for EventThreadView.xaml
	/// </summary>
	public partial class EventThreadView : UserControl
	{
		public EventThreadView()
		{
			InitializeComponent();

			ThreadToolsPanel.Visibility = Visibility.Collapsed;

			foreach (CallStackReason reason in Enum.GetValues(typeof(CallStackReason)))
				CallstackFilter.Add(new CallstackFilterItem() { IsChecked = true, Reason = reason });
			CallstackFilterPopup.DataContext = CallstackFilter;

			ThreadViewControl.OnShowPopup += OnShowPopup;
		}

		Dictionary<int, ThreadRow> id2row = new Dictionary<int, ThreadRow>();

		FrameGroup group;
		public FrameGroup Group
		{
			set
			{
				if (value != group)
				{
					group = value;

					InitThreadList(group);

					Visibility visibility = value == null ? Visibility.Collapsed : Visibility.Visible;

					ThreadToolsPanel.Visibility = visibility;

					FunctionSearchControl.DataContext = group;
					SummaryView.ItemsSource = group?.Summary?.SummaryTable;
				}
			}

			get
			{
				return group;
			}
		}

		int ThreadNameSorter(EventsThreadRow a, EventsThreadRow b)
		{
			if (a.Description.ThreadID == ThreadDescription.InvalidThreadID && b.Description.ThreadID != ThreadDescription.InvalidThreadID)
				return -1;

			if (a.Description.ThreadID != ThreadDescription.InvalidThreadID && b.Description.ThreadID == ThreadDescription.InvalidThreadID)
				return 1;

			int nameCompare = a.Name.CompareTo(b.Name);
			return nameCompare != 0 ? nameCompare : a.Description.ThreadID.CompareTo(b.Description.ThreadID);
		}

		public bool GenerateSamplingThreads { get; set; } = false;



		List<EventsThreadRow> GenerateThreadRows(FrameGroup group)
		{
			id2row.Clear();

			List<EventsThreadRow> eventThreads = new List<EventsThreadRow>();

			for (int i = 0; i < Math.Min(group.Board.Threads.Count, group.Threads.Count); ++i)
			{
				ThreadDescription thread = group.Board.Threads[i];
				ThreadData data = group.Threads[i];

				bool threadHasData = false;
				if ((data.Callstacks != null && data.Callstacks.Count > 3) ||
					(data.Events != null && data.Events.Count > 0))

				{
					threadHasData = true;
				}

				if (threadHasData)
				{
					EventsThreadRow row = new EventsThreadRow(group, thread, data);
					eventThreads.Add(row);

					id2row.Add(row.Description.ThreadIndex, row);
					row.EventNodeHover += Row_EventNodeHover;
					row.EventNodeSelected += Row_EventNodeSelected;
				}

				if (GenerateSamplingThreads && data.Callstacks != null && data.Callstacks.Count > 3)
				{
					ThreadData samplingData = GenerateSamplingThread(group, data);
					EventsThreadRow row = new EventsThreadRow(group, new ThreadDescription() { Name = thread.Name + " [Sampling]", Origin = ThreadDescription.Source.Sampling }, samplingData);
					eventThreads.Add(row);
					row.EventNodeHover += Row_EventNodeHover;
					row.EventNodeSelected += Row_EventNodeSelected;
				}
			}

			eventThreads.Sort(ThreadNameSorter);
			return eventThreads;
		}

		enum ProcessGroup
		{
			None,
			CurrentProcess,
			OtherProcess,
		}

		ProcessGroup GetProcessGroup(FrameGroup group, UInt64 threadID)
		{
			return threadID == 0 ? ProcessGroup.None : (group.IsCurrentProcess(threadID) ? ProcessGroup.CurrentProcess : ProcessGroup.OtherProcess);
		}

		ChartRow GenerateCoreChart(FrameGroup group)
		{
			group.Synchronization.Events.Sort();

			int eventsCount = group.Synchronization.Events.Count;

			List<Tick> timestamps = new List<Tick>(eventsCount);
			ChartRow.Entry currProcess = new ChartRow.Entry(eventsCount) { Fill = Colors.LimeGreen, Name = "Current Process" };
			ChartRow.Entry otherProcess = new ChartRow.Entry(eventsCount) { Fill = Colors.Tomato, Name = "Other Process" };

			List<bool> isCoreInUse = new List<bool>(group.Board.CPUCoreCount);
			for (int i = 0; i < group.Board.CPUCoreCount; ++i)
				isCoreInUse.Add(false);

			int currCores = 0;
			int otherCores = 0;

			foreach (SyncEvent ev in group.Synchronization.Events)
			{
				ProcessGroup prevGroup = GetProcessGroup(group, ev.OldThreadID);
				ProcessGroup currGroup = GetProcessGroup(group, ev.NewThreadID);

				if ((prevGroup != currGroup) || !isCoreInUse[ev.CPUID])
				{
					timestamps.Add(ev.Timestamp);

					if (isCoreInUse[ev.CPUID])
					{
						switch (prevGroup)
						{
							case ProcessGroup.CurrentProcess:
								--currCores;
								break;
							case ProcessGroup.OtherProcess:
								--otherCores;
								break;
						}
					}

					isCoreInUse[ev.CPUID] = true;
					switch (currGroup)
					{
						case ProcessGroup.CurrentProcess:
							++currCores;
							break;
						case ProcessGroup.OtherProcess:
							++otherCores;
							break;
					}

					currProcess.Values.Add((double)currCores);
					otherProcess.Values.Add((double)otherCores);
				}
			}

			ChartRow chart = new ChartRow("CPU", timestamps, new List<ChartRow.Entry>() { currProcess, otherProcess }, group.Board.CPUCoreCount);
			chart.ChartHover += Row_ChartHover;
			return chart;
		}

		ThreadData GenerateSamplingThread(FrameGroup group, ThreadData thread)
		{
			List<Entry> entries = new List<Entry>();
			List<Entry> stack = new List<Entry>();
			List<EventFrame> frames = new List<EventFrame>();

			Callstack current = new Callstack();

			for (int csIndex = 0; csIndex < thread.Callstacks.Count; ++csIndex)
			{
				Callstack callstack = thread.Callstacks[csIndex];

				if (current.Start == callstack.Start)
					continue;

				int matchCount = 0;

				for (int i = 0; i < Math.Min(current.Count, callstack.Count); ++i, ++matchCount)
					if (current[i].Name != callstack[i].Name)
						break;

				for (int i = matchCount; i < stack.Count; ++i)
				{
					stack[i].Finish = callstack.Start;
				}

				stack.RemoveRange(matchCount, stack.Count - matchCount);

				if (stack.Count == 0 && matchCount > 0)
				{
					FrameHeader h = new FrameHeader()
					{
						Start = entries.Min(e => e.Start),
						Finish = entries.Max(e => e.Finish),
					};

					frames.Add(new EventFrame(h, entries, group));
					entries.Clear();
				}

				for (int i = matchCount; i < callstack.Count; ++i)
				{
					Entry entry = new Entry(new EventDescription(callstack[i].Name), callstack.Start, long.MaxValue);
					entries.Add(entry);
					stack.Add(entry);
				}

				current = callstack;
			}

			foreach (Entry e in stack)
			{
				e.Finish = current.Start;
			}


			FrameHeader header = new FrameHeader()
			{
				Start = thread.Callstacks.First().Start,
				Finish = thread.Callstacks.Last().Start,
			};

			frames.Add(new EventFrame(header, entries, group));
			ThreadData result = new ThreadData()
			{
				Events = frames
			};
			return result;
		}

		void InitThreadList(FrameGroup group)
		{
			List<ThreadRow> rows = new List<ThreadRow>();

			if (group != null)
			{
				rows.Add(new HeaderThreadRow(group)
				{
					GradientTop = (ThreadViewControl.BroAlternativeBackground as SolidColorBrush).Color,
					GradientBottom = (ThreadViewControl.BroBackground as SolidColorBrush).Color,
					SplitLines = (ThreadViewControl.BroBackground as SolidColorBrush).Color,
					TextColor = Colors.Gray
				});

				rows.Add(GenerateCoreChart(group));
				rows.AddRange(GenerateThreadRows(group));
			}

			ThreadViewControl.InitRows(rows, group != null ? group.Board.TimeSlice : null);
		}

		public void Highlight(EventFrame frame, IDurable focus)
		{
			Group = frame.Group;
			ThreadRow row = null;
			if (id2row.TryGetValue(frame.Header.ThreadIndex, out row))
				Highlight(new ThreadView.Selection[] { new ThreadView.Selection() { Frame = frame, Focus = focus, Row = row } });
		}


		public void Highlight(IEnumerable<ThreadView.Selection> items, bool focus = true)
		{
			ThreadViewControl.Highlight(items, focus);
		}

		private void Row_EventNodeSelected(ThreadRow row, EventFrame frame, EventNode node)
		{
			EventFrame focusFrame = frame;
			if (node != null && node.Entry.CompareTo(frame.Header) != 0)
				focusFrame = new EventFrame(frame, node);
			RaiseEvent(new TimeLine.FocusFrameEventArgs(TimeLine.FocusFrameEvent, focusFrame, null));
		}

		private void Row_EventNodeHover(Point mousePos, Rect rect, ThreadRow row, EventNode node)
		{
			ThreadViewControl.ToolTipPanel = node != null ? new ThreadView.TooltipInfo { Text = String.Format("{0}   {1:0.000}ms", node.Name, node.Duration), Rect = rect } : null;
		}

		private void Row_ChartHover(Point mousePos, Rect rect, String text)
		{
			ThreadViewControl.ToolTipPanel = text != null ? new ThreadView.TooltipInfo { Text = text, Rect = rect } : null;
		}

		private void OnShowPopup(List<Object> dataContext)
		{
			SurfacePopup.DataContext = dataContext;
			SurfacePopup.IsOpen = dataContext.Count > 0 ? true : false;
		}

		class CallstackFilterItem : INotifyPropertyChanged
		{
			public bool IsChecked { get; set; }
			public CallStackReason Reason { get; set; }
			public event PropertyChangedEventHandler PropertyChanged;
		}
		List<CallstackFilterItem> CallstackFilter = new List<CallstackFilterItem>();


		private void ShowSyncWorkButton_Click(object sender, RoutedEventArgs e)
		{
			ThreadViewControl.Scroll.SyncDraw = ShowSyncWorkButton.IsChecked.Value ? ThreadScroll.SyncDrawType.Work : ThreadScroll.SyncDrawType.Wait;
			ThreadViewControl.UpdateSurface();
		}

		private void CallstackFilterDrowpdown_Click(object sender, RoutedEventArgs e)
		{
			CallstackFilterPopup.IsOpen = true;
		}

		private void ShowCallstacksButton_Checked(object sender, RoutedEventArgs e)
		{
			CallStackReason reason = 0;
			CallstackFilter.ForEach(filter => reason |= filter.IsChecked ? filter.Reason : 0);

			ThreadViewControl.Scroll.DrawCallstacks = reason;
			ThreadViewControl.UpdateSurface();
		}

		private void ShowCallstacksButton_Unchecked(object sender, RoutedEventArgs e)
		{
			ThreadViewControl.Scroll.DrawCallstacks = 0;
			ThreadViewControl.UpdateSurface();
		}
	}
}
