using Profiler.InfrastructureMvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Controls
{
	public enum ExpandMode
	{
		[Display(Name = "Expand MainThread", Description = "Expand MainThread Only")]
		ExpandMain,
		[Display(Name = "Expand All", Description = "Expand All Threads")]
		ExpandAll,
		[Display(Name = "Collapse All", Description = "Collapse All Threads")]
		CollapseAll,
	}

	public class ThreadViewSettings : BaseViewModel
	{
		public int CollapsedMaxThreadDepth { get; set; } = 2;
		public int ExpandedMaxThreadDepth { get; set; } = 12;
		public ExpandMode ThreadExpandMode { get; set; } = ExpandMode.ExpandAll;
	}
}
