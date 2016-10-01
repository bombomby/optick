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
            public UInt32 Core { get; set; }

            public static SyncInterval Read(BinaryReader reader)
            {
                SyncInterval interval = new SyncInterval();
                interval.ReadDurable(reader);
                interval.Core = reader.ReadUInt32();
                return interval;
            }
        }

        public DataResponse Response { get; set; }
        public int ThreadIndex { get; set; }
        public FrameGroup Group { get; set; }

        public List<SyncInterval> Intervals { get; set; }

        public Synchronization(DataResponse response, FrameGroup group)
        {
            Group = group;
            Response = response;
            ThreadIndex = response.Reader.ReadInt32();

            int count = response.Reader.ReadInt32();
            Intervals = new List<SyncInterval>(count);

            for (int i = 0; i < count; ++i)
                Intervals.Add(SyncInterval.Read(response.Reader));
        }
    }
}
