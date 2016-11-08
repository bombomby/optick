using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using Profiler.Data;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;

namespace Profiler
{
	public delegate void SelectedTreeNodeChangedHandler(Data.Frame frame, BaseTreeNode node);

    /// <summary>
    /// Interaction logic for FrameInfo.xaml
    /// </summary>
    public partial class FrameInfo : UserControl
	{

		public FrameInfo()
		{
			this.InitializeComponent();
			SummaryTable.FilterApplied += new ApplyFilterEventHandler(ApplyFilterToEventTree);
			SummaryTable.DescriptionFilterApplied += new ApplyDescriptionFilterEventHandler(ApplyDescriptionFilterToEventTree);

			EventTreeView.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(EventTreeView_SelectedItemChanged);
		}

		private Data.Frame frame;

		public void SetFrame(Data.Frame frame)
		{
			this.frame = frame;
			Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => { frame.Load(); this.DataContext = frame; }));
		}

		public void RefreshFilter(object sender, RoutedEventArgs e)
		{
			SummaryTable.RefreshFilter();
		}

		private void ApplyFilterToEventTree(HashSet<Object> filter, FilterMode mode)
		{
			if (FocusCallStack.IsChecked ?? true)
				mode.HideNotRelative = true;

			if (FilterByTime.IsChecked ?? true)
			{
				double limit = 0.0;
				if (Double.TryParse(TimeLimit.Text.Replace('.', ','), out limit))
					mode.TimeLimit = limit;
			}

			HashSet<Object> roof = null;

			if (filter != null && filter.Count > 0)
			{
				roof = new HashSet<Object>();

				foreach (Object node in filter)
				{
					BaseTreeNode current = (node as BaseTreeNode).Parent;
					while (current != null)
					{
						if (!roof.Add(current))
							break;

						current = current.Parent;
					}
				}
			}

			foreach (var node in EventTreeView.ItemsSource)
			{
				if (node is BaseTreeNode)
				{
					BaseTreeNode eventNode = node as BaseTreeNode;

					Application.Current.Dispatcher.BeginInvoke(new Action(() =>
					{
						eventNode.ApplyFilter(roof, filter, mode);
					}), DispatcherPriority.Loaded);
				}
			}
		}

		private void ApplyDescriptionFilterToEventTree(HashSet<Object> filter)
		{
			if (filter == null)
			{
				SummaryTable.FilterSummaryControl.Visibility = Visibility.Collapsed;
			}
			else
			{
				Application.Current.Dispatcher.BeginInvoke(new Action(() =>
				{
					if (frame is EventFrame)
					{
						double time = (frame as EventFrame).CalculateFilteredTime(filter);
						SummaryTable.FilterSummaryText.Content = String.Format("Filter Coverage: {0:0.###}ms", time).Replace(',', '.');
					}
					else if (frame is SamplingFrame)
					{
						SamplingFrame samplingFrame = frame as SamplingFrame;
						double count = samplingFrame.Root.CalculateFilteredTime(filter);
						SummaryTable.FilterSummaryText.Content = String.Format("Filter Coverage: {0:0.###}% ({1}/{2})", 100.0 * count / samplingFrame.Root.Duration, count, samplingFrame.Root.Duration).Replace(',', '.');
					}
					SummaryTable.FilterSummaryControl.Visibility = Visibility.Visible;
				}));
			}
		}

		public event SelectedTreeNodeChangedHandler SelectedTreeNodeChanged;

		void EventTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (e.NewValue is BaseTreeNode && DataContext is Data.Frame)
			{
				SelectedTreeNodeChanged(DataContext as Data.Frame, e.NewValue as BaseTreeNode);
			}
		}

		public bool FocusOnNode(Durable focusRange)
		{
			if (EventTreeView == null)
			{
				return false;
			}

			if (EventTreeView.ItemsSource == null)
			{
				return false;
			}

			List<BaseTreeNode> treePath = new List<BaseTreeNode>();

			foreach (var node in EventTreeView.ItemsSource)
			{
				BaseTreeNode baseTreeNode = node as BaseTreeNode;
				if (baseTreeNode == null)
				{
					continue;
				}

				baseTreeNode.ForEach( (curNode, level) =>
				{
					EventNode treeEventNode = curNode as EventNode;
					if (treeEventNode == null)
					{
						return true;
					}

					if (treeEventNode.Entry.Start > focusRange.Finish)
					{
						return false;
					}

					if (treeEventNode.Entry.Intersect(focusRange))
					{
						treePath.Add(curNode);

						//find desired node in tree
						if (treeEventNode.Entry.Start >= focusRange.Start && treeEventNode.Entry.Finish <= focusRange.Finish)
						{
							return false;
						}
					}


					return true;
				});

				ItemsControl root = EventTreeView;

				int pathElementsCount = treePath.Count;
				if (pathElementsCount > 0)
				{
					//expand path in tree
					int index = 0;
					for (index = 0; index < (pathElementsCount - 1); index++)
					{
						BaseTreeNode expandNode = treePath[index];

						if (root != null)
						{
							root = root.ItemContainerGenerator.ContainerFromItem(expandNode) as ItemsControl;
						}

						treePath[index].IsExpanded = true;
					}

					BaseTreeNode finalNode = treePath[index];

					// select target node
					finalNode.IsExpanded = false;
					finalNode.IsSelected = true;

					// focus on finalNode
					if (root != null)
					{
						root = root.ItemContainerGenerator.ContainerFromItem(finalNode) as ItemsControl;
						if (root != null)
						{
							root.BringIntoView();
						}
					}

					EventTreeView.InvalidateVisual();

					return true;
				}
			
			}

			return false;
		}

        private void MenuShowSourceCode(object sender, RoutedEventArgs e)
        {
            if (e.Source is FrameworkElement)
            {
                FrameworkElement item = e.Source as FrameworkElement;

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    Object windowDataContext = null;
                    if (item.DataContext is SamplingNode)
                    {
                        windowDataContext = SourceView<SamplingBoardItem, SamplingDescription, SamplingNode>.Create(SummaryTable.DataContext as Board<SamplingBoardItem, SamplingDescription, SamplingNode>, (item.DataContext as SamplingNode).Description.Path);
                    }
                    else
                    {
                        if (item.DataContext is EventNode)
                        {
                            windowDataContext = SourceView<EventBoardItem, EventDescription, EventNode>.Create(SummaryTable.DataContext as Board<EventBoardItem, EventDescription, EventNode>, (item.DataContext as EventNode).Description.Path);
                        }
                    }

                    if (windowDataContext != null)
                    {
                        new SourceWindow() { DataContext = windowDataContext, Owner = Application.Current.MainWindow }.Show();
                    }
                }));
            }
        }

        private void SampleFunction(EventFrame eventFrame, EventNode node, bool single)
        {
            List<Callstack> callstacks = new List<Callstack>();
            FrameGroup group = eventFrame.Group;

            if (single)
            {
                Utils.ForEachInsideIntervalStrict(group.Threads[eventFrame.Header.ThreadIndex].Callstacks, node.Entry, callstack => callstacks.Add(callstack));
            }
            else
            {
                EventDescription desc = node.Entry.Description;

                foreach (ThreadData thread in group.Threads)
                {
                    HashSet<Callstack> accumulator = new HashSet<Callstack>();
                    foreach (EventFrame currentFrame in thread.Events)
                    {
                        List<Entry> entries = null;
                        if (currentFrame.ShortBoard.TryGetValue(desc, out entries))
                        {
                            foreach (Entry entry in entries)
                            {
                                Utils.ForEachInsideIntervalStrict(thread.Callstacks, entry, c => accumulator.Add(c));
                            }
                        }
                    }

                    callstacks.AddRange(accumulator);
                }
            }


            if (callstacks.Count > 0)
            {
                SamplingFrame frame = new SamplingFrame(callstacks);
				Profiler.TimeLine.FocusFrameEventArgs args = new Profiler.TimeLine.FocusFrameEventArgs(Profiler.TimeLine.FocusFrameEvent, frame);
                RaiseEvent(args);
            }
        }

        private void MenuSampleFunction(object sender, RoutedEventArgs e)
        {
            if (frame is EventFrame && e.Source is FrameworkElement)
            {
                FrameworkElement item = e.Source as FrameworkElement;
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    SampleFunction(frame as EventFrame, item.DataContext as EventNode, true);
                }));
            }
        }

        private void MenuSampleFunctions(object sender, RoutedEventArgs e)
        {
            if (frame is EventFrame && e.Source is FrameworkElement)
            {
                FrameworkElement item = e.Source as FrameworkElement;
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    SampleFunction(frame as EventFrame, item.DataContext as EventNode, false);
                }));
            }
        }
    }
}