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
	public class EditTaskTrackerListVM : BaseViewModel
	{
		public class TrackerDescription : BaseViewModel
		{
			public String Name { get; set; }
			public TaskTracker Tracker { get; set; }
			public String Description { get; set; }
		}

		private TrackerDescription _activeItem;
		public TrackerDescription ActiveItem
		{
			get { return _activeItem; }
			set { SetProperty(ref _activeItem, value);  }
		}

		public ObservableCollection<TrackerDescription> Trackers { get; set; } = new ObservableCollection<TrackerDescription>();

		public EditTaskTrackerListVM()
		{
			Trackers.Add(new TrackerDescription()
			{
				Name = "GitHub",
				Tracker = new GithubTaskTracker("https://github.com/{USER_NAME}/{PROJECT_NAME}"),
				Description = "https://help.github.com/en/articles/about-automation-for-issues-and-pull-requests-with-query-parameters"
			});

			Trackers.Add(new TrackerDescription()
			{
				Name = "Jira",
				Tracker = new JiraTaskTracker("http://localhost:8080/secure/CreateIssueDetails!init.jspa?issuetype=10002&pid={PROJECT_ID}"),
				Description = "https://confluence.atlassian.com/jirakb/how-to-get-project-id-from-the-jira-user-interface-827341414.html"
			});

			ActiveItem = Trackers.First();
		}
	}

	/// <summary>
	/// Interaction logic for EditTaskTrackerListDialog.xaml
	/// </summary>
	public partial class EditTaskTrackerListDialog : BaseMetroDialog
	{
		EditTaskTrackerListVM VM { get; set; } = new EditTaskTrackerListVM();

		public EditTaskTrackerListDialog()
		{
			InitializeComponent();
			DataContext = VM;
		}

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}

		public TaskTracker GetTaskTracker()
		{
			TaskTracker tracker = VM.ActiveItem.Tracker;
			tracker.Address = URL.Text;
			return tracker;
		}

		public ICommand OKPressed { set { PART_AffirmativeButton.Command = value; } }
		public ICommand CancelPressed { set { PART_NegativeButton.Command = value; } }
	}
}
