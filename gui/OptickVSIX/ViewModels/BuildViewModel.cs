using EnvDTE;
using Profiler.Data;
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
				ProcessFilters = new String[] { "CL.exe", "link.exe", "MSBuild.exe" }
			});
		}

		public DataCollector Collector { get; private set; }

		private FrameGroup _group;
		public FrameGroup Group { get { return _group; } private set { _group = value; OnPropertyChanged(); } }

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
			GenerateData();
		}

		private void GenerateData()
		{
			EventDescriptionBoard board = new EventDescriptionBoard() { TimeSettings = new TimeSettings() { Origin = 0, PrecisionCut = 0, TicksToMs = 0.0001 }, TimeSlice = new Durable(StartTime.Ticks, FinishTime.Ticks), CPUCoreCount = Environment.ProcessorCount };
			FrameGroup group = new FrameGroup(board);

			List<SyncEvent> syncEvents = new List<SyncEvent>(Collector.SwitchContexts.Events.Count);
			foreach (SwitchContextData sc in Collector.SwitchContexts.Events)
			{
				if (board.TimeSlice.Start <= sc.Timestamp.Ticks && sc.Timestamp.Ticks <= board.TimeSlice.Finish)
					syncEvents.Add(new SyncEvent() { CPUID = sc.CPUID, NewThreadID = sc.NewThreadID, OldThreadID = sc.OldThreadID, Timestamp = new Tick() { Start = sc.Timestamp.Ticks } });
			}
				
			SynchronizationMap syncMap = new SynchronizationMap(syncEvents);

			group.AddSynchronization(syncMap);

			Group = group;
		}
	}
}
