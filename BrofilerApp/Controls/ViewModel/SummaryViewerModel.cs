using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Profiler.Data;

namespace Profiler.Controls.ViewModel
{
    public class SummaryViewerModel: BaseViewModel
    {
        #region fields

        SummaryPack _summary;
        bool _visibility;
        ObservableCollection<SummaryPack.Attachment> _attachments;
        SummaryPack.Attachment _currentAttachment;
        UIElement _attachmentContent;

        #endregion

        #region propertyes

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

        public bool Visibility
        {
            get { return _visibility; }
            set{SetField(ref _visibility, value);}
        }

        public ObservableCollection<SummaryPack.Attachment> Attachments
        {
            get { return _attachments; }
            set { SetField(ref _attachments, value); }
        }

        public UIElement AttachmentContent
        {
            get { return _attachmentContent; }
            set { SetField(ref _attachmentContent, value); }
        }

        public  SummaryPack.Attachment CurrentAttachment
        {
            get { return _currentAttachment; }
            set {
                if (value != null)
                {
                    if (value.FileType == SummaryPack.Attachment.Type.BRO_IMAGE)
                    {
                        value.Data.Position = 0;

                        var imageSource = new BitmapImage();
                        imageSource.BeginInit();
                        imageSource.StreamSource = value.Data;
                        imageSource.EndInit();

                        AttachmentContent = new Image() { Source = imageSource, Stretch = Stretch.UniformToFill };
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
                    }
                }
                SetField(ref _currentAttachment, value);
            }
        }

        #endregion
    }
}
