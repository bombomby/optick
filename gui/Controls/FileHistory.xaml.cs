using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Profiler.Controls
{
	public class HistoryItem
	{
		public String Name { get { return System.IO.Path.GetFileNameWithoutExtension(Path); } }
		public String Path { get; set; }
		public DateTime Date { get; set; }
	}

	public class History
	{
		public List<HistoryItem> Items { get; set; }

		public History()
		{
			Items = new List<HistoryItem>();
		}
	}

	/// <summary>
	/// Interaction logic for FileHistory.xaml
	/// </summary>
	public partial class FileHistory : UserControl
	{
		public SharedSettings<History> History { get; set; }

		public FileHistory()
		{
			History = new SharedSettings<History>("Optick.Recent.xml", SettingsType.Local);
			History.OnChanged += History_OnChanged;
			InitializeComponent();
			History_OnChanged();
		}

		private void History_OnChanged()
		{
			Application.Current.Dispatcher.Invoke(() => { HistoryViewControl.ItemsSource = History.Data.Items; });
		}

		public void Add(string file)
		{
			History.Data.Items.RemoveAll(item => item.Path.Equals(file));
			History.Data.Items.Insert(0, new HistoryItem() { Path = file, Date = DateTime.Now });
			History.Save();
		}

		private void HistoryViewControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			HistoryItem item = HistoryViewControl.SelectedItem as HistoryItem;
			if (item != null)
			{
				RaiseEvent(new OpenCaptureEventArgs(item.Path));
			}
		}
	}
}
