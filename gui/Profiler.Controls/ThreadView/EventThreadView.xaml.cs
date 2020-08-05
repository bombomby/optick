using Profiler.Data;
using Profiler.InfrastructureMvvm;
using Profiler.Views;
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

namespace Profiler.Controls
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
			get
			{
				return group;
			}
			set
			{
				if (value != group)
				{
					group = value;

					InitThreadList(group);

					Visibility visibility = value == null ? Visibility.Collapsed : Visibility.Visible;

					ThreadToolsPanel.Visibility = visibility;
					FunctionSearchControl.DataContext = group;
					GroupStats.DataContext = group != null ? new FrameGroupStats(group) : null;
					SummaryView.ItemsSource = group?.Summary?.SummaryTable;
				}
			}
		}

		private static void OnGroupPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			FrameGroup value = e.NewValue as FrameGroup;
			EventThreadView threadView = d as EventThreadView;
			if (value != threadView.group)
			{
				threadView.group = value;

				threadView.InitThreadList(threadView.group);

				Visibility visibility = value == null ? Visibility.Collapsed : Visibility.Visible;

				threadView.ThreadToolsPanel.Visibility = visibility;
				threadView.FunctionSearchControl.DataContext = threadView.group;
				threadView.SummaryView.ItemsSource = threadView.group?.Summary?.SummaryTable;
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

		public ThreadViewSettings Settings { get; set; }

		public void OpenFunctionSearch()
		{
			FunctionSearchControl.Open();
		}

		private class EventsThreadGroup : IComparable<EventsThreadGroup>
		{
			public List<EventsThreadRow> Threads { get; set; } = new List<EventsThreadRow>();

			public bool IsMatch(EventsThreadRow thread)
			{
				// Max distance between thread names
				int maxLevensteinDistance = thread.Description.Name.Length / 3;

				foreach (EventsThreadRow candidate in Threads)
				{
					if (candidate.Description.Mask == thread.Description.Mask && candidate.Description.Origin == thread.Description.Origin)
						if (Utils.ComputeLevenshteinDistance(candidate.Description.Name, thread.Description.Name) < maxLevensteinDistance)
							return true;
				}
				return false;
			}

			private double CalcScore()
			{
				Int64 score = 0;
				foreach (EventsThreadRow thread in Threads)
				{
					foreach (EventFrame frame in thread.EventData.Events)
						score += frame.Entries.Count;
				}
				return (double)score / Threads.Count;
			}

			public void Sort()
			{
				Threads.Sort((a, b) =>
				{
					String rowA = a.Description.Name;
					String rowB = b.Description.Name;

					// Sorting by length at first to order "Thread 2" and Thread "12" correctly
					int res = rowA.Length.CompareTo(rowB.Length);
					if (res != 0)
						return res;

					// Sorting by name for the same length
					return rowA.CompareTo(rowB);
				});

				Score = CalcScore();
			}

			public int CompareTo(EventsThreadGroup other)
			{
				int compareOrigin = Threads[0].Description.Origin.CompareTo(other.Threads[0].Description.Origin);
				if (compareOrigin != 0)
					return compareOrigin;

				int compareMasks = Threads[0].Description.Mask.CompareTo(other.Threads[0].Description.Mask);
				if (compareMasks != 0)
					return -compareMasks;

				return -Score.CompareTo(other.Score);
			}

			public double Score { get; set; }
		}

		List<EventsThreadRow> SortRows(List<EventsThreadRow> threads)
		{
			List<EventsThreadGroup> groups = new List<EventsThreadGroup>();

			foreach (EventsThreadRow thread in threads)
			{
				EventsThreadGroup outputGroup = null;
				foreach (EventsThreadGroup group in groups)
				{
					if (group.IsMatch(thread))
					{
						outputGroup = group;
						break;
					}
				}

				if (outputGroup == null)
				{
					outputGroup = new EventsThreadGroup();
					groups.Add(outputGroup);
				}

				outputGroup.Threads.Add(thread);
			}

			List<EventsThreadRow> result = new List<EventsThreadRow>();
			groups.ForEach(g => g.Sort());
			groups.Sort();
			foreach (var group in groups)
				result.AddRange(group.Threads);

			return result;
		}

		const double MIN_THREAD_ACCUMULATED_DURATION = 0.1;

		List<EventsThreadRow> GenerateThreadRows(FrameGroup group)
		{
			id2row.Clear();

			List<EventsThreadRow> eventThreads = new List<EventsThreadRow>();

			for (int i = 0; i < Math.Min(group.Board.Threads.Count, group.Threads.Count); ++i)
			{
				ThreadData data = group.Threads[i];
				ThreadDescription thread = data.Description;

                if (thread.IsIdle)
                    continue;

				bool threadHasData = false;
				if (data.Events != null)
				{
					double duration = 0.0;
					foreach (EventFrame frame in data.Events)
					{
						duration += frame.Duration;
						if (duration > MIN_THREAD_ACCUMULATED_DURATION)
						{
							threadHasData = true;
							break;
						}
					}
				}

				if (thread.Origin == ThreadDescription.Source.Core)
					threadHasData = true;

				if (threadHasData)
				{
					EventsThreadRow row = new EventsThreadRow(group, thread, data, Settings);
					eventThreads.Add(row);

					id2row.Add(row.Description.ThreadIndex, row);
					row.EventNodeHover += Row_EventNodeHover;
					row.EventNodeSelected += Row_EventNodeSelected;
				}

				if (GenerateSamplingThreads && data.Callstacks != null && data.Callstacks.Count > 3)
				{
					ThreadData samplingData = GenerateSamplingThread(group, data);
					EventsThreadRow row = new EventsThreadRow(group, new ThreadDescription() { Name = thread.Name + " [Sampling]", Origin = ThreadDescription.Source.Sampling }, samplingData, Settings);
					eventThreads.Add(row);
					row.EventNodeHover += Row_EventNodeHover;
					row.EventNodeSelected += Row_EventNodeSelected;
				}
			}

			return SortRows(eventThreads);
		}


		enum ProcessGroup
		{
			None,
			CurrentProcess,
			OtherProcess,
		}

		private static ProcessGroup GetProcessGroup(FrameGroup group, UInt64 threadID)
		{
            if (threadID == 0)
                return ProcessGroup.None;

            ThreadData thread = group.GetThread(threadID);
            if (thread != null)
                return ProcessGroup.CurrentProcess;

            ThreadDescription desc = null;
            if (group.Board.ThreadDescriptions.TryGetValue(threadID, out desc))
                if (desc.IsIdle)
                    return ProcessGroup.None;

            return ProcessGroup.OtherProcess;
		}

		public static ChartRow GenerateCoreChart(FrameGroup group)
		{
			if (group.Synchronization == null || group.Synchronization.Events.Count == 0)
				return null;

			group.Synchronization.Events.Sort();

			int eventsCount = group.Synchronization.Events.Count;

			List<Tick> timestamps = new List<Tick>(eventsCount);
			ChartRow.Entry currProcess = new ChartRow.Entry(eventsCount) { Fill = Colors.LimeGreen, Name = "Current Process" };
			ChartRow.Entry otherProcess = new ChartRow.Entry(eventsCount) { Fill = Colors.Tomato, Name = "Other Process" };

			List<bool> isCoreInUse = new List<bool>(group.Board.CPUCoreCount);

			int currCores = 0;
			int otherCores = 0;

			foreach (SyncEvent ev in group.Synchronization.Events)
			{
				ProcessGroup prevGroup = GetProcessGroup(group, ev.OldThreadID);
				ProcessGroup currGroup = GetProcessGroup(group, ev.NewThreadID);

                while (isCoreInUse.Count <= ev.CPUID)
                    isCoreInUse.Add(false);

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

			ChartRow chart = new ChartRow("CPU", timestamps, new List<ChartRow.Entry>() { currProcess, otherProcess }, isCoreInUse.Count);
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
			ThreadData result = new ThreadData(null)
			{
				Events = frames
			};
			return result;
		}

		List<ThreadRow> coreRows = new List<ThreadRow>();

		void InitThreadList(FrameGroup group)
		{
			List<ThreadRow> rows = new List<ThreadRow>();

			if (group != null)
			{
				rows.Add(new HeaderThreadRow(group)
				{
					GradientTop = (ThreadViewControl.OptickAlternativeBackground as SolidColorBrush).Color,
					GradientBottom = (ThreadViewControl.OptickBackground as SolidColorBrush).Color,
					TextColor = Colors.Gray,
					Header = new ThreadFilterView(),
				});

				ChartRow cpuCoreChart = GenerateCoreChart(group);
				if (cpuCoreChart != null)
				{
					cpuCoreChart.IsExpanded = false;
					cpuCoreChart.ExpandChanged += CpuCoreChart_ExpandChanged;
					cpuCoreChart.ChartHover += Row_ChartHover;
					rows.Add(cpuCoreChart);
				}

				List<EventsThreadRow> threadRows = GenerateThreadRows(group);
				foreach (EventsThreadRow row in threadRows)
				{
					if (row.Description.Origin == ThreadDescription.Source.Core)
					{
						row.IsVisible = false;
						coreRows.Add(row);
					}
				}
				rows.AddRange(threadRows);
			}

			ThreadViewControl.InitRows(rows, group != null ? group.Board.TimeSlice : null);

			List<ITick> frames = null;

			if (Group != null && Group.Frames != null && Group.Frames[FrameList.Type.CPU] != null)
			{
				FrameList list = Group.Frames[FrameList.Type.CPU];
				frames = list.Events.ConvertAll(frame => frame as ITick);
			}
			else if (Group != null)
			{
				frames = new List<ITick>();
				long step = Durable.MsToTick(1000.0);
				for (long timestamp = Group.Board.TimeSlice.Start; timestamp < Group.Board.TimeSlice.Finish; timestamp += step)
					frames.Add(new Tick() { Start = timestamp });
			}

			ThreadViewControl.InitForegroundLines(frames);
		}

		private void CpuCoreChart_ExpandChanged(ThreadRow row)
		{
			if (row.IsExpanded)
			{
				if (!Group.IsCoreDataGenerated)
				{
					bool isVisible = row.IsExpanded;
					Task.Run(() =>
					{
						row.SetBusy(true);
						Group.GenerateRealCoreThreads();
						ThreadViewControl.ReinitRows(coreRows);
						row.SetBusy(false);
						Application.Current.Dispatcher.Invoke(new Action(() => coreRows.ForEach(core => core.IsVisible = isVisible)));
					});
					return;
				}
			}

			coreRows.ForEach(core => core.IsVisible = row.IsExpanded);
			ThreadViewControl.UpdateRows();
		}

		public void Highlight(EventFrame frame, IDurable focus)
		{
			Group = frame.Group;
			ThreadRow row = null;
			id2row.TryGetValue(frame.Header.ThreadIndex, out row);
			Highlight(new ThreadViewControl.Selection[] { new ThreadViewControl.Selection() { Frame = frame, Focus = focus, Row = row } });
		}


		public void Highlight(IEnumerable<ThreadViewControl.Selection> items, bool focus = true)
		{
			ThreadViewControl.Highlight(items, focus);
		}

		private void Row_EventNodeSelected(ThreadRow row, EventFrame frame, EventNode node)
		{
			EventFrame focusFrame = frame;
			if (node != null && node.Entry.CompareTo(frame.Header) != 0)
				focusFrame = new EventFrame(frame, node);
			RaiseEvent(new FocusFrameEventArgs(GlobalEvents.FocusFrameEvent, focusFrame, null));
		}

		private void Row_EventNodeHover(Point mousePos, Rect rect, ThreadRow row, EventNode node)
		{
			ThreadViewControl.ToolTipPanel = node != null ? new ThreadViewControl.TooltipInfo { Text = String.Format("{0}   {1:0.000}ms", node.Name, node.Duration), Rect = rect } : null;
		}

		private void Row_ChartHover(Point mousePos, Rect rect, String text)
		{
			ThreadViewControl.ToolTipPanel = text != null ? new ThreadViewControl.TooltipInfo { Text = text, Rect = rect } : null;
		}

		private void OnShowPopup(List<Object> dataContext)
		{
			SurfacePopup.DataContext = dataContext;
			SurfacePopup.IsOpen = dataContext.Count > 0 ? true : false;
		}

		class CallstackFilterItem : BaseViewModel
		{
			public bool IsChecked { get; set; }
			public CallStackReason Reason { get; set; }
		}
		List<CallstackFilterItem> CallstackFilter = new List<CallstackFilterItem>();


		private void ShowSyncWorkButton_Click(object sender, RoutedEventArgs e)
		{
			ThreadViewControl.Scroll.SyncDraw = ShowSyncWorkButton.IsChecked.Value ? ThreadScroll.SyncDrawType.Work : ThreadScroll.SyncDrawType.Wait;
			ThreadViewControl.UpdateSurface();
		}

		private void ShowFrameLinesButton_Click(object sender, RoutedEventArgs e)
		{
			ThreadViewControl.ShowFrameLines = ShowFrameLinesButton.IsChecked.GetValueOrDefault(true);
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

		//private void ShowTagsButton_Click(object sender, RoutedEventArgs e)
		//{
		//	ThreadViewControl.Scroll.DrawDataTags = ShowTagsButton.IsChecked.Value;
		//	ThreadViewControl.UpdateSurface();
		//}
	}
}
