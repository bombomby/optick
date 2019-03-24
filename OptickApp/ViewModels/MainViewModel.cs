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
    }
}
