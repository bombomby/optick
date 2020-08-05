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
using System.Diagnostics;
using Profiler.Controls;
using System.Threading.Tasks;

namespace Profiler.Controls
{
	/// <summary>
	/// Interaction logic for ThreadView.xaml
	/// </summary>
	public partial class ThreadViewControl : UserControl
	{
		public ThreadScroll Scroll { get; set; } = new ThreadScroll();
		private List<ThreadRow> Rows = new List<ThreadRow>();


		private SolidColorBrush _optickBackground;
		public SolidColorBrush OptickBackground { get { if (_optickBackground == null) _optickBackground = FindResource("OptickBackground") as SolidColorBrush; return _optickBackground; } }

		private SolidColorBrush _optickAlternativeBackground;
		public SolidColorBrush OptickAlternativeBackground { get { if (_optickAlternativeBackground == null) _optickAlternativeBackground = FindResource("OptickAlternative") as SolidColorBrush; return _optickAlternativeBackground; } }

		private SolidColorBrush _frameSelection;
		public SolidColorBrush FrameSelection { get { if (_frameSelection == null) _frameSelection = FindResource("OptickFrameSelection") as SolidColorBrush; return _frameSelection; } }

		private SolidColorBrush _frameHover;
		public SolidColorBrush FrameHover { get { if (_frameHover == null) _frameHover = FindResource("OptickFrameHover") as SolidColorBrush; return _frameHover; } }

		Color MeasureBackground;
		Color HoverBackground;


		void InitColors()
		{
			MeasureBackground = Color.FromArgb(100, 0, 0, 0);
			HoverBackground = Color.FromArgb(170, 0, 0, 0);
		}

		public bool ShowThreadHeaders { get; set; } = true;
		public bool ShowFrameLines { get; set; } = true;

		Mesh BackgroundMesh { get; set; }
		Mesh ForegroundMesh { get; set; }

		public delegate void HighlightFrameEventHandler(object sender, HighlightFrameEventArgs e);
        public static readonly RoutedEvent HighlightFrameEvent = EventManager.RegisterRoutedEvent("HighlightFrameEvent", RoutingStrategy.Bubble, typeof(HighlightFrameEventHandler), typeof(ThreadViewControl));

		public void UpdateRows()
		{
			ThreadList.Children.Clear();
			ThreadList.RowDefinitions.Clear();
			ThreadList.Margin = new Thickness(0, 0, 3, 0);
			double offset = 0.0;
			bool isAlternative = false;
			int rowCount = 0;
			for (int threadIndex = 0; threadIndex < Rows.Count; ++threadIndex)
			{
				ThreadRow row = Rows[threadIndex];
				row.Offset = offset;

				if (!row.IsVisible)
					continue;

				if (ShowThreadHeaders)
				{
					ThreadList.RowDefinitions.Add(new RowDefinition());

					Border border = new Border()
					{
						Height = row.Height / RenderSettings.dpiScaleY,
					};

					FrameworkElement header = row.Header;
					if (header != null && header.Parent != null && (header.Parent is Border))
						(header.Parent as Border).Child = null;
						
					border.Child = row.Header;
					border.Background = isAlternative ? OptickAlternativeBackground : OptickBackground;
					Grid.SetRow(border, rowCount);

					ThreadList.Children.Add(border);
				}
				isAlternative = !isAlternative;
				offset += row.Height;
				rowCount += 1;
			}

			Scroll.Height = offset;

			double controlHeight = offset / RenderSettings.dpiScaleY;
			surface.Height = controlHeight;
			surface.MaxHeight = controlHeight;

			InitBackgroundMesh();
		}

		public void InitRows(List<ThreadRow> rows, IDurable timeslice)
		{
			Rows = rows;

			if (rows.Count > 0)
			{
				Scroll.TimeSlice = new Durable(timeslice.Start, timeslice.Finish);
				Scroll.Width = surface.ActualWidth * RenderSettings.dpiScaleX;
				rows.ForEach(row => row.BuildMesh(surface, Scroll));
				rows.ForEach(row => row.ExpandChanged += Row_ExpandChanged);
				rows.ForEach(row => row.VisibilityChanged += Row_VisibilityChanged);
			}

			UpdateRows();
		}

		public void ReinitRows(List<ThreadRow> rows)
		{
			rows.ForEach(row => row.BuildMesh(surface, Scroll));
			UpdateRowsAsync();
		}

		private void Row_ExpandChanged(ThreadRow row)
		{
			//Task.Run(()=>
			//{
				row.BuildMesh(surface, Scroll);
				UpdateRowsAsync();
			//});
		}

		private void UpdateRowsAsync()
		{
			Application.Current.Dispatcher.BeginInvoke(new Action(() => UpdateRows()));
		}

		private void Row_VisibilityChanged(ThreadRow row)
		{
			//throw new NotImplementedException();
		}

		private void InitBackgroundMesh()
		{
			if (BackgroundMesh != null)
				BackgroundMesh.Dispose();

			DynamicMesh backgroundBuilder = surface.CreateMesh();
			backgroundBuilder.Projection = Mesh.ProjectionType.Pixel;

			double offset = 0.0;
			bool isAlternative = false;

			for (int threadIndex = 0; threadIndex < Rows.Count; ++threadIndex)
			{
				ThreadRow row = Rows[threadIndex];
				if (!row.IsVisible)
					continue;

				backgroundBuilder.AddRect(new Rect(0.0, offset, Scroll.Width, row.Height), isAlternative ? OptickAlternativeBackground.Color : OptickBackground.Color);
				isAlternative = !isAlternative;

				offset += row.Height;
			}

			BackgroundMesh = backgroundBuilder.Freeze(surface.RenderDevice);
		}

		public void InitForegroundLines(List<ITick> lines)
		{
			if (ForegroundMesh != null)
				ForegroundMesh.Dispose();

			DynamicMesh builder = surface.CreateMesh();
			builder.Geometry = Mesh.GeometryType.Lines;

			// Adding Frame separators
			if (lines != null)
			{
				foreach (ITick line in lines)
				{
					double x = Scroll.TimeToUnit(line);
					builder.AddLine(new Point(x, 0.0), new Point(x, 1.0), OptickAlternativeBackground.Color);
				}
			}

			ForegroundMesh = builder.Freeze(surface.RenderDevice);
		}

		DynamicMesh SelectionMesh;
		DynamicMesh HoverMesh;
		DynamicMesh HoverLines;
		DynamicMesh MeasureMesh;
		public TooltipInfo ToolTipPanel { get; set; }

		public class TooltipInfo
		{
			public String Text;
			public Rect Rect;

			public void Reset()
			{
				Text = String.Empty;
				Rect = new Rect();
			}
		}

		const double DefaultFrameZoom = 1.10;

        public void Highlight(IEnumerable<Selection> items, bool focus = true)
        {
            SelectionList = new List<Selection>(items);

            if (focus)
            {
                foreach (Selection s in items)
                {
                    Interval interval = Scroll.TimeToUnit(s.Focus != null ? s.Focus : (IDurable)s.Frame);
                    if (!Scroll.ViewUnit.IsValid || (!Scroll.ViewUnit.Contains(interval) && !interval.Contains(Scroll.ViewUnit)))
                    {
                        Scroll.ViewUnit.Width = interval.Width * DefaultFrameZoom;
                        Scroll.ViewUnit.Left = interval.Left - (Scroll.ViewUnit.Width - interval.Width) * 0.5;
                        Scroll.ViewUnit.Normalize();
                        UpdateBar();
                    }
                }
            }

            UpdateSurface();
        }

		public ThreadViewControl()
		{
			InitializeComponent();

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
			ToolTipPanel?.Reset();
			UpdateSurface();
		}

		public delegate void OnShowPopupHandler(List<Object> dataContext);
		public event OnShowPopupHandler OnShowPopup;

		private void MouseShowPopup(System.Windows.Forms.MouseEventArgs args)
		{
			System.Drawing.Point e = new System.Drawing.Point(args.X, args.Y);
			List<Object> dataContext = new List<object>();
			foreach (ThreadRow row in Rows)
			{
				if (row.Offset <= e.Y && e.Y <= row.Offset + row.Height)
				{
					row.OnMouseHover(new Point(e.X, e.Y - row.Offset), Scroll, dataContext);
				}
			}
			OnShowPopup?.Invoke(dataContext);
		}

		private void MouseClickLeft(System.Windows.Forms.MouseEventArgs args)
		{
			System.Drawing.Point e = new System.Drawing.Point(args.X, args.Y);
			foreach (ThreadRow row in Rows)
			{
				if (row.Offset <= e.Y && e.Y <= row.Offset + row.Height)
				{
					row.OnMouseClick(new Point(e.X, e.Y - row.Offset), Scroll);
				}
			}
		}

		ThreadRow GetRow(double posY)
		{
			foreach (ThreadRow row in Rows)
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

				double deltaUnit = Scroll.PixelToUnitLength(deltaPixel);
				Scroll.ViewUnit.Left -= deltaUnit;
				Scroll.ViewUnit.Normalize();

				UpdateBar();
				updateSurface = true;

				Input.DragPosition = e.Location;
			}
			else if (Input.IsMeasure)
			{
				Input.MeasureInterval.Finish = Scroll.PixelToTime(e.X).Start;
				updateSurface = true;
			}
			else
			{
				ThreadRow row = GetRow(e.Y);
				if (row != null)
				{
					row.OnMouseMove(new Point(e.X, e.Y - row.Offset), Scroll);
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
				if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && !IsMeasuring())
				{
					Input.IsSelect = true;
					Input.SelectStartPosition = e.Location;
					MouseClickLeft(e);
				}
				Input.IsMeasure = false;
			}
		}

		private bool IsMeasuring()
		{
			Interval interval = Scroll.TimeToPixel(Input.MeasureInterval);
			return Math.Abs(interval.Right - interval.Left) > 3;
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
					long time = Scroll.PixelToTime(e.X).Start;
					Input.MeasureInterval.Start = time;
					Input.MeasureInterval.Finish = time;
				}
			}
		}

		private void ScrollBar_Scroll(object sender, ScrollEventArgs e)
		{
			Scroll.ViewUnit.Left = scrollBar.Value;
			Scroll.ViewUnit.Normalize();
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

				double prevWidth = Scroll.ViewUnit.Width;
				Scroll.ViewUnit.Width *= scale;
				Scroll.ViewUnit.Left += (prevWidth - Scroll.ViewUnit.Width) * ratio;
				Scroll.ViewUnit.Normalize();

				ThreadRow row = GetRow(e.Y);
				if (row != null)
				{
					row.OnMouseMove(new Point(e.X, e.Y - row.Offset), Scroll);
				}

				UpdateBar();
				UpdateSurface();
			}
		}

		public void UpdateSurface()
		{
			surface.Update();
		}

		private void UpdateBar()
		{
			scrollBar.Value = Scroll.ViewUnit.Left;
			scrollBar.Maximum = 1.0 - Scroll.ViewUnit.Width;
			scrollBar.ViewportSize = Scroll.ViewUnit.Width;
		}

		const int SelectionBorderCount = 3;
		const double SelectionBorderStep = 0.75;

		void DrawSelection(DirectX.DirectXCanvas canvas)
		{
			foreach (Selection selection in SelectionList)
			{
				if (selection.Frame != null && selection.Row != null)
				{
					ThreadRow row = selection.Row;

					IDurable intervalTime = selection.Focus == null ? selection.Frame.Header : selection.Focus;
					Interval intervalPx = Scroll.TimeToPixel(intervalTime);

					Rect rect = new Rect(intervalPx.Left, row.Offset /*+ 2.0 * RenderParams.BaseMargin*/, intervalPx.Width, row.Height /*- 4.0 * RenderParams.BaseMargin*/);

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
			if (IsMeasuring())
			{
				Durable activeInterval = Input.MeasureInterval.Normalize();
				Interval pixelInterval = Scroll.TimeToPixel(activeInterval);
				MeasureMesh.AddRect(new Rect(pixelInterval.Left, 0, pixelInterval.Width, Scroll.Height), MeasureBackground);
				canvas.Text.Draw(new Point(pixelInterval.Left, Scroll.Height * 0.5), activeInterval.DurationF3, Colors.White, TextAlignment.Center, pixelInterval.Width);

				MeasureMesh.Update(canvas.RenderDevice);
				canvas.Draw(MeasureMesh);
			}
		}

		static Size ToolTipMargin = new Size(4, 2);
		static Vector ToolTipOffset = new Vector(0, -3);

		void DrawHover(DirectXCanvas canvas)
		{
			if (Input.IsDrag)
				return;

			if (ToolTipPanel != null && !String.IsNullOrWhiteSpace(ToolTipPanel.Text))
			{
				Size size = surface.Text.Measure(ToolTipPanel.Text);

				Rect textArea = new Rect(Input.MousePosition.X - size.Width * 0.5 + ToolTipOffset.X, ToolTipPanel.Rect.Top - size.Height + ToolTipOffset.Y, size.Width, size.Height);
				surface.Text.Draw(textArea.TopLeft, ToolTipPanel.Text, Colors.White, TextAlignment.Left);

				textArea.Inflate(ToolTipMargin);
				HoverMesh.AddRect(textArea, HoverBackground);
			}

			if (ToolTipPanel != null && !ToolTipPanel.Rect.IsEmpty)
			{
				HoverLines.AddRect(ToolTipPanel.Rect, FrameHover.Color);
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

			Rect box = new Rect(0, 0, Scroll.Width, Scroll.Height);
			foreach (ThreadRow row in Rows)
			{
				box.Height = row.Height;
				row.Render(canvas, Scroll, layer, box);
				box.Y = box.Y + row.Height;
			}

			if (layer == DirectXCanvas.Layer.Foreground)
			{
				if (ShowFrameLines && ForegroundMesh != null)
				{
					Matrix world = new Matrix(Scroll.Zoom, 0.0, 0.0, 1.0, -Scroll.ViewUnit.Left * Scroll.Zoom, 0.0);
					ForegroundMesh.WorldTransform = world;
					canvas.Draw(ForegroundMesh);
				}

				DrawSelection(canvas);
				DrawHover(canvas);
				DrawMeasure(canvas);
			}
		}

		void ThreadView_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Scroll.Width = surface.ActualWidth * RenderSettings.dpiScaleX;
			InitBackgroundMesh();
		}

		public struct Selection
		{
			public EventFrame Frame { get; set; }
			public IDurable Focus { get; set; }
			public ThreadRow Row { get; set; }
		}

		List<Selection> SelectionList = new List<Selection>();
	}
}
