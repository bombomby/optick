using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Profiler.Data;
using Profiler.InfrastructureMvvm;
using Autofac;

namespace Profiler.ViewModels
{
    public class SummaryViewerModel: BaseViewModel
    {
    #region private fields
         
        IFileDialogService _dialogService;

    #endregion

    #region properties

        SummaryPack _summary;
        public SummaryPack Summary
        {
            get { return _summary; }
            set {
                if (value != null && value.Attachments.Count > 0)
                {
                    Visible = Visibility.Visible;
                    Attachments = new ObservableCollection<FileAttachment>(value.Attachments);
                }
                else
                    Visible = Visibility.Collapsed;


                SetProperty(ref _summary, value);
            }
        }

        ObservableCollection<FileAttachment> _attachments;
        public ObservableCollection<FileAttachment> Attachments
        {
            get { return _attachments; }
            set {
                CurrentAttachment = value?.FirstOrDefault(x => x.FileType == FileAttachment.Type.IMAGE);
                SetProperty(ref _attachments, value);
            }
        } 

        FileAttachment _currentAttachment;
        public FileAttachment CurrentAttachment
        {
            get { return _currentAttachment; }
            set
            {
                if (value != null)
                {
                    if (value.FileType == FileAttachment.Type.IMAGE)
                    {
                        AttachmentContent = new Image() { Source = GetImageFromAttachment(value), Stretch = Stretch.UniformToFill };
                        IsEnableOpenScreenShotView = true;
                    }

                    if (value.FileType == FileAttachment.Type.TEXT)
                    {
                        value.Data.Position = 0;

                        StreamReader reader = new StreamReader(value.Data);

                        AttachmentContent = new TextBox()
                        {
                            Text = reader.ReadToEnd(),
                            IsReadOnly = true
                        };

                        IsEnableOpenScreenShotView = false;
                    }
                }
                SetProperty(ref _currentAttachment, value);
            }
        }

        Visibility _visible;
        public Visibility Visible
        {
            get { return _visible; }
            set{SetProperty(ref _visible, value);}
        }

        bool _isEnableOpenScreenShotView;
        public bool IsEnableOpenScreenShotView
        {
            get { return _isEnableOpenScreenShotView; }
            set { SetProperty(ref _isEnableOpenScreenShotView, value); }
        }

        UIElement _attachmentContent;
        public UIElement AttachmentContent
        {
            get { return _attachmentContent; }
            set { SetProperty(ref _attachmentContent, value); }
        }

        public string CaptureName { get; set; }

    #endregion

    #region Commands

        private ICommand _openScreenShotViewCommand;
        public ICommand OpenScreenShotViewCommand
        {
            get
            {
                return _openScreenShotViewCommand ??
                    (_openScreenShotViewCommand = new RelayCommand(obj =>
                    {
                        if (IsEnableOpenScreenShotView && CurrentAttachment.FileType == FileAttachment.Type.IMAGE)
                        {
                            ScreenShotViewModel viewModel = new ScreenShotViewModel();
                            viewModel.AttachmentImage = GetImageFromAttachment(CurrentAttachment);
                            viewModel.Title = (CaptureName?.Length > 0) ? String.Format("{0} ({1})", CurrentAttachment.Name, CaptureName) : CurrentAttachment.Name;
                            using (var scope = BootStrapperBase.Container.BeginLifetimeScope())
                            {
                                var screenShotView = scope.Resolve<IWindowManager>().ShowWindow(viewModel);
                            }                               
                        }
                    },
                  // Condition execute command
                  enable => CurrentAttachment != null
                  ));
            }
        }

        private ICommand _exportCurrentAttachmentCommand;
        public ICommand ExportCurrentAttachmentCommand
        {
            get
            {
                return _exportCurrentAttachmentCommand ??
                  (_exportCurrentAttachmentCommand = new RelayCommand(obj =>
                  {
                      try
                      {
                          string defaultPath = Controls.Settings.LocalSettings.Data.TempDirectoryPath;

                          // Generate unique folder name
                          string uniqueFolderName = Guid.NewGuid().ToString();
                          defaultPath = Path.Combine(defaultPath, uniqueFolderName);

                          DirectoryInfo dirInfo = new DirectoryInfo(defaultPath);
                          if (!dirInfo.Exists)
                              dirInfo.Create();

                          string filePath = Path.Combine(defaultPath, CurrentAttachment.Name);
                        
                          SaveAttachment(CurrentAttachment, filePath);
                          System.Diagnostics.Process.Start(filePath);

                          // System.Diagnostics.Process.Start doesn't block file,
                          // the file can be removed immediately
                          // File.Delete(defaultPath);
                      }
                      catch (Exception ex)
                      {
                          _dialogService.ShowMessage(ex.Message);
                      }
                  },
                  // Condition execute command
                  enable => CurrentAttachment != null
                  ));
            }
        }

        private ICommand _saveCurrentAttachmentCommand;
        public ICommand SaveCurrentAttachmentCommand
        {
            get
            {
                return _saveCurrentAttachmentCommand ??
                  (_saveCurrentAttachmentCommand = new RelayCommand(obj =>
                  {
                      try
                      {
                          string defaultExt = Path.GetExtension(CurrentAttachment.Name);
                          string filter = String.Format("(*{0})|*{0}", defaultExt);

                          if (_dialogService.SaveFileDialog(CurrentAttachment.Name, defaultExt, filter) == true)
                          {
                              SaveAttachment(CurrentAttachment, _dialogService.FilePath);
                          }
                      }
                      catch (Exception ex)
                      {
                          _dialogService.ShowMessage(ex.Message);
                      }
                  },
                  // Condition execute command
                  enable => CurrentAttachment !=null    
                  ));
            }
        }

        private ICommand _saveAllAttachmentCommand;
        public ICommand SaveAllAttachmentCommand
        {
            get
            {
                return _saveAllAttachmentCommand ??
                  (_saveAllAttachmentCommand = new RelayCommand(obj =>
                  {
                      try
                      {
                          if (_dialogService.OpenFolderDialog() == true)
                          {
                              foreach (var attachment in Summary.Attachments)
                                 SaveAttachment(attachment,String.Format("{0}\\{1}", _dialogService.FilePath, attachment.Name));
                          }
                      }
                      catch (Exception ex)
                      {
                          _dialogService.ShowMessage(ex.Message);
                      }
                  },
                  // Condition execute command
                  enable => Summary?.Attachments?.Count>0    
                  ));
            }
        }

    #endregion

    #region Constructor

        public SummaryViewerModel(IFileDialogService dialogService)
        {
            _dialogService = dialogService;
            Visible = Visibility.Collapsed;
        }


    #endregion

    #region Private Methods

        private static BitmapImage GetImageFromAttachment(FileAttachment attachment)
        {
            attachment.Data.Position = 0;
            var imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.StreamSource = attachment.Data;
            imageSource.EndInit();

            return imageSource;
        }

        private void SaveAttachment(FileAttachment attachment, string filePath)
        {
            attachment.Data.Position = 0;
            
            try
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    attachment.Data.CopyTo(fileStream);
                    fileStream.Flush();
                }
            }
            catch (Exception e)
            {
                throw new Exception(String.Format(@"Error create file (0)", e.Message));
            }
        }

    #endregion
    }
}
