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

namespace Profiler
{
	/// <summary>
	/// Interaction logic for TimeLineItem.xaml
	/// </summary>
	public partial class TimeLineItem : UserControl
	{
		public TimeLineItem()
		{
			this.InitializeComponent();
			Init();

			DataContextChanged += new DependencyPropertyChangedEventHandler(TimeLineItem_DataContextChanged);
		}

		void TimeLineItem_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			Init();
		}

		void Init()
		{
			if (DataContext is Data.EventFrame)
			{
        Data.EventFrame frame = (Data.EventFrame)DataContext;
				LayoutRoot.Children.Clear();

				double frameTime = frame.Duration;
				double frameStartMS = frame.Header.StartMS;

				foreach (var entry in frame.Categories)
				{
					Rectangle rect = new Rectangle();
					rect.Width = double.NaN;
					rect.Height = FrameHeightConverter.Convert(entry.Duration);
          rect.Fill = new SolidColorBrush(entry.Description.Color);

					double startTime = (entry.StartMS - frameStartMS);
					rect.Margin = new Thickness(0, FrameHeightConverter.Convert(startTime), 0, 0);
					rect.VerticalAlignment = VerticalAlignment.Top;
			
					LayoutRoot.Children.Add(rect);
				}
			}
		}
	}
}