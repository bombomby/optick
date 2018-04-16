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
using System.ComponentModel;

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

		public FrameCollection Frames
		{
			get
			{
				return frames;
			}
		}

        public TimeLine()
        {
            this.InitializeComponent();
            this.DataContext = frames;

            statusToError.Add(ETWStatus.ETW_ERROR_ACCESS_DENIED, new KeyValuePair<string, string>("ETW can't start: launch your game as administrator to collect context switches", "https://github.com/bombomby/brofiler/wiki/Event-Tracing-for-Windows"));
            statusToError.Add(ETWStatus.ETW_ERROR_ALREADY_EXISTS, new KeyValuePair<string, string>("ETW session already started (Reboot should help)", "https://github.com/bombomby/brofiler/wiki/Event-Tracing-for-Windows"));
            statusToError.Add(ETWStatus.ETW_FAILED, new KeyValuePair<string, string>("ETW session failed", "https://github.com/bombomby/brofiler/wiki/Event-Tracing-for-Windows"));
        }

        public void LoadFile(string file)
        {
            if (File.Exists(file))
            {
                using (new WaitCursor())
                {
                    using (FileStream stream = new FileStream(file, FileMode.Open))
                    {
                        Open(stream);
                    }
                }
            }
        }
        
        private bool Open(Stream stream)
        {
            DataResponse response = DataResponse.Create(stream);
            while (response != null)
            {
                if (!ApplyResponse(response))
                    return false;

                response = DataResponse.Create(stream);
            }

            frames.Flush();
            ScrollToEnd();

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
            if (response.Version >= NetworkProtocol.NETWORK_PROTOCOL_MIN_VERSION)
            {
                //SaveTestResponse(response);

                switch (response.ResponseType)
                {
                    case DataResponse.Type.ReportProgress:
                        Int32 length = response.Reader.ReadInt32();
                        StatusText.Text = new String(response.Reader.ReadChars(length));
                        break;

                    case DataResponse.Type.NullFrame:
						StatusText.Visibility = System.Windows.Visibility.Collapsed;
                        lock (frames)
                        {
                            frames.Flush();
                            ScrollToEnd();
                        }
                        break;

                    case DataResponse.Type.Handshake:
                        ETWStatus status = (ETWStatus)response.Reader.ReadUInt32();

                        KeyValuePair<string, string> warning;
                        if (statusToError.TryGetValue(status, out warning))
                        {
                            RaiseEvent(new ShowWarningEventArgs(warning.Key, warning.Value));
                        }
                        break;

                    default:
                        lock (frames)
                        {
                            frames.Add(response);
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
                frameList.SelectedItem = frames[frames.Count - 1];
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

        public class ShowWarningEventArgs : RoutedEventArgs
        {
            public String Message { get; set; }
            public String URL { get; set; }

            public ShowWarningEventArgs(String message, String url)
                : base(ShowWarningEvent)
            {
                Message = message;
                URL = url;
            }
        }


        public delegate void FocusFrameEventHandler(object sender, FocusFrameEventArgs e);
        public delegate void ShowWarningEventHandler(object sender, ShowWarningEventArgs e);

        public ClearAllFramesHandler OnClearAllFrames;

        public static readonly RoutedEvent FocusFrameEvent = EventManager.RegisterRoutedEvent("FocusFrame", RoutingStrategy.Bubble, typeof(FocusFrameEventHandler), typeof(TimeLine));
        public static readonly RoutedEvent ShowWarningEvent = EventManager.RegisterRoutedEvent("ShowWarning", RoutingStrategy.Bubble, typeof(ShowWarningEventArgs), typeof(TimeLine));

        public event RoutedEventHandler FocusFrame
        {
            add { AddHandler(FocusFrameEvent, value); }
            remove { RemoveHandler(FocusFrameEvent, value); }
        }

        public event RoutedEventHandler ShowWarning
        {
            add { AddHandler(ShowWarningEvent, value); }
            remove { RemoveHandler(ShowWarningEvent, value); }
        }
        #endregion

        private void frameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (frameList.SelectedItem is Data.Frame)
            {
                FocusOnFrame((Data.Frame)frameList.SelectedItem);
            }
        }

        public void Save()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Brofiler files (*.prof)|*.prof";
            dlg.Title = "Where should I save profiler results?";

            if (dlg.ShowDialog() == true)
            {
                lock (frames)
                {
                    FileStream stream = new FileStream(dlg.FileName, FileMode.Create);

                    HashSet<EventDescriptionBoard> boards = new HashSet<EventDescriptionBoard>();
                    HashSet<FrameGroup> groups = new HashSet<FrameGroup>();

                    FrameGroup currentGroup = null;

                    foreach (Frame frame in frames)
                    {
                        if (frame is EventFrame)
                        {
                            EventFrame eventFrame = frame as EventFrame;
                            if (eventFrame.Group != currentGroup && currentGroup != null)
                            {
                                currentGroup.Responses.ForEach(response => response.Serialize(stream));
                            }
                            currentGroup = eventFrame.Group;
                        }
                        else if (frame is SamplingFrame)
                        {
                            if (currentGroup != null)
                            {
                                currentGroup.Responses.ForEach(response => response.Serialize(stream));
                                currentGroup = null;
                            }

                            (frame as SamplingFrame).Response.Serialize(stream);
                        }
                    }

                    if (currentGroup != null)
                    {
                        currentGroup.Responses.ForEach(response =>
                        {
                            response.Serialize(stream);
                        });
                    }

                    stream.Close();
                }
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

        public void Clear()
        {
            lock (frames)
            {
                frames.Clear();
            }

            OnClearAllFrames();
        }

        private void FrameFilterSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(frameList.ItemsSource);
            view.Filter = new Predicate<object>((item) => { return (item is Frame) ? (item as Frame).Duration >= FrameFilterSlider.Value : true; });
        }

        public void StartCapture()
        {
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
            return 1.85 * value;
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


	public class WaitCursor : IDisposable
	{
		private Cursor _previousCursor;

		public WaitCursor()
		{
			_previousCursor = Mouse.OverrideCursor;

			Mouse.OverrideCursor = Cursors.Wait;
		}

		public void Dispose()
		{
			Mouse.OverrideCursor = _previousCursor;
		}
	}
}