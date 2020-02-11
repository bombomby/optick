using EnvDTE;
using Profiler.InfrastructureMvvm;
using Profiler.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptickVSIX.ViewModels
{
	class BuildViewModel : BaseViewModel
	{
		public BuildViewModel()
		{
			Collector = new DataCollector(new Config()
			{
				ProcessFilters = new String[] { "notepad.exe", "calc.exe", "CL.exe", "link.exe", "MSBuild.exe" }
			});
		}

		public DataCollector Collector { get; private set; }

		private String _name = String.Empty;
		public String Name { get { return _name; } set { _name = value; OnPropertyChanged(); } }

		private DateTime _startTime;
		public DateTime StartTime { get { return _startTime; } set { _startTime = value; OnPropertyChanged(); } }

		private DateTime _finishTime;
		public DateTime FinishTime { get { return _finishTime; } set { _finishTime = value; OnPropertyChanged(); } }

		public void Start(vsBuildScope Scope, vsBuildAction Action)
		{
			StartTime = DateTime.Now;
			Collector.Start();
		}

		public void Finish(vsBuildScope Scope, vsBuildAction Action)
		{
			FinishTime = DateTime.Now;
			Collector.Stop();
		}
	}
}
