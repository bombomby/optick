using MahApps.Metro.Controls.Dialogs;
using Profiler.InfrastructureMvvm;
using Profiler.TaskManager;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
	/// <summary>
	/// Interaction logic for EditTaskTrackerListDialog.xaml
	/// </summary>
	public partial class EditStorageListDialog : BaseMetroDialog
	{
		public EditStorageListDialog()
		{
			InitializeComponent();
		}

		public ICommand OKPressed { set { PART_AffirmativeButton.Command = value; } }
		public ICommand CancelPressed { set { PART_NegativeButton.Command = value; } }

		public ExternalStorage GetStorage()
		{
			return new NetworkStorage(UploadURL.Text, DownloadURL.Text);
		}
	}
}
