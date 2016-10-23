using System;
using System.Collections.Generic;

namespace Profiler.Data
{
	public enum SyncReason
	{
		Executive,
		FreePage,
		PageIn,
		PoolAllocation,
		DelayExecution,
		Suspended,
		UserRequest,
		WrExecutive,
		WrFreePage,
		WrPageIn,
		WrPoolAllocation,
		WrDelayExecution,
		WrSuspended,
		WrUserRequest,
		WrEventPair,
		WrQueue,
		WrLpcReceive,
		WrLpcReply,
		WrVirtualMemory,
		WrPageOut,
		WrRendezvous,
		WrKeyedEvent,
		WrTerminated,
		WrProcessInSwap,
		WrCpuRateControl,
		WrCalloutStack,
		WrKernel,
		WrResource,
		WrPushLock,
		WrMutex,
		WrQuantumEnd,
		WrDispatchInt,
		WrPreempted,
		WrYieldExecution,
		WrFastMutex,
		WrGuardedMutex,
		WrRundown,
		MaximumWaitReason
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
