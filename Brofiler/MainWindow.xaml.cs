using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Profiler.Data;
using MahApps.Metro.Controls;

namespace Profiler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            this.AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(this.CloseTab));
            this.AddHandler(TimeLine.FocusFrameEvent, new TimeLine.FocusFrameEventHandler(this.OpenTab));

            timeLine.OnClearAllFrames += new ClearAllFramesHandler(ClearAllTabs);

            frameTabs.SelectionChanged += new SelectionChangedEventHandler(frameTabs_SelectionChanged);

            ParseCommandLine();
        }

        void frameTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (frameTabs.SelectedItem is TabItem)
            {
                var dataContext = (frameTabs.SelectedItem as TabItem).DataContext;

                if (dataContext is Data.EventFrame)
                {
                    Data.EventFrame frame = dataContext as Data.EventFrame;
                    ThreadView.FocusOn(frame, null);
                }
            }
        }

        private void ClearAllTabs()
        {
            frameTabs.Items.Clear();
            ThreadView.Group = null;
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
			Durable focusRange = null;
			if (args.Node != null)
			{
				focusRange = args.Node.Entry;
			}
			else if (args.Tick != null)
			{
				focusRange = new Durable(args.Tick.Start, args.Tick.Start + 1 );
			}
            else if (args.Frame is EventFrame)
            {
                focusRange = (args.Frame as EventFrame).Header;
            }

            Data.Frame frame = args.Frame;
            foreach (var tab in frameTabs.Items)
            {
                if (tab is TabItem)
                {
                    TabItem item = (TabItem)tab;
                    frameTabs.SelectedItem = item;
                    if (item.DataContext.Equals(frame))
                    {
                        FrameInfo frameInfo = item.Content as FrameInfo;
						if (frameInfo != null)
						{
                            frameInfo.FocusOnNode(focusRange);
						}
                        return;
                    }
                }
            }


            CloseableTabItem tabItem = new CloseableTabItem() { Header = "Loading...", DataContext = frame, CloseButtonEnabled = true };
            
			FrameInfo info = new FrameInfo(timeLine.Frames) { Height = Double.NaN, Width = Double.NaN, DataContext = null };
            info.DataContextChanged += new DependencyPropertyChangedEventHandler((object sender, DependencyPropertyChangedEventArgs e) => { tabItem.Header = frame.Description; });
            info.SelectedTreeNodeChanged += new SelectedTreeNodeChangedHandler(FrameInfo_OnSelectedTreeNodeChanged);
            info.SetFrame(frame, focusRange);

            tabItem.AddFrameInfo(info);

            frameTabs.Items.Add(tabItem);
            frameTabs.SelectedItem = tabItem;

			info.FocusOnNode(focusRange);

/*
			if (!string.IsNullOrEmpty(currFiltredText))
			{
				info.SummaryTable.FilterText.SetFilterText(currFiltredText);
			}
 */ 
        }

        void FrameInfo_OnSelectedTreeNodeChanged(Data.Frame frame, BaseTreeNode node)
        {
            if (node is EventNode && frame is EventFrame)
            {
                ThreadView.FocusOn(frame as EventFrame, node as EventNode);
            }
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
            for (int i = 1; i < args.Length; ++i)
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
            timeLine.LoadFile(file);
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
        }
    }



	public static class Extensions
	{
		// extension method
		public static T GetChildOfType<T>(this DependencyObject depObj) where T : DependencyObject
		{
			if (depObj == null) return null;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
			{
				var child = VisualTreeHelper.GetChild(depObj, i);
				var result = (child as T) ?? GetChildOfType<T>(child);
				if (result != null) return result;
			}
			return null;
		}
	}


}
