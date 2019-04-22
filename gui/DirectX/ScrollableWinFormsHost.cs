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
	public class WindowsFormsHostEx : WindowsFormsHost
	{
		#region DllImports
		[DllImport("User32.dll", SetLastError = true)]
		static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

		[DllImport("gdi32.dll")]
		static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

		#endregion

		#region Events
		public event EventHandler LocationChanged;
		#endregion

		#region Members
		private PresentationSource _presentationSource;
		#endregion

		#region Properties
		private ScrollViewer ParentScrollViewer { get; set; }
		private bool Scrolling { get; set; }
		public bool Resizing { get; set; }
		private Visual RootVisual
		{
			get
			{
				_presentationSource = PresentationSource.FromVisual(this);
				return _presentationSource.RootVisual;
			}
		}
		#endregion

		#region Constructors
		public WindowsFormsHostEx()
		{
			PresentationSource.AddSourceChangedHandler(this, SourceChangedEventHandler);
		}
		#endregion

		#region Methods

		public class DpiScale
		{
			public double DpiScaleX { get; set; }
			public double DpiScaleY { get; set; }
		}

		protected override void OnWindowPositionChanged(Rect rcBoundingBox)
		{
			DpiScale dpiScale = new DpiScale() { DpiScaleX = RenderSettings.dpiScaleX, DpiScaleY = RenderSettings.dpiScaleY };

			base.OnWindowPositionChanged(rcBoundingBox);

			Rect newRect = ScaleRectDownFromDPI(rcBoundingBox, dpiScale);
			Rect finalRect;
			if (ParentScrollViewer != null)
			{
				ParentScrollViewer.ScrollChanged += ParentScrollViewer_ScrollChanged;
				ParentScrollViewer.SizeChanged += ParentScrollViewer_SizeChanged;
				ParentScrollViewer.Loaded += ParentScrollViewer_Loaded;
			}

			if (Scrolling || Resizing)
			{
				if (ParentScrollViewer == null)
					return;
				MatrixTransform tr = RootVisual.TransformToDescendant(ParentScrollViewer) as MatrixTransform;

				var scrollRect = new Rect(new Size(ParentScrollViewer.ViewportWidth, ParentScrollViewer.ViewportHeight));
				var c = tr.TransformBounds(newRect);

				var intersect = Rect.Intersect(scrollRect, c);
				if (!intersect.IsEmpty)
				{
					tr = ParentScrollViewer.TransformToDescendant(this) as MatrixTransform;
					intersect = tr.TransformBounds(intersect);
					finalRect = ScaleRectUpToDPI(intersect, dpiScale);
				}
				else
					finalRect = intersect = new Rect();

				int x1 = (int)Math.Round(finalRect.X);
				int y1 = (int)Math.Round(finalRect.Y);
				int x2 = (int)Math.Round(finalRect.Right);
				int y2 = (int)Math.Round(finalRect.Bottom);

				SetRegion(x1, y1, x2, y2);
				this.Scrolling = false;
				this.Resizing = false;

			}
			LocationChanged?.Invoke(this, new EventArgs());
		}

		private void ParentScrollViewer_Loaded(object sender, RoutedEventArgs e)
		{
			this.Resizing = true;
		}

		private void ParentScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			this.Resizing = true;
		}

		private void ParentScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (e.VerticalChange != 0 || e.HorizontalChange != 0 || e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
				Scrolling = true;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				PresentationSource.RemoveSourceChangedHandler(this, SourceChangedEventHandler);
				_presentationSource = null;
			}
		}

		private void SourceChangedEventHandler(Object sender, SourceChangedEventArgs e)
		{
			if (ParentScrollViewer != null)
			{
				ParentScrollViewer.ScrollChanged -= ParentScrollViewer_ScrollChanged;
				ParentScrollViewer.SizeChanged -= ParentScrollViewer_SizeChanged;
				ParentScrollViewer.Loaded -= ParentScrollViewer_Loaded;
			}
			ParentScrollViewer = FindParentScrollViewer();
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
		#endregion
	}
}
