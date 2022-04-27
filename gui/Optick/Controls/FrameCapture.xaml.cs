﻿using MahApps.Metro.Controls;
using Profiler.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;            //CallerMemberName
using Profiler.ViewModels;
using Profiler.InfrastructureMvvm;
using Autofac;
using Profiler.Controls.ViewModels;
using Profiler.ViewModels.Plots;
using Profiler.Views;
using Xceed.Wpf.AvalonDock.Layout;
using Frame = Profiler.Data.Frame;

namespace Profiler.Controls
{
    /// <summary>
    /// Interaction logic for FrameCapture.xaml
    /// </summary>
    public partial class FrameCapture : UserControl, INotifyPropertyChanged
	{
		private readonly List<PlotsViewModel> _plotPanels  = new List<PlotsViewModel>();
		private string _captureName;
		
		public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        SummaryViewerModel _summaryVM;
		CaptureSettingsViewModel CaptureSettingsVM { get; set; }

		public SummaryViewerModel SummaryVM
        {
            get { return _summaryVM; }
            set
            {
                _summaryVM = value;
                OnPropertyChanged("SummaryVM");
            }
        }

        AddressBarViewModel AddressBarVM { get; set; }
		public double DefaultWarningTimerInSeconds { get; set; } = 15.0;

		public FrameCapture()
		{
			using (var scope = BootStrapperBase.Container.BeginLifetimeScope())
            {
                SummaryVM = scope.Resolve<SummaryViewerModel>();
            }


            InitializeComponent();

			this.AddHandler(GlobalEvents.FocusFrameEvent, new FocusFrameEventArgs.Handler(this.OpenFrame));
            this.AddHandler(ThreadViewControl.HighlightFrameEvent, new ThreadViewControl.HighlightFrameEventHandler(this.ThreadView_HighlightEvent));

            ProfilerClient.Get().ConnectionChanged += MainWindow_ConnectionChanged;

			WarningTimer = new DispatcherTimer(TimeSpan.FromSeconds(DefaultWarningTimerInSeconds), DispatcherPriority.Background, OnWarningTimeout, Application.Current.Dispatcher);


			timeLine.NewConnection += TimeLine_NewConnection;
			timeLine.CancelConnection += TimeLine_CancelConnection;
			timeLine.ShowWarning += TimeLine_ShowWarning;
			timeLine.DataLoaded += TimeLine_DataLoaded;
			warningBlock.Visibility = Visibility.Collapsed;

            AddressBarVM = (AddressBarViewModel)FindResource("AddressBarVM");
            FunctionSummaryVM = (FunctionSummaryViewModel)FindResource("FunctionSummaryVM");
			FunctionInstanceVM = (FunctionInstanceViewModel)FindResource("FunctionInstanceVM");
			CaptureSettingsVM = (CaptureSettingsViewModel)FindResource("CaptureSettingsVM");

			var plotPanelsSettingsViewModel = (PlotPanelsSettingsViewModel)FindResource("CountersSettingsVM");
			plotPanelsSettingsViewModel.CustomPanels = _plotPanels;
			if (string.IsNullOrEmpty(plotPanelsSettingsViewModel.CurrentPath))
				plotPanelsSettingsViewModel.CurrentPath = "counters.json";

			var plotPanelsSettings = PlotPanelsSettingsStorage.Load(plotPanelsSettingsViewModel.CurrentPath);
			CreatePlotPanels(plotPanelsSettings);
			
			FunctionSamplingVM = (SamplingViewModel)FindResource("FunctionSamplingVM");
			SysCallsSamplingVM = (SamplingViewModel)FindResource("SysCallsSamplingVM");

			EventThreadViewControl.Settings = Settings.LocalSettings.Data.ThreadSettings;
			Settings.LocalSettings.OnChanged += LocalSettings_OnChanged;
		}

		private void LocalSettings_OnChanged()
		{
			EventThreadViewControl.Settings = Settings.LocalSettings.Data.ThreadSettings;
		}

		private void CancelConnection()
		{
			StartButton.IsChecked = false;
		}

		private void TimeLine_CancelConnection(object sender, RoutedEventArgs e)
		{
			CancelConnection();
		}

		FunctionSummaryViewModel FunctionSummaryVM { get; set; }
		FunctionInstanceViewModel FunctionInstanceVM { get; set; }

		SamplingViewModel FunctionSamplingVM { get; set; }
		SamplingViewModel SysCallsSamplingVM { get; set; }

		public bool LoadFile(string path)
		{
            timeLine.Clear();
			if (timeLine.LoadFile(path))
			{
				_captureName = path;
				return true;
			}
			return false;
		}

		private void MainWindow_ConnectionChanged(IPAddress address, UInt16 port, ProfilerClient.State state, String message)
		{
			if (state == ProfilerClient.State.Disconnected)
			{
				CancelConnection();
			}
		}

		private void TimeLine_ShowWarning(object sender, RoutedEventArgs e)
		{
			TimeLine.ShowWarningEventArgs args = e as TimeLine.ShowWarningEventArgs;
			ShowWarning(args.Message, args.URL.ToString());
		}

		private void TimeLine_NewConnection(object sender, RoutedEventArgs e)
		{
			TimeLine.NewConnectionEventArgs args = e as TimeLine.NewConnectionEventArgs;
            AddressBarVM?.Update(args.Connection);
        }

		private void TimeLine_DataLoaded(object sender, RoutedEventArgs e)
		{
			foreach (var plotPanel in _plotPanels)
				plotPanel.SetModel(timeLine.Counters);
		}
		
		private void OpenFrame(object source, FocusFrameEventArgs args)
		{
			Data.Frame frame = args.Frame;
			
			if (frame is EventFrame eventFrame)
			{
				var group = eventFrame.Group;
				
				EventThreadViewControl.Highlight(frame as EventFrame, null);

				if (args.FocusPlot)
				{
					var interval = (IDurable)frame;

					var startRelative = interval.Start - frame.Group.Board.TimeSlice.Start;
					var startRelativeMs = group.Board.TimeSettings.TicksToMs * startRelative;
					
					foreach (var plotPanel in _plotPanels)
					{
						plotPanel.Zoom(startRelativeMs, interval.Duration);
					}
				}

				if (eventFrame.RootEntry != null)
				{
					EventDescription desc = eventFrame.RootEntry.Description;

					FunctionSummaryVM.Load(group, desc);
					FunctionInstanceVM.Load(group, desc);

					FunctionSamplingVM.Load(group, desc);
					SysCallsSamplingVM.Load(group, desc);

					FrameInfoControl.SetFrame(frame, null);
				}
			}

			if (frame != null && frame.Group != null)
			{
                if (!ReferenceEquals(SummaryVM.Summary, frame.Group.Summary))
                {
                    SummaryVM.Summary = frame.Group.Summary;
                    SummaryVM.CaptureName = _captureName;
                }
            }
		}

        private void ThreadView_HighlightEvent(object sender, HighlightFrameEventArgs e)
        {
			EventThreadViewControl.Highlight(e.Items);
        }

		public void Close()
		{
			timeLine.Close();
			ProfilerClient.Get().Close();
		}

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}

		DispatcherTimer WarningTimer { get; set; }

		void OnWarningTimeout(object sender, EventArgs e)
		{
			warningBlock.Visibility = Visibility.Collapsed;
		}

		public void ShowWarning(String message, String url)
		{
			if (!String.IsNullOrEmpty(message))
			{
				warningText.Text = message;
				warningUrl.NavigateUri = !String.IsNullOrWhiteSpace(url) ? new Uri(url) : null;
				warningBlock.Visibility = Visibility.Visible;
			}
			else
			{
				warningBlock.Visibility = Visibility.Collapsed;
			}
		}

		private void ClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			timeLine.Clear();
			EventThreadViewControl.Group = null;
			SummaryVM.Summary = null;
            SummaryVM.CaptureName = null;

			FunctionSummaryVM.Load(null, null);
			FunctionInstanceVM.Load(null, null);

			FunctionSamplingVM.Load(null, null);
			SysCallsSamplingVM.Load(null, null);

			FrameInfoControl.DataContext = null;
			SampleInfoControl.DataContext = null; 
			SysCallInfoControl.DataContext = null;

			//SamplingTreeControl.SetDescription(null, null);
			
			foreach (var plotPanel in _plotPanels)
				plotPanel.Clear();
			
			timeLine.Counters.Clear();

			MainViewModel vm = DataContext as MainViewModel;
			if (vm.IsCapturing)
			{
				ProfilerClient.Get().SendMessage(new CancelMessage());
			}
		}

        private void ClearSamplingButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ProfilerClient.Get().SendMessage(new TurnSamplingMessage(-1, false));
		}

		private void StartButton_Unchecked(object sender, System.Windows.RoutedEventArgs e)
		{
			Task.Run(() => ProfilerClient.Get().SendMessage(new StopMessage()));
		}

		private void StartButton_Checked(object sender, System.Windows.RoutedEventArgs e)
		{
            var platform = AddressBarVM.Selection;

			if (platform == null)
				return;

            IPAddress address = null;
            if (!IPAddress.TryParse(platform.Address, out address))
                return;

			CaptureSettings settings = CaptureSettingsVM.GetSettings();
			timeLine.StartCapture(address, platform.Port, settings, platform.Password);
		}

		private void OnOpenCommandExecuted(object sender, ExecutedRoutedEventArgs args)
		{
			System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
			dlg.Filter = "Optick Capture (*.opt)|*.opt|Chrome Trace (*.json)|*.json|FTrace Capture (*.ftrace)|*.ftrace";
			dlg.Title = "Load profiler results?";
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				RaiseEvent(new OpenCaptureEventArgs(dlg.FileName));
			}
		}

		private void OnSaveCommandExecuted(object sender, ExecutedRoutedEventArgs args)
		{
			String path = timeLine.Save();
			if (path != null)
			{
				RaiseEvent(new SaveCaptureEventArgs(path));
			}
		}

		private void OnSearchCommandExecuted(object sender, ExecutedRoutedEventArgs args)
		{
			EventThreadViewControl.OpenFunctionSearch();
		}

		private void OnShowDebugInfoCommandExecuted(object sender, ExecutedRoutedEventArgs args)
		{
			MemoryStatsViewModel vm = new MemoryStatsViewModel();
			timeLine.ForEachResponse((group, response) => vm.Load(response));
			vm.Update();
			DebugInfoPopup.DataContext = vm;
			DebugInfoPopup.IsOpen = true;
		}

		/// <summary>
		/// Gets called when a different counters layout is loaded
		/// </summary>
		private void OnPlotPanelsSettingsLoaded(List<PlotPanelSerialized> plotPanelsSerialized)
		{
			CreatePlotPanels(plotPanelsSerialized);
		}

		private void CreatePlotPanels(List<PlotPanelSerialized> plotPanelsSerialized)
		{
			PlotsPane.Children.Clear();
			_plotPanels.Clear();
			foreach (var plotPanelSerialized in plotPanelsSerialized)
				AddPlotPanel(plotPanelSerialized.Title, plotPanelSerialized.Counters);
		}
		
		/// <summary>
		/// Gets called when "Create panel" button is being pressed 
		/// </summary>
		private void OnCreatePanel(string title)
		{
			AddPlotPanel(title, null);
			PlotsPane.SelectedContentIndex = PlotsPane.Children.Count - 1;
		}
		
		/// <summary>
		/// Create a new panel.
		/// </summary>
		/// <param name="title">Panel's title</param>
		/// <param name="selectedCounters">Optional, pre selected counters</param>
		private void AddPlotPanel(string title, List<PlotPanelSerialized.CounterSerialized> selectedCounters)
		{
			var viewModel = new PlotsViewModel(title, timeLine.Counters);
			var view = new PlotView
			{
				DataContext = viewModel
			};
			view.PlotClicked += (s, e) =>
			{
				var frame = GetFrame(timeLine.Frames, e.Time);
				if (frame != null)
					RaiseEvent(new FocusFrameEventArgs(GlobalEvents.FocusFrameEvent, frame, focusPlot: false));
			};
			if (selectedCounters != null)
			{
				foreach (var selectedCounter in selectedCounters)
				{
					var color = Color.FromArgb(selectedCounter.Color.A, selectedCounter.Color.R, selectedCounter.Color.G, selectedCounter.Color.B);
					viewModel.SelectCounter(selectedCounter.Key, selectedCounter.Name, new SolidColorBrush(color), selectedCounter.DataUnits, selectedCounter.ViewUnits);
				}
			}
			
			var anchorable = new LayoutAnchorable
			{
				CanClose = true,
				Content = view,
				CanHide = false
			};
			
			var titleBinding = new Binding(nameof(viewModel.Title))
			{
				Mode = BindingMode.OneWay,
				Source = viewModel
			};
			BindingOperations.SetBinding(anchorable, LayoutContent.TitleProperty, titleBinding);
			
			// Restoring layout because when an user closes a tab this elements get removed from the layout
			if (PlotsPaneGroup.Parent == null)
				mainPanel.Children.Add(PlotsPaneGroup);
			if (PlotsPane.Parent == null)
				PlotsPaneGroup.Children.Add(PlotsPane);
			
			PlotsPane.Children.Add(anchorable);
			
			anchorable.Closed += (s,e) =>
			{
				_plotPanels.Remove(viewModel);
			};
			
			_plotPanels.Add(viewModel);
		}
		
		private static Frame GetFrame(FrameCollection frameCollection, double time)
		{
			// binary search
			var left = 0;
			var right = frameCollection.Count - 1;
			while (left <= right)
			{
				var middle = (left + right) / 2;
				var frame = frameCollection[middle];

				var interval = (IDurable)frame;
				var startRelative = interval.Start - frame.Group.Board.TimeSlice.Start;
				var startRelativeMs = frame.Group.Board.TimeSettings.TicksToMs * startRelative;

				if (startRelativeMs <= time && time <= startRelativeMs + interval.Duration)
					return frame;
				
				if (startRelativeMs < time)
					left = middle + 1;
				else
					right = middle - 1;
			}
			
			return null;
		}
	}
}
