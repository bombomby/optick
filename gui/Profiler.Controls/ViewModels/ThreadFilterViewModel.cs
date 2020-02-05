using Profiler.Data;
using Profiler.InfrastructureMvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Controls.ViewModels
{
	class ThreadFilterViewModel : BaseViewModel
	{
		private FrameGroup _group = null;
		public FrameGroup Group
		{
			get { return _group; }
			set { SetProperty(ref _group, value); }
		}
	}
}
