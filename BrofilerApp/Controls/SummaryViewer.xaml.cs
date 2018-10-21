using Profiler.Data;
using System;
using System.Collections.Generic;
using System.IO;
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
	/// Interaction logic for SummaryViewer.xaml
	/// </summary>
	public partial class SummaryViewer : UserControl
	{
		public SummaryViewer()
		{
			InitializeComponent();
			DataContextChanged += SummaryViewer_DataContextChanged;
		}

		private void SummaryViewer_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is SummaryPack)
			{
				SummaryPack pack = e.NewValue as SummaryPack;
				AttachmentsComboBox.SelectedIndex = 0;
			}

			if (DataContext != null && (DataContext is SummaryPack) && (DataContext as SummaryPack).Attachments.Count > 0)
				Visibility = Visibility.Visible;
			else
				Visibility = Visibility.Collapsed;
		}

		private void AttachmentsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Data.SummaryPack.Attachment attachment = AttachmentsComboBox.SelectedItem as SummaryPack.Attachment;
			if (attachment != null)
			{
				if (attachment.FileType == SummaryPack.Attachment.Type.BRO_IMAGE)
				{
					attachment.Data.Position = 0;

					var imageSource = new BitmapImage();
					imageSource.BeginInit();
					imageSource.StreamSource = attachment.Data;
					imageSource.EndInit();

					AttachmentContent.Child = new Image() { Source = imageSource, Stretch = Stretch.UniformToFill};
				}

				if (attachment.FileType == SummaryPack.Attachment.Type.BRO_TEXT)
				{
					attachment.Data.Position = 0;

					StreamReader reader = new StreamReader(attachment.Data);

					AttachmentContent.Child = new TextBox()
					{
						Text = reader.ReadToEnd(),
						IsReadOnly = true
				};
				}
			}
		}
	}
}
