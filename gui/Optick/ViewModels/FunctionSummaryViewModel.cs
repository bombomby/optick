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
using Profiler.Controls;
using Profiler.Data;
using Profiler.InfrastructureMvvm;
using Profiler.Views;

namespace Profiler.ViewModels
{
    public class FunctionViewModel : BaseViewModel
    {
		private FrameGroup Group { get; set; }
		private EventDescription Description { get; set; }

        private String _title;
        public String Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        private FunctionStats _stats;
        public FunctionStats Stats
        {
            get { return _stats; }
            set { SetProperty(ref _stats, value); }
        }

		private FunctionStats.Sample _hoverSample;
		public FunctionStats.Sample HoverSample
		{
			get { return _hoverSample; }
			set { SetProperty(ref _hoverSample, value); }
		}

		public FunctionStats.Origin Origin { get; set; }

        public virtual void OnLoaded(FunctionStats stats)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Stats = stats;
                IsLoading = false;
				OnChanged?.Invoke();
			}));
        }

        public void Load(FrameGroup group, EventDescription desc)
        {
			if (Group == group && Description == desc)
				return;

			Group = group;
			Description = desc;

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

		public void OnDataClick(FrameworkElement parent, List<int> indices)
		{
			if (Stats != null)
			{
				List<FunctionStats.Sample> samples = new List<FunctionStats.Sample>();
				indices.ForEach(i => { if (i > 0 && i < Stats.Samples.Count) samples.Add(Stats.Samples[i]); });

				Entry maxEntry = null;
				double maxDuration = 0;

				samples.ForEach(s => s.Entries.ForEach(e => { if (maxDuration < e.Duration) { maxDuration = e.Duration; maxEntry = e; } }));

				if (maxEntry != null)
				{
					EventNode maxNode = maxEntry.Frame.Root.FindNode(maxEntry);
					parent.RaiseEvent(new FocusFrameEventArgs(GlobalEvents.FocusFrameEvent, new EventFrame(maxEntry.Frame, maxNode), null));
				}
			}
		}

		public void OnDataHover(FrameworkElement parent, int index)
		{
			if (Stats != null && 0 <= index && index < Stats.Samples.Count)
			{
				HoverSample = Stats.Samples[index];
			}
			else
			{
				HoverSample = null;
			}
		}


		public delegate void OnChangedHandler();
		public event OnChangedHandler OnChanged;
	}

    public class FunctionInstanceViewModel : FunctionViewModel
    {
    }

    public class FunctionSummaryViewModel : FunctionViewModel
    {
        public FunctionSummaryViewModel()
        {
        }

        public class FunctionSummaryItem : BaseViewModel
        {
            public Style Icon { get; set; }
            public String Name { get; set; }
            public String Description { get; set; }
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
            set { SetProperty(ref _summaryItems, value); }
        }

        private ObservableCollection<FunctionSummaryItem> GenerateSummaryItems(FunctionStats frameStats)
        {
            ObservableCollection<FunctionSummaryItem> items = new ObservableCollection<FunctionSummaryItem>();

			if (frameStats != null)
			{
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
			}

            return items;
        }

        public override void OnLoaded(FunctionStats frameStats)
        {
            ObservableCollection<FunctionSummaryItem> items = GenerateSummaryItems(frameStats);

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                SummaryItems = items;
            }));

            base.OnLoaded(frameStats);
        }

        public double StrokeOpacity { get; set; } = 1.0;
        public double StrokeThickness { get; set; } = 1.0;
    }
}
