using Profiler.Data;
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

namespace Profiler.Controls
{
	/// <summary>
	/// Interaction logic for FunctionSearch.xaml
	/// </summary>
	public partial class FunctionSearch : UserControl
	{
		public FunctionSearch()
		{
			InitializeComponent();
			DataContextChanged += FunctionSearch_DataContextChanged;
		}

		FrameGroup Group { get; set; }

		private void FunctionSearch_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			Group = (DataContext as FrameGroup);
			FunctionSearchDataGrid.ItemsSource = Group != null ? Group.Board.Board.OrderBy(d => d.Name) : null;
		}

		private void FunctionSearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			String text = FunctionSearchBox.Text;

			CollectionView itemsView = (CollectionView)CollectionViewSource.GetDefaultView(FunctionSearchDataGrid.ItemsSource);

			if (!String.IsNullOrEmpty(text))
				itemsView.Filter = (item) => (item as EventDescription).Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) != -1;
			else
				itemsView.Filter = null;

			FunctionSearchDataGrid.SelectedIndex = 0;
		}

		private void FunctionSearchDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

		}

		public void Open()
		{
			FunctionSearchBox.Focus();
			SearchPopup.Visibility = Visibility.Visible;
			SearchPopup.IsOpen = true;
		}

		private void Flush()
		{
			EventDescription desc = FunctionSearchDataGrid.SelectedItem as EventDescription;
			if (desc != null)
			{
				if (Group != null)
				{
					double maxDuration = 0;
					EventFrame maxFrame = null;
					Entry maxEntry = null;

					foreach (ThreadData thread in Group.Threads)
					{
						foreach (EventFrame frame in thread.Events)
						{
							List<Entry> entries = null;
							if (frame.ShortBoard.TryGetValue(desc, out entries))
							{
								foreach (Entry entry in entries)
								{
									if (entry.Duration > maxDuration)
									{
										maxFrame = frame;
										maxEntry = entry;
										maxDuration = entry.Duration;
									}
								}
							}
						}
					}

					if (maxFrame != null && maxEntry != null)
					{
						EventNode maxNode = maxFrame.Root.Find(maxEntry);
						RaiseEvent(new TimeLine.FocusFrameEventArgs(TimeLine.FocusFrameEvent, new EventFrame(maxFrame, maxNode), null));
					}
				}
			}
		}

		public void Close()
		{
			SearchPopup.IsOpen = false;
			Flush();
		}

		private void FunctionSearchBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Down)
				FunctionSearchDataGrid.SelectedIndex = FunctionSearchDataGrid.SelectedIndex + 1;

			if (e.Key == Key.Up)
				if (FunctionSearchDataGrid.SelectedIndex > 0)
					FunctionSearchDataGrid.SelectedIndex = FunctionSearchDataGrid.SelectedIndex - 1;

			if (e.Key == Key.Enter)
				Close();
		}

		private void FunctionSearchDataGrid_MouseUp(object sender, MouseButtonEventArgs e)
		{
			Close();
		}
	}
}
