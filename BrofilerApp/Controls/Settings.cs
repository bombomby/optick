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
		private SharedSettings<GlobalSettings> globalSettings = new SharedSettings<GlobalSettings>("Config.xml", SettingsType.Global);
		public SharedSettings<GlobalSettings> GlobalSettings => globalSettings;

		private SharedSettings<LocalSettings> localSettings = new SharedSettings<LocalSettings>("Brofiler.LocalConfig.xml", SettingsType.Local);
		public SharedSettings<LocalSettings> LocalSettings => localSettings;
	}

}
