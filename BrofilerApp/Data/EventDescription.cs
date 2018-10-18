using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Media;

namespace Profiler.Data
{
	public class FileLine
	{
		public FileLine(String file, int line)
		{
			File = file;
			Line = line;

			if (!String.IsNullOrEmpty(File))
			{
				int index = File.LastIndexOfAny(new char[] { '\\', '/' });
				ShortName = index != -1 ? File.Substring(index + 1) : File;
				ShortPath = String.Format("{0}:{1}", ShortName, Line);
			}
		}

		public String File { get; private set; }
		public int Line { get; private set; }
		public String ShortName { get; private set; }
		public String ShortPath { get; private set; }

		public override string ToString()
		{
			return ShortPath;
		}

		public static FileLine Empty = new FileLine(String.Empty, 0);
	}

	public class EventDescription : Description
	{
		private bool isSampling = false;
		public bool IsSampling
		{
			get { return isSampling; }
			set
			{
				if (isSampling != value)
				{
					ProfilerClient.Get().SendMessage(new TurnSamplingMessage(id, value));
					isSampling = value;
				}
			}
		}

		private int id;
		private Color forceColor;

		public Color Color { get; private set; }
		public Color ForceColor
		{
			get
			{
				if (forceColor.A == 0)
				{
					if (Color.A > 0)
					{
						forceColor = Color;
					}
					else
					{
						Random rnd = new Random(FullName.GetHashCode());

						do
						{
							forceColor = Color.FromRgb((byte)rnd.Next(), (byte)rnd.Next(), (byte)rnd.Next());
						} while (DirectX.Utils.GetLuminance(forceColor) < DirectX.Utils.LuminanceThreshold);
					}
				}
				return forceColor;
			}
		}
		public Brush Brush { get; private set; }
		public UInt32 Filter { get; private set; }
		public float Budget { get; private set; }

		public bool IsSleep { get { return Color == Colors.White; } }

		public EventDescription() { }
		public EventDescription(String name, int id)
		{
			FullName = name;
			this.id = id;
		}

		const byte IS_SAMPLING_FLAG = 0x1;

		public void SetOverrideColor(Color color)
		{
			Color = color;
			Brush = new SolidColorBrush(color);
		}

		static public EventDescription Read(BinaryReader reader, int id)
		{
			EventDescription desc = new EventDescription();
			int nameLength = reader.ReadInt32();
			desc.FullName = new String(reader.ReadChars(nameLength));
			desc.id = id;

			int fileLength = reader.ReadInt32();
			String file = new String(reader.ReadChars(fileLength));
			desc.Path = new FileLine(file, reader.ReadInt32());
			desc.Filter = reader.ReadUInt32();
			UInt32 color = reader.ReadUInt32();
			desc.Color = Color.FromArgb((byte)(color >> 24),
										(byte)(color >> 16),
										(byte)(color >> 8),
										(byte)(color));

			desc.Brush = new SolidColorBrush(desc.Color);
			desc.Budget = reader.ReadSingle();
			byte flags = reader.ReadByte();
			desc.isSampling = (flags & IS_SAMPLING_FLAG) != 0;

			return desc;
		}

		public override Object GetSharedKey()
		{
			return this;
		}
	}

	public class FiberDescription
	{
		public UInt64 fiberID { get; set; }

		public static FiberDescription Read(DataResponse response)
		{
			BinaryReader reader = response.Reader;
			FiberDescription res = new FiberDescription();
			res.fiberID = reader.ReadUInt64();
			return res;
		}
	}

	public class ThreadDescription
	{
		public String Name { get; set; }
		public UInt64 ThreadID { get; set; }
		public Int32 MaxDepth { get; set; }
		public Int32 Priority { get; set; }
		public Int32 Mask { get; set; }

		public const UInt64 InvalidThreadID = UInt64.MaxValue;

		public static ThreadDescription Read(DataResponse response)
		{
			BinaryReader reader = response.Reader;
			ThreadDescription res = new ThreadDescription();

			res.ThreadID = reader.ReadUInt64();
			int nameLength = reader.ReadInt32();
			res.Name = new String(reader.ReadChars(nameLength));
			res.MaxDepth = reader.ReadInt32();
			res.Priority = reader.ReadInt32();
			res.Mask = reader.ReadInt32();
			return res;
		}
	}

	public class EventDescriptionBoard : IResponseHolder
	{
		public Stream BaseStream { get; private set; }
		public int ID { get; private set; }
		public Durable TimeSlice { get; private set; }
		public int MainThreadIndex { get; private set; }
		public TimeSettings TimeSettings { get; private set; }
		public List<ThreadDescription> Threads { get; private set; }
		public Dictionary<UInt64, int> ThreadID2ThreadIndex { get; private set; }
		public List<FiberDescription> Fibers { get; private set; }

		private List<EventDescription> board = new List<EventDescription>();
		public List<EventDescription> Board
		{
			get { return board; }
		}
		public EventDescriptionBoard() { }

		public EventDescription this[int pos]
		{
			get
			{
				return board[pos];
			}
		}

		public static EventDescriptionBoard Read(DataResponse response)
		{
			BinaryReader reader = response.Reader;

			EventDescriptionBoard desc = new EventDescriptionBoard();
			desc.Response = response;
			desc.BaseStream = reader.BaseStream;
			desc.ID = reader.ReadInt32();

			desc.TimeSettings = new TimeSettings();
			desc.TimeSettings.TicksToMs = 1000.0 / (double)reader.ReadInt64();
			desc.TimeSettings.Origin = reader.ReadInt64();
			desc.TimeSettings.PrecisionCut = reader.ReadInt32();
			Durable.InitSettings(desc.TimeSettings);

			desc.TimeSlice = new Durable();
			desc.TimeSlice.ReadDurable(reader);

			int threadCount = reader.ReadInt32();
			desc.Threads = new List<ThreadDescription>(threadCount);
			desc.ThreadID2ThreadIndex = new Dictionary<UInt64, int>();

			for (int i = 0; i < threadCount; ++i)
			{
				ThreadDescription threadDesc = ThreadDescription.Read(response);
				desc.Threads.Add(threadDesc);

				if (!desc.ThreadID2ThreadIndex.ContainsKey(threadDesc.ThreadID))
				{
					desc.ThreadID2ThreadIndex.Add(threadDesc.ThreadID, i);
				}
				else
				{
					// The old thread was finished and the new thread was started 
					// with the same threadID during one profiling session.
					// Can't do much here - lets show information for the new thread only.
					desc.ThreadID2ThreadIndex[threadDesc.ThreadID] = i;
				}
			}

			if (response.ApplicationID == NetworkProtocol.BROFILER_APP_ID)
			{
				int fibersCount = reader.ReadInt32();
				desc.Fibers = new List<FiberDescription>(fibersCount);
				for (int i = 0; i < fibersCount; ++i)
				{
					FiberDescription fiberDesc = FiberDescription.Read(response);
					desc.Fibers.Add(fiberDesc);
				}
			}

			desc.MainThreadIndex = reader.ReadInt32();

			int count = reader.ReadInt32();
			for (int i = 0; i < count; ++i)
			{
				desc.board.Add(EventDescription.Read(reader, i));
			}

			// TODO: Tags

			// TODO: Run Info

			// TODO: Run Info

			// TODO: Filters

			// TODO: Mode

			// TODO: Thread Descriptions

			return desc;
		}

		public override DataResponse Response { get; set; }
	}

	public class Entry : EventData, IComparable<Entry>
	{
		public EventDescription Description { get; private set; }
		public EventFrame Frame { get; set; }

		Entry() { }

		public Entry(EventDescription desc, long start, long finish) : base(start, finish)
		{
			this.Description = desc;
		}

		public void SetOverrideColor(Color color)
		{
			Description.SetOverrideColor(color);
		}

		public static Entry Read(BinaryReader reader, EventDescriptionBoard board)
		{
			Entry res = new Entry();
			res.ReadEventData(reader);

			int descriptionID = reader.ReadInt32();
			res.Description = board[descriptionID];

			return res;
		}

		public double CalculateWork()
		{
			return Frame == null ? Duration : Frame.CalculateWork(this);
		}

		public int CompareTo(Entry other)
		{
			if (other.Start != Start)
				return Start < other.Start ? -1 : 1;
			else
				return Finish == other.Finish ? 0 : Finish > other.Finish ? -1 : 1;
		}
	}
}
