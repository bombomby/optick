using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Data
{
	public class Callstack : List<SamplingDescription>, ITick, IComparable<Callstack>
	{
		public long Start { get; set; }
		public CallStackReason Reason { get; set; }

		public int CompareTo(Callstack other)
		{
			return Start.CompareTo(other.Start);
		}
	}

	public class CallstackPack : IResponseHolder
	{
		public Dictionary<UInt64, List<Callstack>> CallstackMap { get; set; }
		public override DataResponse Response { get; set; }

		public static CallstackPack Create(DataResponse response, ISamplingBoard board, SysCallBoard sysCallBoard)
		{
			CallstackPack result = new CallstackPack() { Response = response, CallstackMap = new Dictionary<ulong, List<Callstack>>() };

			ulong totalCount = response.Reader.ReadUInt32();

			for (ulong i = 0; i < totalCount;)
			{
				UInt64 threadID = response.Reader.ReadUInt64();
				UInt64 timestamp = response.Reader.ReadUInt64();
				UInt64 count = response.Reader.ReadUInt64();

				Callstack callstack = new Callstack() { Start = (long)timestamp, Reason = CallStackReason.AutoSample };

				if (sysCallBoard != null)
				{
					if (sysCallBoard.HasSysCall(threadID, callstack.Start))
					{
						callstack.Reason = CallStackReason.SysCall;
					}
				}

				for (ulong addressIndex = 0; addressIndex < count; ++addressIndex)
				{
					UInt64 address = response.Reader.ReadUInt64();
					SamplingDescription desc = board.GetDescription(address);
					if (!desc.IsIgnore)
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

			foreach (List<Callstack> cs in result.CallstackMap.Values)
			{
				cs.Sort();
			}

			return result;
		}
	}
}
