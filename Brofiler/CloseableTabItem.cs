using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Profiler.Data;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace Profiler
{
	public class CloseableTabItem : MetroTabItem
	{
		private FrameInfo _frameInfo;

		public FrameInfo frameInfo
		{
			get
			{
				return _frameInfo;
			}
		}

		public void AddFrameInfo(FrameInfo info)
		{
			_frameInfo = info;
			Add(_frameInfo);
		}

		public void Add(FrameworkElement element)
		{
			AddChild(element);
			MouseDown += new System.Windows.Input.MouseButtonEventHandler(OnMouseDown);
			KeyDown += new System.Windows.Input.KeyEventHandler(OnKeyDown);
		}

		void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.W && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
			{
				this.RaiseEvent(new RoutedEventArgs(CloseTabEvent, this));
			}
		}

		void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.MiddleButton == System.Windows.Input.MouseButtonState.Pressed)
			{
				this.RaiseEvent(new RoutedEventArgs(CloseTabEvent, this));
			}
		}

		#region CloseTab
		public static readonly RoutedEvent CloseTabEvent =
		EventManager.RegisterRoutedEvent("CloseTab", RoutingStrategy.Bubble,
			typeof(RoutedEventHandler), typeof(CloseableTabItem));

		public event RoutedEventHandler CloseTab
		{
			add { AddHandler(CloseTabEvent, value); }
			remove { RemoveHandler(CloseTabEvent, value); }
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			Button closeButton = base.GetTemplateChild("PART_Close") as Button;
			if (closeButton != null)
				closeButton.Click += new System.Windows.RoutedEventHandler(closeButton_Click);
		}

		void closeButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			this.RaiseEvent(new RoutedEventArgs(CloseTabEvent, this));
		}
		#endregion
	}
}
