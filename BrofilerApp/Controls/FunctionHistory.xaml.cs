using LiveCharts;
using LiveCharts.Wpf;
using Profiler.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Profiler.Controls
{
	/// <summary>
	/// Interaction logic for FunctionHistory.xaml
	/// </summary>
	public partial class FunctionHistory : UserControl
	{
		public FrameGroup Group { get; set; }
		public EventDescription Description { get; set; }
		public FunctionStats Stats { get; set; }

		public FunctionHistory()
		{
			InitializeComponent();

			DataContextChanged += FunctionHistory_DataContextChanged;

			FrameChart.TooltipTimeout = new TimeSpan();
			FrameChart.DataTooltip.Background = FindResource("BroBackground") as SolidColorBrush;
			FrameChart.DataTooltip.BorderBrush = FindResource("AccentColorBrush") as SolidColorBrush;
			FrameChart.DataTooltip.BorderThickness = new Thickness(0.5);
		}

		private void UpdateGroup(FrameGroup group)
		{
			if (group != Group)
			{
				Group = group;
			}
		}

		private void FunctionHistory_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is FrameGroup)
			{
				Group = e.NewValue as FrameGroup;
			}
		}

		static Color WorkColor = Colors.LimeGreen;
		static Color WaitColor = Colors.Tomato;
		const double AreaOpacity = 0.66;
		const double ChartStrokeThickness = 0;
		const double ChartColumnPadding = 0.5;

		private SeriesCollection BuildAreaChart(FunctionStats function)
		{
			return new SeriesCollection
				{
					new StackedColumnSeries
					{
						Title = "Work",
						Values = new ChartValues<double>(function.Samples.Select(sample => sample.Work)),
						LabelPoint = p => String.Format("{0:0.000}ms", p.Y),
						Fill = new SolidColorBrush { Color = WorkColor, Opacity = AreaOpacity },
						Stroke = new SolidColorBrush { Color = WorkColor },
						StrokeThickness = ChartStrokeThickness,
						ColumnPadding = ChartColumnPadding
					},
					new StackedColumnSeries
					{
						Title = "Wait",
						Values = new ChartValues<double>(function.Samples.Select(sample => sample.Wait)),
						LabelPoint = p => String.Format("{0:0.000}ms", p.Y),
						Fill = new SolidColorBrush { Color = WaitColor, Opacity = AreaOpacity },
						Stroke = new SolidColorBrush { Color = WaitColor },
						StrokeThickness = ChartStrokeThickness,
						ColumnPadding = ChartColumnPadding
					},
				};
		}

		public void LoadAsync(Data.Frame frame)
		{
			if (frame is EventFrame)
			{
				EventFrame eventFrame = frame as EventFrame;
				if (eventFrame.Entries.Count > 0)
				{
					Entry entry = eventFrame.Entries[0];
					UpdateGroup(frame.Group);
					Task.Run(() => Load(entry.Description));
				}
			}
		}

		public void Clear()
		{
			UpdateGroup(null);
			ClearCharts();
		}

		private void ClearCharts()
		{
			FrameChart.Series = null;
			FunctionStatsSummary.DataContext = null;
		}

		class FunctionSummary
		{
			public EventDescription Description { get; set; }
			public double Total { get; set; }
			public double Wait { get; set; }
			public double Work { get { return Total - Wait; } }
		}

		public void Load(EventDescription desc)
		{
			if (desc != null && Description != desc)
			{
				// Frame Chart
				FunctionStats frameStats = new FunctionStats(Group, desc);
				frameStats.Load(FunctionStats.Origin.MainThread);

				FunctionSummary summary = new FunctionSummary()
				{
					Description = desc,
					Total = frameStats.Samples.Sum(s => s.Duration) / frameStats.Samples.Count,
					Wait = frameStats.Samples.Sum(s => s.Wait) / frameStats.Samples.Count
				};

				Application.Current.Dispatcher.BeginInvoke(new Action(() =>
				{
					Stats = frameStats;
					FunctionStatsSummary.DataContext = summary;
					FrameChart.Series = BuildAreaChart(frameStats); ;
				}));


				// Function Chart
				//FunctionStats functionStats = new FunctionStats(Group, desc);
				//functionStats.Load(FunctionStats.Origin.IndividualCalls);
				//Application.Current.Dispatcher.BeginInvoke(new Action(() => FunctionChart.Series = BuildAreaChart(functionStats)));
			}

			Description = desc;
		}

		private void FrameChart_DataClick(object sender, ChartPoint point)
		{
			int index = (int)point.X;
			if (Stats != null && 0 <= index && index < Stats.Samples.Count)
			{
				FunctionStats.Sample sample = Stats.Samples[index];

				Entry maxEntry = null;
				double maxDuration = 0;

				sample.Entries.ForEach(e => { if (maxDuration < e.Duration) { maxDuration = e.Duration; maxEntry = e; } });

				EventNode maxNode = maxEntry.Frame.Root.FindNode(maxEntry);

				RaiseEvent(new TimeLine.FocusFrameEventArgs(TimeLine.FocusFrameEvent, new EventFrame(maxEntry.Frame, maxNode), null));
			}
		}
	}
}
