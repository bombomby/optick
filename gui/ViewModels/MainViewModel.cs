using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Profiler.InfrastructureMvvm;

namespace Profiler.ViewModels
{
    public class MainViewModel: BaseViewModel
    {
		public String Version { get { return Assembly.GetEntryAssembly().GetName().Version.ToString(); } }

		private bool _isCapturing = false;
		public bool IsCapturing
		{
			get { return _isCapturing; }
			set { SetProperty(ref _isCapturing, value); }
		}
    }
}
