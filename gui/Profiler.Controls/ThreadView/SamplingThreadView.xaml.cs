using Profiler.Data;
using Profiler.Controls.ViewModels;
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
	/// Interaction logic for SamlingThreadView.xaml
	/// </summary>
	public partial class SamplingThreadView : UserControl
	{
		public SamplingThreadView()
		{
			InitializeComponent();

			DataContextChanged += SamplingThreadView_DataContextChanged;
			IsVisibleChanged += SamplingThreadView_IsVisibleChanged;
		}

		private void VM_OnLoaded(SamplingFrame frame)
		{
			InitThreadList(frame);
		}

		private void SamplingThreadView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			VM = DataContext as SamplingViewModel;
			VM.OnLoaded += VM_OnLoaded;
		}

		SamplingViewModel VM { get; set; }

		private void SamplingThreadView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			VM?.SetActive(IsVisible);
		}

		void BuildEntryList(List<Entry> entries, SamplingNode node, double offset)
		{
			if (node.Description != null)
				entries.Add(new Entry(new EventDescription(node.NameWithModule), Durable.MsToTick(offset), Durable.MsToTick(offset + node.Duration)));

			offset += node.SelfDuration * 0.5;

			foreach (SamplingNode child in node.Children)
			{
				BuildEntryList(entries, child, offset);
				offset += child.Duration;
			}
		}

		private ThreadViewSettings Settings { get; set; } = new ThreadViewSettings();

		void InitThreadList(SamplingFrame frame)
		{
			Frame = frame;

			List<ThreadRow> rows = new List<ThreadRow>();

			if (frame != null)
			{
				List<Entry> entries = new List<Entry>();

				SamplingNode root = frame.Root;

				BuildEntryList(entries, root, 0.0);

				EventFrame eventFrame = new EventFrame(new FrameHeader() { Start = 0, Finish = Durable.MsToTick(root.Duration) }, entries, frame.Group);
				ThreadData threadData = new ThreadData(null) { Events = new List<EventFrame> { eventFrame } };
				EventsThreadRow row = new EventsThreadRow(frame.Group, new ThreadDescription() { Name = "Sampling Node" }, threadData, Settings);
				row.LimitMaxDepth = false;
				row.EventNodeHover += Row_EventNodeHover;
				rows.Add(row);
				ThreadViewControl.Scroll.ViewUnit.Width = 1.0;
				ThreadViewControl.InitRows(rows, eventFrame.Header);
			}
			else
			{
				ThreadViewControl.InitRows(rows, null);
			}

		}

		private void Row_EventNodeHover(Point mousePos, Rect rect, ThreadRow row, EventNode node)
		{
			ThreadViewControl.ToolTipPanel = node != null ? new ThreadViewControl.TooltipInfo { Text = String.Format("{0}   {1:0.#} ({2:0.#}%)", node.Description.FullName, node.Duration, 100.0 * node.Duration / Frame.Root.Duration), Rect = rect } : null;
		}

		private FrameGroup Group { get; set; }
		private EventDescription Description { get; set; }
		private SamplingFrame Frame { get; set; }
	}
}
