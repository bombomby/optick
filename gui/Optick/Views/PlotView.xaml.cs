using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Profiler.ViewModels;
using Profiler.ViewModels.Plots;

namespace Profiler.Views
{
	public partial class PlotView : UserControl
	{
		public static readonly RoutedEvent PlotClickEvent = EventManager.RegisterRoutedEvent("PlotClick", RoutingStrategy.Bubble, typeof(PlotClickedEventHandler), typeof(TimeLine));
		
		public PlotView()
		{
			InitializeComponent();
			Chart.PlotTransformChanged += UpdateMouse;
			MouseMove += UpdateMouse;
		}
		
		public event PlotClickedEventHandler PlotClicked
		{
			add => AddHandler(PlotClickEvent, value);
			remove => RemoveHandler(PlotClickEvent, value);
		}

		private void UpdateMouse(Object s, EventArgs e)
		{
			var vm = (PlotsViewModel)DataContext;
			var pos = Mouse.GetPosition(PART_axisGrid);
			var cursorInside = !(pos.X < 0 || pos.Y < 0 || pos.X > PART_axisGrid.ActualWidth || pos.Y > PART_axisGrid.ActualHeight);
			
			vm.HoverVisibility = cursorInside ? Visibility.Visible : Visibility.Hidden;

			if (cursorInside)
			{
				double plotX = Chart.XFromLeft(pos.X);

				foreach (var selectedCounterViewModel in vm.SelectedCounterViewModels)
				{
					if (selectedCounterViewModel.Points.Count == 0)
						continue;

					var pointIndex = PlotsViewModel.GetPointIndex(selectedCounterViewModel.Points, plotX);
					selectedCounterViewModel.HoverPoint = new Point(selectedCounterViewModel.Points[pointIndex].X, selectedCounterViewModel.Points[pointIndex].Y);
					var offset = SelectedCounterViewModel.SelectedPointEllipseSize / 2;
					selectedCounterViewModel.HoverPointInScreenSpace = new Point(Chart.LeftFromX(selectedCounterViewModel.HoverPoint.X) - offset, Chart.TopFromY(selectedCounterViewModel.HoverPoint.Y) - offset);
				}
			}

			vm.MousePositionX = pos.X;
			vm.MousePositionY = pos.Y;
		}
		
		private void BtnAddCounter_OnClick(object sender, RoutedEventArgs e)
		{
			var vm = (PlotsViewModel)DataContext;
			vm.SelectCurrentCounter();
		}
		
		private void BtnAutofit_OnClick(object sender, RoutedEventArgs e)
		{
			Chart.IsAutoFitEnabled = true;
		}

		private void MouseNav_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var pos = Mouse.GetPosition(PART_axisGrid);
			var cursorInside = !(pos.X < 0 || pos.Y < 0 || pos.X > PART_axisGrid.ActualWidth || pos.Y > PART_axisGrid.ActualHeight);
			if (cursorInside)
			{
				double plotX = Chart.XFromLeft(pos.X);
				RaiseEvent(new PlotClickedEventArgs(plotX));
			}
		}
		
		public delegate void PlotClickedEventHandler(object sender, PlotClickedEventArgs e);
		
		public class PlotClickedEventArgs : RoutedEventArgs
		{
			public double Time { get; }

			public PlotClickedEventArgs(double time) : base(PlotClickEvent)
			{
				Time = time;
			}
		}
	}
}