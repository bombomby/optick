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

namespace Profiler.Controls
{
	/// <summary>
	/// Interaction logic for FrameCapture.xaml
	/// </summary>
	public partial class FrameCapture : UserControl
	{
		public FrameCapture()
		{
			InitializeComponent();

			this.AddHandler(TimeLine.FocusFrameEvent, new TimeLine.FocusFrameEventHandler(this.OpenFrame));

			ProfilerClient.Get().ConnectionChanged += MainWindow_ConnectionChanged;

			WarningTimer = new DispatcherTimer(TimeSpan.FromSeconds(12.0), DispatcherPriority.Background, OnWarningTimeout, Application.Current.Dispatcher);

			FrameInfoControl.SelectedTreeNodeChanged += new SelectedTreeNodeChangedHandler(FrameInfo_OnSelectedTreeNodeChanged);

			timeLine.NewConnection += TimeLine_NewConnection;
			timeLine.ShowWarning += TimeLine_ShowWarning;
			warningBlock.Visibility = Visibility.Collapsed;

			// Workaround for WPF bug with MaxHeight binding
			SizeChanged += FrameCapture_SizeChanged;
			this.ThreadView.ThreadList.SizeChanged += ThreadList_SizeChanged;
		}

		const double BOTTOM_GRID_MIN_HEIGHT = 350;

		private void UpdateThreadViewLayout()
		{
			double maxHeight = Math.Max(ThreadView.MinHeight, BottomGrid.ActualHeight - BOTTOM_GRID_MIN_HEIGHT);
			if (this.ThreadView.ThreadList.ActualHeight > maxHeight)
				ThreadView.Height = maxHeight;
			else
				ThreadView.Height = double.NaN;
		}

		private void ThreadList_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateThreadViewLayout();
		}


		private void FrameCapture_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateThreadViewLayout();
		}

		public bool LoadFile(string path)
		{
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
			// TODO: Implement new connection processing
			Debug.Print("New Connection: [{0}] {1}:{2} {3}", args.Connection.Target, args.Connection.Address, args.Connection.Port, args.Connection.Name);
		}

		private void OpenFrame(object source, TimeLine.FocusFrameEventArgs args)
		{
			Durable focusRange = null;
			if (args.Node != null)
			{
				focusRange = args.Node.Entry;
			}
			else if (args.Frame is EventFrame)
			{
				focusRange = (args.Frame as EventFrame).Header;
			}

			Data.Frame frame = args.Frame;

			if (FrameInfoControl.DataContext == null || !FrameInfoControl.DataContext.Equals(frame))
			{
				FrameInfoControl.SetFrame(frame, focusRange);
				FunctionHistoryControl.LoadAsync(frame);
			}

			if (frame != null && frame.Group != null)
			{
				SummaryViewerControl.DataContext = frame.Group.Summary;
			}
		}

		void FrameInfo_OnSelectedTreeNodeChanged(Data.Frame frame, BaseTreeNode node)
		{
			if (node is EventNode && frame is EventFrame)
			{
				ThreadView.FocusOn(frame as EventFrame, node as EventNode);
			}
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
			FunctionHistoryControl.Clear();
			ThreadView.Group = null;
			FrameInfoControl.SetFrame(null, null);
			SummaryViewerControl.DataContext = null;
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
			var platform = PlatformCombo.ActivePlatform;

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
			ThreadView.FunctionSearchControl.Open();
		}
	}
}
