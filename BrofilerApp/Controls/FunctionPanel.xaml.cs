using System;
using System.Collections.Generic;
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
using Profiler.Data;
using Profiler.ViewModels;

namespace Profiler.Controls
{
    /// <summary>
    /// Interaction logic for FunctionPanel.xaml
    /// </summary>
    public partial class FunctionPanel : UserControl
    {
        public FunctionPanel()
        {
            InitializeComponent();

            HamburgerMenuContentItem defaultItem = FunctionTreeItem;
            HamburgerMenuControl.SelectedItem = defaultItem;
            HamburgerMenuControl.Content = defaultItem;

            FunctionSummaryVM = (FunctionSummaryViewModel)FindResource("FunctionSummaryVM");
            FunctionInstanceVM = (FunctionInstanceViewModel)FindResource("FunctionInstanceVM");
        }
        FunctionSummaryViewModel FunctionSummaryVM { get; set; }
        FunctionInstanceViewModel FunctionInstanceVM { get; set; }

        private void HamburgerMenuControl_ItemClick(object sender, MahApps.Metro.Controls.ItemClickEventArgs e)
        {
            ApplyFrame(e.ClickedItem, Frame);
            HamburgerMenuControl.Content = e.ClickedItem;
        }

        Data.Frame Frame { get; set; }

        public void Clear()
        {
            FrameInfoControl.DataContext = null; // SetFrame(null, null);
            SampleInfoControl.DataContext = null; // SetFrame(null, null);
            SysCallInfoControl.DataContext = null; // SetFrame(null, null);
        }

        public void SetFrame(Data.Frame frame)
        {
            Frame = frame;
            ApplyFrame(HamburgerMenuControl.SelectedItem, frame);
        }

        public void ApplyFrame(Object selectedItem, Data.Frame frame)
        {
            if (selectedItem == FunctionChartsItem)
            {
                if (frame is EventFrame)
                {
                    EventFrame eventFrame = frame as EventFrame;
                    FunctionSummaryVM.Load(eventFrame.Group, eventFrame.RootEntry.Description);
                    FunctionInstanceVM.Load(eventFrame.Group, eventFrame.RootEntry.Description);
                }
            }

            if (selectedItem is HamburgerMenuContentItem)
            {
                HamburgerMenuContentItem item = selectedItem as HamburgerMenuContentItem;

                if (item.Content is FrameInfo)
                {
                    FrameInfo info = item.Content as FrameInfo;

                    if (frame is EventFrame)
                    {
                        info.SetFrame(frame, null);
                    }
                }
            }
        }
    }
}
