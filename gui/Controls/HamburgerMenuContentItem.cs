using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Media;

namespace Profiler.Controls
{
	/// <summary>
	/// The HamburgerMenuImageItem provides an image based implementation for HamburgerMenu entries.
	/// </summary>
	public class HamburgerMenuContentItem : HamburgerMenuItem
	{
		public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(FrameworkElement), typeof(HamburgerMenuImageItem), new PropertyMetadata(null));
		public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(FrameworkElement), typeof(HamburgerMenuImageItem), new PropertyMetadata(null));

		public FrameworkElement Icon
		{
			get
			{
				return (FrameworkElement)GetValue(IconProperty);
			}

			set
			{
				SetValue(IconProperty, value);
			}
		}

		public FrameworkElement Content
		{
			get
			{
				return (FrameworkElement)GetValue(ContentProperty);
			}

			set
			{
				SetValue(ContentProperty, value);
			}
		}


	}
}