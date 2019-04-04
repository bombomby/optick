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
	public class AttachmnetTemplateSelector : DataTemplateSelector
	{
		public DataTemplate ImageTemplate { get; set; }
		public DataTemplate TextTemplate { get; set; }
		public DataTemplate OtherTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			FileAttachment attachment = item as FileAttachment;
			if (attachment != null)
			{
				switch (attachment.FileType)
				{
					case FileAttachment.Type.IMAGE:
						return ImageTemplate;

					case FileAttachment.Type.TEXT:
						return TextTemplate;
				}
			}
			return OtherTemplate;
		}
	}

	/// <summary>
	/// Interaction logic for AttachmentViewControl.xaml
	/// </summary>
	public partial class AttachmentViewControl : UserControl
	{
		public AttachmentViewControl()
		{
			InitializeComponent();
		}
	}
}
