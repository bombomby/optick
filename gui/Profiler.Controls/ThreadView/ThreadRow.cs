using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Profiler.Data;
using System.Windows.Media;
using Profiler.DirectX;
using System.Windows.Forms;
using Profiler.InfrastructureMvvm;
using System.Threading;

namespace Profiler.Controls
{
	public static class RenderParams
	{
		private static double baseHeight = 16.0;
		public static double BaseHeight { get { return baseHeight * DirectX.RenderSettings.dpiScaleY; } }
		private static double baseMargin = 0.75;
		public static double BaseMargin { get { return baseMargin * DirectX.RenderSettings.dpiScaleY; } }
	}

	public struct Interval
	{
		public double Left;
		public double Width;

		public double Right { get { return Left + Width; } }

		public void Normalize()
		{
			Width = Math.Max(0.0, Math.Min(Width, 1.0));
			Left = Math.Max(0.0, Math.Min(Left, 1.0 - Width));
		}

		public Interval(double left, double width)
		{
			Left = left;
			Width = width;
		}

		public bool Intersect(Interval other)
		{
			return Right >= other.Left && other.Right >= Left;
		}

		public bool Contains(Interval other)
		{
			return Left <= other.Left && other.Right <= Right;
		}

		public static Interval Unit = new Interval(0.0, 1.0);
		public static Interval Zero = new Interval(0.0, 0.0);

		public bool IsValid { get { return Width > double.Epsilon; } }
	}

	public class ThreadScroll
	{
		public Durable TimeSlice { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }

		public Interval ViewUnit = Interval.Zero;

		public Durable ViewTime { get { return UnitToTime(ViewUnit); } }


		const double MIN_WIDTH = 0.000001;
		public double Zoom { get { return 1.0 / Math.Max(MIN_WIDTH, ViewUnit.Width); } }

		public double TimeToUnit(ITick tick)
		{
			double durationTicks = TimeSlice.Finish - TimeSlice.Start;
			return (tick.Start - TimeSlice.Start) / durationTicks;
		}

		public Interval TimeToUnit(IDurable d)
		{
			double durationTicks = TimeSlice.Finish - TimeSlice.Start;
			return new Interval((d.Start - TimeSlice.Start) / durationTicks, (d.Finish - d.Start) / durationTicks);
		}

		public Interval TimeToPixel(IDurable d)
		{
			Interval unit = TimeToUnit(d);
			double scale = Width * Zoom;
			return new Interval((unit.Left - ViewUnit.Left) * scale, unit.Width * scale);
		}

		public double TimeToPixel(ITick t)
		{
			double unit = TimeToUnit(t);
			double scale = Width * Zoom;
			return (unit - ViewUnit.Left) * scale;
		}

		public double PixelToUnitLength(double pixelX)
		{
			return (pixelX / Width) * ViewUnit.Width;
		}

		public ITick PixelToTime(double pixelX)
		{
			double unit = ViewUnit.Left + PixelToUnitLength(pixelX);
			return TimeSlice != null ? new Tick() { Start = TimeSlice.Start + (long)(unit * (TimeSlice.Finish - TimeSlice.Start)) } : new Tick();
		}

		public Durable UnitToTime(Interval unit)
		{
			long duration = TimeSlice.Finish - TimeSlice.Start;
			return new Durable(TimeSlice.Start + (long)(ViewUnit.Left * duration), TimeSlice.Start + (long)(ViewUnit.Right * duration));
		}

		public CallStackReason DrawCallstacks { get; set; }
		public bool DrawDataTags { get; set; }

		public enum SyncDrawType
		{
			Wait,
			Work,
		}

		public SyncDrawType SyncDraw { get; set; }
	}

	public abstract class ThreadRow : BaseViewModel
	{
		public FrameworkElement Header { get; set; }

		public double Offset { get; set; }
		public abstract double Height { get; }
		public abstract String Name { get; }
		public FrameGroup Group { get; set; }

		public abstract void Render(DirectX.DirectXCanvas canvas, ThreadScroll scroll, DirectXCanvas.Layer layer, Rect box);
		public abstract void BuildMesh(DirectX.DirectXCanvas canvas, ThreadScroll scroll);

		public abstract void OnMouseMove(Point point, ThreadScroll scroll);
		public abstract void OnMouseHover(Point point, ThreadScroll scroll, List<object> dataContext);
		public abstract void OnMouseClick(Point point, ThreadScroll scroll);
		public abstract void ApplyFilter(DirectX.DirectXCanvas canvas, ThreadScroll scroll, HashSet<EventDescription> descriptions);

		public Matrix GetWorldMatrix(ThreadScroll scroll, bool useMargin = true)
		{
			return new Matrix(scroll.Zoom, 0.0, 0.0, (Height - (useMargin ? 2.0 * RenderParams.BaseMargin : 0.0)) / scroll.Height,
							  -(scroll.ViewUnit.Left * scroll.Zoom),
							  (Offset + (useMargin ? 1.0 * RenderParams.BaseMargin : 0.0)) / scroll.Height);
		}

		public delegate void OnVisibilityChangedHandler(ThreadRow row);
		public event OnVisibilityChangedHandler VisibilityChanged;

		private bool _isVisible = true;
		public bool IsVisible
		{
			get { return _isVisible; }
			set { SetProperty(ref _isVisible, value); VisibilityChanged?.Invoke(this); }
		}

		public delegate void OnExpanedChangedHandler(ThreadRow row);
		public event OnExpanedChangedHandler ExpandChanged;

		protected bool _isExpanded = true;
		public bool IsExpanded
		{
			get { return _isExpanded; }
			set { SetProperty(ref _isExpanded, value); ExpandChanged?.Invoke(this); }
		}

		private long _busyCounter = 0;
		public bool IsBusy
		{
			get { return Interlocked.Read(ref _busyCounter) > 0; }
		}

		public void SetBusy(bool isBusy)
		{
			bool changed = false;
			if (isBusy)
			{
				if (Interlocked.Increment(ref _busyCounter) == 1)
					changed = true;
			}
			else
			{
				if (Interlocked.Decrement(ref _busyCounter) == 0)
					changed = true;
			}
					
			if (changed)
			{
				System.Windows.Application.Current.Dispatcher.Invoke(new Action(() => OnPropertyChanged("IsBusy")));
			}
		}
	}

	public class HeaderThreadRow : ThreadRow
	{
		public static double DefaultHeaderHeight => RenderParams.BaseHeight * 1.25;
		public static double DefaultHeaderHeightDPI => DefaultHeaderHeight / RenderSettings.dpiScaleY;
		public override double Height { get { return DefaultHeaderHeight; } }
		public override string Name { get { return String.Empty; } }

		public Color GradientTop { get; set; }
		public Color GradientBottom { get; set; }
		public Color TextColor { get; set; }
		public Color TickColor { get; set; } = Colors.Gray;
		public HeaderThreadRow(FrameGroup group)
		{
			Group = group;
		}

		DirectX.Mesh BackgroundMeshLines { get; set; }
		DirectX.Mesh BackgroundMeshTris { get; set; }

		bool EnableTickers { get; set; }

		public override void BuildMesh(DirectX.DirectXCanvas canvas, ThreadScroll scroll)
		{
			DirectX.DynamicMesh builder = canvas.CreateMesh();
			builder.Geometry = DirectX.Mesh.GeometryType.Lines;

			double headerHeight = 1.0;//(Height - RenderParams.BaseMargin) / scroll.Height;

			// Adding Tickers
			if (EnableTickers)
			{
				for (double tick = Math.Ceiling(scroll.TimeSlice.StartMS); tick < Math.Ceiling(scroll.TimeSlice.FinishMS); tick += 1.0)
				{
					double longX = scroll.TimeToUnit(new Tick { Start = Durable.MsToTick(tick) });
					builder.AddLine(new Point(longX, headerHeight * 3.0 / 6.0), new Point(longX, headerHeight), TickColor);

					double medX = scroll.TimeToUnit(new Tick { Start = Durable.MsToTick(tick + 0.5) });
					builder.AddLine(new Point(medX, headerHeight * 4.0 / 6.0), new Point(medX, headerHeight), TickColor);


					for (double miniTick = 0.1; miniTick < 1.0; miniTick += 0.1)
					{
						double miniX = scroll.TimeToUnit(new Tick { Start = Durable.MsToTick(tick + miniTick) });
						builder.AddLine(new Point(miniX, headerHeight * 5.0 / 6.0), new Point(miniX, headerHeight), TickColor);
					}
				}
			}
			BackgroundMeshLines = builder.Freeze(canvas.RenderDevice);

			DirectX.DynamicMesh builderHeader = canvas.CreateMesh();
			builderHeader.AddRect(new Rect(0.0, 0.0, 1.0, headerHeight), new Color[] { GradientTop, GradientTop, GradientBottom, GradientBottom });
			BackgroundMeshTris = builderHeader.Freeze(canvas.RenderDevice);
		}

		public override void Render(DirectXCanvas canvas, ThreadScroll scroll, DirectXCanvas.Layer layer, Rect box)
		{
			if (layer == DirectXCanvas.Layer.Foreground)
			{
				Matrix world = GetWorldMatrix(scroll, false);

				//Matrix world = new Matrix(scroll.Zoom, 0.0, 0.0, 1.0, -scroll.ViewUnit.Left * scroll.Zoom, 0.0);

				if (BackgroundMeshTris != null)
				{
					BackgroundMeshTris.WorldTransform = world;
					canvas.Draw(BackgroundMeshTris);
				}

				if (BackgroundMeshLines != null)
				{
					BackgroundMeshLines.WorldTransform = world;
					canvas.Draw(BackgroundMeshLines);
				}

				double yOffset = Offset + (Height - RenderParams.BaseHeight) * 0.5;

				FrameList focusThread = Group.FocusThread;
				if (focusThread != null)
				{
					Data.Utils.ForEachInsideInterval(focusThread.Events, scroll.ViewTime, (frame, index) =>
					{
						Interval interval = scroll.TimeToPixel(frame);
						String text = String.Format(System.Globalization.CultureInfo.InvariantCulture, "Frame {0} ({1:0.0}ms)", (uint)index, frame.Duration);

						// 2 times to emulate "bold"
						for (int i = 0; i < 2; ++i)
							canvas.Text.Draw(new Point(interval.Left, yOffset), text, TextColor, TextAlignment.Center, interval.Width);
					});
				}
			}
		}

		public override void OnMouseMove(Point point, ThreadScroll scroll) { }
		public override void OnMouseHover(Point point, ThreadScroll scroll, List<object> dataContext) { }
		public override void OnMouseClick(Point point, ThreadScroll scroll) { }
		public override void ApplyFilter(DirectXCanvas canvas, ThreadScroll scroll, HashSet<EventDescription> descriptions) { }
	}

	class EventFilter
	{
		public FrameGroup Group { get; set; }
		public HashSet<EventDescription> Descriptions { get; set; }

		public List<Entry> Entries = new List<Entry>();

		Object criticalSection = new Object();

		public delegate void LoadedEventHandler();

		void Load(ThreadData data)
		{
			data.Events.ForEach(frame =>
			{
				foreach (EventDescription desc in Descriptions)
				{
					List<Entry> filteredEntries = frame.ShortBoard.Get(desc);

					if (filteredEntries != null)
					{
						lock (criticalSection)
						{
							Entries.AddRange(filteredEntries);
						}
					}

				}
			});

			lock (criticalSection)
			{
				Entries.Sort();
			}
		}


		public static EventFilter Create(ThreadData data, HashSet<EventDescription> descriptions)
		{
			if (descriptions == null)
				return null;

			EventFilter result = new EventFilter() { Descriptions = descriptions };
			result.Load(data);
			return result;
		}
	}
}
