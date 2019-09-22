using Profiler.TaskManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel.DataAnnotations;

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

		public enum ExpandMode
		{
			[Display(Name = "Expand MainThread", Description = "Expand MainThread Only")]
			ExpandMain,
			[Display(Name = "Expand All", Description = "Expand All Threads")]
			ExpandAll,
			[Display(Name = "Collapse All", Description = "Collapse All Threads")]
			CollapseAll,
		}

		public List<Platform.Connection> Connections { get; set; } = new List<Platform.Connection>();
        public Platform.Connection LastConnection { get; set; }

		public int CollapsedMaxThreadDepth { get; set; } = 2;
		public int ExpandedMaxThreadDepth { get; set; } = 12;
		public ExpandMode ThreadExpandMode { get; set; } = ExpandMode.ExpandAll;

        public string TempDirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Optick\\Temp\\");

        public LocalSettings()
		{
			Version = CURRENT_VERSION;
		}
    }

	public class Settings
	{
		private static SharedSettings<GlobalSettings> globalSettings = new SharedSettings<GlobalSettings>("Config.xml", SettingsType.Global);
		public static SharedSettings<GlobalSettings> GlobalSettings => globalSettings;

		private static SharedSettings<LocalSettings> localSettings = new SharedSettings<LocalSettings>("Optick.LocalConfig.xml", SettingsType.Local);
		public static SharedSettings<LocalSettings> LocalSettings => localSettings;
	}

}
