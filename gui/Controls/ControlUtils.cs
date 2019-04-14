using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Profiler.Controls
{
	static class ControlUtils
	{
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
			public int Width {  get { return Right - Left; } }
			public int Height { get { return Bottom - Top; } }
		}

		public static Stream CaptureScreenshot(Window window, ImageFormat format)
		{
			MemoryStream result = null;

			IntPtr windowHandle = new WindowInteropHelper(window).Handle;
			RECT rect;
			if (GetWindowRect(new HandleRef(null, windowHandle), out rect))
			{
				using (Bitmap bitmap = new Bitmap(rect.Width, rect.Height))
				{
					using (Graphics g = Graphics.FromImage(bitmap))
					{
						g.CopyFromScreen(new System.Drawing.Point(rect.Left, rect.Top), System.Drawing.Point.Empty, new System.Drawing.Size(rect.Width, rect.Height));
					}
					result = new MemoryStream();
					bitmap.Save(result, format);
					result.Position = 0;
				}
			}
			return result;
		}

	}
}
