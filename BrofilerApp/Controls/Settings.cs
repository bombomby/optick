using System;
using System.Collections.Generic;
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

		public enum PlatformType
		{
			Unknown,
			Windows,
			Linux,
			MacOS,
			XBox,
			Playstation,
		}

		public class Platform
		{
			public PlatformType Target { get; set; }
			public String Name { get; set; }
			public IPAddress Address { get; set; }
			public int Port { get; set; }
		}

		public int Version { get; set; }

		public List<Platform> Platforms { get; set; }

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
