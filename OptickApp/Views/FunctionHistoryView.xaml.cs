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

namespace Profiler.Views
{
    /// <summary>
    /// Interaction logic for FunctionHistoryView.xaml
    /// </summary>
    public partial class FunctionHistoryView : UserControl
    {
        public FunctionHistoryView()
        {
            InitializeComponent();

            FrameChart.TooltipTimeout = new TimeSpan(0, 0, 0, 0, 100);
            FrameChart.DataTooltip.Background = FindResource("BroBackground") as SolidColorBrush;
            FrameChart.DataTooltip.BorderBrush = FindResource("AccentColorBrush") as SolidColorBrush;
            FrameChart.DataTooltip.BorderThickness = new Thickness(0.5);
        }
    }
}
