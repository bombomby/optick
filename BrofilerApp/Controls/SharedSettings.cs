using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Profiler.Controls
{
    public class SharedSettings<T> where T : new()
    {
        public T Data { get; set; }
        public String FileName { get; set; }
        public String FilePath { get; set; }
        public String DirectoryPath { get; set; }
        public FileSystemWatcher Watcher { get; set; }

        public delegate void OnChangedHandler();
        public event OnChangedHandler OnChanged;

        public SharedSettings(String name)
        {
            FileName = name;
            DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Brofiler");
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
            finally
            {
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
