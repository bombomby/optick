using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.DirectX
{
	class Utils
	{
		public static SharpDX.Color Convert(System.Windows.Media.Color color)
		{
			return new SharpDX.Color(color.R, color.G, color.B, color.A);
		}

		public static double GetLuminance(System.Windows.Media.Color color)
		{
			return 0.2126 * color.ScR + 0.7152 * color.ScG + 0.0722 * color.ScB;
		}

		public const double LuminanceThreshold = 0.2;

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

	}
}
