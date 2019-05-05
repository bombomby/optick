using InteractiveDataDisplay.WPF;
using Profiler.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Profiler.Views
{
	/// <summary>
	/// Interaction logic for FunctionHistoryChartView.xaml
	/// </summary>
	public partial class FunctionHistoryChartView : UserControl
    {
        public FunctionHistoryChartView()
        {
            InitializeComponent();
			DataContextChanged += FunctionHistoryChartView_DataContextChanged;

			Chart.MouseLeftButtonUp += Chart_MouseLeftButtonUp;
		}

		private int GetIndex(Point pos)
		{
			double plotX = Chart.XFromLeft(pos.X);
			return (int)Chart.XDataTransform.PlotToData(plotX);
		}


		private void FunctionHistoryChartView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			FunctionSummaryViewModel vm = DataContext as FunctionSummaryViewModel;
			if (vm != null)
			{
				vm.OnChanged += Update;
			}
		}

		public void Update()
		{
			FunctionSummaryViewModel vm = DataContext as FunctionSummaryViewModel;
			if (vm != null && vm.Stats != null)
			{
				Chart.IsAutoFitEnabled = true;
				WorkChart.PlotY(vm.Stats.Samples.Select(s => s.Work));
				WaitChart.PlotY(vm.Stats.Samples.Select(s => s.Wait));
			}
			else
			{
				WorkChart.Plot(Array.Empty<double>(), Array.Empty<double>());
				WaitChart.Plot(Array.Empty<double>(), Array.Empty<double>());
			}
		}

		private void Chart_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			int index = GetIndex(e.GetPosition(this));

			FunctionViewModel vm = DataContext as FunctionViewModel;
			if (vm != null)
			{
				vm.OnDataClick(this, index);
			}
		}

	}
}
