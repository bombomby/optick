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

    HashSet<DataResponse.Type> testResponses = new HashSet<DataResponse.Type>();

    private void SaveTestResponse(DataResponse response)
    {
      if (!testResponses.Contains(response.ResponseType))
      {
        testResponses.Add(response.ResponseType);
        String data = response.SerializeToBase64();
        String path = response.ResponseType.ToString() + ".bin";
        File.WriteAllText(path, data);
      }
    }

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

          default:
            StatusText.Visibility = System.Windows.Visibility.Collapsed;
            lock (frames)
            {
              frames.Add(response.ResponseType, response.Reader);
							ScrollToEnd();
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
          Application.Current.Dispatcher.Invoke(new Action(() => ApplyResponse(response)));
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
      private Data.Frame frame;
      public Data.Frame Frame
      {
        get { return frame; }
        set { frame = value; }
      }

      public FocusFrameEventArgs(RoutedEvent routedEvent, Data.Frame frame)
        : base(routedEvent)
      {
        this.frame = frame;
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

    private void serverIP_TextChanged(object sender, TextChangedEventArgs e)
    {
      IPAddress ip = IPAddress.Parse(serverIP.Text);
      if (ip != null)
        ProfilerClient.Get().IpAddress = ip;
    }

    private void serverPort_TextChanged(object sender, TextChangedEventArgs e)
    {
      int value;
      if (int.TryParse(serverPort.Text, out value))
        ProfilerClient.Get().Port = value;
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

          foreach (Frame frame in frames)
            if (frame is EventFrame)
              boards.Add((frame as EventFrame).DescriptionBoard);

          foreach (EventDescriptionBoard board in boards)
          {
            DataResponse.Serialize(DataResponse.Type.FrameDescriptionBoard, board.BaseStream, stream);
          }

          foreach (Frame frame in frames)
          {
            DataResponse.Serialize(frame.ResponseType, frame.BaseStream, stream);
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

    private void frameNumber_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        TraversalRequest down = new TraversalRequest(FocusNavigationDirection.Down);
        frameNumber.MoveFocus(down);
      }
    }

    private void StartButton_Unchecked(object sender, System.Windows.RoutedEventArgs e)
    {
			ProfilerClient.Get().SendMessage(new StopMessage());
    }

    private void StartButton_Checked(object sender, System.Windows.RoutedEventArgs e)
    {

			StartMessage message = new StartMessage();
			if (ProfilerClient.Get().SendMessage(message))
			{
				Application.Current.Dispatcher.Invoke(new Action(() => 
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
}