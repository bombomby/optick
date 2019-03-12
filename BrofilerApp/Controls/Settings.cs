using Profiler.TaskManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Controls
{
	public class GlobalSettings
	{
		const int CURRENT_VERSION = 1;

		public int Version { get; set; }

		// Task Trackers
		public class Tracker
		{
			public TrackerType Type { get; set; }
			public String Address { get; set; }
		}
		public List<Tracker> Trackers { get; set; } = new List<Tracker>();
		public Tracker ActiveTracker { get; set; }

		// Storages
		public class Storage
		{
			public String UploadURL { get; set; }
			public String DownloadURL { get; set; }
		}
		public List<Storage> Storages { get; set; } = new List<Storage>();
		public String ActiveStorage { get; set; }

		public GlobalSettings()
		{
			Version = CURRENT_VERSION;
		}
	}

	public class LocalSettings
	{
		const int CURRENT_VERSION = 1;

		public int Version { get; set; }

		public List<Platform.Connection> Connections { get; set; }

        public string TempDirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Brofiler\\Temp\\");

        public LocalSettings()
		{
			Version = CURRENT_VERSION;
		}
    }

	public class Settings
	{
		private static SharedSettings<GlobalSettings> globalSettings = new SharedSettings<GlobalSettings>("Config.xml", SettingsType.Global);
		public static SharedSettings<GlobalSettings> GlobalSettings => globalSettings;

		private static SharedSettings<LocalSettings> localSettings = new SharedSettings<LocalSettings>("Brofiler.LocalConfig.xml", SettingsType.Local);
		public static SharedSettings<LocalSettings> LocalSettings => localSettings;
	}

}
