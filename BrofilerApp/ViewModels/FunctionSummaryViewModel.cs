using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using Profiler.Data;
using Profiler.InfrastructureMvvm;

namespace Profiler.ViewModels
{
    public class FunctionViewModel : BaseViewModel
    {
        private String _title;
        public String Title
        {
            get { return _title; }
            set { SetField(ref _title, value); }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetField(ref _isLoading, value); }
        }

        private FunctionStats _stats;
        public FunctionStats Stats
        {
            get { return _stats; }
            set { SetField(ref _stats, value); }
        }

        public FunctionStats.Origin Origin { get; set; }

        public virtual void OnLoaded(FunctionStats stats)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                //Series = GenerateSeries(stats);
                Stats = stats;
                IsLoading = false;
            }));
        }

        public void Load(FrameGroup group, EventDescription desc)
        {
            Task.Run(() =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    IsLoading = true;
                }));

                FunctionStats frameStats = null;

                if (group != null && desc != null)
                {
                    frameStats = new FunctionStats(group, desc);
                    frameStats.Load(Origin);
                }

                OnLoaded(frameStats);
            });
        }
    }

    public class FunctionInstanceViewModel : FunctionViewModel
    {
        private SeriesCollection _seriesTotal = new SeriesCollection();
        public SeriesCollection SeriesTotal
        {
            get { return _seriesTotal; }
            set { SetField(ref _seriesTotal, value); }
        }

        private SeriesCollection _seriesWork = new SeriesCollection();
        public SeriesCollection SeriesWork
        {
            get { return _seriesWork; }
            set { SetField(ref _seriesWork, value); }
        }

        private SeriesCollection _seriesWait = new SeriesCollection();
        public SeriesCollection SeriesWait
        {
            get { return _seriesWait; }
            set { SetField(ref _seriesWait, value); }
        }

        const double AreaOpacity = 0.33;
        const double ChartStrokeThickness = 0;
        const double ChartColumnPadding = 0.5;

        private SeriesCollection GenerateFunctionHistogram(FunctionStats frameStats, Func<FunctionStats.Sample, double> func, Color color, double range)
        {
            double min = 0;// frameStats.Samples.Min(func);
            double max = range;

            int numBuckets = (int)max;

            List<double> histogram = new List<double>(numBuckets);
            for (int i = 0; i < numBuckets; ++i)
                histogram.Add(0.0);

            foreach (FunctionStats.Sample s in frameStats.Samples)
            {
                double val = func(s);
                int bucket = (int)((val - min) * numBuckets / max);
                histogram[Math.Min(bucket, numBuckets - 1)] += 1.0;
            }

            return new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = frameStats.Description.Name,
                    Values = new ChartValues<double>(histogram),
                    LabelPoint = p => String.Format("{0}", p.Y),
                    Fill = new SolidColorBrush { Color = color, Opacity = AreaOpacity },
                    Stroke = new SolidColorBrush { Color = color },
                    StrokeThickness = ChartStrokeThickness,
                    ColumnPadding = ChartColumnPadding,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };
        }

        public override void OnLoaded(FunctionStats frameStats)
        {
            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            //    double range = Math.Ceiling(frameStats.Samples.Max(s => s.Total));

            //    SeriesTotal = GenerateFunctionHistogram(frameStats, (s => s.Total), Colors.Gray, range);
            //    SeriesWork = GenerateFunctionHistogram(frameStats, (s => s.Work), Colors.LimeGreen, range);
            //    SeriesWait = GenerateFunctionHistogram(frameStats, (s => s.Wait), Colors.Tomato, range);
            //}));

            base.OnLoaded(frameStats);
        }
    }

    public class FunctionSummaryViewModel : FunctionViewModel
    {
        public class FunctionSummaryItem : INotifyPropertyChanged
        {
            public Style Icon { get; set; }
            public String Name { get; set; }
            public String Description { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public class MinMaxFunctionSummaryItem : FunctionSummaryItem
        {
            public Brush Foreground { get; set; }
            public double MaxValue { get; set; }
            public double MinValue { get; set; }
            public double AvgValue { get; set; }

            public MinMaxFunctionSummaryItem(IEnumerable<double> values)
            {
                int count = values.Count();

                if (count > 0)
                {
                    MinValue = values.Min();
                    MaxValue = values.Max();
                    AvgValue = values.Sum() / count;
                }
            }
        }

        public class HyperlinkFunctionSummaryItem : FunctionSummaryItem
        {
            public FileLine Path { get; set; }
        }

        private ObservableCollection<FunctionSummaryItem> _summaryItems = new ObservableCollection<FunctionSummaryItem>();
        public ObservableCollection<FunctionSummaryItem> SummaryItems
        {
            get { return _summaryItems; }
            set { SetField(ref _summaryItems, value); }
        }

        private ObservableCollection<FunctionSummaryItem> GenerateSummaryItems(FunctionStats frameStats)
        {
            ObservableCollection<FunctionSummaryItem> items = new ObservableCollection<FunctionSummaryItem>();

            items.Add(new MinMaxFunctionSummaryItem(frameStats.Samples.Select(s => s.Total))
            {
                Icon = (Style)Application.Current.FindResource("appbar_timer"),
                Name = "Time\\Frame(ms)",
                Description = "Total duration of the function per frame in milliseconds",
                Foreground = Brushes.White,

            });

            items.Add(new MinMaxFunctionSummaryItem(frameStats.Samples.Select(s => s.Work))
            {
                Icon = (Style)Application.Current.FindResource("appbar_timer_play"),
                Name = "Work\\Frame(ms)",
                Description = "Total work time of the function per frame in milliseconds (excluding synchronization and pre-emption)",
                Foreground = Brushes.LimeGreen,
            });


            items.Add(new MinMaxFunctionSummaryItem(frameStats.Samples.Select(s => s.Wait))
            {
                Icon = (Style)Application.Current.FindResource("appbar_timer_pause"),
                Name = "Wait\\Frame(ms)",
                Description = "Total wait time of the function per frame in milliseconds (synchronization and pre-emption)",
                Foreground = Brushes.Tomato,
            });

            items.Add(new MinMaxFunctionSummaryItem(frameStats.Samples.Select(s => (double)s.Count))
            {
                Icon = (Style)Application.Current.FindResource("appbar_cell_function"),
                Name = "Calls\\Frame",
                Description = "Average number of calls per frame",
                Foreground = Brushes.Wheat,
            });

            items.Add(new HyperlinkFunctionSummaryItem()
            {
                Icon = (Style)Application.Current.FindResource("appbar_page_code"),
                Name = "File",
                Description = "Open Source Code",
                Path = frameStats.Description.Path,
            });

            return items;
        }

        public override void OnLoaded(FunctionStats frameStats)
        {
            ObservableCollection<FunctionSummaryItem> items = GenerateSummaryItems(frameStats);

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                WorkValues = new ChartValues<double>(frameStats.Samples.Select(sample => sample.Work));
                WaitValues = new ChartValues<double>(frameStats.Samples.Select(sample => sample.Wait));
                SummaryItems = items;
            }));

            base.OnLoaded(frameStats);
        }


        ChartValues<double> _workValues;
        public ChartValues<double> WorkValues
        {
            get { return _workValues; }
            set { SetField(ref _workValues, value); }
        }

        ChartValues<double> _waitValues;
        public ChartValues<double> WaitValues
        {
            get { return _waitValues; }
            set { SetField(ref _waitValues, value); }
        }

        public double AreaOpacity { get; set; } = 0.66;
        public double ChartStrokeThickness { get; set; } = 0;
        public double ChartColumnPadding { get; set; } = 0.5;
    }
}
