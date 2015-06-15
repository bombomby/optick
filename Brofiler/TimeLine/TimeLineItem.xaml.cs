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
using Profiler.Data;

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

		void InitNode(EventNode node, double frameStartMS, int level)
		{
			double height = FrameHeightConverter.Convert(node.Entry.Duration);

			if (height < 6.0 && level != 0)
				return;

			Rectangle rect = new Rectangle();
			rect.Width = double.NaN;
			rect.Height = height;
			rect.Fill = new SolidColorBrush(node.Entry.Description.Color);

			double startTime = (node.Entry.StartMS - frameStartMS);
			rect.Margin = new Thickness(0, FrameHeightConverter.Convert(startTime), 0, 0);
			rect.VerticalAlignment = VerticalAlignment.Top;

			LayoutRoot.Children.Add(rect);

			foreach (EventNode child in node.Children)
			{
				InitNode(child, frameStartMS, level + 1);
			}
		}

		void Init()
		{
			if (DataContext is Data.EventFrame)
			{
        Data.EventFrame frame = (Data.EventFrame)DataContext;
				LayoutRoot.Children.Clear();

				double frameStartMS = frame.Header.StartMS;

				foreach (EventNode node in frame.CategoriesTree.Children)
				{
					InitNode(node, frameStartMS, 0);
				}
			}
		}
	}
}