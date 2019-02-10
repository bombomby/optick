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
using Profiler.Controls.Helpers;
using Profiler.Services;


namespace Profiler.Controls.ViewModel
{
    public class SummaryViewerModel: BaseViewModel
    {
    #region private fields
         
        IDialogService _dialogService;

    #endregion

    #region propertyes

        SummaryPack _summary;
        public SummaryPack Summary
        {
            get { return _summary; }
            set {
                if (value != null && value.Attachments.Count > 0)
                {
                    Visibility = true;
                    Attachments = new ObservableCollection<SummaryPack.Attachment>(value.Attachments);
                }
                else
                    Visibility = false;

                SetField(ref _summary, value);
            }
        }

        ObservableCollection<SummaryPack.Attachment> _attachments;
        public ObservableCollection<SummaryPack.Attachment> Attachments
        {
            get { return _attachments; }
            set { SetField(ref _attachments, value); }
        }

        SummaryPack.Attachment _currentAttachment;
        public SummaryPack.Attachment CurrentAttachment
        {
            get { return _currentAttachment; }
            set
            {
                if (value != null)
                {
                    if (value.FileType == SummaryPack.Attachment.Type.BRO_IMAGE)
                    {
                        AttachmentContent = new Image() { Source = GetImageFromAttachment(value), Stretch = Stretch.UniformToFill };
                        IsEnableMagnifyingGlass = true;
                    }

                    if (value.FileType == SummaryPack.Attachment.Type.BRO_TEXT)
                    {
                        value.Data.Position = 0;

                        StreamReader reader = new StreamReader(value.Data);

                        AttachmentContent = new TextBox()
                        {
                            Text = reader.ReadToEnd(),
                            IsReadOnly = true
                        };

                        IsEnableMagnifyingGlass = false;
                    }
                }
                SetField(ref _currentAttachment, value);
            }
        }

        bool _visibility;
        public bool Visibility
        {
            get { return _visibility; }
            set{SetField(ref _visibility, value);}
        }

        bool _isEnableMagnifyingGlass;
        public bool IsEnableMagnifyingGlass
        {
            get { return _isEnableMagnifyingGlass; }
            set { SetField(ref _isEnableMagnifyingGlass, value); }
        }

        UIElement _attachmentContent;
        public UIElement AttachmentContent
        {
            get { return _attachmentContent; }
            set { SetField(ref _attachmentContent, value); }
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
                        if (IsEnableMagnifyingGlass && CurrentAttachment.FileType == SummaryPack.Attachment.Type.BRO_IMAGE)
                        {
                            BitmapImage image = GetImageFromAttachment(CurrentAttachment);
                            string title = String.Format("{0} ({1})", CurrentAttachment.Name, CaptureName);
                            var screenShotVM = new ScreenShotViewModel(image, title);
                            var screenShotView = new Profiler.Controls.View.ScreenShotView();
                            screenShotView.DataContext = screenShotVM;
                            screenShotView.Show();
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
                          string defaultExt =
                            (CurrentAttachment.FileType == SummaryPack.Attachment.Type.BRO_IMAGE) ? "png" : "txt";

                          if (_dialogService.SaveFileDialog(CurrentAttachment.Name, defaultExt) == true)
                          {
                              ExportAttachment(CurrentAttachment, _dialogService.FilePath);
                              System.Diagnostics.Process.Start(_dialogService.FilePath);
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

        private ICommand _exportAllAttachmentCommand;
        public ICommand ExportAllAttachmentCommand
        {
            get
            {
                return _exportAllAttachmentCommand ??
                  (_exportAllAttachmentCommand = new RelayCommand(obj =>
                  {
                      try
                      {
                          if (_dialogService.OpenFolderDialog() == true)
                          {
                              foreach (var attachment in Summary.Attachments)
                                 ExportAttachment(attachment,String.Format("{0}\\{1}", _dialogService.FilePath, attachment.Name));

                              _dialogService.ShowMessage("Attachments export completed.");
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

        public SummaryViewerModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }


    #endregion

    #region Private Methods


        private BitmapImage GetImageFromAttachment(SummaryPack.Attachment attachment)
        {
            attachment.Data.Position = 0;
            var imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.StreamSource = attachment.Data;
            imageSource.EndInit();

            return imageSource;
        }

        private void ExportAttachment(SummaryPack.Attachment attachment, string filePath)
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
                throw new Exception(String.Format("Error create file (0)", e.Message));
            }
        }

    #endregion
    }
}
