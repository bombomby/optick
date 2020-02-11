using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Profiler.Trace
{
    class DiagnosticsCollector
    {
        public List<PerformanceCounter> Counters = new List<PerformanceCounter>();
        public List<CounterSample> Samples = new List<CounterSample>();

        private Timer timer = new Timer(1000.0);

        public DiagnosticsCollector()
        {
            Counters.Add(new PerformanceCounter("Processor", "% Processor Time", "_Total"));

            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Update();
        }

        public void Update()
        {
            CounterSample sample = new CounterSample() { Timestamp = DateTime.Now, Values = new float[Counters.Count] };
            for (int i = 0; i < Counters.Count; ++i)
                sample.Values[i] = Counters[i].NextValue();
            Samples.Add(sample);
        }

        public CounterGroup CreateCounterGroup(DateTime start, DateTime finish)
        {
            CounterGroup group = new CounterGroup();

            group.Descriptions = new List<CounterDescription>(Counters.Count);
            Counters.ForEach(counter => group.Descriptions.Add(new CounterDescription()
            {
                Name = counter.CounterName
            }));

            group.Samples = Samples.FindAll(sample => start <= sample.Timestamp && sample.Timestamp <= finish);

            return group;
        }
    }
}
