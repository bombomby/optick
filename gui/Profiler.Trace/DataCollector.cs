using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Profiler.Trace
{
    public class DataCollector
    {
        public object GroupLock = new Object();
        public ProcessGroup Group { get; set; }

        private ETWCollector ETWCollector { get; set; }
        private DiagnosticsCollector DiagCollector { get; set; }

        public DataCollector(Config config)
        {
            Group = new ProcessGroup();

            ETWCollector = new ETWCollector();
            ETWCollector.SetProcessFilter(config.ProcessFilters);
            ETWCollector.ProcessEvent += ETWCollector_ProcessEvent;

            DiagCollector = new DiagnosticsCollector();
        }

        private void ETWCollector_ProcessEvent(ProcessData obj)
        {
			Application.Current.Dispatcher.Invoke((Action)(() =>
			{
				lock (GroupLock)
				{
					Group.Add(obj);
				}
			}));
		}

        public void LoadCounters()
        {
            lock (GroupLock)
            {
                if (Group.Counters == null && Group.Processes.Count > 0)
                {
                    DateTime start = Group.Processes.Min(p => p.Start);
                    DateTime finish = Group.Processes.Max(p => p.Finish);
                    Group.Counters = DiagCollector.CreateCounterGroup(start, finish);
                }
            }
        }

        public void Start()
        {
            ETWCollector.Start();
        }

        public void Stop()
        {
            ETWCollector.Stop();
        }
    }
}
