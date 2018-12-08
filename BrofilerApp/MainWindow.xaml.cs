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
using System.Threading.Tasks;
using System.Windows.Threading;

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

			UpdateTitle(String.Empty);

			this.AddHandler(OpenCaptureEvent, new OpenCaptureEventHandler(MainWindow_OpenCapture));
			this.AddHandler(SaveCaptureEvent, new SaveCaptureEventHandler(MainWindow_SaveCapture));

			this.Loaded += MainWindow_Loaded;

			this.Closing += MainWindow_Closing;
		}

		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			FrameCaptureControl.Close();
		}

		public delegate void OpenCaptureEventHandler(object sender, OpenCaptureEventArgs e);
		public static readonly RoutedEvent OpenCaptureEvent = EventManager.RegisterRoutedEvent("OpenCaptureEvent", RoutingStrategy.Bubble, typeof(OpenCaptureEventHandler), typeof(MainWindow));

		public delegate void SaveCaptureEventHandler(object sender, SaveCaptureEventArgs e);
		public static readonly RoutedEvent SaveCaptureEvent = EventManager.RegisterRoutedEvent("SaveCaptureEvent", RoutingStrategy.Bubble, typeof(SaveCaptureEventHandler), typeof(MainWindow));

		const String DefaultTitle = "What the hell is going on?";

		private void UpdateTitle(String message)
		{
			Title = String.IsNullOrWhiteSpace(message) ? DefaultTitle : message;
		}

		private void MainWindow_OpenCapture(object sender, OpenCaptureEventArgs e)
		{
			//HamburgerMenuControl.SelectedItem = CaptureMenuItem;
			//HamburgerMenuControl.Content = CaptureMenuItem;

			if (FrameCaptureControl.LoadFile(e.Path))
			{
				//FileHistory.Add(e.Path);
				UpdateTitle(e.Path);
			}
		}

		private void MainWindow_SaveCapture(object sender, SaveCaptureEventArgs e)
		{
			//FileHistory.Add(e.Path);
			UpdateTitle(e.Path);
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			ParseCommandLine();
		}

		private void ParseCommandLine()
		{
			string[] args = Environment.GetCommandLineArgs();
			for (int i = 1; i < args.Length; ++i)
			{
				String fileName = args[i];
				if (File.Exists(fileName))
					RaiseEvent(new OpenCaptureEventArgs(fileName));
			}
		}

		private void Window_Drop(object sender, System.Windows.DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
			foreach (string file in files)
			{
				RaiseEvent(new OpenCaptureEventArgs(file));
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

						FrameCaptureControl.ShowWarning(message, url);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.Print(ex.Message);
			}
		}

		private void SafeCopy(Stream from, Stream to)
		{
			long pos = from.Position;
			from.Seek(0, SeekOrigin.Begin);
			from.CopyTo(to);
			from.Seek(pos, SeekOrigin.Begin);
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
