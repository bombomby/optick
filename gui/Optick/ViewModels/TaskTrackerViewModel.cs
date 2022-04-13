using Profiler.Data;
using Profiler.InfrastructureMvvm;
using Profiler.TaskManager;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls.Dialogs;
using Profiler.Controls;

namespace Profiler.ViewModels
{
	class TaskTrackerViewModel : BaseViewModel
	{
		private FrameGroup _group = null;
		public FrameGroup Group
		{
			get { return _group; }
			set { SetProperty(ref _group, value); }
		}

		private ExternalStorage _storage = null;
		public ExternalStorage Storage
		{
			get { return _storage; }
			set { SetProperty(ref _storage, value); ResetUploadedFiles(); }
		}

		private TaskTracker _tracker;
		public TaskTracker Tracker
		{
			get { return _tracker; }
			set { SetProperty(ref _tracker, value); }
		}

		public class AttachmentVM : BaseViewModel
		{
			public FileAttachment Attachment { get; set; }
			public bool IsChecked { get; set; } = true;
			public bool IsExpanded { get; set; }

			private double _progress = 0.0;
			public double Progress { get { return _progress; } set { SetProperty(ref _progress, value); OnPropertyChanged("ProgressUploaded"); } }
			public long ProgressUploaded { get { return (long)(Attachment.Data.Length * Progress); } }

			private bool _isUploading = false;
			public bool IsUploading { get { return _isUploading; } set { SetProperty(ref _isUploading, value); } }

			private String _status = String.Empty;
			public String Status { get { return _status; } set { SetProperty(ref _status, value); } }

			private Uri _url = null;
			public Uri URL { get { return _url; } set { SetProperty(ref _url, value); } }

			private long _size = 0;
			public long Size { get { return _size; } set { SetProperty(ref _size, value); } }

			public AttachmentVM(FileAttachment attachment)
			{
				Attachment = attachment;

				if (attachment != null && attachment.Data != null)
					Size = attachment.Data.Length;
			}
		}

		public ObservableCollection<AttachmentVM> Attachments { get; set; } = new ObservableCollection<AttachmentVM>();

		public ObservableCollection<TaskTracker> Trackers { get; set; } = new ObservableCollection<TaskTracker>();
		public ObservableCollection<ExternalStorage> Storages { get; set; } = new ObservableCollection<ExternalStorage>();

		private String _bodyTemplate = String.Empty;
		public String BodyTemplate
		{
			get { return _bodyTemplate; }
			set { SetProperty(ref _bodyTemplate, value); }
		}

		private String _titleTemplate = String.Empty;
		public String TitleTemplate
		{
			get { return _titleTemplate; }
			set { SetProperty(ref _titleTemplate, value); }
		}

		private String _uploadStatus = String.Empty;
		public String UploadStatus
		{
			get { return _uploadStatus; }
			set { SetProperty(ref _uploadStatus, value); }
		}

		private double _uploadProgress = 0.0;
		public double UploadProgress
		{
			get { return _uploadProgress; }
			set { SetProperty(ref _uploadProgress, value); }
		}

		private bool _isUploading = false;
		public bool IsUploading
		{
			get { return _isUploading; }
			set { SetProperty(ref _isUploading, value); OnPropertyChanged("IsNotUploading"); }
		}
		public bool IsNotUploading { get { return !IsUploading; } }

		private CancellationTokenSource TokenSource = null;

		private void SetActiveSettings()
		{
			Settings.GlobalSettings.Data.ActiveTracker = new GlobalSettings.Tracker()
			{
				Address = Tracker.Address,
				Type = Tracker.TrackerType
			};
			Settings.GlobalSettings.Data.ActiveStorage = Storage.DisplayName;
			Settings.GlobalSettings.Save();
		}

		public ICommand CreateIssueCommand
		{
			get
			{
				return new RelayCommand(obj =>
				{
					SetActiveSettings();

					foreach (var att in Attachments)
						if (att.IsUploading)
							return;

					TokenSource?.Dispose();
					TokenSource = new CancellationTokenSource();
					CancellationToken token = TokenSource.Token;

					Task.Run(() =>
					{
						try
						{
							Application.Current.Dispatcher.Invoke(() => { UploadProgress = 0.0; IsUploading = true; });

							Issue issue = new Issue()
							{
								Title = TitleTemplate,
								Body = BodyTemplate,
							};

							long totalSize = 0;

							foreach (var att in Attachments)
								if (att.IsChecked)
									totalSize += att.Attachment.Data.Length;

							double totalProgress = 0.0;

							foreach (var att in Attachments)
							{
								if (token.IsCancellationRequested)
									break;

								if (att.IsChecked)
								{
									Application.Current.Dispatcher.Invoke(() => {
										att.IsUploading = true;
										UploadStatus = "Uploading " + att.Attachment.Name;
									});

									if (att.URL == null)
									{
										try
										{
											att.URL = Storage.UploadFile(att.Attachment.Name, att.Attachment.Data, (p) =>
											{
												Application.Current.Dispatcher.Invoke(() => {
													att.Progress = p;
													UploadProgress = totalProgress + p * att.Attachment.Data.Length / totalSize;
												});
											}, token);
										}
										catch (Exception /*ex*/)
										{
											att.Status = "Failed to upload!";
										}
									}

									Application.Current.Dispatcher.Invoke(() => att.IsUploading = false);

									totalProgress += (double)att.Attachment.Data.Length / totalSize;

									if (att.URL != null)
									{
										issue.Attachments.Add(new Attachment()
										{
											Name = att.Attachment.Name,
											URL = att.URL,
											Type = att.Attachment.FileType
										});
									}
								}
							}

							if (!token.IsCancellationRequested)
								Tracker.CreateIssue(issue);
						}
						catch (AggregateException ex)
						{
							foreach (Exception e in ex.InnerExceptions)
								Console.WriteLine(e.Message);
						}
						finally
						{
							Application.Current.Dispatcher.Invoke(() => IsUploading = false);
						}
					}, token);
				},
				enable => Storage != null && Tracker != null
				);
			}
		}

		public ICommand CancelIssueCommand
		{
			get
			{
				return new RelayCommand(obj =>
				{
					if (TokenSource != null)
					{
						TokenSource.Cancel(true);
					}
				});
			}
		}

		void SaveTrackers()
		{
			List<GlobalSettings.Tracker> trackers = new List<GlobalSettings.Tracker>();
			foreach (TaskTracker tracker in Trackers)
				trackers.Add(new GlobalSettings.Tracker() { Address = tracker.Address, Type = tracker.TrackerType });

			Settings.GlobalSettings.Data.Trackers = trackers;
			Settings.GlobalSettings.Save();
		}

		public ICommand AddNewTaskTrackerCommand
		{
			get
			{
				return new RelayCommand(obj =>
				{
					var editDialog = new EditTaskTrackerListDialog();

					editDialog.CancelPressed = new RelayCommand((o) =>
					{
						dialogCoordinator.HideMetroDialogAsync(this, editDialog);
					});

					editDialog.OKPressed = new RelayCommand((o) =>
					{
						TaskTracker tracker = editDialog.GetTaskTracker();
						Trackers.Add(tracker);
						Tracker = tracker;
						SaveTrackers();
						dialogCoordinator.HideMetroDialogAsync(this, editDialog);
					});

					dialogCoordinator.ShowMetroDialogAsync(this, editDialog);

				});
			}
		}

		public ICommand RemoveTaskTrackerCommand
		{
			get
			{
				return new RelayCommand(obj =>
				{
					Trackers.Remove(Tracker);
					Tracker = Trackers.Count > 0 ? Trackers.First() : null;
					SaveTrackers();
				});
			}
		}

		void SaveStorages()
		{
			List<GlobalSettings.Storage> storages = new List<GlobalSettings.Storage>();
			foreach (ExternalStorage storage in Storages)
			{
				NetworkStorage networkStorage = storage as NetworkStorage;
				if (networkStorage != null)
				{
					storages.Add(new GlobalSettings.Storage() { UploadURL = networkStorage.UploadURL, DownloadURL = networkStorage.DownloadURL });
				}
			}
			Settings.GlobalSettings.Data.Storages = storages;
			Settings.GlobalSettings.Save();
		}

		public ICommand AddNewStorageCommand
		{
			get
			{
				return new RelayCommand(obj =>
				{
					var editDialog = new EditStorageListDialog();

					editDialog.CancelPressed = new RelayCommand((o) =>
					{
						dialogCoordinator.HideMetroDialogAsync(this, editDialog);
					});

					editDialog.OKPressed = new RelayCommand((o) =>
					{
						ExternalStorage storage = editDialog.GetStorage();
						Storages.Add(storage);
						Storage = storage;
						SaveStorages();
						dialogCoordinator.HideMetroDialogAsync(this, editDialog);
					});

					dialogCoordinator.ShowMetroDialogAsync(this, editDialog);
				});
			}
		}

		public ICommand RemoveStorageCommand
		{
			get
			{
				return new RelayCommand(obj =>
				{
					Storages.Remove(Storage);
					Storage = Storages.Count > 0 ? Storages.First() : null;
				});
			}
		}

		private void ResetUploadedFiles()
		{
			foreach (var att in Attachments)
			{
				att.URL = null;
				att.Progress = 0.0;
			}
				
		}

		private IDialogCoordinator dialogCoordinator;

		private void LoadTrackers()
		{
			foreach (var tracker in Settings.GlobalSettings.Data.Trackers)
			{
				switch (tracker.Type)
				{
					case TrackerType.GitHub:
						Trackers.Add(new GithubTaskTracker(tracker.Address));
						break;

					case TrackerType.Jira:
						Trackers.Add(new JiraTaskTracker(tracker.Address));
						break;
				}
			}

			if (Trackers.Count == 0)
			{
				Trackers.Add(new GithubTaskTracker("https://github.com/bombomby/optick"));
			}

			var targetTracker = Settings.GlobalSettings.Data.ActiveTracker;
			if (targetTracker != null)
			{
				Tracker = Trackers.FirstOrDefault(t => t.Address == targetTracker.Address && t.TrackerType == targetTracker.Type);
			}

			if (Tracker == null && Trackers.Count > 0)
			{
				Tracker = Trackers[0];
			}
		}

		private void LoadStorages()
		{
			foreach (var storage in Settings.GlobalSettings.Data.Storages)
			{
				Storages.Add(new NetworkStorage(storage.UploadURL, storage.DownloadURL));
			}

			Storages.Add(new GDriveStorage());

			var targetStorage = Settings.GlobalSettings.Data.ActiveStorage;
			if (!String.IsNullOrEmpty(targetStorage))
			{
				Storage = Storages.FirstOrDefault(s => (s.DisplayName == targetStorage));
			}

			if (Storage == null && Storages.Count > 0)
				Storage = Storages[0];
		}

		public TaskTrackerViewModel(IDialogCoordinator coordinator = null)
		{
			dialogCoordinator = coordinator;

			LoadTrackers();
			LoadStorages();
		}

		public void SetGroup(FrameGroup group)
		{
			if (group == null)
				return;

			// Attaching capture
			AttachmentVM capture = new AttachmentVM(new FileAttachment() { FileType = FileAttachment.Type.CAPTURE });
			if (!String.IsNullOrEmpty(group.Name) && File.Exists(group.Name) && false)
			{
				capture.Attachment.Name = Path.GetFileName(group.Name);
				capture.Attachment.Data = new FileStream(group.Name, FileMode.Open);
			}
			else
			{
				capture.Attachment.Name = "Capture.opt";
				capture.Attachment.Data = new MemoryStream();
				capture.IsUploading = true;
				Task.Run(()=> 
				{
					Application.Current.Dispatcher.Invoke(() => { capture.IsUploading = true; capture.Status = "Saving capture..."; });

					Stream captureStream = Capture.Create(capture.Attachment.Data, leaveStreamOpen: true);
					group.Save(captureStream);
					if (captureStream != capture.Attachment.Data)
					{
						// If Capture.Create made a new stream, Dispose that to ensure
						// it finishes writing to the main MemoryStream
						captureStream.Dispose();
					}

					Application.Current.Dispatcher.Invoke(() => { capture.IsUploading = false; capture.Status = String.Empty; capture.Size = capture.Attachment.Data.Length; });
				});
			}
			Attachments.Add(capture);

			// Attaching all the extracted attachments
			foreach (FileAttachment att in group.Summary.Attachments)
			{
				Attachments.Add(new AttachmentVM(att));
			}
		}

		internal void AttachScreenshot(String name, Stream screenshot)
		{
			Attachments.Add(new AttachmentVM(new FileAttachment() { Data = screenshot, FileType = FileAttachment.Type.IMAGE, Name = name })
			{
				IsExpanded = true,
			});
		}
	}
}
