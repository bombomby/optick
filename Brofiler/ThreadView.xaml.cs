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

namespace Profiler
{
	/// <summary>
	/// Interaction logic for ThreadView.xaml
	/// </summary>
	public partial class ThreadView : UserControl
	{
		ThreadCanvas canvas;

		public FrameGroup Group
		{
			set
			{
				if (canvas == null && value != null)
				{
					canvas = new ThreadCanvas(surface, scrollBar);
					AdornerLayer.GetAdornerLayer(surface).Add(canvas);
				}

				canvas.Group = value;

				Visibility visibility = value == null ? Visibility.Collapsed : Visibility.Visible;

				scrollBar.Visibility = visibility;
				search.Visibility = visibility;

				surface.Height = value == null ? 0.0 : canvas.Extent.Height;
				
			}
		}

		public void FocusOn(EventFrame frame)
		{
			Group = frame.Group;
			canvas.FocusOn(frame);
		}

		public ThreadView()
		{
			InitializeComponent();
			scrollBar.Visibility = Visibility.Collapsed;
			search.Visibility = Visibility.Collapsed;

			search.DelayedTextChanged += new SearchBox.DelayedTextChangedEventHandler(Search_DelayedTextChanged); 
		}

		void Search_DelayedTextChanged(string text)
		{
			if (canvas != null)
			{
				canvas.SetFilter(text);
			}
		}
	}

	public class ThreadCanvas : Adorner
	{
		public const int BlockHeight = 16;
		public const int HeaderHeight = 5;
		public const int RowHeight = 18;
		FrameGroup group;

		Durable timeRange;

		Double duration;

		Double currentPosition;
		Double Position
		{
			get { return currentPosition; }
			set
			{
				currentPosition = Math.Max(0.0, Math.Min(value, duration - Range));
			}
		}

		Double currentRange;
		Double Range
		{
			get { return currentRange; }
			set
			{
				currentRange = Math.Min(value, duration);

				if (Position + currentRange > duration)
					Position = duration - currentRange;
			}
		}

		public FrameGroup Group
		{
			set
			{
				if (group != value)
				{
					group = value;

					if (group != null)
					{
						timeRange = group.Board.TimeSlice;
						duration = timeRange.Duration;

						Range = 0;
						Position = 0;

						UpdateBar();
					}
				}
			}
		}

		class EventFilter
		{
			public HashSet<EventDescription> descriptions = new HashSet<EventDescription>();
			Dictionary<EventFrame, double> durations = new Dictionary<EventFrame,double>();

			HashSet<EventFrame> loading = new HashSet<EventFrame>();

			Object criticalSection = new Object();

			bool IsReady
			{
				get
				{
					lock (criticalSection)
					{
						return loading.Count == 0;
					}
				}
			}

			public delegate void LoadedEventHandler();
			public event LoadedEventHandler Loaded;

			DateTime lastUpdate = DateTime.Now;

			public void AddResult(EventFrame frame, double duration)
			{
				lock (criticalSection)
				{
					durations[frame] = duration;
					loading.Remove(frame);

					if (loading.Count == 0 || DateTime.Now.Subtract(lastUpdate).TotalSeconds > 1.0)
					{
						lastUpdate = DateTime.Now;
						Application.Current.Dispatcher.BeginInvoke(new Action(() => { Loaded(); }));
					}
				}
			}

			public bool TryGetFilteredFrameTime(EventFrame frame, out double result)
			{
				lock (criticalSection)
				{
					if (!durations.TryGetValue(frame, out result))
					{
						if (loading.Contains(frame))
							return false;

						if (frame.IsLoaded)
						{
							result = frame.CalculateFilteredTime(descriptions);
							AddResult(frame, result);
						}
						else
						{
							Application.Current.Dispatcher.BeginInvoke(new Action(() => {AddResult(frame, frame.CalculateFilteredTime(descriptions));}));
							return false;
						}
					}
				}
				return true;
			}

			void ProcessFrame(EventFrame frame, HashSet<EventDescription> filter)
			{
				frame.Load();
				double duration = frame.CalculateFilteredTime(filter);

				lock (criticalSection)
				{
					loading.Remove(frame);
					durations.Add(frame, duration);
				}
			}

			public static EventFilter Create(FrameGroup group, String text)
			{
				EventFilter result = new EventFilter();

				foreach (var desc in group.Board.Board)
				{
					if (desc.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) != -1)
						result.descriptions.Add(desc);
				}

				return result;
			}
		}


		EventFilter Filter { get; set; }

		public void SetFilter(String text)
		{
			if (group == null || String.IsNullOrEmpty(text))
			{
				if (Filter != null)
				{
					Filter = null;
					Refresh();
				}

				return;
			}

			Filter = EventFilter.Create(group, text);
			Filter.Loaded +=new EventFilter.LoadedEventHandler(Refresh);
			Refresh();
		}

		const double FocusFrameExtent = 2.0;

		EventFrame FocusedFrame { get; set; }

		bool IsFrameVisible(EventFrame frame)
		{
			double framePos = frame.Header.StartMS - timeRange.StartMS;
			return Position < framePos && framePos < Position + Range;
		}

		public void FocusOn(EventFrame frame)
		{
			if (!IsFrameVisible(frame))
			{

				double minRange = frame.Duration * FocusFrameExtent;
				if (Range < minRange)
				{
					Range = minRange;
				}

				Position = Durable.TicksToMs(frame.Header.Start - timeRange.Start) + (frame.Duration - Range) / 2;
			}

			FocusedFrame = frame;

			UpdateBar();
			Refresh();
		}

		void UpdateBar()
		{
			Bar.Value = Position;
			Bar.Maximum = duration - Range;
			Bar.ViewportSize = Range;
		}

		public Size Extent
		{
			get
			{
				if (group == null)
					return Size.Empty;

				return new Size(duration, group.Threads.Count * RowHeight + HeaderHeight);
			}
		}
		public ScrollBar Bar { get; set; }

		public ThreadCanvas(UIElement parent, ScrollBar bar) : base(parent)
		{
			Bar = bar;

			Bar.Minimum = 0;
			Bar.SmallChange = 33.0;
			Bar.LargeChange = Bar.SmallChange * 10.0;

			Binding binding = new Binding("IsVisible");
			binding.Source = parent;
			binding.Converter = new BooleanToVisibilityConverter();
			base.SetBinding(VisibilityProperty, binding);

			Bar.Scroll += new ScrollEventHandler(Bar_Scroll);

			PreviewMouseWheel += new MouseWheelEventHandler(ThreadCanvas_PreviewMouseWheel);
			MouseLeftButtonUp += new MouseButtonEventHandler(ThreadCanvas_MouseLeftButtonUp);
			MouseLeftButtonDown += new MouseButtonEventHandler(ThreadCanvas_MouseLeftButtonDown);
			MouseMove += new MouseEventHandler(ThreadCanvas_MouseMove);
		}

		double DrawSpaceToTimeLine(double value)
		{
			return Position + Range * value / AdornedElement.RenderSize.Width;
		}

		double TimeLineToDrawSpace(double value)
		{
			return (value - Position) * AdornedElement.RenderSize.Width / Range;
		}

		void ThreadCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (isSelectionScopeDrag && selectedScopes.Count > 0)
			{
				SelectedScope scope = selectedScopes[selectedScopes.Count - 1];
				double pos = DrawSpaceToTimeLine(e.GetPosition(this).X);
				scope.Finish = pos;

				Refresh();
			}
		}

		bool isSelectionScopeDrag = false;
		Point selectionScopeDragStart;

		void ThreadCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			isSelectionScopeDrag = true;

			selectionScopeDragStart = e.GetPosition(this);
			double pos = DrawSpaceToTimeLine(selectionScopeDragStart.X);

			if (!Keyboard.IsKeyDown(Key.LeftShift))
				selectedScopes.Clear();

			selectedScopes.Add(new SelectedScope() { Start = pos, Finish = pos });
		}

		void ThreadCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (group == null)
				return;

			Point pos = e.GetPosition(this);
			if (isSelectionScopeDrag)
			{
				isSelectionScopeDrag = false;
				if (selectionScopeDragStart != pos)
					return;
				else
				{
					selectedScopes.Clear();
					Refresh();
				}
			}

			int threadIndex = (int)((pos.Y - HeaderHeight) / RowHeight);

			if (threadIndex < group.Threads.Count)
			{
				var frames = group.Threads[threadIndex];

				long tick = Durable.MsToTick(timeRange.StartMS + Position + (pos.X / AdornedElement.RenderSize.Width) * Range);

				int index = BinarySearchClosestIndex(frames, tick);

				if (index > 0)
				{
					EventFrame frame = frames[index];
					Rect rect = CalculateRect(frame.Header, threadIndex);
					if (rect.Contains(pos))
					{
						TimeLine.FocusFrameEventArgs args = new TimeLine.FocusFrameEventArgs(TimeLine.FocusFrameEvent, frame);
						RaiseEvent(args);
					}
				}
			}
		}

		void Bar_Scroll(object sender, ScrollEventArgs e)
		{
			Position = Bar.Value;
			Refresh();
		}

		void Refresh()
		{
			AdornerLayer.GetAdornerLayer(AdornedElement).Update();
		}

		void ThreadCanvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta != 0)
			{
				double delta = e.Delta * ZoomSpeed;

				double ratio = e.GetPosition(this).X / AdornedElement.RenderSize.Width;

				double previousRange = Range;

				double scale = delta > 0.0 ? 1 / delta : -delta;

				Range *= scale;
				Position += (previousRange - Range) * ratio;

				Refresh();
				UpdateBar();
			}
		}

		const double ZoomSpeed = 0.01;

		static int BinarySearchClosestIndex(List<EventFrame> frames, long value)
		{
			if (frames.Count == 0)
				return -1;

			int left = 0;
			int right = frames.Count - 1;

			if (value <= frames[0].Header.Start)
				return left;

			if (value >= frames[frames.Count - 1].Header.Start)
				return right;

			while (left != right)
			{
				int index = (left + right + 1) / 2;

				if (frames[index].Header.Start > value)
					right = index - 1;
				else
					left = index;
			}

			return left;
		}

		Pen borderPen = new Pen(Brushes.Black, 0.25);
		Pen selectedPen = new Pen(Brushes.Black, 2);
		Pen fpsPen = new Pen(Brushes.Black, 0.5);
		Pen selectionScopePen = new Pen(Brushes.Black, 1.5);
		
		Vector textOffset = new Vector(1, 2);

		Typeface font = new Typeface(new FontFamily(), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

		Rect CalculateRect(Durable time, int line)
		{
			double scale = AdornedElement.RenderSize.Width / Range;
			return new Rect((time.StartMS - timeRange.StartMS - Position) * scale, line * RowHeight + HeaderHeight, time.Duration * scale, BlockHeight);
		}

		const double DrawThreshold = 3.0;
		const double FPSMark = 30.0;
		const double FPSMarkTime = 1000.0 / FPSMark;
		const double fpsTriangleSize = 6.0;

		void RenderFPSLines(System.Windows.Media.DrawingContext drawingContext)
		{
			double height = Extent.Height;
			double scale = AdornedElement.RenderSize.Width / Range;

			StreamGeometry geometry = new StreamGeometry();

			using (StreamGeometryContext geometryContext = geometry.Open())
			{
				for (int i = (int)(Position / FPSMarkTime); i <= (int)((Position + Range) / FPSMarkTime); ++i)
				{
					double posX = (i * FPSMarkTime - Position) * scale;
					geometryContext.BeginFigure(new Point(posX, fpsTriangleSize), true, true);
					geometryContext.LineTo(new Point(posX - fpsTriangleSize / 2, 0), false, false);
					geometryContext.LineTo(new Point(posX + fpsTriangleSize / 2, 0), false, false);
				}
			}

			drawingContext.DrawGeometry(Brushes.Gray, null, geometry);
		}

		class SelectedScope
		{
			public double Start { get; set; }
			public double Finish { get; set; }
		}

		List<SelectedScope> selectedScopes = new List<SelectedScope>();
		Brush selectionScopeBackground = new SolidColorBrush(Color.FromArgb(180, 55, 55, 55));
		CultureInfo culture = CultureInfo.GetCultureInfo("en-US");

		double CalculateIntersection(EventFrame frame, double start, double finish)
		{
			double left = Math.Max(frame.Header.StartMS - timeRange.StartMS, start);
			double right = Math.Min(frame.Header.FinishMS - timeRange.StartMS, finish);

			return Math.Max(0, right - left);
		}

		void RenderSelectedScopes(System.Windows.Media.DrawingContext drawingContext)
		{
			foreach (SelectedScope scope in selectedScopes)
			{
				if (Math.Abs(scope.Start - scope.Finish) < 1e-6)
					continue;

				double pos0 = Math.Min(scope.Start, scope.Finish);
				double pos1 = Math.Max(scope.Start, scope.Finish);

				double posStart = TimeLineToDrawSpace(pos0);
				double posFinish = TimeLineToDrawSpace(pos1);

				//drawingContext.DrawRectangle(Brushes.Gray, selectionScopePen, new Rect(new Point(posStart, HeaderHeight), new Point(posFinish, Extent.Height)));

				double midHeight = Extent.Height / 2;

				Rect area = new Rect(new Point(posStart, HeaderHeight), new Point(posFinish, Extent.Height));
				drawingContext.DrawRectangle(selectionScopeBackground, null, area);

				drawingContext.DrawLine(selectionScopePen, new Point(posStart, HeaderHeight), new Point(posStart, Extent.Height));
				drawingContext.DrawLine(selectionScopePen, new Point(posFinish, HeaderHeight), new Point(posFinish, Extent.Height));


				var intervals = CalculateFrameRange(pos0, pos1);
				for (int threadIndex = 0; threadIndex < intervals.Count; ++threadIndex)
				{
					var frames = group.Threads[threadIndex];

					double sum = 0;
					for (int index = intervals[threadIndex].Key; index <= intervals[threadIndex].Value; ++index)
					{
						sum += CalculateIntersection(frames[index], pos0, pos1);
					}

					FormattedText text = new FormattedText(String.Format("{0:0.000}", sum).Replace(',', '.'), culture, FlowDirection.LeftToRight, font, 12, Brushes.White);

					if (text.Width < area.Width)
					{
						drawingContext.DrawText(text, new Point((posStart + posFinish) / 2 - text.Width / 2, HeaderHeight + threadIndex * RowHeight) + textOffset);
					}
				}
			}
		}

		List<KeyValuePair<int, int>> CalculateFrameRange(double left, double right)
		{
			List<KeyValuePair<int, int>> result = new List<KeyValuePair<int,int>>();

			if (group == null)
				return result;

			var threads = group.Threads;

			long leftTick = Durable.MsToTick(left) + timeRange.Start;
			long rightTick = Durable.MsToTick(right) + timeRange.Start;

			for (int threadIndex = 0; threadIndex < threads.Count; ++threadIndex)
			{
				List<EventFrame> frames = threads[threadIndex];

				int leftIndex = BinarySearchClosestIndex(frames, leftTick);
				int rightIndex = BinarySearchClosestIndex(frames, rightTick);

				result.Add(new KeyValuePair<int, int>(leftIndex, rightIndex));
			}

			return result;
		}

		protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
		{
			if (group == null)
				return;

			Rect area = new Rect(0, 0, AdornedElement.RenderSize.Width, AdornedElement.RenderSize.Height);
			drawingContext.PushClip(new RectangleGeometry(area));

			drawingContext.DrawRectangle(Brushes.White, null, area);

			var threads = group.Threads;

			List<KeyValuePair<int, int>> intervals = CalculateFrameRange(Position, Position + Range);
			
			for (int threadIndex = 0; threadIndex < Math.Min(threads.Count, intervals.Count); ++threadIndex)
			{
				List<EventFrame> frames = threads[threadIndex];

				if (frames.Count > 0)
				{
					for (int i = intervals[threadIndex].Key; i <= intervals[threadIndex].Value; ++i)
					{
						EventFrame frame = frames[i];
						Rect rectangle = CalculateRect(frame.Header, threadIndex);

						bool isFilterReady = false;
						double filteredValue = 0.0;

						if (Filter != null)
						{
							isFilterReady = Filter.TryGetFilteredFrameTime(frame, out filteredValue);
						}

						drawingContext.DrawRectangle(Filter != null && isFilterReady ? Brushes.LimeGreen : Brushes.Gray, null, rectangle);

						if (Filter == null)
						{
							foreach (Entry entry in frame.Categories)
							{
								Rect entryRectangle = CalculateRect(entry, threadIndex);

								if (entryRectangle.Width < DrawThreshold)
									continue;

								drawingContext.DrawRectangle(entry.Description.Brush, null, entryRectangle);
							}
						}

						if (rectangle.Width > DrawThreshold)
						{
							drawingContext.DrawRectangle(null, borderPen, rectangle);

							String text = String.Empty;

							if (Filter != null)
							{
								if (isFilterReady)
								{
									double ratio = filteredValue / frame.Duration;
									text = String.Format("{0}ms", (int)filteredValue);
									drawingContext.DrawRectangle(Brushes.Tomato, null, new Rect(rectangle.X, rectangle.Y, rectangle.Width * ratio, rectangle.Height));
								}
								else
								{
									text = "?";
								}
							}
							else
							{
								text = String.Format("{0}ms", (int)frame.Duration);
							}

							if (!String.IsNullOrEmpty(text))
							{
								FormattedText timingText = new FormattedText(text, culture, FlowDirection.LeftToRight, font, 12, Brushes.Black);
								if (timingText.Width < rectangle.Width)
								{
									drawingContext.DrawText(timingText, rectangle.Location + textOffset);
								}
							}
						}
					}
				}
			}

			if (FocusedFrame != null)
			{
				Rect focusedRectangle = CalculateRect(FocusedFrame.Header, FocusedFrame.Header.ThreadIndex);
				drawingContext.DrawRectangle(null, selectedPen, focusedRectangle);
			}

			RenderFPSLines(drawingContext);
			RenderSelectedScopes(drawingContext);

			drawingContext.Pop();

			base.OnRender(drawingContext);
		}

		
	}
}
