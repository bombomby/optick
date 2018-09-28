using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Profiler.Data;
using MahApps.Metro.Controls;
using System.Net;
using System.Xml;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Reflection;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Net.Cache;
using Profiler.Controls;

namespace Profiler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            this.AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(this.CloseTab));
            this.AddHandler(TimeLine.FocusFrameEvent, new TimeLine.FocusFrameEventHandler(this.OpenTab));

            timeLine.OnClearAllFrames += new ClearAllFramesHandler(ClearAllTabs);
            timeLine.ShowWarning += TimeLine_ShowWarning;
            frameTabs.SelectionChanged += new SelectionChangedEventHandler(frameTabs_SelectionChanged);
            warningBlock.Visibility = Visibility.Collapsed;

            ParseCommandLine();

            AddHandler(OpenCaptureEvent, new OpenCaptureEventHandler(MainWindow_OpenCapture));
        }

        private void MainWindow_OpenCapture(object sender, OpenCaptureEventArgs e)
        {
            timeLine.Clear();
            HamburgerMenuControl.SelectedItem = CaptureMenuItem;
            HamburgerMenuControl.Content = CaptureMenuItem;
            timeLine.LoadFile(e.Path);
        }

        public delegate void OpenCaptureEventHandler(object sender, OpenCaptureEventArgs e);
        public static readonly RoutedEvent OpenCaptureEvent = EventManager.RegisterRoutedEvent("OpenCaptureEvent", RoutingStrategy.Bubble, typeof(OpenCaptureEventHandler), typeof(MainWindow));

        private void TimeLine_ShowWarning(object sender, RoutedEventArgs e)
        {
            TimeLine.ShowWarningEventArgs args = e as TimeLine.ShowWarningEventArgs;
            ShowWarning(args.Message, args.URL.ToString());
        }

        void frameTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (frameTabs.SelectedItem is TabItem)
            {
                var dataContext = (frameTabs.SelectedItem as TabItem).DataContext;

                if (dataContext is Data.EventFrame)
                {
                    Data.EventFrame frame = dataContext as Data.EventFrame;
                    ThreadView.FocusOn(frame, null);
                }
            }
        }

        private void ClearAllTabs()
        {
            frameTabs.Items.Clear();
            ThreadView.Group = null;
        }

        private void CloseTab(object source, RoutedEventArgs args)
        {
            TabItem tabItem = args.Source as TabItem;
            if (tabItem != null)
            {
                TabControl tabControl = tabItem.Parent as TabControl;
                if (tabControl != null)
                    tabControl.Items.Remove(tabItem);
            }
        }

        private void OpenTab(object source, TimeLine.FocusFrameEventArgs args)
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
            foreach (var tab in frameTabs.Items)
            {
                if (tab is TabItem)
                {
                    TabItem item = (TabItem)tab;
                    if (item.DataContext.Equals(frame))
                    {
                        FrameInfo frameInfo = item.Content as FrameInfo;
                        frameTabs.SelectedItem = tab;
                        return;
                    }
                }
            }


            CloseableTabItem tabItem = new CloseableTabItem() { Header = "Loading...", DataContext = frame, CloseButtonEnabled = true };
            
			FrameInfo info = new FrameInfo(timeLine.Frames) { Height = Double.NaN, Width = Double.NaN, DataContext = null };
            info.DataContextChanged += new DependencyPropertyChangedEventHandler((object sender, DependencyPropertyChangedEventArgs e) => { tabItem.Header = frame.Description; });
            info.SelectedTreeNodeChanged += new SelectedTreeNodeChangedHandler(FrameInfo_OnSelectedTreeNodeChanged);
            info.SetFrame(frame, focusRange);

            tabItem.AddFrameInfo(info);

            frameTabs.Items.Add(tabItem);
            frameTabs.SelectedItem = tabItem;

			info.FocusOnNode(focusRange);

/*
			if (!string.IsNullOrEmpty(currFiltredText))
			{
				info.SummaryTable.FilterText.SetFilterText(currFiltredText);
			}
 */ 
        }

        void FrameInfo_OnSelectedTreeNodeChanged(Data.Frame frame, BaseTreeNode node)
        {
            if (node is EventNode && frame is EventFrame)
            {
                ThreadView.FocusOn(frame as EventFrame, node as EventNode);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            timeLine.Close();
            ProfilerClient.Get().Close();
            base.OnClosing(e);
        }

        private void ParseCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; ++i)
            {
                String fileName = args[i];
                if (File.Exists(fileName))
                    LoadFile(fileName);
            }
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            foreach (string file in files)
            {
                LoadFile(file);
            }
        }

        private void LoadFile(string file)
        {
            if (timeLine.LoadFile(file))
            {
                FileHistory.Add(file);
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
        }


        Version CurrentVersion { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

        String GetUniqueID()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            return nics.Length > 0 ? nics[0].GetPhysicalAddress().ToString() : new Random().Next().ToString();
        }

        void SendReportToGoogleAnalytics()
        {
            var postData = new Dictionary<string, string>
            {
                { "v", "1" },
                { "tid", "UA-58006599-1" },
                { "cid", GetUniqueID() },
                { "t", "pageview" },
                { "dh", "brofiler.com" },
                { "dp", "/app.html" },
                { "dt", CurrentVersion.ToString() }
            };

            StringBuilder text = new StringBuilder();

            foreach (var pair in postData)
            {
                if (text.Length != 0)
                    text.Append("&");

                text.Append(String.Format("{0}={1}", pair.Key, HttpUtility.UrlEncode(pair.Value)));
            }

            using (WebClient client = new WebClient())
            {
                client.UploadStringAsync(new Uri("http://www.google-analytics.com/collect"), "POST", text.ToString());
            }
        }

        WebClient checkVersion;

        void MainToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            checkVersion = new WebClient();

            checkVersion.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            checkVersion.DownloadStringCompleted += new DownloadStringCompletedEventHandler(OnVersionDownloaded);

            try
            {
                checkVersion.DownloadStringAsync(new Uri("http://brofiler.com/update"));
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

        }

        void OnVersionDownloaded(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null || String.IsNullOrEmpty(e.Result))
                return;

            try
            {
                SendReportToGoogleAnalytics();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(e.Result);

                XmlElement versionNode = doc.SelectSingleNode("//div[@id='version']") as XmlElement;

                if (versionNode != null)
                {
                    Version version = Version.Parse(versionNode.InnerText);

                    if (version != CurrentVersion)
                    {
                        XmlElement messageNode = doc.SelectSingleNode("//div[@id='message']") as XmlElement;
                        String message = messageNode != null ? messageNode.InnerText : String.Empty;

                        XmlElement urlNode = doc.SelectSingleNode("//div[@id='url']") as XmlElement;
                        String url = urlNode != null ? urlNode.InnerText : String.Empty;

                        ShowWarning(message, url);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        void ShowWarning(String message, String url)
        {
            if (!String.IsNullOrEmpty(message))
            {
                warningText.Text = message;
                warningUrl.NavigateUri = new Uri(url);
                warningBlock.Visibility = Visibility.Visible;
            }
            else
            {
                warningBlock.Visibility = Visibility.Collapsed;
            }
        }


        private void SafeCopy(Stream from, Stream to)
        {
            long pos = from.Position;
            from.Seek(0, SeekOrigin.Begin);
            from.CopyTo(to);
            from.Seek(pos, SeekOrigin.Begin);
        }

        private void OpenButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.Filter = "Brofiler files (*.prof)|*.prof";
            dlg.Title = "Load profiler results?";
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LoadFile(dlg.FileName);
            }
        }

        private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            String path = timeLine.Save();
            if (path != null)
            {
                FileHistory.Add(path);
            }
        }

        private void ClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            timeLine.Clear();
        }

        private void ClearSamplingButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ProfilerClient.Get().SendMessage(new TurnSamplingMessage(-1, false));
        }

        private void StartButton_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            ProfilerClient.Get().SendMessage(new StopMessage());
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

        private void HamburgerMenuControl_ItemClick(object sender, ItemClickEventArgs e)
        {
            HamburgerMenuControl.Content = e.ClickedItem;
        }
    }


    public static class Extensions
	{
		// extension method
		public static T GetChildOfType<T>(this DependencyObject depObj) where T : DependencyObject
		{
			if (depObj == null) return null;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
			{
				var child = VisualTreeHelper.GetChild(depObj, i);
				var result = (child as T) ?? GetChildOfType<T>(child);
				if (result != null) return result;
			}
			return null;
		}
	}


}
