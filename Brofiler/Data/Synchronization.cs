using System;
using System.Collections.Generic;

namespace Profiler.Data
{
	public enum SyncReason
	{
		Win_Executive = 0,
        Win_FreePage = 1,
        Win_PageIn = 2,
        Win_PoolAllocation = 3,
        Win_DelayExecution = 4,
        Win_Suspended = 5,
        Win_UserRequest = 6,
        Win_WrExecutive = 7,
        Win_WrFreePage = 8,
        Win_WrPageIn = 9,
        Win_WrPoolAllocation = 10,
        Win_WrDelayExecution = 11,
        Win_WrSuspended = 12,
        Win_WrUserRequest = 13,
        Win_WrEventPair = 14,
        Win_WrQueue = 15,
        Win_WrLpcReceive = 16,
        Win_WrLpcReply = 17,
        Win_WrVirtualMemory = 18,
        Win_WrPageOut = 19,
        Win_WrRendezvous = 20,
        Win_WrKeyedEvent = 21,
        Win_WrTerminated = 22,
        Win_WrProcessInSwap = 23,
        Win_WrCpuRateControl = 24,
        Win_WrCalloutStack = 25,
        Win_WrKernel = 26,
        Win_WrResource = 27,
        Win_WrPushLock = 28,
        Win_WrMutex = 29,
        Win_WrQuantumEnd = 30,
        Win_WrDispatchInt = 31,
        Win_WrPreempted = 32,
        Win_WrYieldExecution = 33,
        Win_WrFastMutex = 34,
        Win_WrGuardedMutex = 35,
        Win_WrRundown = 36,
        Win_MaximumWaitReason = 37,


        SyncReasonCount
	}

	public class SyncInterval : Durable
	{
		public UInt64 Core { get; set; }
		public SyncReason Reason { get; set; }

		public static SyncInterval Read(DataResponse response)
		{
			SyncInterval interval = new SyncInterval();
			interval.ReadDurable(response.Reader);

			if (response.Version >= NetworkProtocol.NETWORK_PROTOCOL_VERSION_8)
				interval.Core = response.Reader.ReadUInt64();
			else
				interval.Core = response.Reader.ReadUInt32();

			if (response.Version >= NetworkProtocol.NETWORK_PROTOCOL_VERSION_9)
				interval.Reason = (SyncReason)response.Reader.ReadByte();

			return interval;
		}
	}

	public class WaitInterval : Durable
	{
		public SyncReason Reason { get; set; }
		public WaitInterval() { }
	}

    public class NodeWaitInterval : Durable
    {
        public int Count { get; set; }
        public Durable NodeInterval { get; set; }
        public SyncReason Reason { get; set; }
        public double Percent
        {
            get
            {
                long nodeTime = (NodeInterval.Finish - NodeInterval.Start);
                long waitTime = (this.Finish - this.Start);
                double percent = ((double)waitTime / (double)nodeTime) * 100.0;
                return percent;
            }
        }

        public string Desc
        {
            get
            {
                return Percent.ToString("F3") + "%, " + DurationF3 + " ms, " + Count;
            }
        }


        public NodeWaitInterval() { }
    }


	public class Synchronization : IResponseHolder
    {
        public override DataResponse Response { get; set; }
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
