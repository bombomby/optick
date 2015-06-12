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
		}

    private Data.Frame frame;

    public void SetFrame(Data.Frame frame)
    {
      this.frame = frame;
      Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => { frame.Load();  this.DataContext = frame; }));
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

		private void OnTreeViewItemMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.Source is FrameworkElement)
			{
				e.Handled = true;

				FrameworkElement item = e.Source as FrameworkElement;

				Application.Current.Dispatcher.Invoke(new Action(() =>
				{
					Object windowDataContext = null;
					if (item.DataContext is SamplingNode)
						windowDataContext = SourceView<SamplingBoardItem, SamplingDescription, SamplingNode>.Create(SummaryTable.DataContext as Board<SamplingBoardItem, SamplingDescription, SamplingNode>, (item.DataContext as SamplingNode).Description.Path);
					else if (item.DataContext is EventNode)
						windowDataContext = SourceView<EventBoardItem, EventDescription, EventNode>.Create(SummaryTable.DataContext as Board<EventBoardItem, EventDescription, EventNode>, (item.DataContext as EventNode).Description.Path);

					if (windowDataContext != null)
					{
						new SourceWindow() { DataContext = windowDataContext, Owner = Application.Current.MainWindow }.Show();
					}
				}));
			}
		}
	}
}