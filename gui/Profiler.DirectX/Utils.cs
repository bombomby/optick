using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.DirectX
{
	public class Utils
	{
		public static SharpDX.Color Convert(System.Windows.Media.Color color)
		{
			return new SharpDX.Color(color.R, color.G, color.B, color.A);
		}

		public static SharpDX.Matrix Convert(System.Windows.Media.Matrix m)
		{
			return new SharpDX.Matrix((float)m.M11, (float)m.M12, 0.0f, 0.0f,
									  (float)m.M21, (float)m.M22, 0.0f, 0.0f,
									  0.0f, 0.0f, 1.0f, 0.0f,
									  (float)m.OffsetX, (float)m.OffsetY, 0.0f, 1.0f);
		}

		public static System.Windows.Point Convert(SharpDX.Vector2 pos)
		{
			return new System.Windows.Point(pos.X, pos.Y);
		}

		public static System.Windows.Media.Color MultiplyColor(System.Windows.Media.Color color, float mul)
		{
			return System.Windows.Media.Color.FromRgb((byte)(color.R * mul), (byte)(color.G * mul), (byte)(color.B * mul));
		}
	}
}
