using LiveCharts;
using LiveCharts.Wpf;
using Profiler.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Profiler.Controls
{
	/// <summary>
	/// Interaction logic for CaptureStats.xaml
	/// </summary>
	public partial class CaptureStats : UserControl
	{
		const int HistogramStep = 5;
		const int MinHistogramValue = 15;
		const int MaxHistogramValue = 100;

		public CaptureStats()
		{
			InitializeComponent();
			DataContextChanged += OnDataContextChanged;

			FrameTimeAxis.LabelFormatter = Formatter;
		}

		private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (DataContext is String)
			{
				Load(DataContext as String);
			}
		}

		public SummaryPack Summary { get; set; }

		public void Load(String path)
		{
			if (File.Exists(path))
			{
				using (Stream stream = Capture.Open(path))
				{
					DataResponse response = DataResponse.Create(stream);
					if (response != null)
					{
						if (response.ResponseType == DataResponse.Type.SummaryPack)
						{
							Load(new SummaryPack(response));
						}
					}
				}
			}
		}

		public SeriesCollection FrameTimeSeriesCollection { get; set; }
		public Func<double, string> Formatter = l => l.ToString("N1");

		private void Load(SummaryPack summary)
		{
			Summary = summary;

			// Image
			foreach (SummaryPack.Attachment attachment in summary.Attachments)
			{
				if (attachment.FileType == SummaryPack.Attachment.Type.BRO_IMAGE)
				{
					attachment.Data.Position = 0;

					var imageSource = new BitmapImage();
					imageSource.BeginInit();
					imageSource.StreamSource = attachment.Data;
					imageSource.EndInit();

					ScreenshotThumbnail.Source = imageSource;

					break;
				}
			}

			// Frame Chart
			FrameTimeChart.Series = new SeriesCollection
			{
				new LineSeries
				{
					Values = new ChartValues<double>(summary.Frames),
					LabelPoint = p => p.Y.ToString("N1"),
					PointGeometrySize = 0,
					LineSmoothness = 0,
				},
			};

			// Histogram
			Dictionary<int, int> histogramDict = new Dictionary<int, int>();

			for (int i = 0; i < summary.Frames.Count; ++i)
			{
				double duration = summary.Frames[i];

				int bucket = Math.Min(MaxHistogramValue, Math.Max(MinHistogramValue, (int)Math.Round(duration)));
				if (!histogramDict.ContainsKey(bucket))
					histogramDict.Add(bucket, 0);

				histogramDict[bucket] += 1;
			}

			List<int> values = new List<int>();
			List<String> labels = new List<String>();

			for (int i = MinHistogramValue; i <= MaxHistogramValue; ++i)
			{
				int val = 0;
				histogramDict.TryGetValue(i, out val);

				values.Add(val);
				labels.Add(i.ToString());
			}

			FrameHistogramChart.Series = new SeriesCollection
			{
				new ColumnSeries
				{
					Values = new ChartValues<int>(values),
					ColumnPadding = 0.5
				},
			};

			FrameHistogramAxis.Labels = labels;
		}
	}
}
