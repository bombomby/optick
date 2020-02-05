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
using System.Globalization;
using Profiler.ViewModels;
using Profiler.Controls.ViewModels;
using Profiler.Controls;

namespace Profiler
{
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
			SummaryTable.DescriptionFilterApplied += new ApplyDescriptionFilterEventHandler(ApplyDescriptionFilterToFramesTimeLine);

			EventTreeView.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(EventTreeView_SelectedItemChanged);
		}

		private Data.Frame frame;

		public virtual void SetFrame(Data.Frame frame, IDurable node)
		{
            if (this.frame != frame)
            {
                this.frame = frame;
                this.DataContext = frame;
            }
            else if (node != null)
            {
                FocusOnNode(node);
            }
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
				if (Double.TryParse(TimeLimit.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out limit))
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


		private void ApplyDescriptionFilterToFramesTimeLine(HashSet<Object> filter)
		{
			Application.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				Data.Frame frame = DataContext as Data.Frame;

				EventFrame eventFrame = frame as EventFrame;
				if (eventFrame != null)
				{
					if (filter == null)
					{
						eventFrame.FilteredDescription = "";
					}
					else
					{
						double timeInMs = eventFrame.CalculateFilteredTime(filter);
						if (timeInMs > 0)
							eventFrame.FilteredDescription = String.Format("{0:0.000}", timeInMs);
						else
							eventFrame.FilteredDescription = "";
					}
				}
			}));
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

		void EventTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (e.NewValue is EventNode && DataContext is Data.EventFrame)
			{
                RaiseEvent(new Controls.HighlightFrameEventArgs(new ThreadViewControl.Selection[] { new ThreadViewControl.Selection() { Frame = DataContext as Data.EventFrame, Focus = (e.NewValue as EventNode).Entry } }));
			}
		}

		public bool FocusOnNode(IDurable focusRange)
		{
            if (focusRange == null)
                return false;

			if (EventTreeView == null)
				return false;

			if (EventTreeView.ItemsSource == null)
				return false;

			List<BaseTreeNode> treePath = new List<BaseTreeNode>();

			foreach (var node in EventTreeView.ItemsSource)
			{
				BaseTreeNode baseTreeNode = node as BaseTreeNode;
				if (baseTreeNode == null)
				{
					continue;
				}

				baseTreeNode.ForEach((curNode, level) =>
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

				   if (treeEventNode.Entry.Contains(focusRange))
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

		private void SampleFunction(EventFrame eventFrame, CallStackReason callstackFilter, EventNode node)
		{
			List<Callstack> callstacks = new List<Callstack>();
			FrameGroup group = eventFrame.Group;

			EventDescription desc = node.Entry.Description;
			callstacks = group.GetCallstacks(desc);

			if (callstacks.Count > 0)
			{
				SamplingFrame frame = new SamplingFrame(callstacks, group);
				FocusFrameEventArgs args = new FocusFrameEventArgs(GlobalEvents.FocusFrameEvent, frame);
				RaiseEvent(args);
			}
		}

		private void MenuSampleFunctions(object sender, RoutedEventArgs e)
		{
			if (frame is EventFrame && e.Source is FrameworkElement)
			{
				FrameworkElement item = e.Source as FrameworkElement;
				Application.Current.Dispatcher.Invoke(new Action(() =>
				{
					SampleFunction(frame as EventFrame, CallStackReason.AutoSample, item.DataContext as EventNode);
				}));
			}
		}

		private void TreeViewCopy_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			TreeView tree = (TreeView)sender;
			Clipboard.SetText(tree.SelectedItem.ToString());
		}
	}


    public class SampleInfo : FrameInfo
    {
        public CallStackReason CallstackType { get; set; }

		public SampleInfo()
		{
			IsVisibleChanged += SampleInfo_IsVisibleChanged;
		}

		private void SampleInfo_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			VM.SetActive(IsVisible);
		}

		private SamplingViewModel vm = null;
		public SamplingViewModel VM
		{
			get { return vm; }
			set
			{
				vm = value;
				if (vm != null)
					vm.OnLoaded += VM_OnLoaded;
			}
		}

		private void VM_OnLoaded(SamplingFrame frame)
		{
			SetFrame(frame, null);
		}

		public Data.Frame SourceFrame { get; set; }

        public override void SetFrame(Data.Frame frame, IDurable node)
        {
            if (frame is EventFrame)
            {
                if (SourceFrame == frame)
                    return;

                SourceFrame = frame;

                SamplingFrame samplingFrame = frame.Group.CreateSamplingFrame((frame as EventFrame).RootEntry.Description, CallstackType);
                base.SetFrame(samplingFrame, null);
            }
            else
            {
                base.SetFrame(frame, null);
            }
        }
    }
}