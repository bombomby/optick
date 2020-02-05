using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.DirectX
{
	public class RenderSettings
	{
		public static double dpiScaleX = 1.0;
		public static double dpiScaleY = 1.0;

		static RenderSettings()
		{
			using (System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
			{
				dpiScaleX = (g.DpiX / 96.0);
				dpiScaleY = (g.DpiY / 96.0);
			}
		}
	}
}
