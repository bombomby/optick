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
using Profiler.InfrastructureMvvm;
using Autofac;
using System.Threading.Tasks;
using System.Windows.Threading;
using Profiler.TaskManager;
using Profiler.ViewModels;
using System.Drawing;
using System.Drawing.Imaging;
using MahApps.Metro.Controls.Dialogs;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Profiler.Views
{
	/// <summary>
	/// Interaction logic for MainView.xaml
	/// </summary>
	public partial class MainView : MetroWindow
	{
        public MainView()
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
            DeleteTempFiles();
		}

		public delegate void OpenCaptureEventHandler(object sender, OpenCaptureEventArgs e);
		public static readonly RoutedEvent OpenCaptureEvent = EventManager.RegisterRoutedEvent("OpenCaptureEvent", RoutingStrategy.Bubble, typeof(OpenCaptureEventHandler), typeof(MainView));

		public delegate void SaveCaptureEventHandler(object sender, SaveCaptureEventArgs e);
		public static readonly RoutedEvent SaveCaptureEvent = EventManager.RegisterRoutedEvent("SaveCaptureEvent", RoutingStrategy.Bubble, typeof(SaveCaptureEventHandler), typeof(MainView));

		private String DefaultTitle
		{
			get
			{
				return Settings.GlobalSettings.Data.IsCensored ? "Optick Profiler" : "What the hell is going on? - Optick Profiler";
			}
		}

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
			CheckVersionOnGithub();
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


		const String LatestVersionGithubURL = "http://api.github.com/repos/bombomby/optick/releases/latest";


		class NewVersionVM : BaseViewModel
		{
			public String Version { get; set; }
			public String Name { get; set; }
			public String Body { get; set; }
		}


		public void CheckVersionOnGithub()
		{
			Task.Run(() =>
			{
				try
				{
					using (WebClient client = new WebClient())
					{
						client.Headers.Add("user-agent", "Optick");
						String data = client.DownloadString(new Uri(LatestVersionGithubURL));

						dynamic array = JsonConvert.DeserializeObject(data);

						Application.Current.Dispatcher.BeginInvoke(new Action(() =>
						{
							NewVersionVM vm = new NewVersionVM()
							{
								Version = array["tag_name"],
								Name = array["name"],
								Body = array["body"],
							};
							VersionTooltip.DataContext = vm;
							NewVersionButtonTooltip.DataContext = vm;
							Version version = Version.Parse(vm.Version);
							if (version > CurrentVersion)
								OpenLatestRelease.Visibility = Visibility.Visible;
						}));


						SendReportToGoogleAnalytics();
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
				}
			});
		}

		String GetUniqueID()
		{
			NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
			return nics.Length > 0 ? nics[0].GetPhysicalAddress().ToString().GetHashCode().ToString() : new Random().Next().ToString();
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

		private void SafeCopy(Stream from, Stream to)
		{
			long pos = from.Position;
			from.Seek(0, SeekOrigin.Begin);
			from.CopyTo(to);
			from.Seek(pos, SeekOrigin.Begin);
		}

        private void DeleteTempFiles()
        {
            string defaultPath = Settings.LocalSettings.Data.TempDirectoryPath;
            
            DirectoryInfo dirInfo = new DirectoryInfo(defaultPath);

            if (dirInfo.Exists)
            try
            {
               foreach (FileInfo file in dirInfo.GetFiles())
                 file.Delete();

               foreach (DirectoryInfo dir in dirInfo.GetDirectories())               
                 dir.Delete(true);                   
             }
            catch (Exception)
            {
               
            }
        }

		private void ReportBugIcon_Click(object sender, RoutedEventArgs e)
		{
			TaskTrackerViewModel viewModel = new TaskTrackerViewModel(DialogCoordinator.Instance);

			Stream screenshot = ControlUtils.CaptureScreenshot(this, ImageFormat.Png);
			if (screenshot != null)
				viewModel.AttachScreenshot("OptickApp.png", screenshot);

			viewModel.SetGroup(FrameCaptureControl.EventThreadViewControl.Group);

			using (var scope = BootStrapperBase.Container.BeginLifetimeScope())
			{
				var screenShotView = scope.Resolve<IWindowManager>().ShowWindow(viewModel);
			}
		}

		private void ContactDeveloperIcon_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("mailto:support@optick.dev");
		}

		private void OpenWikiIcon_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://github.com/bombomby/optick/wiki");
		}

		private void OpenLatestRelease_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://github.com/bombomby/optick/releases");
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
