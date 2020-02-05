using Profiler.Data;
using System;
using System.Collections.Generic;

using System.Windows;

namespace Profiler.Controls
{
	public class HighlightFrameEventArgs : RoutedEventArgs
	{
		public List<ThreadViewControl.Selection> Items { get; set; }

		public HighlightFrameEventArgs(IEnumerable<ThreadViewControl.Selection> items, bool focus = true)
			: base(ThreadViewControl.HighlightFrameEvent)
		{
			Items = new List<ThreadViewControl.Selection>(items);
		}
	}

	public class FocusFrameEventArgs : RoutedEventArgs
	{
		public Data.Frame Frame { get; set; }
		public IDurable Focus { get; set; }

		public FocusFrameEventArgs(RoutedEvent routedEvent, Data.Frame frame, IDurable focus = null) : base(routedEvent)
		{
			Frame = frame;
			Focus = focus;
		}

		public delegate void Handler(object sender, FocusFrameEventArgs e);
	}

	public class GlobalEvents
	{
		public static readonly RoutedEvent FocusFrameEvent = EventManager.RegisterRoutedEvent("FocusFrame", RoutingStrategy.Bubble, typeof(FocusFrameEventArgs.Handler), typeof(GlobalEvents));
	}
}
