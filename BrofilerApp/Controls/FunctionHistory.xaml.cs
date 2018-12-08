using LiveCharts;
using LiveCharts.Wpf;
using Profiler.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Profiler.Controls
{
	/// <summary>
	/// Interaction logic for FunctionHistory.xaml
	/// </summary>
	public partial class FunctionHistory : UserControl
	{
		public FrameGroup Group { get; set; }

		public FunctionHistory()
		{
			InitializeComponent();

			DataContextChanged += FunctionHistory_DataContextChanged;
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
		const double AreaOpacity = 0.33;
		const double AreaStrokeThickness = 2;

		private SeriesCollection BuildAreaChart(FunctionStats function)
		{
			return new SeriesCollection
				{
					new StackedAreaSeries
					{
						Title = "Work",
						Values = new ChartValues<double>(function.Samples.Select(sample => sample.Work)),
						LineSmoothness = 0,
						LabelPoint = p => p.Y.ToString("N3"),
						Fill = new SolidColorBrush { Color = WorkColor, Opacity = AreaOpacity },
						Stroke = new SolidColorBrush { Color = WorkColor },
						StrokeThickness = AreaStrokeThickness
					},
					new StackedAreaSeries
					{
						Title = "Wait",
						Values = new ChartValues<double>(function.Samples.Select(sample => sample.Wait)),
						LineSmoothness = 0,
						LabelPoint = p => p.Y.ToString("N3"),
						Fill = new SolidColorBrush { Color = WaitColor, Opacity = AreaOpacity },
						Stroke = new SolidColorBrush { Color = WaitColor },
						 StrokeThickness = AreaStrokeThickness
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
			//FunctionChart.Series = null;
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
			if (desc != null)
			{
				// Frame Chart
				FunctionStats frameStats = new FunctionStats(Group, desc);
				frameStats.Load(FunctionStats.Origin.MainThread);

				Application.Current.Dispatcher.BeginInvoke(new Action(() =>
				{
					FunctionStatsSummary.DataContext = new FunctionSummary() {
						Description = desc,
						Total = frameStats.Samples.Sum(s => s.Duration) / frameStats.Samples.Count,
						Wait = frameStats.Samples.Sum(s => s.Wait) / frameStats.Samples.Count
					};
					FrameChart.Series = BuildAreaChart(frameStats);
				}));

				// Function Chart
				//FunctionStats functionStats = new FunctionStats(Group, desc);
				//functionStats.Load(FunctionStats.Origin.IndividualCalls);
				//Application.Current.Dispatcher.BeginInvoke(new Action(() => FunctionChart.Series = BuildAreaChart(functionStats)));
			}
		}
	}
}
