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
using System.IO.Compression;
using System.Threading.Tasks;
using System.Security;
using Profiler.Controls;

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

			statusToError.Add(TracerStatus.TRACER_ERROR_ACCESS_DENIED, new KeyValuePair<string, string>("ETW can't start: launch your Game/VisualStudio/UE4Editor as administrator to collect context switches", "https://github.com/bombomby/optick/wiki/Event-Tracing-for-Windows"));
			statusToError.Add(TracerStatus.TRACER_ERROR_ALREADY_EXISTS, new KeyValuePair<string, string>("ETW session already started (Reboot should help)", "https://github.com/bombomby/optick/wiki/Event-Tracing-for-Windows"));
			statusToError.Add(TracerStatus.TRACER_FAILED, new KeyValuePair<string, string>("ETW session failed (Run your Game or Visual Studio as Administrator to get ETW data)", "https://github.com/bombomby/optick/wiki/Event-Tracing-for-Windows"));
			statusToError.Add(TracerStatus.TRACER_INVALID_PASSWORD, new KeyValuePair<string, string>("Tracing session failed: invalid root password. Run the game as a root or pass a valid password through Optick GUI", "https://github.com/bombomby/optick/wiki/Event-Tracing-for-Windows"));
            statusToError.Add(TracerStatus.TRACER_NOT_IMPLEMENTED, new KeyValuePair<string, string>("Tracing sessions are not supported yet on the selected platform! Stay tuned!", "https://github.com/bombomby/optick"));

            ProfilerClient.Get().ConnectionChanged += TimeLine_ConnectionChanged;

			socketThread = new Thread(RecieveMessage);
			socketThread.Start();
		}

		private void TimeLine_ConnectionChanged(IPAddress address, UInt16 port, ProfilerClient.State state, String message)
		{
			switch (state)
			{
				case ProfilerClient.State.Connecting:
					StatusText.Text = String.Format("Connecting {0}:{1} ...", address.ToString(), port);
					StatusText.Visibility = System.Windows.Visibility.Visible;
					break;

				case ProfilerClient.State.Disconnected:
					RaiseEvent(new ShowWarningEventArgs("Connection Failed! " + message, String.Empty));
					StatusText.Visibility = System.Windows.Visibility.Collapsed;
					break;

				case ProfilerClient.State.Connected:
					break;
			}
		}

		public bool LoadFile(string file)
		{
			if (File.Exists(file))
			{
				using (new WaitCursor())
				{
					if (System.IO.Path.GetExtension(file) == ".trace")
					{
						return OpenTrace<FTraceGroup>(file);
					}
					else if (System.IO.Path.GetExtension(file) == ".json")
					{
						return OpenTrace<ChromeTracingGroup>(file);	
					}
					else
					{
						using (Stream stream = Data.Capture.Open(file))
						{
							return Open(file, stream);
						}
					}
				}
			}
			return false;
		}

		private bool Open(String name, Stream stream)
		{
			DataResponse response = DataResponse.Create(stream);
			while (response != null)
			{
				if (!ApplyResponse(response))
					return false;

				response = DataResponse.Create(stream);
			}

			frames.UpdateName(name);
			frames.Flush();
			ScrollToEnd();

			return true;
		}

		private bool OpenTrace<T>(String path) where T : ITrace, new()
		{
			if (File.Exists(path))
			{
				using (Stream stream = File.OpenRead(path))
				{
					T trace = new T();
					trace.Init(path, stream);
					frames.AddGroup(trace.MainGroup);
					frames.Add(trace.MainFrame);
					FocusOnFrame(trace.MainFrame);
				}
				return true;
			}
			return false;
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

		enum TracerStatus
		{
			TRACER_OK = 0,
            TRACER_ERROR_ALREADY_EXISTS = 1,
            TRACER_ERROR_ACCESS_DENIED = 2,
            TRACER_FAILED = 3,
            TRACER_INVALID_PASSWORD = 4,
            TRACER_NOT_IMPLEMENTED = 5,
		}

		Dictionary<TracerStatus, KeyValuePair<String, String>> statusToError = new Dictionary<TracerStatus, KeyValuePair<String, String>>();

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
						RaiseEvent(new CancelConnectionEventArgs());
						StatusText.Visibility = System.Windows.Visibility.Collapsed;
						lock (frames)
						{
							frames.Flush();
							ScrollToEnd();
						}
						break;

					case DataResponse.Type.Handshake:
						TracerStatus status = (TracerStatus)response.Reader.ReadUInt32();

						KeyValuePair<string, string> warning;
						if (statusToError.TryGetValue(status, out warning))
						{
							RaiseEvent(new ShowWarningEventArgs(warning.Key, warning.Value));
						}

						if (response.Version >= NetworkProtocol.NETWORK_PROTOCOL_VERSION_23)
						{
							Platform.Connection connection = new Platform.Connection() {
								Address = response.Source.Address.ToString(),
								Port = response.Source.Port
							};
							Platform.Type target = Platform.Type.Unknown;
							String targetName = Utils.ReadBinaryString(response.Reader);
							Enum.TryParse(targetName, true, out target);
							connection.Target = target;
							connection.Name = Utils.ReadBinaryString(response.Reader);
							RaiseEvent(new NewConnectionEventArgs(connection));
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
				RaiseEvent(new ShowWarningEventArgs("Invalid NETWORK_PROTOCOL_VERSION", String.Empty));
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
			FocusFrameEventArgs args = new FocusFrameEventArgs(GlobalEvents.FocusFrameEvent, frame);
			RaiseEvent(args);
		}

        public class ShowWarningEventArgs : RoutedEventArgs
		{
			public String Message { get; set; }
			public String URL { get; set; }

			public ShowWarningEventArgs(String message, String url) : base(ShowWarningEvent)
			{
				Message = message;
				URL = url;
			}
		}

		public class NewConnectionEventArgs : RoutedEventArgs
		{
			public Platform.Connection Connection { get; set; }

			public NewConnectionEventArgs(Platform.Connection connection) : base(NewConnectionEvent)
			{
				Connection = connection;
			}
		}

		public class CancelConnectionEventArgs : RoutedEventArgs
		{
			public CancelConnectionEventArgs() : base(CancelConnectionEvent)
			{
			}
		}

		public delegate void ShowWarningEventHandler(object sender, ShowWarningEventArgs e);
		public delegate void NewConnectionEventHandler(object sender, NewConnectionEventArgs e);
		public delegate void CancelConnectionEventHandler(object sender, CancelConnectionEventArgs e);

		public static readonly RoutedEvent ShowWarningEvent = EventManager.RegisterRoutedEvent("ShowWarning", RoutingStrategy.Bubble, typeof(ShowWarningEventArgs), typeof(TimeLine));
		public static readonly RoutedEvent NewConnectionEvent = EventManager.RegisterRoutedEvent("NewConnection", RoutingStrategy.Bubble, typeof(NewConnectionEventHandler), typeof(TimeLine));
		public static readonly RoutedEvent CancelConnectionEvent = EventManager.RegisterRoutedEvent("CancelConnection", RoutingStrategy.Bubble, typeof(CancelConnectionEventHandler), typeof(TimeLine));

		public event RoutedEventHandler FocusFrame
		{
			add { AddHandler(GlobalEvents.FocusFrameEvent, value); }
			remove { RemoveHandler(GlobalEvents.FocusFrameEvent, value); }
		}

		public event RoutedEventHandler ShowWarning
		{
			add { AddHandler(ShowWarningEvent, value); }
			remove { RemoveHandler(ShowWarningEvent, value); }
		}

		public event RoutedEventHandler NewConnection
		{
			add { AddHandler(NewConnectionEvent, value); }
			remove { RemoveHandler(NewConnectionEvent, value); }
		}

		public event RoutedEventHandler CancelConnection
		{
			add { AddHandler(CancelConnectionEvent, value); }
			remove { RemoveHandler(CancelConnectionEvent, value); }
		}
		#endregion

		private void frameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (frameList.SelectedItem is Data.Frame)
			{
				FocusOnFrame((Data.Frame)frameList.SelectedItem);
			}
		}

		public void ForEachResponse(Action<FrameGroup, DataResponse> action)
		{
			FrameGroup currentGroup = null;
			foreach (Frame frame in frames)
			{
				if (frame is EventFrame)
				{
					EventFrame eventFrame = frame as EventFrame;
					if (eventFrame.Group != currentGroup && currentGroup != null)
					{
						currentGroup.Responses.ForEach(response => action(currentGroup, response));
					}
					currentGroup = eventFrame.Group;
				}
				else if (frame is SamplingFrame)
				{
					if (currentGroup != null)
					{
						currentGroup.Responses.ForEach(response => action(currentGroup, response));
						currentGroup = null;
					}

					action(null, (frame as SamplingFrame).Response);
				}
			}

			currentGroup?.Responses.ForEach(response => action(currentGroup, response));
		}

		public void Save(Stream stream)
		{
			ForEachResponse((group, response) => response.Serialize(stream));
		}

		public String Save()
		{
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.Filter = "Optick Performance Capture (*.opt)|*.opt";
			dlg.Title = "Where should I save profiler results?";

			if (dlg.ShowDialog() == true)
			{
				lock (frames)
				{
					using (Stream stream = Capture.Create(dlg.FileName))
						Save(stream);

					frames.UpdateName(dlg.FileName, true);
				}
				return dlg.FileName;
			}

			return null;
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
		}

		//private void FrameFilterSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		//{
		//	ICollectionView view = CollectionViewSource.GetDefaultView(frameList.ItemsSource);
		//	view.Filter = new Predicate<object>((item) => { return (item is Frame) ? (item as Frame).Duration >= FrameFilterSlider.Value : true; });
		//}

		public void StartCapture(IPAddress address, UInt16 port, CaptureSettings settings, SecureString password)
		{
            ProfilerClient.Get().IpAddress = address;
            ProfilerClient.Get().Port = port;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				StatusText.Text = "Connecting...";
				StatusText.Visibility = System.Windows.Visibility.Visible;
			}));

			Task.Run(() => { ProfilerClient.Get().SendMessage(new StartMessage() { Settings = settings, Password = password } , true); });
		}
	}

	public class FrameHeightConverter : IValueConverter
	{
		public static double Convert(double value)
		{
			return 2.775 * value;
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
