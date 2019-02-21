using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;                 //ICommand
using Profiler.InfrastructureMvvm;
using System.Windows;

namespace Profiler.ViewModels
{
    public class ScreenShotViewModel: BaseViewModel, IDisposable
    {
        ImageSource _attachmentImage;
        public ImageSource AttachmentImage
        {
            get { return _attachmentImage; }
            set { SetField(ref _attachmentImage, value); }
        }

        public string Title { get; set; }

        public ICommand CloseViewCommand { get; set; }

        public ScreenShotViewModel(BitmapImage image =null, string title=null)
        {
            AttachmentImage = image;
            Title = title;

            CloseViewCommand = new RelayCommand<Window>(x =>
            {
                if (x != null)
                {
                    x.Close();
                    this.Dispose();
                }
                    
            });
        }

        public void Dispose()
        {
            AttachmentImage = null;
        }
    }
}
