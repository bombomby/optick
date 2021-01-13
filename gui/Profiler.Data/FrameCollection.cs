using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.IO;
using System.Diagnostics;
using System.Windows.Media;
using System.ComponentModel;

namespace Profiler.Data
{

	public class ThreadData
	{
		public ThreadDescription Description { get; set; }
		public List<EventFrame> Events { get; set; }
		public List<Callstack> Callstacks { get; set; }
		public List<SysCallEntry> SysCalls { get; set; }
		public Synchronization Sync { get; set; }
		public FiberSynchronization FiberSync { get; set; }
		public TagsPack TagsPack { get; set; }
		public bool IsDirty { get; set; }

		public ThreadData(ThreadDescription desc)
		{
			Description = desc;
			Events = new List<EventFrame>();
		}

		public void ApplyFiberSynchronization()
		{
			if (FiberSync == null)
				return;

			int currentInterval = 0;

			foreach (EventFrame frame in Events)
			{
				while (currentInterval < FiberSync.Intervals.Count && FiberSync.Intervals[currentInterval].Finish <= frame.Header.Start)
					++currentInterval;

				while (currentInterval < FiberSync.Intervals.Count && FiberSync.Intervals[currentInterval].Finish <= frame.Header.Finish)
					frame.FiberSync.Add(FiberSync.Intervals[currentInterval++]);

				if (currentInterval < FiberSync.Intervals.Count && FiberSync.Intervals[currentInterval].Start <= frame.Header.Finish)
					frame.FiberSync.Add(FiberSync.Intervals[currentInterval]);
			}
		}


		public void ApplySynchronization()
		{
			if (Sync == null)
				return;

			int currentInterval = 0;

			foreach (EventFrame frame in Events)
			{
				while (currentInterval < Sync.Count && Sync[currentInterval].Finish <= frame.Header.Start)
					++currentInterval;

				while (currentInterval < Sync.Count && Sync[currentInterval].Finish <= frame.Header.Finish)
					frame.Synchronization.Add(Sync[currentInterval++]);

				if (currentInterval < Sync.Count && Sync[currentInterval].Start <= frame.Header.Finish)
					frame.Synchronization.Add(Sync[currentInterval]);
			}
		}

		public void AddWithMerge(EventFrame frame)
		{
			if (frame.Start == frame.Finish)
				return;

			int indexStart = Utils.BinarySearchExactIndex(Events, frame.Start);
			int indexFinish = Utils.BinarySearchExactIndex(Events, frame.Finish - 1);

			Debug.Assert(indexStart == indexFinish, "Can merge only events inside the same frame");

			if (indexStart != -1)
			{
				EventFrame parent = Events[indexStart];
				parent.MergeWith(frame);
			}
			else
			{
				Events.Add(frame);
				IsDirty = true;
			}
		}
	}

	public class FrameGroupStats : INotifyPropertyChanged
	{
		public FrameGroupStats(FrameGroup group)
		{
			Duration = group.Board.TimeSlice.Duration;
			foreach (ThreadData thread in group.Threads)
			{
				foreach (EventFrame frame in thread.Events)
					NumScopes = NumScopes + (UInt64)frame.Entries.Count;

				NumTags = NumTags + (UInt64)(thread.TagsPack != null ? thread.TagsPack.Tags.Count : 0);
				NumSysCalls = NumSysCalls + (UInt64)(thread.SysCalls != null ? thread.SysCalls.Count : 0);
				NumCallstacks = NumCallstacks + (UInt64)(thread.Callstacks != null ? thread.Callstacks.Count : 0);
			}
		}

		public double Duration { get; set; }
		public UInt64 NumScopes { get; set; }
		public UInt64 NumTags { get; set; }
		public UInt64 NumSysCalls { get; set; }
		public UInt64 NumCallstacks { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;
	}


	public class FrameGroup
	{
		public String Name { get; set; }
		public SysCallBoard SysCallsBoard { get; protected set; }
		public EventDescriptionBoard Board { get; set; }
		public ISamplingBoard SamplingBoard { get; set; }
		public List<ThreadData> Threads { get; set; }
		public List<ThreadData> Cores { get; set; }
		public List<ThreadData> Fibers { get; set; }
		//public ThreadData MainThread { get { return Board.MainThreadIndex != -1 ? Threads[Board.MainThreadIndex] : null; } }
		public FrameList FocusThread { get { return Frames != null ? Frames[FrameList.Type.CPU] : null; } }
		public SummaryPack Summary { get; set; }
		public FramePack Frames { get; set; }
		public SynchronizationMap Synchronization { get; set; }
		public List<DataResponse> Responses { get; set; }

		public bool IsCoreDataGenerated { get; set; }

		public FrameList GetFocusThread(ThreadMask mask)
		{
			if (Frames == null)
				return null;

			if (mask == ThreadMask.GPU)
			{
				FrameList gpuFrames = Frames[FrameList.Type.GPU];
				if (gpuFrames != null && gpuFrames.Events.Count > 0)
					return gpuFrames;
			}

			if (mask == ThreadMask.Render)
			{
				FrameList renderFrames = Frames[FrameList.Type.Render];
				if (renderFrames != null && renderFrames.Events.Count > 0)
					return renderFrames;
			}

			FrameList cpuFrames = Frames[FrameList.Type.CPU];
			return (cpuFrames != null && cpuFrames.Events.Count > 0) ? cpuFrames : null;
		}

		public List<ThreadData> GetThreads(ThreadDescription.Source origin)
		{
			List<ThreadData> threads = new List<ThreadData>();
			for (int i = 0; i < Board.Threads.Count; ++i)
			{
				if (Board.Threads[i].Origin == origin)
				{
					threads.Add(Threads[i]);
				}
			}
			return threads;
		}

		public FrameGroup(EventDescriptionBoard board)
		{
			//System.Diagnostics.Debug.Assert(board != null && board.Response != null, "Invalid EventDescriptionBoard response");

			Board = board;

			if (board.Threads != null)
			{
				Threads = new List<ThreadData>(board.Threads.Count);
				foreach (ThreadDescription desc in board.Threads)
					Threads.Add(new ThreadData(desc));
			}

			Fibers = new List<ThreadData>();
			Responses = new List<DataResponse>();

			if (board.Response != null)
				Responses.Add(board.Response);
		}

		public void AddFrame(EventFrame frame)
		{
			System.Diagnostics.Debug.Assert(frame != null && frame.Response != null, "Invalid EventFrame response");

			Responses.Add(frame.Response);

			int threadIndex = frame.Header.ThreadIndex;
			if (threadIndex >= 0)
			{
				while (threadIndex >= Threads.Count)
				{
					Threads.Add(new ThreadData(Board.Threads[threadIndex]));
				}
				Threads[threadIndex].Events.Add(frame);
			}
			else
			{
				int fiberIndex = frame.Header.FiberIndex;
				if (fiberIndex >= 0)
				{
					while (fiberIndex >= Fibers.Count)
					{
						Fibers.Add(new ThreadData(null));
					}
					Fibers[fiberIndex].Events.Add(frame);
				}
			}
		}

		private void GenerateDummyCoreThreads()
		{
			if (Cores == null)
			{
				Cores = new List<ThreadData>(Board.CPUCoreCount);

				foreach (KeyValuePair<UInt64, Synchronization> pair in Synchronization.SyncMap)
				{
					foreach (SyncInterval interval in pair.Value)
					{
						while (Cores.Count <= interval.Core)
						{
							ThreadDescription desc = new ThreadDescription() { Name = String.Format("CPU Core {0:00}", Cores.Count), ThreadID = UInt64.MaxValue, Origin = ThreadDescription.Source.Core };
							Cores.Add(AddThread(desc));
						}
					}
				}
			}
		}

		public void GenerateRealCoreThreads()
		{
			if (Cores != null && !IsCoreDataGenerated)
			{
				foreach (KeyValuePair<UInt64, Synchronization> pair in Synchronization.SyncMap)
				{
					ThreadDescription threadDesc = null;
					Board.ThreadDescriptions.TryGetValue(pair.Key, out threadDesc);

					if (threadDesc != null && threadDesc.IsIdle)
						continue;

					EventDescription eventDesc = new EventDescription(threadDesc != null ? String.Format("{0}:0x{1:X}", threadDesc.FullName, threadDesc.ThreadID) : pair.Key.ToString("X"));
					if (threadDesc != null && threadDesc.ProcessID != Board.ProcessID)
						eventDesc.Color = Colors.SlateGray;

					foreach (SyncInterval interval in pair.Value)
					{
						byte core = interval.Core;

						Entry entry = new Entry(eventDesc, interval.Start, interval.Finish);

						EventFrame frame = new EventFrame(new FrameHeader(interval, Cores[interval.Core].Description.ThreadIndex), new List<Entry>() { entry }, this);
						entry.Frame = frame;

						Cores[interval.Core].Events.Add(frame);
					}
				}

				Cores.ForEach(core => core.Events.Sort());
				IsCoreDataGenerated = true;
			}
		}

		private void GenerateMiscThreads()
		{
			foreach (KeyValuePair<UInt64, ThreadDescription> pair in Board.ThreadDescriptions)
			{
				ThreadDescription desc = pair.Value;
				if (desc.ProcessID == Board.ProcessID && !desc.IsIdle)
				{
					ThreadData threadData = GetThread(pair.Key);
					if (threadData == null || threadData.Events.Count == 0)
					{
						Synchronization sync = null;
						if (Synchronization.SyncMap.TryGetValue(pair.Key, out sync))
						{
							if (threadData == null)
								threadData = AddThread(pair.Value);

							EventDescription eventDesc = new EventDescription(desc.FullName);

							foreach (SyncInterval interval in sync)
							{
								Entry entry = new Entry(eventDesc, interval.Start, interval.Finish);
								EventFrame frame = new EventFrame(new FrameHeader(interval, Threads.Count - 1), new List<Entry>() { entry }, this);
								entry.Frame = frame;

								threadData.Events.Add(frame);
							}
						}
					}
				}
			}

		}

		public void Save(Stream stream)
		{
			Responses.ForEach(response => response.Serialize(stream));
		}

		public void AddSynchronization(SynchronizationMap syncMap)
		{
			Responses.Add(syncMap.Response);
			Synchronization = syncMap;

			for (int i = 0; i < Math.Min(Board.Threads.Count, Threads.Count); ++i)
			{
				ThreadDescription desc = Board.Threads[i];
				Synchronization sync = null;
				if (syncMap.SyncMap.TryGetValue(desc.ThreadID, out sync))
				{
					Threads[i].Sync = sync;
				}
			}

			GenerateDummyCoreThreads();
			GenerateMiscThreads();
		}

		public void AddFiberSynchronization(FiberSynchronization fiberSync)
		{
			System.Diagnostics.Debug.Assert(fiberSync != null && fiberSync.Response != null, "Invalid FiberSynchronization response");

			Responses.Add(fiberSync.Response);

			int index = fiberSync.FiberIndex;
			while (index >= Fibers.Count)
			{
				Fibers.Add(new ThreadData(null));
			}
			Fibers[index].FiberSync = fiberSync;

			SplitFiber(index);
		}


		public void AddSysCalls(SysCallBoard sysCallsBoard)
		{
			System.Diagnostics.Debug.Assert(sysCallsBoard != null && sysCallsBoard.Response != null, "Invalid SysCallResponse response");

			Responses.Add(sysCallsBoard.Response);
			SysCallsBoard = sysCallsBoard;

			foreach (var pair in sysCallsBoard.SysCallMap)
			{
				ThreadData thread = GetThread(pair.Key);
				if (thread != null)
					thread.SysCalls = pair.Value;
			}
		}

		public void AddCallStackPack(CallstackPack pack)
		{
			System.Diagnostics.Debug.Assert(pack != null && pack.Response != null, "Invalid CallstackPack response");

			Responses.Add(pack.Response);

			for (int i = 0; i < Threads.Count; ++i)
			{
				List<Callstack> callstacks;
				if (pack.CallstackMap.TryGetValue(Board.Threads[i].ThreadID, out callstacks))
				{
					Threads[i].Callstacks = callstacks;
				}
			}
		}

		public void AddSymbolPack(SamplingDescriptionPack pack)
		{
			System.Diagnostics.Debug.Assert(pack != null && pack.Response != null, "Invalid SamplingDescriptionPack response");

			Responses.Add(pack.Response);
			SamplingBoard = pack;
		}

		public void AddSummary(SummaryPack summary)
		{
			Responses.Insert(0, summary.Response);
			Summary = summary;
		}

		public void AddFramePack(FramePack pack)
		{
			Responses.Add(pack.Response);
			Frames = pack;
		}

		private void SplitFiber(int fiberIndex)
		{
			ThreadData data = Fibers[fiberIndex];
			data.ApplyFiberSynchronization();

			foreach (EventFrame frame in data.Events)
			{
				foreach (FiberSyncInterval fiberSync in frame.FiberSync)
				{
					int threadIndex = 0;
					if (!Board.ThreadID2ThreadIndex.TryGetValue(fiberSync.threadId, out threadIndex))
					{
						continue;
					}

					Durable border = new Durable(Math.Max(frame.Start, fiberSync.Start), Math.Min(frame.Finish, fiberSync.Finish));

					int lastIndex = Utils.BinarySearchClosestIndex(frame.Entries, border.Finish);

					List<Entry> entries = null;
					for (int i = 0; i <= lastIndex; i++)
					{
						Entry entry = frame.Entries[i];
						if (entry.Intersect(border))
						{
							if (entries == null)
							{
								entries = new List<Entry>();
							}
							entries.Add(new Entry(entry.Description, Math.Max(border.Start, entry.Start), Math.Min(border.Finish, entry.Finish)));
						}
					}

					if (entries != null && entries.Count > 0)
					{
						FrameHeader header = new FrameHeader(border, threadIndex, fiberIndex);
						EventFrame block = new EventFrame(header, entries, this);
						entries.ForEach(e => e.Frame = block);
						Threads[threadIndex].AddWithMerge(block);
					}


				}
			}
		}

		public void UpdateEventsSynchronization()
		{
			foreach (ThreadData data in Threads)
			{
				if (data.IsDirty)
				{
					data.Events.Sort();
					data.IsDirty = false;
				}

				data.ApplySynchronization();
			}
		}

		public void Add(TagsPack pack)
		{
			Responses.Add(pack.Response);
			if (0 <= pack.ThreadIndex && pack.ThreadIndex < Threads.Count)
				Threads[pack.ThreadIndex].TagsPack = pack;
		}

		public ThreadData GetThread(UInt64 threadID)
		{
			int threadIndex = -1;
			if (Board.ThreadID2ThreadIndex.TryGetValue(threadID, out threadIndex))
			{
				return Threads[threadIndex];
			}
			return null;
		}

		public ThreadData AddThread(ThreadDescription desc)
		{
			int index = Threads.Count;
			desc.ThreadIndex = index;
			if (desc.ThreadID != UInt64.MaxValue)
				Board.ThreadID2ThreadIndex.Add(desc.ThreadID, index);
			ThreadData threadData = new ThreadData(desc);
			Threads.Add(threadData);
			Board.Threads.Add(desc);
			return threadData;
		}

		public List<Callstack> GetCallstacks(EventDescription desc, CallStackReason type = CallStackReason.AutoSample)
		{
			List<Callstack> callstacks = new List<Callstack>();

			foreach (ThreadData thread in Threads)
			{
				HashSet<Callstack> accumulator = new HashSet<Callstack>();
				foreach (EventFrame currentFrame in thread.Events)
				{
					List<Entry> entries = null;
					if (currentFrame.ShortBoard.TryGetValue(desc, out entries))
					{
						foreach (Entry entry in entries)
						{
							Utils.ForEachInsideIntervalStrict(thread.Callstacks, entry, c => {
								if ((c.Reason & type) != 0)
									accumulator.Add(c);
							});
						}
					}
				}

				callstacks.AddRange(accumulator);
			}

			return callstacks;
		}

        public SamplingFrame CreateSamplingFrame(EventDescription desc, CallStackReason type = CallStackReason.AutoSample)
        {
            return new SamplingFrame(GetCallstacks(desc, type), this);
        }

		public static uint? GetFrameNumber(EventFrame frame)
		{
			TagUInt32 tag = frame.FindTag<TagUInt32>("Frame");
			if (tag != null)
				return tag.Value;
			return null;
		}

		public bool UpdateDescriptionMask(EventDescription description)
		{
			if (description.Mask != null)
				return false;

			ThreadMask mask = ThreadMask.None;

			foreach (ThreadData thread in Threads)
			{
				foreach (EventFrame frame in thread.Events)
				{
					if (frame.ShortBoard.ContainsKey(description))
					{
						mask |= (ThreadMask)thread.Description.Mask;
					}
				}
			}

			description.Mask = mask;

			return true;
		}

	}

	public class FrameCollection : ObservableCollection<Frame>
	{
		Dictionary<int, FrameGroup> groups = new Dictionary<int, FrameGroup>();
		Dictionary<int, SummaryPack> summaries = new Dictionary<int, SummaryPack>();

		public void AddGroup(FrameGroup group)
		{
			groups[group.Board.ID] = group;
		}

		public void Flush()
		{
			foreach (FrameGroup group in groups.Values)
			{
				group.UpdateEventsSynchronization();
				group.Frames.FinishUpdate();

				FrameList frameList = group.Frames[FrameList.Type.CPU];
				foreach (EventFrame frame in frameList.Frames)
					Add(frame);
			}

			groups.Clear();
			summaries.Clear();
		}

		public void UpdateName(String name, bool force = false)
		{
			foreach (FrameGroup group in groups.Values)
				if (String.IsNullOrEmpty(group.Name) || force)
					group.Name = name;
		}

		public void Add(DataResponse response)
		{
			switch (response.ResponseType)
			{
				case DataResponse.Type.SummaryPack:
					{
						SummaryPack summary = new SummaryPack(response);
						summaries[summary.BoardID] = summary;
						break;
					}

				case DataResponse.Type.FrameDescriptionBoard:
					{
						EventDescriptionBoard board = EventDescriptionBoard.Read(response);
						FrameGroup group = new FrameGroup(board);

						AddGroup(group);

						SummaryPack summary = null;
						if (summaries.TryGetValue(board.ID, out summary))
							group.AddSummary(summary);

						break;
					}

				case DataResponse.Type.EventFrame:
					{
						int id = response.Reader.ReadInt32();
						FrameGroup group = groups[id];
						EventFrame frame = new EventFrame(response, group);

						group.AddFrame(frame);

						if (group.Board.MainThreadIndex != -1 && group.Board.MainThreadIndex == frame.Header.ThreadIndex)
						{
							Add(frame);
						}
						else if (frame.Header.FrameType != FrameList.Type.None)
						{
							if (frame.Header.Duration > 0.0)
							{
								FrameList frameList = group.Frames[frame.Header.FrameType];
								if (frameList != null)
								{
									frameList.Frames.Add(frame);
								}
							}
						}

						break;
					}

				case DataResponse.Type.SynchronizationData:
					{
						int id = response.Reader.ReadInt32();
						FrameGroup group = groups[id];

						group.AddSynchronization(new SynchronizationMap(response, group));

						break;
					}

				case DataResponse.Type.FiberSynchronizationData:
					{
						int id = response.Reader.ReadInt32();
						FrameGroup group = groups[id];

						group.AddFiberSynchronization(new FiberSynchronization(response, group));

						break;
					}

				case DataResponse.Type.CallstackDescriptionBoard:
					{
						int id = response.Reader.ReadInt32();
						FrameGroup group = groups[id];

						group.AddSymbolPack(SamplingDescriptionPack.CreatePack(response));

						break;
					}

				case DataResponse.Type.SyscallPack:
					{
						int id = response.Reader.ReadInt32();
						FrameGroup group = groups[id];

						SysCallBoard sysCallsBoard = SysCallBoard.Create(response, group);
						group.AddSysCalls(sysCallsBoard);

						break;
					}

				case DataResponse.Type.CallstackPack:
					{
						int id = response.Reader.ReadInt32();
						FrameGroup group = groups[id];

						ISamplingBoard samplingBoard = group.SamplingBoard;

						if (samplingBoard == null)
						{
							// TODO: replace Dummy Sampling Board with proper Platform-dependent ISamplingBoard
							samplingBoard = DummySamplingBoard.Instance;
						}

						CallstackPack pack = CallstackPack.Create(response, samplingBoard, group.SysCallsBoard);
						group.AddCallStackPack(pack);

						break;
					}

				case DataResponse.Type.TagsPack:
					{
						int id = response.Reader.ReadInt32();
						if (groups.ContainsKey(id))
						{
							FrameGroup group = groups[id];

							TagsPack pack = new TagsPack(response, group);
							group.Add(pack);
						}
						break;
					}


				case DataResponse.Type.FramesPack:
					{
						int id = response.Reader.ReadInt32();
						FrameGroup group = groups[id];

						FramePack pack = FramePack.Create(response, group.Board);
						group.AddFramePack(pack);

						break;
					}

				default:
					{
						Debug.Fail("Skipping response: ", response.ResponseType.ToString());
						break;
					}
			}
		}
	}

	public class TestFrameCollection : FrameCollection
	{
		// Encoded network stream with test frames
		static String descriptionBoard = "BQAAAGADAAAAAAAAAQAAAMcWLgAAAAAAx4pn3kUAAAAHzIneRQAAAAMAAADUDwAABgAAAFdvcmtlctAUAAAGAAAAV29ya2VyOCQAAAoAAABNYWluVGhyZWFkAgAAABEAAAAFAAAAU2xlZXAMAAAAS2VybmVsMzIuZGxsAAAAAP////8ABwAAAFNsZWVwRXgMAAAAS2VybmVsMzIuZGxsAAAAAP////8AEwAAAFdhaXRGb3JTaW5nbGVPYmplY3QMAAAAS2VybmVsMzIuZGxsAAAAAP////8AFQAAAFdhaXRGb3JTaW5nbGVPYmplY3RFeAwAAABLZXJuZWwzMi5kbGwAAAAA/////wAWAAAAV2FpdEZvck11bHRpcGxlT2JqZWN0cwwAAABLZXJuZWwzMi5kbGwAAAAA/////wAYAAAAV2FpdEZvck11bHRpcGxlT2JqZWN0c0V4DAAAAEtlcm5lbDMyLmRsbAAAAAD/////AAUAAABTbGVlcAgAAABIb29rLmNwcPsAAAD/////AAUAAABGcmFtZQ4AAABUZXN0RW5naW5lLmNwcDcAAAAAAAAAAAsAAABVcGRhdGVJbnB1dA4AAABUZXN0RW5naW5lLmNwcEgAAAC0gkb/ACYAAAB2b2lkIF9fY2RlY2wgVGVzdDo6U2xvd0Z1bmN0aW9uMih2b2lkKQ4AAABUZXN0RW5naW5lLmNwcCgAAAAAAAAAAA0AAABVcGRhdGVQaHlzaWNzDgAAAFRlc3RFbmdpbmUuY3BwYQAAALPe9f8ADgAAAFVwZGF0ZU1lc3NhZ2VzDgAAAFRlc3RFbmdpbmUuY3BwTQAAAACl//8ALgAAAHZvaWQgX19jZGVjbCBUZXN0OjpTbG93RnVuY3Rpb248MHg0MDAwMD4odm9pZCkOAAAAVGVzdEVuZ2luZS5jcHAfAAAAAAAAAAALAAAAVXBkYXRlTG9naWMOAAAAVGVzdEVuZ2luZS5jcHBSAAAA1nDa/wALAAAAVXBkYXRlU2NlbmUOAAAAVGVzdEVuZ2luZS5jcHBXAAAA686H/wAEAAAARHJhdw4AAABUZXN0RW5naW5lLmNwcFwAAABygPr/ABMAAABXYWl0Rm9yU2luZ2xlT2JqZWN0CAAAAEhvb2suY3BwCwEAAP////8A";
		static String eventFrame = "BQAAANwLAAABAAAAAQAAAAIAAAC76WreRQAAAIhubt5FAAAABgAAALzpat5FAAAAbmxr3kUAAAAIAAAAcWxr3kUAAAAz/2veRQAAAAsAAAA0/2veRQAAAA56bN5FAAAADQAAAA96bN5FAAAA2gZt3kUAAAAOAAAA2wZt3kUAAADW8m3eRQAAAAoAAADY8m3eRQAAAIdubt5FAAAADwAAAKUAAADf62reRQAAALjsat5FAAAA/uxq3kUAAAA17WreRQAAAEztat5FAAAAXO1q3kUAAADk7WreRQAAAKTuat5FAAAArO5q3kUAAACy7mreRQAAAFXzat5FAAAAgPNq3kUAAACCAmveRQAAAJMCa95FAAAAAgNr3kUAAABkBGveRQAAABMFa95FAAAAKgVr3kUAAAAtBWveRQAAADIFa95FAAAANgVr3kUAAABABWveRQAAAEYFa95FAAAAUwVr3kUAAABdBWveRQAAAGoFa95FAAAAcAVr3kUAAAB2BWveRQAAAH4Fa95FAAAAhQVr3kUAAACIBWveRQAAAIwFa95FAAAAkQVr3kUAAACoBWveRQAAALEFa95FAAAAyAVr3kUAAADQBWveRQAAANMFa95FAAAA1wVr3kUAAADvBWveRQAAAPoFa95FAAAA/gVr3kUAAAAMBmveRQAAACgGa95FAAAAKwZr3kUAAAAsBmveRQAAADAGa95FAAAANQZr3kUAAAB/CGveRQAAALoIa95FAAAA2RRr3kUAAACjI2veRQAAADAma95FAAAAPiZr3kUAAAC0JmveRQAAAIMoa95FAAAAjChr3kUAAADKKGveRQAAAJMpa95FAAAAqylr3kUAAADyhmveRQAAAPeGa95FAAAA+oZr3kUAAAAGh2veRQAAAAiHa95FAAAAv4dr3kUAAAAJiGveRQAAAAyIa95FAAAAt5Jr3kUAAADWkmveRQAAAM2Ua95FAAAA9pRr3kUAAACIlmveRQAAAJyWa95FAAAAgpdr3kUAAACLl2veRQAAAK2Xa95FAAAAK5hr3kUAAACwmGveRQAAAIyaa95FAAAAmZpr3kUAAAChmmveRQAAAM+aa95FAAAA0Zpr3kUAAAD9mmveRQAAAAGba95FAAAATJtr3kUAAABOm2veRQAAAHKba95FAAAAdZtr3kUAAACUm2veRQAAAJaba95FAAAArZtr3kUAAACwm2veRQAAAHqca95FAAAAfJxr3kUAAACRnGveRQAAAJSca95FAAAArpxr3kUAAACvnGveRQAAAMica95FAAAAy5xr3kUAAADynGveRQAAAPSca95FAAAAC51r3kUAAAAOnWveRQAAACSda95FAAAAJZ1r3kUAAAA4nWveRQAAADuda95FAAAAz51r3kUAAADRnWveRQAAAOada95FAAAA6Z1r3kUAAAAAnmveRQAAAAKea95FAAAAGZ5r3kUAAAAcnmveRQAAAD+ea95FAAAAQZ5r3kUAAABXnmveRQAAAFqea95FAAAAcJ5r3kUAAABxnmveRQAAAIiea95FAAAAQJ9r3kUAAAAnpWveRQAAACula95FAAAAVaVr3kUAAABYpWveRQAAAHGla95FAAAAdKVr3kUAAACgpWveRQAAAKKla95FAAAAWqpr3kUAAABcqmveRQAAAGCqa95FAAAAYqpr3kUAAACQqmveRQAAAJOqa95FAAAAu6pr3kUAAAC9qmveRQAAANKqa95FAAAA1apr3kUAAADrqmveRQAAAOyqa95FAAAA/qpr3kUAAAACq2veRQAAAIGra95FAAAAg6tr3kUAAACXq2veRQAAAJqra95FAAAAsatr3kUAAACyq2veRQAAAMmra95FAAAAzKtr3kUAAADtq2veRQAAAO+ra95FAAAABKxr3kUAAAAHrGveRQAAABysa95FAAAAHaxr3kUAAAAwrGveRQAAADOsa95FAAAAtaxr3kUAAAC3rGveRQAAAMusa95FAAAAzqxr3kUAAADkrGveRQAAAOasa95FAAAA/Kxr3kUAAAAArWveRQAAACGta95FAAAAIq1r3kUAAAA2rWveRQAAADmta95FAAAATq1r3kUAAABPrWveRQAAAGGta95FAAAAZK1r3kUAAADkrWveRQAAAOata95FAAAA+61r3kUAAAD+rWveRQAAABSua95FAAAAFq5r3kUAAAAtrmveRQAAADCua95FAAAAUa5r3kUAAABTrmveRQAAAGiua95FAAAAa65r3kUAAAB/rmveRQAAAIGua95FAAAAlK5r3kUAAACWrmveRQAAABeva95FAAAAGa9r3kUAAAAsr2veRQAAAC+va95FAAAARa9r3kUAAABHr2veRQAAAF2va95FAAAAYK9r3kUAAACCr2veRQAAAISva95FAAAAmK9r3kUAAACcr2veRQAAALCva95FAAAAsq9r3kUAAADEr2veRQAAAMeva95FAAAATLBr3kUAAABWsGveRQAAAAPKa95FAAAAYMpr3kUAAACi2WveRQAAAHnqa95FAAAA3O1r3kUAAADm7WveRQAAAELua95FAAAAT+5r3kUAAADR7mveRQAAAOvua95FAAAA5+9r3kUAAADp72veRQAAAMQ4bN5FAAAAyjhs3kUAAADXOGzeRQAAAOY4bN5FAAAA6Ths3kUAAADxOGzeRQAAAPo4bN5FAAAA/jhs3kUAAAABOWzeRQAAABE5bN5FAAAAHTls3kUAAAAmOWzeRQAAADI5bN5FAAAAnDls3kUAAACnOWzeRQAAAKs5bN5FAAAAxjls3kUAAADOOWzeRQAAANA5bN5FAAAA0zls3kUAAADWOWzeRQAAANo5bN5FAAAADjps3kUAAAAWOmzeRQAAAGk6bN5FAAAAezps3kUAAACjimzeRQAAALeKbN5FAAAAXZ5s3kUAAABnqWzeRQAAAEuqbN5FAAAATaps3kUAAACXqmzeRQAAAL2qbN5FAAAAxaps3kUAAADQqmzeRQAAAE6rbN5FAAAAhLJs3kUAAACgDG3eRQAAAKEMbd5FAAAApQxt3kUAAACuDG3eRQAAALANbd5FAAAAuw1t3kUAAAAzY23eRQAAAD5jbd5FAAAAx2ht3kUAAAAWaW3eRQAAAB5pbd5FAAAALGlt3kUAAACAam3eRQAAAI9qbd5FAAAAmWpt3kUAAACjam3eRQAAAPJqbd5FAAAAVGtt3kUAAABMbG3eRQAAAIxsbd5FAAAA22xt3kUAAAAPbW3eRQAAAF18bd5FAAAAZHxt3kUAAACggm3eRQAAALCCbd5FAAAAAaZt3kUAAAAAqW3eRQAAAHipbd5FAAAAeqlt3kUAAACmqW3eRQAAALqpbd5FAAAADqpt3kUAAAAfqm3eRQAAADqqbd5FAAAAVapt3kUAAAC3t23eRQAAAOO3bd5FAAAATtVt3kUAAABU1W3eRQAAAFntbd5FAAAAXO1t3kUAAAD8J27eRQAAAAYobt5FAAAAHyhu3kUAAAAsKG7eRQAAAFcobt5FAAAAWyhu3kUAAACZKG7eRQAAAKoobt5FAAAA3yhu3kUAAADkKG7eRQAAAM8tbt5FAAAA9y1u3kUAAAABLm7eRQAAAAoubt5FAAAAGi9u3kUAAAAfL27eRQAAAEsvbt5FAAAAxi9u3kUAAAC0M27eRQAAAMozbt5FAAAAIjRu3kUAAAAxNG7eRQAAAME3bt5FAAAA5Ddu3kUAAAAMAAAAu+lq3kUAAACIbm7eRQAAAAcAAAC86WreRQAAAG5sa95FAAAACAAAALzpat5FAAAAbWxr3kUAAAAJAAAAcWxr3kUAAAAz/2veRQAAAAsAAABxbGveRQAAADL/a95FAAAADAAAADT/a95FAAAADnps3kUAAAANAAAANf9r3kUAAAANemzeRQAAAAwAAAAPemzeRQAAANoGbd5FAAAADgAAAA96bN5FAAAA2QZt3kUAAAAMAAAA2wZt3kUAAADW8m3eRQAAAAoAAADY8m3eRQAAAIdubt5FAAAADwAAANnybd5FAAAAh25u3kUAAAAMAAAA";
		static String samplingFrame = "BAAAAExxAAACAAAAXAMAAIYAAADkEOoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAIAAAAbQBhAGkAbgBeAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXABwAHIAbwBmAGkAbABlAHIAdABlAHMAdAAuAGMAcABwAA4AAADHKeoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAiAAAAXwBfAHQAbQBhAGkAbgBDAFIAVABTAHQAYQByAHQAdQBwAF4AAABmADoAXABkAGQAXAB2AGMAdABvAG8AbABzAFwAYwByAHQAXwBiAGwAZABcAHMAZQBsAGYAXwB4ADgANgBcAGMAcgB0AFwAcwByAGMAXABjAHIAdABlAHgAZQAuAGMAKwIAAGZOUnIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAA+AAAAXwBjAGwAZQBhAG4AXwB0AHkAcABlAF8AaQBuAGYAbwBfAG4AYQBtAGUAcwBfAGkAbgB0AGUAcgBuAGEAbAAAAAAAAAAAAE06T3IAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAAKAAAAQwBJAHMAaQBuAAAAAAAAAAAApk1ScgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAD4AAABfAGMAbABlAGEAbgBfAHQAeQBwAGUAXwBpAG4AZgBvAF8AbgBhAG0AZQBzAF8AaQBuAHQAZQByAG4AYQBsAAAAAAAAAAAA5k5ScgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAD4AAABfAGMAbABlAGEAbgBfAHQAeQBwAGUAXwBpAG4AZgBvAF8AbgBhAG0AZQBzAF8AaQBuAHQAZQByAG4AYQBsAAAAAAAAAAAA4pJWdwAAAAA6AAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAFMAeQBzAFcATwBXADYANABcAG4AdABkAGwAbAAuAGQAbABsADYAAABSAHQAbABJAG4AaQB0AGkAYQBsAGkAegBlAEUAeABjAGUAcAB0AGkAbwBuAEMAaABhAGkAbgAAAAAAAAAAANYY6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlACYAAABUAGUAcwB0ADoAOgBTAGwAbwB3AEYAdQBuAGMAdABpAG8AbgAyAFoAAABkADoAXABiAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABwAHIAbwBmAGkAbABlAHIAdABlAHMAdABcAHQAZQBzAHQAZQBuAGcAaQBuAGUALgBjAHAAcAAxAAAAVjDqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUACgAAAEMASQBzAGkAbgAAAAAAAAAAAL4Z6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlACgAAABUAGUAcwB0ADoAOgBFAG4AZwBpAG4AZQA6ADoAVQBwAGQAYQB0AGUAWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwADoAAADeGOoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAmAAAAVABlAHMAdAA6ADoAUwBsAG8AdwBGAHUAbgBjAHQAaQBvAG4AMgBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAAMQAAADseSXIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFAAMQAwADAALgBkAGwAbAC6AAAAcwB0AGQAOgA6AGIAYQBzAGkAYwBfAG8AcwB0AHIAZQBhAG0APABjAGgAYQByACwAcwB0AGQAOgA6AGMAaABhAHIAXwB0AHIAYQBpAHQAcwA8AGMAaABhAHIAPgAgAD4AOgA6AGIAYQBzAGkAYwBfAG8AcwB0AHIAZQBhAG0APABjAGgAYQByACwAcwB0AGQAOgA6AGMAaABhAHIAXwB0AHIAYQBpAHQAcwA8AGMAaABhAHIAPgAgAD4AAAAAAAAAAAB6Mxx2AAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdwBvAHcANgA0AFwAawBlAHIAbgBlAGwAMwAyAC4AZABsAGwAJgAAAEIAYQBzAGUAVABoAHIAZQBhAGQASQBuAGkAdABUAGgAdQBuAGsAAAAAAAAAAAB7GuoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAyAAAAVABlAHMAdAA6ADoARQBuAGcAaQBuAGUAOgA6AFUAcABkAGEAdABlAEkAbgBwAHUAdABaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAASgAAAM0Y6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlACYAAABUAGUAcwB0ADoAOgBTAGwAbwB3AEYAdQBuAGMAdABpAG8AbgAyAFoAAABkADoAXABiAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABwAHIAbwBmAGkAbABlAHIAdABlAHMAdABcAHQAZQBzAHQAZQBuAGcAaQBuAGUALgBjAHAAcAAwAAAAs0ZPcgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAAoAAABDAEkAcABvAHcAAAAAAAAAAACgH+oAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQBqAAAAcwB0AGQAOgA6AHYAZQBjAHQAbwByADwAZgBsAG8AYQB0ACwAcwB0AGQAOgA6AGEAbABsAG8AYwBhAHQAbwByADwAZgBsAG8AYQB0AD4AIAA+ADoAOgBvAHAAZQByAGEAdABvAHIAWwBdAIoAAABjADoAXABwAHIAbwBnAHIAYQBtACAAZgBpAGwAZQBzACAAKAB4ADgANgApAFwAbQBpAGMAcgBvAHMAbwBmAHQAIAB2AGkAcwB1AGEAbAAgAHMAdAB1AGQAaQBvACAAMQAwAC4AMABcAHYAYwBcAGkAbgBjAGwAdQBkAGUAXAB2AGUAYwB0AG8AcgCgAwAAyBjqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUAJgAAAFQAZQBzAHQAOgA6AFMAbABvAHcARgB1AG4AYwB0AGkAbwBuADIAWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwADAAAACQGOoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAmAAAAVABlAHMAdAA6ADoAUwBsAG8AdwBGAHUAbgBjAHQAaQBvAG4AMgBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAALgAAAA9PUnIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAA+AAAAXwBjAGwAZQBhAG4AXwB0AHkAcABlAF8AaQBuAGYAbwBfAG4AYQBtAGUAcwBfAGkAbgB0AGUAcgBuAGEAbAAAAAAAAAAAAJAf6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlAF4AAABzAHQAZAA6ADoAdgBlAGMAdABvAHIAPABmAGwAbwBhAHQALABzAHQAZAA6ADoAYQBsAGwAbwBjAGEAdABvAHIAPABmAGwAbwBhAHQAPgAgAD4AOgA6AHMAaQB6AGUAigAAAGMAOgBcAHAAcgBvAGcAcgBhAG0AIABmAGkAbABlAHMAIAAoAHgAOAA2ACkAXABtAGkAYwByAG8AcwBvAGYAdAAgAHYAaQBzAHUAYQBsACAAcwB0AHUAZABpAG8AIAAxADAALgAwAFwAdgBjAFwAaQBuAGMAbAB1AGQAZQBcAHYAZQBjAHQAbwByAG4DAACPTlJyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAACzGOoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAmAAAAVABlAHMAdAA6ADoAUwBsAG8AdwBGAHUAbgBjAHQAaQBvAG4AMgBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAALwAAANsY6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlACYAAABUAGUAcwB0ADoAOgBTAGwAbwB3AEYAdQBuAGMAdABpAG8AbgAyAFoAAABkADoAXABiAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABwAHIAbwBmAGkAbABlAHIAdABlAHMAdABcAHQAZQBzAHQAZQBuAGcAaQBuAGUALgBjAHAAcAAxAAAAATpPcgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAAoAAABDAEkAcwBpAG4AAAAAAAAAAAAbJeoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQA0AAAAVABlAHMAdAA6ADoAUwBsAG8AdwBGAHUAbgBjAHQAaQBvAG4APAAyADYAMgAxADQANAA+AFoAAABkADoAXABiAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABwAHIAbwBmAGkAbABlAHIAdABlAHMAdABcAHQAZQBzAHQAZQBuAGcAaQBuAGUALgBjAHAAcAAkAAAA+k5ScgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAD4AAABfAGMAbABlAGEAbgBfAHQAeQBwAGUAXwBpAG4AZgBvAF8AbgBhAG0AZQBzAF8AaQBuAHQAZQByAG4AYQBsAAAAAAAAAAAA2BjqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUAJgAAAFQAZQBzAHQAOgA6AFMAbABvAHcARgB1AG4AYwB0AGkAbwBuADIAWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwADEAAADDGOoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAmAAAAVABlAHMAdAA6ADoAUwBsAG8AdwBGAHUAbgBjAHQAaQBvAG4AMgBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAAMAAAAGJPUnIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAA+AAAAXwBjAGwAZQBhAG4AXwB0AHkAcABlAF8AaQBuAGYAbwBfAG4AYQBtAGUAcwBfAGkAbgB0AGUAcgBuAGEAbAAAAAAAAAAAAMMZ6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlACgAAABUAGUAcwB0ADoAOgBFAG4AZwBpAG4AZQA6ADoAVQBwAGQAYQB0AGUAWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwADwAAACpGOoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAmAAAAVABlAHMAdAA6ADoAUwBsAG8AdwBGAHUAbgBjAHQAaQBvAG4AMgBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAALgAAAPodwFYAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAQwBvAHIAZQAuAGQAbABsAEoAAABQAHIAbwBmAGkAbABlAHIAOgA6AEMAbwByAGUAOgA6AEQAdQBtAHAAQwBhAHAAdAB1AHIAaQBuAGcAUAByAG8AZwByAGUAcwBzAE4AAABkADoAXABiAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABwAHIAbwBmAGkAbABlAHIAYwBvAHIAZQBcAGMAbwByAGUALgBjAHAAcADBAAAA4xjqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUAJgAAAFQAZQBzAHQAOgA6AFMAbABvAHcARgB1AG4AYwB0AGkAbwBuADIAWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwADEAAAC6GOoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAmAAAAVABlAHMAdAA6ADoAUwBsAG8AdwBGAHUAbgBjAHQAaQBvAG4AMgBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAALwAAAOA5T3IAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAAKAAAAQwBJAHMAaQBuAAAAAAAAAAAAlxjqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUAJgAAAFQAZQBzAHQAOgA6AFMAbABvAHcARgB1AG4AYwB0AGkAbwBuADIAWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwAC4AAAC2TVJyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAAD2TlJyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAACxGOoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAmAAAAVABlAHMAdAA6ADoAUwBsAG8AdwBGAHUAbgBjAHQAaQBvAG4AMgBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAALgAAAM8Y6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlACYAAABUAGUAcwB0ADoAOgBTAGwAbwB3AEYAdQBuAGMAdABpAG8AbgAyAFoAAABkADoAXABiAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABwAHIAbwBmAGkAbABlAHIAdABlAHMAdABcAHQAZQBzAHQAZQBuAGcAaQBuAGUALgBjAHAAcAAxAAAAbk5ScgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAD4AAABfAGMAbABlAGEAbgBfAHQAeQBwAGUAXwBpAG4AZgBvAF8AbgBhAG0AZQBzAF8AaQBuAHQAZQByAG4AYQBsAAAAAAAAAAAAHRvqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUAOAAAAFQAZQBzAHQAOgA6AEUAbgBnAGkAbgBlADoAOgBVAHAAZABhAHQAZQBNAGUAcwBzAGEAZwBlAHMAWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwAE8AAABwF+oAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAGAAAAcwBpAG4AigAAAGMAOgBcAHAAcgBvAGcAcgBhAG0AIABmAGkAbABlAHMAIAAoAHgAOAA2ACkAXABtAGkAYwByAG8AcwBvAGYAdAAgAHYAaQBzAHUAYQBsACAAcwB0AHUAZABpAG8AIAAxADAALgAwAFwAdgBjAFwAaQBuAGMAbAB1AGQAZQBcAG0AYQB0AGgALgBoABoCAABLF+oAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAIAAAAcwBpAG4AZgCKAAAAYwA6AFwAcAByAG8AZwByAGEAbQAgAGYAaQBsAGUAcwAgACgAeAA4ADYAKQBcAG0AaQBjAHIAbwBzAG8AZgB0ACAAdgBpAHMAdQBhAGwAIABzAHQAdQBkAGkAbwAgADEAMAAuADAAXAB2AGMAXABpAG4AYwBsAHUAZABlAFwAbQBhAHQAaAAuAGgArgEAAPE5T3IAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAAKAAAAQwBJAHMAaQBuAAAAAAAAAAAAYjpPcgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAAoAAABDAEkAcwBpAG4AAAAAAAAAAACbTlJyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAACgTVJyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAABSOk9yAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwACgAAAEMASQBzAGkAbgAAAAAAAAAAAItOUnIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAA+AAAAXwBjAGwAZQBhAG4AXwB0AHkAcABlAF8AaQBuAGYAbwBfAG4AYQBtAGUAcwBfAGkAbgB0AGUAcgBuAGEAbAAAAAAAAAAAAE86T3IAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAAKAAAAQwBJAHMAaQBuAAAAAAAAAAAARTpPcgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAAoAAABDAEkAcwBpAG4AAAAAAAAAAAC+TVJyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAABQOk9yAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwACgAAAEMASQBzAGkAbgAAAAAAAAAAAPJOUnIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAA+AAAAXwBjAGwAZQBhAG4AXwB0AHkAcABlAF8AaQBuAGYAbwBfAG4AYQBtAGUAcwBfAGkAbgB0AGUAcgBuAGEAbAAAAAAAAAAAAFM6T3IAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAAKAAAAQwBJAHMAaQBuAAAAAAAAAAAAx01ScgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAD4AAABfAGMAbABlAGEAbgBfAHQAeQBwAGUAXwBpAG4AZgBvAF8AbgBhAG0AZQBzAF8AaQBuAHQAZQByAG4AYQBsAAAAAAAAAAAAh05ScgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAD4AAABfAGMAbABlAGEAbgBfAHQAeQBwAGUAXwBpAG4AZgBvAF8AbgBhAG0AZQBzAF8AaQBuAHQAZQByAG4AYQBsAAAAAAAAAAAAxE5ScgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAD4AAABfAGMAbABlAGEAbgBfAHQAeQBwAGUAXwBpAG4AZgBvAF8AbgBhAG0AZQBzAF8AaQBuAHQAZQByAG4AYQBsAAAAAAAAAAAAODpPcgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAAoAAABDAEkAcwBpAG4AAAAAAAAAAAD5TVJyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAAAXT1JyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAABAF+oAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAIAAAAcwBpAG4AZgCKAAAAYwA6AFwAcAByAG8AZwByAGEAbQAgAGYAaQBsAGUAcwAgACgAeAA4ADYAKQBcAG0AaQBjAHIAbwBzAG8AZgB0ACAAdgBpAHMAdQBhAGwAIABzAHQAdQBkAGkAbwAgADEAMAAuADAAXAB2AGMAXABpAG4AYwBsAHUAZABlAFwAbQBhAHQAaAAuAGgArgEAAOw5T3IAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAAKAAAAQwBJAHMAaQBuAAAAAAAAAAAARhfqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUACAAAAHMAaQBuAGYAigAAAGMAOgBcAHAAcgBvAGcAcgBhAG0AIABmAGkAbABlAHMAIAAoAHgAOAA2ACkAXABtAGkAYwByAG8AcwBvAGYAdAAgAHYAaQBzAHUAYQBsACAAcwB0AHUAZABpAG8AIAAxADAALgAwAFwAdgBjAFwAaQBuAGMAbAB1AGQAZQBcAG0AYQB0AGgALgBoAK4BAAAMOk9yAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwACgAAAEMASQBzAGkAbgAAAAAAAAAAAFEX6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlAAgAAABzAGkAbgBmAIoAAABjADoAXABwAHIAbwBnAHIAYQBtACAAZgBpAGwAZQBzACAAKAB4ADgANgApAFwAbQBpAGMAcgBvAHMAbwBmAHQAIAB2AGkAcwB1AGEAbAAgAHMAdAB1AGQAaQBvACAAMQAwAC4AMABcAHYAYwBcAGkAbgBjAGwAdQBkAGUAXABtAGEAdABoAC4AaACuAQAAThfqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUACAAAAHMAaQBuAGYAigAAAGMAOgBcAHAAcgBvAGcAcgBhAG0AIABmAGkAbABlAHMAIAAoAHgAOAA2ACkAXABtAGkAYwByAG8AcwBvAGYAdAAgAHYAaQBzAHUAYQBsACAAcwB0AHUAZABpAG8AIAAxADAALgAwAFwAdgBjAFwAaQBuAGMAbAB1AGQAZQBcAG0AYQB0AGgALgBoAK4BAABrF+oAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAGAAAAcwBpAG4AigAAAGMAOgBcAHAAcgBvAGcAcgBhAG0AIABmAGkAbABlAHMAIAAoAHgAOAA2ACkAXABtAGkAYwByAG8AcwBvAGYAdAAgAHYAaQBzAHUAYQBsACAAcwB0AHUAZABpAG8AIAAxADAALgAwAFwAdgBjAFwAaQBuAGMAbAB1AGQAZQBcAG0AYQB0AGgALgBoABoCAABkF+oAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAGAAAAcwBpAG4AigAAAGMAOgBcAHAAcgBvAGcAcgBhAG0AIABmAGkAbABlAHMAIAAoAHgAOAA2ACkAXABtAGkAYwByAG8AcwBvAGYAdAAgAHYAaQBzAHUAYQBsACAAcwB0AHUAZABpAG8AIAAxADAALgAwAFwAdgBjAFwAaQBuAGMAbAB1AGQAZQBcAG0AYQB0AGgALgBoABoCAABgF+oAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAGAAAAcwBpAG4AigAAAGMAOgBcAHAAcgBvAGcAcgBhAG0AIABmAGkAbABlAHMAIAAoAHgAOAA2ACkAXABtAGkAYwByAG8AcwBvAGYAdAAgAHYAaQBzAHUAYQBsACAAcwB0AHUAZABpAG8AIAAxADAALgAwAFwAdgBjAFwAaQBuAGMAbAB1AGQAZQBcAG0AYQB0AGgALgBoABoCAAAIJeoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQA0AAAAVABlAHMAdAA6ADoAUwBsAG8AdwBGAHUAbgBjAHQAaQBvAG4APAAyADYAMgAxADQANAA+AFoAAABkADoAXABiAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABwAHIAbwBmAGkAbABlAHIAdABlAHMAdABcAHQAZQBzAHQAZQBuAGcAaQBuAGUALgBjAHAAcAAkAAAAISXqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUANAAAAFQAZQBzAHQAOgA6AFMAbABvAHcARgB1AG4AYwB0AGkAbwBuADwAMgA2ADIAMQA0ADQAPgBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAAJAAAADEl6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlADQAAABUAGUAcwB0ADoAOgBTAGwAbwB3AEYAdQBuAGMAdABpAG8AbgA8ADIANgAyADEANAA0AD4AWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwACQAAAArJeoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQA0AAAAVABlAHMAdAA6ADoAUwBsAG8AdwBGAHUAbgBjAHQAaQBvAG4APAAyADYAMgAxADQANAA+AFoAAABkADoAXABiAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABwAHIAbwBmAGkAbABlAHIAdABlAHMAdABcAHQAZQBzAHQAZQBuAGcAaQBuAGUALgBjAHAAcAAkAAAAdRfqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUABgAAAHMAaQBuAIoAAABjADoAXABwAHIAbwBnAHIAYQBtACAAZgBpAGwAZQBzACAAKAB4ADgANgApAFwAbQBpAGMAcgBvAHMAbwBmAHQAIAB2AGkAcwB1AGEAbAAgAHMAdAB1AGQAaQBvACAAMQAwAC4AMABcAHYAYwBcAGkAbgBjAGwAdQBkAGUAXABtAGEAdABoAC4AaAAaAgAAEiXqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUANAAAAFQAZQBzAHQAOgA6AFMAbABvAHcARgB1AG4AYwB0AGkAbwBuADwAMgA2ADIAMQA0ADQAPgBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAAJAAAAMgZ6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlACgAAABUAGUAcwB0ADoAOgBFAG4AZwBpAG4AZQA6ADoAVQBwAGQAYQB0AGUAWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwAD4AAAC9G+oAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAyAAAAVABlAHMAdAA6ADoARQBuAGcAaQBuAGUAOgA6AFUAcABkAGEAdABlAEwAbwBnAGkAYwBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAAVAAAAAlPUnIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAA+AAAAXwBjAGwAZQBhAG4AXwB0AHkAcABlAF8AaQBuAGYAbwBfAG4AYQBtAGUAcwBfAGkAbgB0AGUAcgBuAGEAbAAAAAAAAAAAABNPUnIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAA+AAAAXwBjAGwAZQBhAG4AXwB0AHkAcABlAF8AaQBuAGYAbwBfAG4AYQBtAGUAcwBfAGkAbgB0AGUAcgBuAGEAbAAAAAAAAAAAAC86T3IAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAAKAAAAQwBJAHMAaQBuAAAAAAAAAAAAPTpPcgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAAoAAABDAEkAcwBpAG4AAAAAAAAAAAA2Rk9yAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwACgAAAEMASQBwAG8AdwAAAAAAAAAAAL9iwFYAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAQwBvAHIAZQAuAGQAbABsACwAAABQAHIAbwBmAGkAbABlAHIAOgA6AFMAbwBjAGsAZQB0ADoAOgBTAGUAbgBkAE4AAABkADoAXABiAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABwAHIAbwBmAGkAbABlAHIAYwBvAHIAZQBcAHMAbwBjAGsAZQB0AC4AaACQAAAADU5ScgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAD4AAABfAGMAbABlAGEAbgBfAHQAeQBwAGUAXwBpAG4AZgBvAF8AbgBhAG0AZQBzAF8AaQBuAHQAZQByAG4AYQBsAAAAAAAAAAAAJUZPcgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAAoAAABDAEkAcABvAHcAAAAAAAAAAADDTVJyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAADLTVJyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAACzTlJyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAAACT1JyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAACLRk9yAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwACgAAAEMASQBwAG8AdwAAAAAAAAAAAFU6T3IAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAAKAAAAQwBJAHMAaQBuAAAAAAAAAAAAACXqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUANAAAAFQAZQBzAHQAOgA6AFMAbABvAHcARgB1AG4AYwB0AGkAbwBuADwAMgA2ADIAMQA0ADQAPgBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAAJAAAAM0Z6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlACgAAABUAGUAcwB0ADoAOgBFAG4AZwBpAG4AZQA6ADoAVQBwAGQAYQB0AGUAWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwAEAAAABdHOoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAyAAAAVABlAHMAdAA6ADoARQBuAGcAaQBuAGUAOgA6AFUAcABkAGEAdABlAFMAYwBlAG4AZQBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAAWQAAAOJOUnIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAA+AAAAXwBjAGwAZQBhAG4AXwB0AHkAcABlAF8AaQBuAGYAbwBfAG4AYQBtAGUAcwBfAGkAbgB0AGUAcgBuAGEAbAAAAAAAAAAAAEZOUnIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAA+AAAAXwBjAGwAZQBhAG4AXwB0AHkAcABlAF8AaQBuAGYAbwBfAG4AYQBtAGUAcwBfAGkAbgB0AGUAcgBuAGEAbAAAAAAAAAAAAM9NUnIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAA+AAAAXwBjAGwAZQBhAG4AXwB0AHkAcABlAF8AaQBuAGYAbwBfAG4AYQBtAGUAcwBfAGkAbgB0AGUAcgBuAGEAbAAAAAAAAAAAAH5OUnIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAA+AAAAXwBjAGwAZQBhAG4AXwB0AHkAcABlAF8AaQBuAGYAbwBfAG4AYQBtAGUAcwBfAGkAbgB0AGUAcgBuAGEAbAAAAAAAAAAAAG06T3IAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAAKAAAAQwBJAHMAaQBuAAAAAAAAAAAA1U5ScgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsAD4AAABfAGMAbABlAGEAbgBfAHQAeQBwAGUAXwBpAG4AZgBvAF8AbgBhAG0AZQBzAF8AaQBuAHQAZQByAG4AYQBsAAAAAAAAAAAAZxfqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUABgAAAHMAaQBuAIoAAABjADoAXABwAHIAbwBnAHIAYQBtACAAZgBpAGwAZQBzACAAKAB4ADgANgApAFwAbQBpAGMAcgBvAHMAbwBmAHQAIAB2AGkAcwB1AGEAbAAgAHMAdAB1AGQAaQBvACAAMQAwAC4AMABcAHYAYwBcAGkAbgBjAGwAdQBkAGUAXABtAGEAdABoAC4AaAAaAgAA0hnqAAAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUAKAAAAFQAZQBzAHQAOgA6AEUAbgBnAGkAbgBlADoAOgBVAHAAZABhAHQAZQBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAAQgAAAJod6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlADYAAABUAGUAcwB0ADoAOgBFAG4AZwBpAG4AZQA6ADoAVQBwAGQAYQB0AGUAUABoAHkAcwBpAGMAcwBaAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAAYwAAAKVE6nYAAAAARAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB3AG8AdwA2ADQAXABLAEUAUgBOAEUATABCAEEAUwBFAC4AZABsAGwACgAAAFMAbABlAGUAcAAAAAAAAAAAAN39VHcAAAAAOgAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABTAHkAcwBXAE8AVwA2ADQAXABuAHQAZABsAGwALgBkAGwAbAAgAAAAWgB3AEQAZQBsAGEAeQBFAHgAZQBjAHUAdABpAG8AbgAAAAAAAAAAANcZ6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlACgAAABUAGUAcwB0ADoAOgBFAG4AZwBpAG4AZQA6ADoAVQBwAGQAYQB0AGUAWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwAEQAAAD9HOoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAkAAAAVABlAHMAdAA6ADoARQBuAGcAaQBuAGUAOgA6AEQAcgBhAHcAWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwAF4AAABWTlJyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAAAxTlJyAAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdABlAG0AMwAyAFwATQBTAFYAQwBSADEAMAAwAC4AZABsAGwAPgAAAF8AYwBsAGUAYQBuAF8AdAB5AHAAZQBfAGkAbgBmAG8AXwBuAGEAbQBlAHMAXwBpAG4AdABlAHIAbgBhAGwAAAAAAAAAAADxHOoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAkAAAAVABlAHMAdAA6ADoARQBuAGcAaQBuAGUAOgA6AEQAcgBhAHcAWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwAFwAAACwQMBWAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAEMAbwByAGUALgBkAGwAbAA4AAAAUAByAG8AZgBpAGwAZQByADoAOgBDAGEAdABlAGcAbwByAHkAOgA6AEMAYQB0AGUAZwBvAHIAeQBQAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAGMAbwByAGUAXABlAHYAZQBuAHQALgBjAHAAcABMAAAAwT7AVgAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBDAG8AcgBlAC4AZABsAGwALAAAAFAAcgBvAGYAaQBsAGUAcgA6ADoARQB2AGUAbgB0ADoAOgBTAHQAYQByAHQAUAAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgBjAG8AcgBlAFwAZQB2AGUAbgB0AC4AYwBwAHAAJAAAAGkZ6gAAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlACgAAABUAGUAcwB0ADoAOgBFAG4AZwBpAG4AZQA6ADoAVQBwAGQAYQB0AGUAWgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgB0AGUAcwB0AFwAdABlAHMAdABlAG4AZwBpAG4AZQAuAGMAcABwADcAAAC2G8BWAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAEMAbwByAGUALgBkAGwAbAAsAAAAUAByAG8AZgBpAGwAZQByADoAOgBDAG8AcgBlADoAOgBVAHAAZABhAHQAZQBOAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAGMAbwByAGUAXABjAG8AcgBlAC4AYwBwAHAAkgAAAI0SwFYAAAAAVAAAAEQAOgBcAEIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAQwBvAHIAZQAuAGQAbABsADgAAABQAHIAbwBmAGkAbABlAHIAOgA6AEMAbwByAGUAOgA6AEQAdQBtAHAAUAByAG8AZwByAGUAcwBzAE4AAABkADoAXABiAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABwAHIAbwBmAGkAbABlAHIAYwBvAHIAZQBcAGMAbwByAGUALgBjAHAAcAAlAAAA/2XAVgAAAABUAAAARAA6AFwAQgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBDAG8AcgBlAC4AZABsAGwALAAAAFAAcgBvAGYAaQBsAGUAcgA6ADoAUwBlAHIAdgBlAHIAOgA6AFMAZQBuAGQAYgAAAGQAOgBcAGIAcgBvAGYAaQBsAGUAcgBcAHQAcgB1AG4AawBcAHAAcgBvAGYAaQBsAGUAcgBjAG8AcgBlAFwAcAByAG8AZgBpAGwAZQByAHMAZQByAHYAZQByAC4AYwBwAHAALgAAAE2JUnIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAA+AAAAXwBjAGwAZQBhAG4AXwB0AHkAcABlAF8AaQBuAGYAbwBfAG4AYQBtAGUAcwBfAGkAbgB0AGUAcgBuAGEAbAAAAAAAAAAAAPUSHHYAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB3AG8AdwA2ADQAXABrAGUAcgBuAGUAbAAzADIALgBkAGwAbAASAAAAVwByAGkAdABlAEYAaQBsAGUAAAAAAAAAAAB5b0V2AAAAADwAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdwBvAHcANgA0AFwAVwBTADIAXwAzADIALgBkAGwAbAAIAAAAcwBlAG4AZAAAAAAAAAAAAG35VHcAAAAAOgAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABTAHkAcwBXAE8AVwA2ADQAXABuAHQAZABsAGwALgBkAGwAbAAqAAAATgB0AEQAZQB2AGkAYwBlAEkAbwBDAG8AbgB0AHIAbwBsAEYAaQBsAGUAAAAAAAAAAADcEOoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAIAAAAbQBhAGkAbgBeAAAAZAA6AFwAYgByAG8AZgBpAGwAZQByAFwAdAByAHUAbgBrAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXABwAHIAbwBmAGkAbABlAHIAdABlAHMAdAAuAGMAcABwAA4AAADTFeoAAAAAAFQAAABEADoAXABCAHIAbwBmAGkAbABlAHIAXAB0AHIAdQBuAGsAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQBQAAAAcwB0AGQAOgA6AG8AcABlAHIAYQB0AG8AcgA8ADwAPABzAHQAZAA6ADoAYwBoAGEAcgBfAHQAcgBhAGkAdABzADwAYwBoAGEAcgA+ACAAPgCMAAAAYwA6AFwAcAByAG8AZwByAGEAbQAgAGYAaQBsAGUAcwAgACgAeAA4ADYAKQBcAG0AaQBjAHIAbwBzAG8AZgB0ACAAdgBpAHMAdQBhAGwAIABzAHQAdQBkAGkAbwAgADEAMAAuADAAXAB2AGMAXABpAG4AYwBsAHUAZABlAFwAbwBzAHQAcgBlAGEAbQBBAwAATt5IcgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUAAxADAAMAAuAGQAbABsAHIAAABzAHQAZAA6ADoAYgBhAHMAaQBjAF8AcwB0AHIAZQBhAG0AYgB1AGYAPABjAGgAYQByACwAcwB0AGQAOgA6AGMAaABhAHIAXwB0AHIAYQBpAHQAcwA8AGMAaABhAHIAPgAgAD4AOgA6AHMAcAB1AHQAYwAAAAAAAAAAAOgUSXIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFAAMQAwADAALgBkAGwAbADCAAAAcwB0AGQAOgA6AGIAYQBzAGkAYwBfAHMAdAByAGUAYQBtAGIAdQBmADwAYwBoAGEAcgAsAHMAdABkADoAOgBjAGgAYQByAF8AdAByAGEAaQB0AHMAPABjAGgAYQByAD4AIAA+ADoAOgBiAGEAcwBpAGMAXwBzAHQAcgBlAGEAbQBiAHUAZgA8AGMAaABhAHIALABzAHQAZAA6ADoAYwBoAGEAcgBfAHQAcgBhAGkAdABzADwAYwBoAGEAcgA+ACAAPgAAAAAAAAAAAM01VXIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAAIAAAAcAB1AHQAYwAAAAAAAAAAAOHsUHIAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB0AGUAbQAzADIAXABNAFMAVgBDAFIAMQAwADAALgBkAGwAbAAKAAAAdwByAGkAdABlAAAAAAAAAAAAt/BQcgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsABoAAABmAGYAbAB1AHMAaABfAG4AbwBsAG8AYwBrAAAAAAAAAAAA6hIcdgAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHcAbwB3ADYANABcAGsAZQByAG4AZQBsADMAMgAuAGQAbABsABIAAABXAHIAaQB0AGUARgBpAGwAZQAAAAAAAAAAAD0THHYAAAAAQAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB3AG8AdwA2ADQAXABrAGUAcgBuAGUA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";

		public EventFrame EventFrame
		{
			get
			{
				return this[0] as EventFrame;
			}
		}

		public SamplingFrame SamplingFrame
		{
			get
			{
				return this[Count - 1] as SamplingFrame;
			}
		}

		public TestFrameCollection()
		{
			DataResponse descriptionResponse = DataResponse.Create(descriptionBoard);
			Add(descriptionResponse);

			for (int i = 0; i < 32; ++i)
			{
				DataResponse eventResponse = DataResponse.Create(eventFrame);
				Add(eventResponse);
			}

			DataResponse samplingResponse = DataResponse.Create(samplingFrame);
			Add(samplingResponse);
		}
	}

	public class TestEventFrame
	{
		public static EventFrame Frame
		{
			get
			{
				return (new TestFrameCollection()).EventFrame;
			}
		}


		public static Board<EventBoardItem, EventDescription, EventNode> Board
		{
			get
			{
				return Frame.Board;
			}
		}
	}

	public class TestSamplingFrame
	{
		public static SamplingFrame Frame
		{
			get
			{
				return (new TestFrameCollection()).SamplingFrame;
			}
		}
	}

	public class TestSamplingNode
	{
		public static SamplingNode Node
		{
			get
			{
				return (new TestFrameCollection()).SamplingFrame.Root;
			}
		}
	}
}
