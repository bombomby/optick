using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Profiler.Views;

namespace Profiler.Controls
{
	public class OpenCaptureEventArgs : RoutedEventArgs
	{
		public String Path { get; set; }
		public Data.SummaryPack Summary { get; set; }

		public OpenCaptureEventArgs(String path, Data.SummaryPack summary = null)
			: base(MainView.OpenCaptureEvent)
		{
			Path = path;
			Summary = summary;
		}
	}

	public class SaveCaptureEventArgs : RoutedEventArgs
	{
		public String Path { get; set; }
		public Data.SummaryPack Summary { get; set; }

		public SaveCaptureEventArgs(String path)
			: base(MainView.SaveCaptureEvent)
		{
			Path = path;
		}
	}
}
