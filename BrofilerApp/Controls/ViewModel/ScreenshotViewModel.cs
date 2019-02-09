using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;                 //ICommand
using Profiler.Data;
using Profiler.Controls.Helpers;
using System.Windows;

namespace Profiler.Controls.ViewModel
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

        public ScreenShotViewModel(SummaryPack.Attachment attachment, string nameCapture)
        {
            if(attachment.FileType == SummaryPack.Attachment.Type.BRO_IMAGE)
            {
                attachment.Data.Position = 0;
                BitmapImage imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.StreamSource = attachment.Data;
                imageSource.EndInit();

                AttachmentImage = imageSource;
            }
            Title = String.Format("{0} ({1})", attachment.Name, nameCapture);

            CloseViewCommand = new RelayCommand<Window>(x =>
            {
                if (x != null)
                    x.Close();
            });
        }

    }
}
