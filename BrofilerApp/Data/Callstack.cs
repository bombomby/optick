using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Data
{
	public class SysCallBoard : IResponseHolder
	{
		public override DataResponse Response { get; set; }

		Dictionary<UInt64, UInt64> systemCalls;

		public Tuple<bool, UInt64> GetSystemCallParams(UInt64 timeStamp)
		{
			UInt64 sysCallId = 0;
			bool isSystemCall = false;
			if (systemCalls.TryGetValue(timeStamp, out sysCallId))
			{
				isSystemCall = true;
			}

			return new Tuple<bool, UInt64>(isSystemCall, sysCallId);
		}

		public static SysCallBoard Create(DataResponse response, FrameGroup group)
		{
			SysCallBoard result = new SysCallBoard() { Response = response, systemCalls = new Dictionary<UInt64, UInt64>() };

			ulong totalCount = response.Reader.ReadUInt32();
			for (ulong i = 0; i < totalCount; i += 2)
			{
				UInt64 timestamp = response.Reader.ReadUInt64();
				UInt64 callId = response.Reader.ReadUInt64();

				UInt64 res = 0;
				if (!result.systemCalls.TryGetValue(timestamp, out res))
				{
					result.systemCalls.Add(timestamp, callId);
				}
			}

			return result;
		}
	}


    public class Callstack : List<SamplingDescription>, ITick
    {
        public long Start { get; set; }
		public CallStackReason Reason { get; set; }
    }

	public class CallstackPack : IResponseHolder
	{
		public Dictionary<UInt64, List<Callstack>> CallstackMap { get; set; }
		public override DataResponse Response { get; set; }

		public static CallstackPack Create(DataResponse response, ISamplingBoard board, SysCallBoard sysCallBoard)
        {
			CallstackPack result = new CallstackPack() { Response = response, CallstackMap = new Dictionary<ulong, List<Callstack>>() };

            ulong totalCount = response.Reader.ReadUInt32();

			for (ulong i = 0; i < totalCount; )
            {
                UInt64 threadID = response.Reader.ReadUInt64();
                UInt64 timestamp = response.Reader.ReadUInt64();

				UInt64 count = response.Reader.ReadUInt64();

				Callstack callstack = new Callstack() { Start = (long)timestamp, Reason = CallStackReason.AutoSample };

				if (sysCallBoard != null)
				{
					Tuple<bool, UInt64> sysCallDesc = sysCallBoard.GetSystemCallParams(timestamp);

					if (sysCallDesc.Item1)
					{
						if (sysCallDesc.Item2 < (int)CallStackReason.MaxReasonsCount)
						{
							callstack.Reason = (CallStackReason)sysCallDesc.Item2;
						}
						else
						{
							callstack.Reason = CallStackReason.SysCall;
						}
					}
				}

                for (ulong addressIndex = 0; addressIndex < count; ++addressIndex)
                {
					UInt64 address = response.Reader.ReadUInt64();
					SamplingDescription desc = board.GetDescription(address);
					callstack.Add(desc);
                }

				List<Callstack> callstacks;
				if (!result.CallstackMap.TryGetValue(threadID, out callstacks))
				{
					callstacks = new List<Callstack>();
					result.CallstackMap.Add(threadID, callstacks);
				}

                //callstack.Reverse();
                callstacks.Add(callstack);

				i += (3 + count);
            }

			return result;
        }
    }
}
