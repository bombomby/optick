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
			double duration = FrameHeightConverter.Convert(node.Entry.Duration);

			if (duration < 2.0 && level != 0)
				return;

			Rectangle rect = new Rectangle();
			rect.Width = double.NaN;
			rect.Height = duration;
			rect.Fill = new SolidColorBrush(node.Entry.Description.ForceColor);

			double startTime = (node.Entry.StartMS - frameStartMS);
			rect.Margin = new Thickness(0, 0, 0, FrameHeightConverter.Convert(startTime));
			rect.VerticalAlignment = VerticalAlignment.Bottom;

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

				foreach (EventNode node in frame.Root.Children)
				{
					InitNode(node, frameStartMS, 0);
				}
			}
		}
	}
}