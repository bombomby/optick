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

	struct RowRange
	{
		public double Offset { get; set; }
		public double Height { get; set; }
		public int MaxDepth { get; set; }

		public double Bottom { get { return Offset + Height; } }

		public bool Inside(double value)
		{
			return (Bottom >= value) && (value >= Offset);
		}
	}

	public class ThreadCanvas : Adorner
	{
		public const int BlockHeight = 14;
		public const int HeaderHeight = 10;
		public const int SpaceHeight = 2;
		public const int ThreadNameHeight = 14;
		FrameGroup group;

		List<RowRange> rows = new List<RowRange>();

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

						rows.Clear();
						double rowOffset = HeaderHeight;
						for (int threadIndex = 0; threadIndex < group.Threads.Count; ++threadIndex)
						{
							int maxDepth = group.Threads[threadIndex].Count > 0 ? GetRowMaxDepth(threadIndex) : -1;
							double height = maxDepth >= 0 ? (maxDepth + 1) * BlockHeight : 0.0;
							rows.Add(new RowRange() { Offset = rowOffset, Height = height, MaxDepth = maxDepth });
							rowOffset += maxDepth >= 0 ? SpaceHeight + height : 0.0;
						}

						UpdateBar();
					}
				}
			}
		}

		class EventFilter
		{
			public HashSet<Object> descriptions = new HashSet<Object>();
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

			void ProcessFrame(EventFrame frame, HashSet<Object> filter)
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

				return new Size(duration, rows.Count > 0.0 ? rows[rows.Count-1].Bottom : 0.0);
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

		int GetThreadIndex(double offset)
		{
			for (int i = 0; i < rows.Count; ++i)
			{
				if (rows[i].Inside(offset))
					return i;
			}
			return -1;
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

			int threadIndex = GetThreadIndex(pos.Y);

			if (0 <= threadIndex && threadIndex < group.Threads.Count)
			{
				var frames = group.Threads[threadIndex];

				long tick = Durable.MsToTick(timeRange.StartMS + Position + (pos.X / AdornedElement.RenderSize.Width) * Range);

				int index = BinarySearchClosestIndex(frames, tick);

				if (index > 0)
				{
					EventFrame frame = frames[index];
					Rect rect = CalculateRect(frame.Header, rows[threadIndex].Offset, rows[threadIndex].Height);
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
		Pen selectedPen = new Pen(Brushes.Black, 3);
		Pen fpsPen = new Pen(fpsBrush, 1.5);
		Pen selectionScopePen = new Pen(Brushes.Black, 1.5);
		Pen threadNamePen = new Pen(Brushes.Black, 0.5);
		
		Vector textOffset = new Vector(1, 0);

		Typeface fontDuration = new Typeface(new FontFamily(), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
		Typeface fontCategoryName = new Typeface(new FontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

		Rect CalculateRect(Durable time, double rowOffset, double height)
		{
			double scale = AdornedElement.RenderSize.Width / Range;
			return new Rect((time.StartMS - timeRange.StartMS - Position) * scale, rowOffset, time.Duration * scale, height);
		}

		const double DrawThreshold = 3.0;
		const double DrawTextThreshold = 16.0;
		const double FPSMark = 30.0;
		const double FPSMarkTime = 1000.0 / FPSMark;
		const double fpsTriangleSize = 8.0;

		static Brush fpsBrush = Brushes.DimGray;

		const double RenderFPSLineLimit = 1.0;

		void RenderFPSLines(System.Windows.Media.DrawingContext drawingContext, List<EventFrame> frames, KeyValuePair<int, int> interval)
		{
			double scale = AdornedElement.RenderSize.Width / Range;

			if (scale < RenderFPSLineLimit)
				return;

			double height = Extent.Height;

			StreamGeometry geometry = new StreamGeometry();

			using (StreamGeometryContext geometryContext = geometry.Open())
			{
				for (int i = interval.Key; i <= interval.Value; ++i)
				{
					EventFrame frame = frames[i];

					double posX = (frame.Header.StartMS - timeRange.StartMS - Position) * scale;
					Point point = new Point(posX, fpsTriangleSize);
					geometryContext.BeginFigure(point, true, true);
					geometryContext.LineTo(new Point(posX - fpsTriangleSize / 2, 0), false, false);
					geometryContext.LineTo(new Point(posX + fpsTriangleSize / 2, 0), false, false);

					drawingContext.DrawLine(fpsPen, new Point(posX, 0), new Point(posX, height - SpaceHeight));

					double maxTextWidth = Math.Max(frame.Duration * scale - 8, 0.0);

					if (maxTextWidth > DrawTextThreshold)
					{
						FormattedText text = new FormattedText(String.Format("{0:0.###}ms", frame.Duration).Replace(',', '.'), culture, FlowDirection.LeftToRight, fontDuration, 12, fpsBrush);
						text.MaxLineCount = 1;
						text.MaxTextWidth = maxTextWidth;
						text.Trimming = TextTrimming.None;
						drawingContext.DrawText(text, new Point(posX + maxTextWidth / 2 - text.Width / 2, -2));
					}
				}
			}

			drawingContext.DrawGeometry(fpsBrush, null, geometry);
		}

		class SelectedScope
		{
			public double Start { get; set; }
			public double Finish { get; set; }
		}

		List<SelectedScope> selectedScopes = new List<SelectedScope>();
		Brush selectionScopeBackground = new SolidColorBrush(Color.FromArgb(180, 55, 55, 55));
		Brush timingTextBackground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
		CultureInfo culture = CultureInfo.InvariantCulture;

		double CalculateIntersection(EventData frame, double start, double finish)
		{
			double left = Math.Max(frame.StartMS - timeRange.StartMS, start);
			double right = Math.Min(frame.FinishMS - timeRange.StartMS, finish);

			return Math.Max(0, right - left);
		}

		double CalculateIntersection(EventFrame frame, double start, double finish)
		{
			double sum = CalculateIntersection(frame.Header, start, finish);

			foreach (var entry in frame.Synchronization)
				sum -= CalculateIntersection(entry, start, finish);

			return Math.Max(0, sum);
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

					if (frames.Count > 0)
					{
						double sum = 0;

						if (intervals[threadIndex].Key != -1 && intervals[threadIndex].Value != -1 && frames.Count > 0)
						{
							for (int index = intervals[threadIndex].Key; index <= intervals[threadIndex].Value; ++index)
							{
								sum += CalculateIntersection(frames[index], pos0, pos1);
							}
						}

						FormattedText text = new FormattedText(String.Format("{0:0.000}", sum).Replace(',', '.'), culture, FlowDirection.LeftToRight, fontDuration, 12, Brushes.White);
						text.MaxLineCount = 1;
						text.MaxTextWidth = area.Width;
						text.Trimming = TextTrimming.None;
						drawingContext.DrawText(text, new Point((posStart + posFinish) / 2 - text.Width / 2, rows[threadIndex].Offset + (rows[threadIndex].Height - text.Height) / 2.0) + textOffset);
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

		const double NextLevelRatio = 0.5;

		void GenerateEventNode(EventNode node, Rect frameRect, int currentLevel, int maxDepth, List<KeyValuePair<Entry, Rect>> result)
		{
			Rect entryRectangle = CalculateRect(node.Entry, frameRect.Top, frameRect.Height);

			if (entryRectangle.Width < DrawThreshold)
				return;

			double ratio = (double)currentLevel / (maxDepth + 1);
			double offset = ratio * entryRectangle.Height;

			entryRectangle.Offset(new Vector(0, offset));
			entryRectangle.Height = entryRectangle.Height - offset;

			result.Add(new KeyValuePair<Entry, Rect>(node.Entry, entryRectangle));

			if (currentLevel + 1 <= maxDepth)
				foreach (EventNode child in node.Children)
					GenerateEventNode(child, frameRect, currentLevel + 1, maxDepth, result);
		}

		void OnRenderFrame(System.Windows.Media.DrawingContext drawingContext, EventFrame frame, double rowOffset, int maxDepth)
		{
			Rect rectangle = CalculateRect(frame.Header, rowOffset, (maxDepth + 1) * BlockHeight);

			bool isFilterReady = false;
			double filteredValue = 0.0;

			if (Filter != null)
			{
				isFilterReady = Filter.TryGetFilteredFrameTime(frame, out filteredValue);
			}

			drawingContext.DrawRectangle(Filter != null && isFilterReady ? Brushes.LimeGreen : Brushes.LightGray, null, rectangle);

			if (Filter == null)
			{
				List<KeyValuePair<Entry, Rect>> rects = new List<KeyValuePair<Entry, Rect>>();

				foreach (EventNode node in frame.CategoriesTree.Children)
					GenerateEventNode(node, rectangle, 0, 1, rects);

				if (rects.Count > 0)
				{
					foreach (var item in rects)
					{
						drawingContext.DrawRectangle(item.Key.Description.Brush, borderPen, item.Value);
					}

					foreach (Entry entry in frame.Synchronization)
					{
						Rect rect = CalculateRect(entry, rowOffset, rectangle.Height);
						if (rect.Width > DrawThreshold)
						{
							drawingContext.DrawRectangle(entry.Description.Brush, borderPen, rect);
						}
					}

					foreach (var item in rects)
					{
						if (item.Value.Width < DrawTextThreshold)
							continue;

						FormattedText categoryText = new FormattedText(item.Key.Description.Name, culture, FlowDirection.LeftToRight, fontCategoryName, 11, Brushes.Black, null, TextFormattingMode.Display);
						categoryText.MaxTextWidth = item.Value.Width - textOffset.X;
						categoryText.Trimming = TextTrimming.None;
						categoryText.MaxLineCount = 1;
						drawingContext.DrawText(categoryText, item.Value.Location + textOffset);
					}					
				}
			}

			if (rectangle.Width > DrawThreshold)
			{
				drawingContext.DrawRectangle(null, borderPen, rectangle);

				double value = frame.Duration;

				if (Filter != null)
				{
					if (isFilterReady)
					{
						double ratio = filteredValue / frame.Duration;
						value = filteredValue;
						drawingContext.DrawRectangle(Brushes.Tomato, null, new Rect(rectangle.X, rectangle.Y, rectangle.Width * ratio, rectangle.Height));
					}
				}

				if (Filter != null)
				{
					FormattedText timingText = new FormattedText(String.Format("{0:0.###}ms", value).Replace(',', '.'), culture, FlowDirection.LeftToRight, fontDuration, 12, Brushes.Black);
					timingText.MaxTextWidth = rectangle.Width;
					timingText.MaxLineCount = 1;
					timingText.Trimming = TextTrimming.None;

					Point point = new Point(rectangle.Left + 1, rectangle.Bottom - timingText.Height + 1);
					drawingContext.DrawText(timingText, point);
				}
			}
		}

		int MainMaxDepth = 1;
		int AdditionalMaxDepth = 0;

		int GetRowMaxDepth(int threadIndex)
		{
			return group.Board.MainThreadIndex == threadIndex ? MainMaxDepth : AdditionalMaxDepth;
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
				RowRange rowRange = rows[threadIndex];

				if (rowRange.MaxDepth >= 0 && frames.Count > 0)
				{
					for (int i = intervals[threadIndex].Key; i <= intervals[threadIndex].Value; ++i)
						OnRenderFrame(drawingContext, frames[i], rowRange.Offset, rowRange.MaxDepth);
				}
			}

			int mainThreadIndex = group.Board.MainThreadIndex;
			RenderFPSLines(drawingContext, group.Threads[mainThreadIndex], intervals[mainThreadIndex]);

			for (int threadIndex = 0; threadIndex < threads.Count; ++threadIndex)
			{
				FormattedText threadName = new FormattedText(group.Board.Threads[threadIndex].Name, culture, FlowDirection.LeftToRight, fontDuration, 12, Brushes.Black);
				RowRange rowRange = rows[threadIndex];

				if (rowRange.MaxDepth >= 0 && group.Threads[threadIndex].Count > 0)
				{
					Point threadOrigin = new Point(1, rowRange.Offset + (rowRange.Height - ThreadNameHeight) * 0.5);
					drawingContext.DrawRoundedRectangle(Brushes.White, threadNamePen, new Rect(threadOrigin.X, threadOrigin.Y, threadName.Width + 5, ThreadNameHeight), 4, 4);
					drawingContext.DrawText(threadName, threadOrigin + new Vector(2, 0));
				}
			}

			if (FocusedFrame != null)
			{
				RowRange rowRange = rows[FocusedFrame.Header.ThreadIndex];
				Rect focusedRectangle = CalculateRect(FocusedFrame.Header, rowRange.Offset, rowRange.Height);
				drawingContext.DrawRectangle(null, selectedPen, focusedRectangle);
			}

			RenderSelectedScopes(drawingContext);

			drawingContext.Pop();

			base.OnRender(drawingContext);
		}

		
	}
}
