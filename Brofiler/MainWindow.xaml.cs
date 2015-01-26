using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using Profiler.Data;

namespace Profiler
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

      this.AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(this.CloseTab));
			this.AddHandler(TimeLine.FocusFrameEvent, new TimeLine.FocusFrameEventHandler(this.OpenTab));

			timeLine.OnClearAllFrames += new ClearAllFramesHandler(ClearAllTabs);

			ParseCommandLine();
    }

		private void ClearAllTabs()
		{
			frameTabs.Items.Clear();
		}

		private void CloseTab(object source, RoutedEventArgs args)
    {
      TabItem tabItem = args.Source as TabItem;
      if (tabItem != null)
      {
        TabControl tabControl = tabItem.Parent as TabControl;
        if (tabControl != null)
          tabControl.Items.Remove(tabItem);
      }
    }

		private void OpenTab(object source, TimeLine.FocusFrameEventArgs args)
		{
			Data.Frame frame = args.Frame;
			foreach (var tab in frameTabs.Items)
			{
			  if (tab is CloseableTabItem)
			  {
			    CloseableTabItem item = (CloseableTabItem)tab;
			    if (item.DataContext.Equals(frame))
			    {
			      frameTabs.SelectedItem = item;
			      return;
			    }
			  }
			}

      CloseableTabItem tabItem = new CloseableTabItem() { Header = "Loading...", DataContext = frame };

      FrameInfo info = new FrameInfo() { Height = Double.NaN, Width = Double.NaN, DataContext = null };
      info.DataContextChanged += new DependencyPropertyChangedEventHandler((object sender, DependencyPropertyChangedEventArgs e) => {tabItem.Header = frame.Description;});
      info.SetFrame(frame);
      
      tabItem.Add(info);
  
			frameTabs.Items.Add(tabItem);
      frameTabs.SelectedItem = tabItem;
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
      timeLine.Close();
      ProfilerClient.Get().Close();
			base.OnClosing(e);
		}

		private void ParseCommandLine()
		{
			string[] args = Environment.GetCommandLineArgs();
			for (int i = 1; i < args.Length; ++i )
			{
				String fileName = args[i];
				if (File.Exists(fileName))
					LoadFile(fileName);
			}
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			foreach (string file in files)
			{
				LoadFile(file);
			}
		}

		private void LoadFile(string file)
		{
      if (File.Exists(file))
      {
        using (FileStream stream = new FileStream(file, FileMode.Open))
        {
          timeLine.Open(stream);
        }
      }
		}

		private void Window_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) 
				e.Effects = DragDropEffects.Copy;
		}
	}
}
