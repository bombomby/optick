using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Profiler.Data;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Globalization;
using System.Threading;
using System.ComponentModel;
using Profiler.DirectX;
using System.Windows.Threading;

namespace Profiler
{
	/// <summary>
	/// Interaction logic for ThreadView.xaml
	/// </summary>
	public partial class ThreadView : UserControl
	{
		FrameGroup group;

		ThreadScroll scroll = new ThreadScroll();
		List<ThreadRow> rows = new List<ThreadRow>();
		Dictionary<int, ThreadRow> id2row = new Dictionary<int, ThreadRow>();

		SolidColorBrush BroBackground;
		SolidColorBrush BroAlternativeBackground;
		SolidColorBrush FrameSelection;
		SolidColorBrush FrameHover;
		Color MeasureBackground;
		Color HoverBackground;


		void InitColors()
		{
			BroBackground = FindResource("BroBackground") as SolidColorBrush;
			BroAlternativeBackground = FindResource("BroAlternative") as SolidColorBrush;
			FrameSelection = FindResource("BroFrameSelection") as SolidColorBrush;
			FrameHover = FindResource("BroFrameHover") as SolidColorBrush;
			MeasureBackground = Color.FromArgb(100, 0, 0, 0);
			HoverBackground = Color.FromArgb(170, 0, 0, 0);
		}

		class RowsDescription
		{
			public Durable Range { get; set; }
		}

		public FrameGroup Group
		{
			set
			{
				if (value != group)
				{
					group = value;

					InitThreadList(group);

					Visibility visibility = value == null ? Visibility.Collapsed : Visibility.Visible;

					scrollBar.Visibility = visibility;
					ThreadToolsPanel.Visibility = visibility;

					surface.Height = value == null ? 0.0 : ThreadList.Height;
				}
			}

			get
			{
				return group;
			}
		}


		Mesh BackgroundMesh { get; set; }

		int ThreadNameSorter(EventsThreadRow a, EventsThreadRow b)
		{
			if (a.Description.ThreadID == ThreadDescription.InvalidThreadID && b.Description.ThreadID != ThreadDescription.InvalidThreadID)
				return -1;

			if (a.Description.ThreadID != ThreadDescription.InvalidThreadID && b.Description.ThreadID == ThreadDescription.InvalidThreadID)
				return 1;

			int nameCompare = a.Name.CompareTo(b.Name);
			return nameCompare != 0 ? nameCompare : a.Description.ThreadID.CompareTo(b.Description.ThreadID);
		}

		void InitThreadList(FrameGroup group)
		{
			rows.Clear();
			id2row.Clear();

			ThreadList.RowDefinitions.Clear();
			ThreadList.Children.Clear();

			if (group == null)
				return;

			rows.Add(new HeaderThreadRow(group)
			{
				GradientTop = (BroAlternativeBackground as SolidColorBrush).Color,
				GradientBottom = (BroBackground as SolidColorBrush).Color,
				SplitLines = (BroBackground as SolidColorBrush).Color,
				TextColor = Colors.Gray
			});

			List<EventsThreadRow> eventThreads = new List<EventsThreadRow>();

			for (int i = 0; i < Math.Min(group.Board.Threads.Count, group.Threads.Count); ++i)
			{
				ThreadDescription thread = group.Board.Threads[i];
				ThreadData data = group.Threads[i];

				bool threadHasData = false;
				if ((data.Callstacks != null && data.Callstacks.Count > 3) ||
					(data.Events != null && data.Events.Count > 0))

				{
					threadHasData = true;
				}

				if (threadHasData)
				{
					EventsThreadRow row = new EventsThreadRow(group, thread, data);
					eventThreads.Add(row);
					id2row.Add(i, row);

					row.EventNodeHover += Row_EventNodeHover;
					row.EventNodeSelected += Row_EventNodeSelected;
				}
			}

			eventThreads.Sort(ThreadNameSorter);
			rows.AddRange(eventThreads);

			scroll.TimeSlice = group.Board.TimeSlice;
			scroll.Height = 0.0;
			scroll.Width = surface.ActualWidth * RenderSettings.dpiScaleX;
			rows.ForEach(row => scroll.Height += row.Height);

			rows.ForEach(row => row.BuildMesh(surface, scroll));

			ThreadList.Margin = new Thickness(0, 0, 3, 0);

			double offset = 0.0;

			for (int threadIndex = 0; threadIndex < rows.Count; ++threadIndex)
			{
				ThreadRow row = rows[threadIndex];
				row.Offset = offset;

				ThreadList.RowDefinitions.Add(new RowDefinition());

				Thickness margin = new Thickness(0, 0, 0, 0);

				Label labelName = new Label() { Content = row.Name, Margin = margin, Padding = new Thickness(), FontWeight = FontWeights.Bold, Height = row.Height / RenderSettings.dpiScaleY, VerticalContentAlignment = VerticalAlignment.Center };

				Grid.SetRow(labelName, threadIndex);

				if (threadIndex % 2 == 1)
					labelName.Background = BroAlternativeBackground;

				ThreadList.Children.Add(labelName);
				offset += row.Height;
			}

			InitBackgroundMesh();
		}

		private void InitBackgroundMesh()
		{
			if (BackgroundMesh != null)
				BackgroundMesh.Dispose();

			DynamicMesh backgroundBuilder = surface.CreateMesh();
			backgroundBuilder.Projection = Mesh.ProjectionType.Pixel;

			double offset = 0.0;

			for (int threadIndex = 0; threadIndex < rows.Count; ++threadIndex)
			{
				ThreadRow row = rows[threadIndex];
				row.Offset = offset;

				if (threadIndex % 2 == 1)
					backgroundBuilder.AddRect(new Rect(0.0, offset, scroll.Width, row.Height), BroAlternativeBackground.Color);

				offset += row.Height;
			}

			BackgroundMesh = backgroundBuilder.Freeze(surface.RenderDevice);
		}

		private void Row_EventNodeSelected(ThreadRow row, EventFrame frame, EventNode node)
		{
			EventFrame focusFrame = frame;
			if (node != null && node.Entry.CompareTo(frame.Header) != 0)
				focusFrame = new EventFrame(frame, node);
			RaiseEvent(new TimeLine.FocusFrameEventArgs(TimeLine.FocusFrameEvent, focusFrame, null));
		}

		private void Row_EventNodeHover(Point mousePos, Rect rect, ThreadRow row, EventNode node)
		{
			if (node != null)
			{
				//HoverLines.AddRect(rect, FrameHover.Color);
				ToolTip = new TooltipInfo { Text = String.Format("{0}   {1:0.000}ms", node.Name, node.Duration), Rect = rect };
			}
			else
			{
				ToolTip = new TooltipInfo();
			}
		}

		DynamicMesh SelectionMesh;
		DynamicMesh HoverMesh;
		DynamicMesh HoverLines;
		DynamicMesh MeasureMesh;
		TooltipInfo ToolTip;

		struct TooltipInfo
		{
			public String Text;
			public Rect Rect;

			internal void Reset()
			{
				Text = String.Empty;
				Rect = new Rect();
			}
		}

		const double DefaultFrameZoom = 1.05;

		public void FocusOn(EventFrame frame, EventNode node)
		{
			Group = frame.Group;
			SelectionList.Clear();
			SelectionList.Add(new Selection() { Frame = frame, Node = node });

			Interval interval = scroll.TimeToUnit(node != null ? (IDurable)node.Entry : (IDurable)frame);
			if (!scroll.ViewUnit.Intersect(interval))
			{
				scroll.ViewUnit.Width = interval.Width * DefaultFrameZoom;
				scroll.ViewUnit.Left = interval.Left - (scroll.ViewUnit.Width - interval.Width) * 0.5;
				scroll.ViewUnit.Normalize();
				UpdateBar();
			}

			UpdateSurface();
		}

		public ThreadView()
		{
			InitializeComponent();
			scrollBar.Visibility = Visibility.Collapsed;
			ThreadToolsPanel.Visibility = Visibility.Collapsed;

			surface.SizeChanged += new SizeChangedEventHandler(ThreadView_SizeChanged);
			surface.OnDraw += OnDraw;

			InitInputEvent();

			InitColors();

			SelectionMesh = surface.CreateMesh();
			SelectionMesh.Projection = Mesh.ProjectionType.Pixel;
			SelectionMesh.Geometry = Mesh.GeometryType.Lines;

			HoverLines = surface.CreateMesh();
			HoverLines.Projection = Mesh.ProjectionType.Pixel;
			HoverLines.Geometry = Mesh.GeometryType.Lines;

			HoverMesh = surface.CreateMesh();
			HoverMesh.Projection = Mesh.ProjectionType.Pixel;
			HoverMesh.Geometry = Mesh.GeometryType.Polygons;
			HoverMesh.UseAlpha = true;

			MeasureMesh = surface.CreateMesh();
			MeasureMesh.Projection = Mesh.ProjectionType.Pixel;
			MeasureMesh.UseAlpha = true;
		}

		class InputState
		{
			public bool IsDrag { get; set; }
			public bool IsSelect { get; set; }
			public bool IsMeasure { get; set; }
			public Durable MeasureInterval { get; set; }
			public System.Drawing.Point SelectStartPosition { get; set; }
			public System.Drawing.Point DragPosition { get; set; }
			public System.Drawing.Point MousePosition { get; set; }

			public InputState()
			{
				MeasureInterval = new Durable();
			}
		}

		InputState Input = new InputState();

		private void InitInputEvent()
		{
			surface.RenderCanvas.MouseWheel += RenderCanvas_MouseWheel;
			surface.RenderCanvas.MouseDown += RenderCanvas_MouseDown;
			surface.RenderCanvas.MouseUp += RenderCanvas_MouseUp;
			surface.RenderCanvas.MouseMove += RenderCanvas_MouseMove;
			surface.RenderCanvas.MouseLeave += RenderCanvas_MouseLeave;

			scrollBar.Scroll += ScrollBar_Scroll;
		}

		private void RenderCanvas_MouseLeave(object sender, EventArgs e)
		{
			Mouse.OverrideCursor = null;
			Input.IsDrag = false;
			Input.IsSelect = false;
			ToolTip.Reset();
			UpdateSurface();
		}


		private void MouseShowPopup(System.Windows.Forms.MouseEventArgs args)
		{
			System.Drawing.Point e = new System.Drawing.Point(args.X, args.Y);
			List<Object> dataContext = new List<object>();
			foreach (ThreadRow row in rows)
			{
				if (row.Offset <= e.Y && e.Y <= row.Offset + row.Height)
				{
					row.OnMouseHover(new Point(e.X, e.Y - row.Offset), scroll, dataContext);
				}
			}
			SurfacePopup.DataContext = dataContext;
			SurfacePopup.IsOpen = dataContext.Count > 0 ? true : false;
		}

		private void MouseClickLeft(System.Windows.Forms.MouseEventArgs args)
		{
			System.Drawing.Point e = new System.Drawing.Point(args.X, args.Y);
			foreach (ThreadRow row in rows)
			{
				if (row.Offset <= e.Y && e.Y <= row.Offset + row.Height)
				{
					row.OnMouseClick(new Point(e.X, e.Y - row.Offset), scroll);
				}
			}
		}

		ThreadRow GetRow(double posY)
		{
			foreach (ThreadRow row in rows)
				if (row.Offset <= posY && posY <= row.Offset + row.Height)
					return row;

			return null;
		}

		private void RenderCanvas_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			Input.MousePosition = e.Location;
			bool updateSurface = false;

			if (Input.IsDrag)
			{
				double deltaPixel = e.X - Input.DragPosition.X;

				double deltaUnit = scroll.PixelToUnitLength(deltaPixel);
				scroll.ViewUnit.Left -= deltaUnit;
				scroll.ViewUnit.Normalize();

				UpdateBar();
				updateSurface = true;

				Input.DragPosition = e.Location;
			}
			else if (Input.IsMeasure)
			{
				Input.MeasureInterval.Finish = scroll.PixelToTime(e.X).Start;
				updateSurface = true;
			}
			else
			{
				ThreadRow row = GetRow(e.Y);
				if (row != null)
				{
					row.OnMouseMove(new Point(e.X, e.Y - row.Offset), scroll);
				}

				updateSurface = true;
			}

			if (updateSurface)
				UpdateSurface();
		}

		private void RenderCanvas_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Right)
			{
				Mouse.OverrideCursor = null;
				Input.IsDrag = false;
			}

			if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				Input.IsMeasure = false;
			}
		}

		private void RenderCanvas_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Right)
			{
				Mouse.OverrideCursor = Cursors.ScrollWE;
				Input.IsDrag = true;
				Input.DragPosition = e.Location;
			}
			else if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
				{
					MouseShowPopup(e);
				}
				else
				{
					Input.IsMeasure = true;
					long time = scroll.PixelToTime(e.X).Start;
					Input.MeasureInterval.Start = time;
					Input.MeasureInterval.Finish = time;

					Input.IsSelect = true;
					Input.SelectStartPosition = e.Location;
					MouseClickLeft(e);
				}
			}
		}

		private void ScrollBar_Scroll(object sender, ScrollEventArgs e)
		{
			scroll.ViewUnit.Left = scrollBar.Value;
			scroll.ViewUnit.Normalize();
			UpdateSurface();
		}

		const double ZoomSpeed = 1.2 / 120.0;

		private void RenderCanvas_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Delta != 0)
			{
				double delta = e.Delta * ZoomSpeed;
				double scale = delta > 0.0 ? 1 / delta : -delta;

				double ratio = (double)e.X / surface.RenderCanvas.Width;

				double prevWidth = scroll.ViewUnit.Width;
				scroll.ViewUnit.Width *= scale;
				scroll.ViewUnit.Left += (prevWidth - scroll.ViewUnit.Width) * ratio;
				scroll.ViewUnit.Normalize();

				ThreadRow row = GetRow(e.Y);
				if (row != null)
				{
					row.OnMouseMove(new Point(e.X, e.Y - row.Offset), scroll);
				}

				UpdateBar();
				UpdateSurface();
			}
		}

		private void UpdateSurface()
		{
			surface.Update();
		}

		private void UpdateBar()
		{
			scrollBar.Value = scroll.ViewUnit.Left;
			scrollBar.Maximum = 1.0 - scroll.ViewUnit.Width;
			scrollBar.ViewportSize = scroll.ViewUnit.Width;
		}

		const int SelectionBorderCount = 3;
		const double SelectionBorderStep = 0.75;

		void DrawSelection(DirectX.DirectXCanvas canvas)
		{
			foreach (Selection selection in SelectionList)
			{
				if (selection.Frame != null)
				{
					ThreadRow row = id2row[selection.Frame.Header.ThreadIndex];

					Durable intervalTime = selection.Node == null ? (Durable)selection.Frame.Header : (Durable)selection.Node.Entry;
					Interval intervalPx = scroll.TimeToPixel(intervalTime);

					Rect rect = new Rect(intervalPx.Left, row.Offset + 2.0 * RenderParams.BaseMargin, intervalPx.Width, row.Height - 4.0 * RenderParams.BaseMargin);

					for (int i = 0; i < SelectionBorderCount; ++i)
					{
						rect.Inflate(SelectionBorderStep, SelectionBorderStep);
						SelectionMesh.AddRect(rect, FrameSelection.Color);
					}
				}
			}

			SelectionMesh.Update(canvas.RenderDevice);
			canvas.Draw(SelectionMesh);
		}

		void DrawMeasure(DirectX.DirectXCanvas canvas)
		{
			if (Input.MeasureInterval.Start != Input.MeasureInterval.Finish)
			{
				Durable activeInterval = Input.MeasureInterval.Normalize();
				Interval pixelInterval = scroll.TimeToPixel(activeInterval);
				MeasureMesh.AddRect(new Rect(pixelInterval.Left, 0, pixelInterval.Width, scroll.Height), MeasureBackground);
				canvas.Text.Draw(new Point(pixelInterval.Left, scroll.Height * 0.5), activeInterval.DurationF3, Colors.White, TextAlignment.Center, pixelInterval.Width);

				MeasureMesh.Update(canvas.RenderDevice);
				canvas.Draw(MeasureMesh);
			}
		}

		static Size ToolTipMargin = new Size(4, 2);
		static Vector ToolTipOffset = new Vector(0, -3);

		void DrawHover(DirectXCanvas canvas)
		{
			if (!String.IsNullOrWhiteSpace(ToolTip.Text))
			{
				Size size = surface.Text.Measure(ToolTip.Text);

				Rect textArea = new Rect(Input.MousePosition.X - size.Width * 0.5 + ToolTipOffset.X, ToolTip.Rect.Top - size.Height + ToolTipOffset.Y, size.Width, size.Height);
				surface.Text.Draw(textArea.TopLeft, ToolTip.Text, Colors.White, TextAlignment.Left);

				textArea.Inflate(ToolTipMargin);
				HoverMesh.AddRect(textArea, HoverBackground);
			}

			if (!ToolTip.Rect.IsEmpty)
			{
				HoverLines.AddRect(ToolTip.Rect, FrameHover.Color);
			}

			HoverLines.Update(canvas.RenderDevice);
			canvas.Draw(HoverLines);

			HoverMesh.Update(canvas.RenderDevice);
			canvas.Draw(HoverMesh);
		}

		void OnDraw(DirectX.DirectXCanvas canvas, DirectXCanvas.Layer layer)
		{
			if (layer == DirectXCanvas.Layer.Background)
			{
				canvas.Draw(BackgroundMesh);
			}

			Rect box = new Rect(0, 0, scroll.Width, scroll.Height);
			foreach (ThreadRow row in rows)
			{
				box.Height = row.Height;
				row.Render(canvas, scroll, layer, box);
				box.Y = box.Y + row.Height;
			}

			if (layer == DirectXCanvas.Layer.Foreground)
			{
				DrawSelection(canvas);
				DrawHover(canvas);
				DrawMeasure(canvas);
			}
		}

		void ThreadView_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			scroll.Width = surface.ActualWidth * RenderSettings.dpiScaleX;
			InitBackgroundMesh();
		}

		struct Selection
		{
			public EventFrame Frame { get; set; }
			public EventNode Node { get; set; }
		}

		List<Selection> SelectionList = new List<Selection>();

		private void ShowCallstacksButton_Click(object sender, RoutedEventArgs e)
		{
			scroll.DrawCallstacks = ShowCallstacksButton.IsChecked ?? false;
			UpdateSurface();
		}

		private void ShowSyncWorkButton_Click(object sender, RoutedEventArgs e)
		{
			scroll.SyncDraw = ShowSyncWorkButton.IsChecked.Value ? ThreadScroll.SyncDrawType.Work : ThreadScroll.SyncDrawType.Wait;
			UpdateSurface();
		}
	}
}
