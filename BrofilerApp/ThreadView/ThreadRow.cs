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

namespace Profiler
{
	public static class RenderParams
	{
		private static double baseHeight = 16.0;
		public static double BaseHeight { get { return baseHeight * DirectX.RenderSettings.dpiScaleX; } }
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

		public static Interval Unit = new Interval(0.0, 1.0);
	}

	public class ThreadScroll
	{
		public Durable TimeSlice { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }

		public Interval ViewUnit = Interval.Unit;

		public Durable ViewTime { get { return UnitToTime(ViewUnit); } }

		public double Zoom { get { return 1.0 / ViewUnit.Width; } }

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
			return new Tick() { Start = TimeSlice.Start + (long)(unit * (TimeSlice.Finish - TimeSlice.Start)) };
		}

		public Durable UnitToTime(Interval unit)
		{
			long duration = TimeSlice.Finish - TimeSlice.Start;
			return new Durable(TimeSlice.Start + (long)(ViewUnit.Left * duration), TimeSlice.Start + (long)(ViewUnit.Right * duration));
		}

		public CallStackReason DrawCallstacks { get; set; }

		public enum SyncDrawType
		{
			Wait,
			Work,
		}

		public SyncDrawType SyncDraw { get; set; }
	}

	public abstract class ThreadRow
	{
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
	}

	public class HeaderThreadRow : ThreadRow
	{
		public override double Height { get { return RenderParams.BaseHeight; } }
		public override string Name { get { return String.Empty; } }

		public Color GradientTop { get; set; }
		public Color GradientBottom { get; set; }
		public Color SplitLines { get; set; }
		public Color TextColor { get; set; }
		public HeaderThreadRow(FrameGroup group)
		{
			Group = group;
		}

		DirectX.Mesh BackgroundMeshLines { get; set; }
		DirectX.Mesh BackgroundMeshTris { get; set; }

		public override void BuildMesh(DirectX.DirectXCanvas canvas, ThreadScroll scroll)
		{
			DirectX.DynamicMesh builder = canvas.CreateMesh();
			builder.Geometry = DirectX.Mesh.GeometryType.Lines;

			foreach (EventFrame frame in Group.MainThread.Events)
			{
				double x = scroll.TimeToUnit(frame.Header).Left;
				builder.AddLine(new Point(x, 0.0), new Point(x, 1.0), SplitLines);
			}
			BackgroundMeshLines = builder.Freeze(canvas.RenderDevice);

			DirectX.DynamicMesh builderHeader = canvas.CreateMesh();
			builderHeader.AddRect(new Rect(0.0, 0.0, 1.0, (Height - RenderParams.BaseMargin) / scroll.Height), new Color[] { GradientTop, GradientTop, GradientBottom, GradientBottom });
			BackgroundMeshTris = builderHeader.Freeze(canvas.RenderDevice);
		}

		public override void Render(DirectXCanvas canvas, ThreadScroll scroll, DirectXCanvas.Layer layer, Rect box)
		{
			if (layer == DirectXCanvas.Layer.Foreground)
			{
				Matrix world = new Matrix(scroll.Zoom, 0.0, 0.0, 1.0, -scroll.ViewUnit.Left * scroll.Zoom, 0.0);

				BackgroundMeshLines.WorldTransform = world;
				BackgroundMeshTris.WorldTransform = world;

				canvas.Draw(BackgroundMeshTris);
				canvas.Draw(BackgroundMeshLines);

				Data.Utils.ForEachInsideInterval(Group.MainThread.Events, scroll.ViewTime, frame =>
				{
					Interval interval = scroll.TimeToPixel(frame.Header);

					String text = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} ms", frame.Header.Duration);

					// 2 times to emulate "bold"
					for (int i = 0; i < 2; ++i)
						canvas.Text.Draw(new Point(interval.Left, Offset), text, TextColor, TextAlignment.Center, interval.Width);
				});
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
