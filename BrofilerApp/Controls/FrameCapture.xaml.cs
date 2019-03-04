using MahApps.Metro.Controls;
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

namespace Profiler.Controls
{
    /// <summary>
    /// Interaction logic for FrameCapture.xaml
    /// </summary>
    public partial class FrameCapture : UserControl, INotifyPropertyChanged
	{
        private string _captureName;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        SummaryViewerModel _summaryVM;

        public SummaryViewerModel SummaryVM
        {
            get { return _summaryVM; }
            set
            {
                _summaryVM = value;
                OnPropertyChanged("SummaryVM");
            }
        }

        PlatformSelectorViewModel _platformSelectorVM;
        public PlatformSelectorViewModel PlatformSelectorVM
        {
            get { return _platformSelectorVM; }
            set
            {
                _platformSelectorVM = value;
                OnPropertyChanged("PlatformSelectorVM");
            }
        }


        public FrameCapture()
		{

            using (var scope = BootStrapperBase.Container.BeginLifetimeScope())
            {
                SummaryVM = scope.Resolve<SummaryViewerModel>();
                PlatformSelectorVM = scope.Resolve<PlatformSelectorViewModel>();
            }


            InitializeComponent();

			this.AddHandler(TimeLine.FocusFrameEvent, new TimeLine.FocusFrameEventHandler(this.OpenFrame));
            this.AddHandler(ThreadView.HighlightFrameEvent, new ThreadView.HighlightFrameEventHandler(this.ThreadView_HighlightEvent));

            ProfilerClient.Get().ConnectionChanged += MainWindow_ConnectionChanged;

			WarningTimer = new DispatcherTimer(TimeSpan.FromSeconds(12.0), DispatcherPriority.Background, OnWarningTimeout, Application.Current.Dispatcher);


			timeLine.NewConnection += TimeLine_NewConnection;
			timeLine.ShowWarning += TimeLine_ShowWarning;
			warningBlock.Visibility = Visibility.Collapsed;

			FunctionSummaryVM = (FunctionSummaryViewModel)FindResource("FunctionSummaryVM");
			FunctionInstanceVM = (FunctionInstanceViewModel)FindResource("FunctionInstanceVM");
		}

		FunctionSummaryViewModel FunctionSummaryVM { get; set; }
		FunctionInstanceViewModel FunctionInstanceVM { get; set; }

		public bool LoadFile(string path)
		{
            _captureName = path;
            timeLine.Clear();
			return timeLine.LoadFile(path);
		}

		private void MainWindow_ConnectionChanged(IPAddress address, int port, ProfilerClient.State state, String message)
		{
			if (state == ProfilerClient.State.Disconnected)
			{
				StartButton.IsChecked = false;
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

            if (PlatformSelectorVM !=null)
                PlatformSelectorVM.PlatformUpdate (new PlatformDescription()
                { PlatformType = args.Connection.Target, IP = args.Connection.Address,
                    Port = (short)args.Connection.Port, Name = args.Connection.Name });

		}

		private void OpenFrame(object source, TimeLine.FocusFrameEventArgs args)
		{
			Data.Frame frame = args.Frame;

            if (frame is EventFrame)
				EventThreadViewControl.Highlight(frame as EventFrame, null);

			if (frame is EventFrame)
			{
				EventFrame eventFrame = frame as EventFrame;
				FunctionSummaryVM.Load(eventFrame.Group, eventFrame.RootEntry.Description);
				FunctionInstanceVM.Load(eventFrame.Group, eventFrame.RootEntry.Description);

				FrameInfoControl.SetFrame(frame, null);
				SampleInfoControl.SetFrame(frame, null);
				SysCallInfoControl.SetFrame(frame, null);

				SamplingTreeControl.SetDescription(frame.Group, eventFrame.RootEntry.Description);
			}

			if (frame != null && frame.Group != null)
			{
                SummaryVM.Summary = frame.Group.Summary;
                SummaryVM.CaptureName = _captureName;
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

			FrameInfoControl.DataContext = null;
			SampleInfoControl.DataContext = null; 
			SysCallInfoControl.DataContext = null;
			InstanceHistoryControl.DataContext = null;

			SamplingTreeControl.SetDescription(null, null);
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
			var platform = PlatformSelectorVM.ActivePlatform;

			if (platform == null)
				return;

			Properties.Settings.Default.DefaultIP = platform.IP.ToString();
			Properties.Settings.Default.DefaultPort = platform.Port;
			Properties.Settings.Default.Save();

			ProfilerClient.Get().IpAddress = platform.IP;
			ProfilerClient.Get().Port = platform.Port;

			timeLine.StartCapture();
		}

		private void SettingsButton_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow settingsWindow = new SettingsWindow();
			settingsWindow.Show();
		}

		private void OnOpenCommandExecuted(object sender, ExecutedRoutedEventArgs args)
		{
			System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
			dlg.Filter = "Brofiler files (*.bro)|*.bro";
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
			EventThreadViewControl.FunctionSearchControl.Open();
		}
	}
}
