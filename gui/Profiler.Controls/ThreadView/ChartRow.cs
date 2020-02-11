using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Profiler.Data;
using Profiler.DirectX;
using System.Windows.Media;

namespace Profiler.Controls
{
	public class ChartRow : ThreadRow
	{
		public override double Height => 64.0;

		private String ChartName { get; set; }
		public override string Name => ChartName;

		public override void ApplyFilter(DirectXCanvas canvas, ThreadScroll scroll, HashSet<EventDescription> descriptions)
		{
			throw new NotImplementedException();
		}

		public override void OnMouseClick(Point point, ThreadScroll scroll)
		{
			//throw new NotImplementedException();
		}

		public override void OnMouseHover(Point point, ThreadScroll scroll, List<object> dataContext)
		{
			//throw new NotImplementedException();
		}

		List<Tick> Timestamps { get; set; }
		List<Entry> Entries { get; set; }

		List<Mesh> ChartMeshes { get; set; }

		public class Entry
		{
			public Entry(int capacity)
			{
				Values = new List<double>(capacity);
			}

			public String Name { get; set; }
			public Color Fill { get; set; }
			public Color Stroke { get; set; }
			public List<Double> Values { get; set; }
		}

		public double MaxValue { get; set; }

		public ChartRow(String name, List<Tick> timestamps, List<Entry> entries, double maxValue)
		{
			ChartName = name;
			Timestamps = timestamps;
			Entries = entries;
			MaxValue = maxValue;
			Header = new ThreadNameView() { DataContext = this };
		}

		const int DIPSpltCount = 64;

		const float GradientColorShade = 0.66f;

		public override void BuildMesh(DirectXCanvas canvas, ThreadScroll scroll)
		{
			if (Timestamps.Count < 2)
				return;

			DirectX.ComplexDynamicMesh builder = new ComplexDynamicMesh(canvas, DIPSpltCount);

			List<Color[]> entryColors = new List<Color[]>();
			foreach (Entry entry in Entries)
			{
				Color color = entry.Fill;
				Color gradColor = DirectX.Utils.MultiplyColor(color, GradientColorShade);
				//entryColors.Add(new Color[] { leftColor, rightColor, rightColor, leftColor });
				entryColors.Add(new Color[] { color, color, gradColor, gradColor });
			}

			double left = scroll.TimeToUnit(Timestamps[0]);

			for (int i = 0; i < Timestamps.Count - 1; ++i)
			{
				double right = scroll.TimeToUnit(Timestamps[i+1]);

				double bottom = 0.0;
				for (int entryIndex = 0; entryIndex < Entries.Count; ++entryIndex)
				{
					double val = Entries[entryIndex].Values[i];
					double height = val / MaxValue;

					if (height > 0.0)
					{
						builder.AddRect(new Rect(left, 1.0 - bottom - height, right - left, height), entryColors[entryIndex]);
						bottom += height;
					}
				}

				left = right;
			}

			ChartMeshes = builder.Freeze(canvas.RenderDevice);
		}

		public override void Render(DirectXCanvas canvas, ThreadScroll scroll, DirectXCanvas.Layer layer, Rect box)
		{
			Matrix world = GetWorldMatrix(scroll);

			if (layer == DirectXCanvas.Layer.Background)
			{
				ChartMeshes?.ForEach(mesh =>
				{
					mesh.WorldTransform = world;
					canvas.Draw(mesh);
				});
			}
		}

		public delegate void ChartHoverHandler(Point mousePos, Rect rect, String text);
		public event ChartHoverHandler ChartHover;

		public override void OnMouseMove(Point point, ThreadScroll scroll)
		{
			ITick tick = scroll.PixelToTime(point.X);

			int index = Data.Utils.BinarySearchClosestIndex(Timestamps, tick.Start);

			if (0 <= index && index + 1 < Timestamps.Count)
			{
				double leftPx = scroll.TimeToPixel(Timestamps[index]);
				double rightPx = scroll.TimeToPixel(Timestamps[index + 1]);

				Rect rect = new Rect(leftPx, Offset + RenderParams.BaseMargin, rightPx - leftPx, Height - RenderParams.BaseMargin);

				List<double> values = new List<double>();
				for (int entryIndex = 0; entryIndex < Entries.Count; ++entryIndex)
					values.Add(Entries[entryIndex].Values[index]);

				StringBuilder builder = new StringBuilder();
				builder.AppendFormat("NumCores [{0}]: ", values.Sum());
				for (int i = 0; i < values.Count; ++i)
				{
					if (i != 0)
						builder.Append("+");
					builder.Append(values[i]);
					//builder.AppendFormat("{0}({1})", values[i], entries[i].Name);
				}

				ChartHover?.Invoke(point, rect, builder.ToString());
			}
			else
			{
				ChartHover?.Invoke(point, new Rect(), null);
			}

		}
	}
}
