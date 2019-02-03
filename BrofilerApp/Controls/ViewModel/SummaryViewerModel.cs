using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Profiler.Data;

namespace Profiler.Controls.ViewModel
{
    public class SummaryViewerModel: BaseViewModel
    {
        #region fields

        SummaryPack _summary;
        bool _visibility;
        //ObservableCollection<SummaryPack.Attachment> _attachments;
        List<SummaryPack.Attachment> _attachments;
        SummaryPack.Attachment _currentAttachment;

        #endregion

        #region propertyes

        public SummaryPack Summary
        {
            get { return _summary; }
            set {
                if (value != null && value.Attachments.Count > 0)
                {
                    Visibility = true;
                    // Attachments = new ObservableCollection<SummaryPack.Attachment>(value.Attachments);
                    Attachments = value.Attachments;
                }
                else
                    Visibility = false;

                SetField(ref _summary, value);
                Test = "test";
            }
        }

        public bool Visibility
        {
            get { return _visibility; }
            set{SetField(ref _visibility, value);}
        }

       // public ObservableCollection<SummaryPack.Attachment> Attachments
        public List<SummaryPack.Attachment> Attachments
        {
            get { return _attachments; }
            set { SetField(ref _attachments, value); }
        }

        public  SummaryPack.Attachment CurrentAttachment
        {
            get { return _currentAttachment; }
            set { SetField(ref _currentAttachment, value); }
        }

        public string Test { get; set; }
        #endregion
    }
}
