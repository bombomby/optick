using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;                 //ICommand
using Profiler.InfrastructureMvvm;
using System.Windows;

namespace Profiler.ViewModels
{
    public class ScreenShotViewModel: BaseViewModel
    {
    #region properties

        ImageSource _attachmentImage;
        public ImageSource AttachmentImage
        {
            get { return _attachmentImage; }
            set { SetProperty(ref _attachmentImage, value); }
        }

        public string Title { get; set; }

    #endregion

    #region commands

        public ICommand CloseViewCommand { get; set; }

    #endregion

    #region constructor
        public ScreenShotViewModel(BitmapImage image =null, string title=null)
        {
            AttachmentImage = image;
            Title = title;

            CloseViewCommand = new RelayCommand<Window>(x =>
            {
                if (x != null)
                    x.Close();

                    
            });
        }

        #endregion
    }
}
