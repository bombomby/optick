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



	public class HoverTooltipLayer : Canvas
	{
		public HoverTooltipLayer()
		{
			Loaded += HoverTooltipLayer_Loaded;
			Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
			HoverLine = new Line() { Y1 = 0, Y2 = 0, X1 = 0, X2 = 0, Stroke = Brushes.LightGray, StrokeThickness = 2, StrokeDashArray = new DoubleCollection(new double[]{ 2.0, 1.0 }), Opacity = 0.5 };
			Children.Add(HoverLine);
		}

		private PlotBase parent;

		private void HoverTooltipLayer_Loaded(object sender, RoutedEventArgs e)
		{
			var visualParent = VisualTreeHelper.GetParent(this);
			parent = visualParent as PlotBase;
			while (visualParent != null && parent == null)
			{
				visualParent = VisualTreeHelper.GetParent(visualParent);
				parent = visualParent as PlotBase;
			}
			if (parent != null)
			{
				parent.MouseMove += Parent_MouseMove;
				parent.MouseLeave += Parent_MouseLeave;
				parent.PreviewMouseLeftButtonDown += Parent_PreviewMouseLeftButtonDown;
			}
		}

		public delegate void ItemClickedDelegate(int index);
		public event ItemClickedDelegate ItemClicked;

		public delegate void ItemHoverDelegate(int index);
		public event ItemHoverDelegate ItemHover;


		private void Parent_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			int index = GetIndex(e.GetPosition(this));
			ItemClicked?.Invoke(index);
		}

		public Line HoverLine { get; set; }

		private int hoverIndex = 0;
		public int HoverIndex
		{
			get { return hoverIndex; }
			set
			{
				if (hoverIndex != value)
				{
					hoverIndex = value;
					double posX = parent.LeftFromX(parent.XDataTransform.DataToPlot(hoverIndex));
					posX = Math.Max(0, Math.Min(posX, this.ActualWidth));

					HoverLine.X1 = posX;
					HoverLine.X2 = posX;
					HoverLine.Y1 = 0;
					HoverLine.Y2 = this.ActualHeight;

					ItemHover?.Invoke(value);
				}
			}
		}

		private void Parent_MouseLeave(object sender, MouseEventArgs e)
		{
			Visibility = Visibility.Hidden;
			ItemHover?.Invoke(-1);
		}

		private void Parent_MouseMove(object sender, MouseEventArgs e)
		{
			Visibility = Visibility.Visible;

			HoverIndex = GetIndex(e.GetPosition(this));
		}

		private int GetIndex(Point pos)
		{
			double plotX = parent.XFromLeft(pos.X);
			int index = (int)Math.Round(parent.XDataTransform.PlotToData(plotX));
			return Math.Max(0, index);
		}

	}


	/// <summary>
	/// Interaction logic for FunctionHistoryChartView.xaml
	/// </summary>
	public partial class FunctionHistoryChartView : UserControl
    {
        public FunctionHistoryChartView()
        {
            InitializeComponent();
			DataContextChanged += FunctionHistoryChartView_DataContextChanged;
			HoverTooltip.ItemClicked += HoverTooltip_ItemClicked;
			HoverTooltip.ItemHover += HoverTooltip_ItemHover;
		}

		private void HoverTooltip_ItemHover(int index)
		{
			FunctionViewModel vm = DataContext as FunctionViewModel;
			if (vm != null)
			{
				vm.OnDataHover(this, index);
			}
		}

		private void HoverTooltip_ItemClicked(int index)
		{
			FunctionViewModel vm = DataContext as FunctionViewModel;
			if (vm != null)
			{
				vm.OnDataClick(this, new List<int>() { index });
			}
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

		//private void Chart_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		//{
		//	int index = GetIndex(e.GetPosition(MouseNav));

		//	FunctionViewModel vm = DataContext as FunctionViewModel;
		//	if (vm != null)
		//	{
		//		vm.OnDataClick(this, index);
		//	}
		//}
	}
}
