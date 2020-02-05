using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;

namespace Profiler.DirectX
{
	class WindowsFormsHostEx : WindowsFormsHost
	{
		private PresentationSource _presentationSource;

		public WindowsFormsHostEx()
		{
			PresentationSource.AddSourceChangedHandler(this, SourceChangedEventHandler);
		}

		public class DpiScale
		{
			public double DpiScaleX { get; set; }
			public double DpiScaleY { get; set; }
		}

		protected override void OnWindowPositionChanged(Rect rcBoundingBox)
		{
			DpiScale dpiScale = new DpiScale() { DpiScaleX = RenderSettings.dpiScaleX, DpiScaleY = RenderSettings.dpiScaleY };

			base.OnWindowPositionChanged(rcBoundingBox);

			if (ParentScrollViewer == null)
				return;

			Rect newRect = ScaleRectDownFromDPI(rcBoundingBox, dpiScale);
			GeneralTransform tr = RootVisual.TransformToDescendant(ParentScrollViewer);
			var scrollRect = new Rect(new Size(ParentScrollViewer.ViewportWidth, ParentScrollViewer.ViewportHeight));

			var intersect = Rect.Intersect(scrollRect, tr.TransformBounds(newRect));
			if (!intersect.IsEmpty)
			{
				tr = ParentScrollViewer.TransformToDescendant(this);
				intersect = tr.TransformBounds(intersect);
				intersect = ScaleRectUpToDPI(intersect, dpiScale);
			}
			else
				intersect = new Rect();

			int x1 = (int)Math.Round(intersect.Left);
			int y1 = (int)Math.Round(intersect.Top);
			int x2 = (int)Math.Round(intersect.Right);
			int y2 = (int)Math.Round(intersect.Bottom);

			SetRegion(x1, y1, x2, y2);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				PresentationSource.RemoveSourceChangedHandler(this, SourceChangedEventHandler);
		}

		private void SourceChangedEventHandler(Object sender, SourceChangedEventArgs e)
		{
			ParentScrollViewer = FindParentScrollViewer();
			_presentationSource = null;
		}

		private ScrollViewer FindParentScrollViewer()
		{
			DependencyObject vParent = this;
			ScrollViewer parentScroll = null;
			while (vParent != null)
			{
				parentScroll = vParent as ScrollViewer;
				if (parentScroll != null)
					break;

				vParent = LogicalTreeHelper.GetParent(vParent);
			}
			return parentScroll;
		}

		private void SetRegion(int x1, int y1, int x2, int y2)
		{
			SetWindowRgn(Handle, CreateRectRgn(x1, y1, x2, y2), true);
		}

		private Visual RootVisual
		{
			get
			{
				if (_presentationSource == null)
					_presentationSource = PresentationSource.FromVisual(this);

				return _presentationSource.RootVisual;
			}
		}

		private ScrollViewer ParentScrollViewer { get; set; }

		public static Rect ScaleRectDownFromDPI(Rect _sourceRect, DpiScale dpiScale)
		{
			double dpiX = dpiScale.DpiScaleX;
			double dpiY = dpiScale.DpiScaleY;
			return new Rect(new Point(_sourceRect.X / dpiX, _sourceRect.Y / dpiY), new System.Windows.Size(_sourceRect.Width / dpiX, _sourceRect.Height / dpiY));
		}

		public static Rect ScaleRectUpToDPI(Rect _toScaleUp, DpiScale dpiScale)
		{
			double dpiX = dpiScale.DpiScaleX;
			double dpiY = dpiScale.DpiScaleY;
			return new Rect(new Point(_toScaleUp.X * dpiX, _toScaleUp.Y * dpiY), new System.Windows.Size(_toScaleUp.Width * dpiX, _toScaleUp.Height * dpiY));
		}

		[DllImport("User32.dll", SetLastError = true)]
		static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

		[DllImport("gdi32.dll")]
		static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
	}
}