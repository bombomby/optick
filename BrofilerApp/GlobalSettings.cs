using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Profiler
{
	public class ProfilerGlobalSettings : DependencyObject
	{
		public static readonly DependencyProperty ShowMemoryProperty =
				DependencyProperty.Register("ShowMemory", typeof(bool),
				typeof(ProfilerGlobalSettings), new UIPropertyMetadata("no version!"));

		public bool ShowMemory
		{
			get { return (bool)GetValue(ShowMemoryProperty); }
			set { SetValue(ShowMemoryProperty, value); }
		}

		public static ProfilerGlobalSettings Instance { get; private set; }

		static ProfilerGlobalSettings()
		{
			Instance = new ProfilerGlobalSettings();
		}
	}
}
