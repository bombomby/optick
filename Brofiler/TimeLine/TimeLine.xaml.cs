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
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;
using System.Windows.Threading;
using Profiler.Data;
using Frame = Profiler.Data.Frame;
using Microsoft.Win32;
using System.Xml;
using System.Net.Cache;
using System.Reflection;
using System.Diagnostics;
using System.Web;
using System.Net.NetworkInformation;

namespace Profiler
{
    public delegate void ClearAllFramesHandler();

    /// <summary>
    /// Interaction logic for TimeLine.xaml
    /// </summary>
    public partial class TimeLine : UserControl
    {
        FrameCollection frames = new FrameCollection();
        Thread socketThread = null;

        Object criticalSection = new Object();

        WebClient checkVersion;

        public TimeLine()
        {
            this.InitializeComponent();
            this.DataContext = frames;

            warningBlock.Visibility = Visibility.Collapsed;

            this.Loaded += new RoutedEventHandler(TimeLine_Loaded);


            statusToError.Add(ETWStatus.ETW_ERROR_ACCESS_DENIED, new KeyValuePair<string, string>("ETW can't start: launch your game as administrator to collect context switches", "https://github.com/bombomby/brofiler/wiki/Event-Tracing-for-Windows"));
            statusToError.Add(ETWStatus.ETW_ERROR_ALREADY_EXISTS, new KeyValuePair<string, string>("ETW session already started (Reboot should help)", "https://github.com/bombomby/brofiler/wiki/Event-Tracing-for-Windows"));
            statusToError.Add(ETWStatus.ETW_FAILED, new KeyValuePair<string, string>("ETW session failed", "https://github.com/bombomby/brofiler/wiki/Event-Tracing-for-Windows"));
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

        void TimeLine_Loaded(object sender, RoutedEventArgs e)
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

        public bool Open(Stream stream)
        {
            DataResponse response = DataResponse.Create(stream);
            while (response != null)
            {
                if (!ApplyResponse(response))
                    return false;

                response = DataResponse.Create(stream);
            }

            frames.Flush();
            return true;
        }

        Dictionary<DataResponse.Type, int> testResponses = new Dictionary<DataResponse.Type, int>();

        private void SaveTestResponse(DataResponse response)
        {
            if (!testResponses.ContainsKey(response.ResponseType))
                testResponses.Add(response.ResponseType, 0);

            int count = testResponses[response.ResponseType]++;

            String data = response.SerializeToBase64();
            String path = response.ResponseType.ToString() + "_" + String.Format("{0:000}", count) + ".bin";
            File.WriteAllText(path, data);

        }

        public class ThreadDescription
        {
            public UInt32 ThreadID { get; set; }
            public String Name { get; set; }

            public override string ToString()
            {
                return String.Format("[{0}] {1}", ThreadID, Name);
            }
        }

        enum ETWStatus
        {
            ETW_OK = 0,
            ETW_ERROR_ALREADY_EXISTS = 1,
            ETW_ERROR_ACCESS_DENIED = 2,
            ETW_FAILED = 3,
        }

        Dictionary<ETWStatus, KeyValuePair<String, String>> statusToError = new Dictionary<ETWStatus, KeyValuePair<String, String>>();

        private bool ApplyResponse(DataResponse response)
        {
            if (response.Version == NetworkProtocol.NETWORK_PROTOCOL_VERSION)
            {
                //SaveTestResponse(response);

                switch (response.ResponseType)
                {
                    case DataResponse.Type.ReportProgress:
                        Int32 length = response.Reader.ReadInt32();
                        StatusText.Text = new String(response.Reader.ReadChars(length));
                        break;

                    case DataResponse.Type.NullFrame:
                        lock (frames)
                        {
                            frames.Flush();
                            if (frames.Count > 0)
                            {
                                frameList.SelectedItem = frames[frames.Count - 1];
                                ScrollToEnd();
                            }
                        }
                        break;

                    case DataResponse.Type.Handshake:
                        ETWStatus status = (ETWStatus)response.Reader.ReadUInt32();

                        KeyValuePair<string, string> warning;
                        if (statusToError.TryGetValue(status, out warning))
                        {
                            ShowWarning(warning.Key, warning.Value);
                        }
                        break;

                    default:
                        StatusText.Visibility = System.Windows.Visibility.Collapsed;
                        lock (frames)
                        {
                            frames.Add(response.ResponseType, response.Reader);
                            //ScrollToEnd();
                        }
                        break;
                }
            }
            else
            {
                MessageBox.Show("Invalid NETWORK_PROTOCOL_VERSION");
                return false;
            }
            return true;
        }

        private void ScrollToEnd()
        {
            if (frames.Count > 0)
            {
                frameList.ScrollIntoView(frames[frames.Count - 1]);
            }
        }

        public void RecieveMessage()
        {
            while (true)
            {
                DataResponse response = ProfilerClient.Get().RecieveMessage();

                if (response != null)
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => ApplyResponse(response)));
                else
                    Thread.Sleep(1000);
            }
        }

        #region FocusFrame
        private void FocusOnFrame(Data.Frame frame)
        {
            FocusFrameEventArgs args = new FocusFrameEventArgs(FocusFrameEvent, frame);
            RaiseEvent(args);
        }

        public class FocusFrameEventArgs : RoutedEventArgs
        {
            public Data.Frame Frame { get; set; }
            public Data.EventNode Node { get; set; }

            public FocusFrameEventArgs(RoutedEvent routedEvent, Data.Frame frame, Data.EventNode node = null)
              : base(routedEvent)
            {
                Frame = frame;
                Node = node;
            }
        }

        public delegate void FocusFrameEventHandler(object sender, FocusFrameEventArgs e);

        public ClearAllFramesHandler OnClearAllFrames;

        public static readonly RoutedEvent FocusFrameEvent = EventManager.RegisterRoutedEvent("FocusFrame", RoutingStrategy.Bubble, typeof(FocusFrameEventHandler), typeof(TimeLine));

        public event RoutedEventHandler FocusFrame
        {
            add { AddHandler(FocusFrameEvent, value); }
            remove { RemoveHandler(FocusFrameEvent, value); }
        }
        #endregion

        private void frameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (frameList.SelectedItem is Data.Frame)
            {
                FocusOnFrame((Data.Frame)frameList.SelectedItem);
            }
        }

        public void Close()
        {
            if (socketThread != null)
            {
                socketThread.Abort();
                socketThread = null;
            }
        }

        private void SafeCopy(Stream from, Stream to)
        {
            long pos = from.Position;
            from.Seek(0, SeekOrigin.Begin);
            from.CopyTo(to);
            from.Seek(pos, SeekOrigin.Begin);
        }

        private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "F1 Profiler files (*.prof)|*.prof";
            dlg.Title = "Where should I save profiler results?";

            if (dlg.ShowDialog() == true)
            {
                lock (frames)
                {
                    FileStream stream = new FileStream(dlg.FileName, FileMode.Create);

                    HashSet<EventDescriptionBoard> boards = new HashSet<EventDescriptionBoard>();
                    HashSet<FrameGroup> groups = new HashSet<FrameGroup>();

                    foreach (Frame frame in frames)
                    {
                        if (frame is EventFrame)
                        {
                            EventFrame eventFrame = frame as EventFrame;
                            boards.Add(eventFrame.DescriptionBoard);
                            groups.Add(eventFrame.Group);
                        }
                    }

                    foreach (EventDescriptionBoard board in boards)
                    {
                        DataResponse.Serialize(DataResponse.Type.FrameDescriptionBoard, board.BaseStream, stream);
                    }

                    foreach (Frame frame in frames)
                    {
                        DataResponse.Serialize(frame.ResponseType, frame.BaseStream, stream);
                    }

                    foreach (FrameGroup group in groups)
                    {
                        for (int threadIndex = 0; threadIndex < group.Threads.Count; ++threadIndex)
                        {
                            if (threadIndex != group.Board.MainThreadIndex)
                            {
                                foreach (Frame frame in group.Threads[threadIndex].Events)
                                {
                                    DataResponse.Serialize(frame.ResponseType, frame.BaseStream, stream);
                                }
                            }
                        }
                    }

                    stream.Close();
                }
            }
        }

        private void ClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            lock (frames)
            {
                frames.Clear();
            }

            OnClearAllFrames();
        }

        private void ClearSamplingButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ProfilerClient.Get().SendMessage(new TurnSamplingMessage(-1, false));
        }

        private void ClearHooksButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ProfilerClient.Get().SendMessage(new SetupHookMessage(0, false));
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

            StartMessage message = new StartMessage();
            if (ProfilerClient.Get().SendMessage(message))
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    StatusText.Text = "Capturing...";
                    StatusText.Visibility = System.Windows.Visibility.Visible;
                }));

                if (socketThread == null)
                {
                    socketThread = new Thread(RecieveMessage);
                    socketThread.Start();
                }
            }
        }
    }

    public class FrameHeightConverter : IValueConverter
    {
        public static double Convert(double value)
        {
            return 2.0 * value;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Convert((double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}