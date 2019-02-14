using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Profiler.Controls
{
	public enum SettingsType
	{
		Global,
		Local,
	}

	public class SharedSettings<T> where T : new()
	{
		public T Data { get; set; }
		public String FileName { get; set; }
		public String FilePath { get; set; }
		public String DirectoryPath { get; set; }
		public FileSystemWatcher Watcher { get; set; }

		public delegate void OnChangedHandler();
		public event OnChangedHandler OnChanged;


		public SharedSettings(String name, SettingsType type)
		{
			FileName = name;
			switch (type)
			{
				case SettingsType.Global:
					DirectoryPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
					break;

				case SettingsType.Local:
					DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Brofiler");
					break;
			}
			FilePath = System.IO.Path.Combine(DirectoryPath, name);
			Directory.CreateDirectory(DirectoryPath);
			Watcher = new FileSystemWatcher(DirectoryPath, name);
			Watcher.Changed += Watcher_Changed;
			Watcher.Created += Watcher_Created;
			Watcher.EnableRaisingEvents = true;
			Load();
		}

		private void Watcher_Created(object sender, FileSystemEventArgs e)
		{
			Load();
		}

		private void Watcher_Changed(object sender, FileSystemEventArgs e)
		{
			Load();
		}

		public void Reset()
		{
			Data = new T();
		}

		public bool Save()
		{
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(T));
				using (TextWriter writer = new StreamWriter(FilePath))
				{
					serializer.Serialize(writer, Data);
				}
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public bool Load()
		{
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(T));
				using (TextReader reader = new StreamReader(FilePath))
				{
					Data = (T)serializer.Deserialize(reader);
				}
			}
			catch (Exception)
			{
				Data = new T();
				return false;
			}

			OnChanged?.Invoke();
			return true;
		}
	}
}
