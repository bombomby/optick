using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;                 //ICommand
using Profiler.Helpers;
using System.Windows;

namespace Profiler.ViewModel
{
    public class ScreenShotViewModel: BaseViewModel
    {
        ImageSource _attachmentImage;
        public ImageSource AttachmentImage
        {
            get { return _attachmentImage; }
            set { SetField(ref _attachmentImage, value); }
        }

        public string Title { get; set; }

        public ICommand CloseViewCommand { get; set; }

        public ScreenShotViewModel(BitmapImage image, string title)
        {
            AttachmentImage = image;
            Title = title;

            CloseViewCommand = new RelayCommand<Window>(x =>
            {
                if (x != null)
                    x.Close();
            });
        }

    }
}
