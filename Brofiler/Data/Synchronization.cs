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
            public UInt64 Core { get; set; }

            public static SyncInterval Read(DataResponse response)
            {
                SyncInterval interval = new SyncInterval();
                interval.ReadDurable(response.Reader);

                if (response.Version >= NetworkProtocol.NETWORK_PROTOCOL_VERSION_8)
                    interval.Core = response.Reader.ReadUInt64();
                else
                    interval.Core = response.Reader.ReadUInt32();

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
                Intervals.Add(SyncInterval.Read(response));
        }
    }
}
