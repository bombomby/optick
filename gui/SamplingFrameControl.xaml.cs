using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace Profiler
{
  /// <summary>
  /// Interaction logic for SamplingFrameControl.xaml
  /// </summary>
  public partial class SamplingFrameControl : UserControl
  {
    public SamplingFrameControl()
    {
      InitializeComponent();
      Init();
      DataContextChanged += new DependencyPropertyChangedEventHandler(OnDataContextChanged);
    }

    void Init()
    {
      if (DataContext is SamplingFrame)
      {
        SamplingFrame frame = DataContext as SamplingFrame;


      }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      Init();
    }
  }
}
