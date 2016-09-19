using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Data
{
    public class Synchronization
    {
        public class SyncInterval : Durable
        {
            public int Core { get; set; }

            public static SyncInterval Read(BinaryReader reader)
            {
                SyncInterval interval = new SyncInterval();
                interval.ReadDurable(reader);
                interval.Core = reader.ReadInt32();
                return interval;
            }
        }

        public BinaryReader Reader { get; set; }
        public int ThreadIndex { get; set; }
        public FrameGroup Group { get; set; }

        public List<SyncInterval> Intervals { get; set; }

        public Synchronization(BinaryReader reader, FrameGroup group)
        {
            Group = group;
            Reader = reader;
            ThreadIndex = reader.ReadInt32();

            int count = reader.ReadInt32();
            Intervals = new List<SyncInterval>(count);

            for (int i = 0; i < count; ++i)
                Intervals.Add(SyncInterval.Read(reader));
        }
    }
}
