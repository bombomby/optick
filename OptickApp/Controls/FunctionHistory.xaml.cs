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
        public FunctionHistory()
		{
			InitializeComponent();

			FrameChart.TooltipTimeout = new TimeSpan(0, 0, 0, 0, 100);
			FrameChart.DataTooltip.Background = FindResource("BroBackground") as SolidColorBrush;
			FrameChart.DataTooltip.BorderBrush = FindResource("AccentColorBrush") as SolidColorBrush;
			FrameChart.DataTooltip.BorderThickness = new Thickness(0.5);
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

		public void Clear()
		{
			ClearCharts();
		}

		private void ClearCharts()
		{
			FrameChart.Series = null;
		}

        //public void Load(FunctionStats frameStats)
        //{
        //    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        //    {
        //        Stats = frameStats;
        //        FunctionInstanceDataGrid.DataContext = Stats;
        //        FrameChart.Series = BuildAreaChart(Stats);
        //        HeaderGrid.DataContext = Stats;
        //    }));
        //}


		//private void FrameChart_DataClick(object sender, ChartPoint point)
		//{
		//	int index = (int)point.X;
		//	if (Stats != null && 0 <= index && index < Stats.Samples.Count)
		//	{
		//		FunctionStats.Sample sample = Stats.Samples[index];

		//		Entry maxEntry = null;
		//		double maxDuration = 0;

		//		sample.Entries.ForEach(e => { if (maxDuration < e.Duration) { maxDuration = e.Duration; maxEntry = e; } });

		//		EventNode maxNode = maxEntry.Frame.Root.FindNode(maxEntry);

		//		RaiseEvent(new TimeLine.FocusFrameEventArgs(TimeLine.FocusFrameEvent, new EventFrame(maxEntry.Frame, maxNode), null));
		//	}
		//}
	}
}
