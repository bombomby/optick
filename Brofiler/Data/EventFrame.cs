using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media;
using System.Threading;

namespace Profiler.Data
{
	public class FrameHeader : EventData
	{
		public int ThreadIndex { get; private set; }

    public static FrameHeader Read(BinaryReader reader)
    {
      FrameHeader header = new FrameHeader();
			header.ThreadIndex = reader.ReadInt32();
      header.ReadEventData(reader);
      return header;
    }
  }

  public class EventFrame : Frame
  {
    public override DataResponse.Type ResponseType { get { return DataResponse.Type.EventFrame; } }

    public FrameHeader Header {get; private set;}

    private List<Entry> entries = new List<Entry>();

    private EventTree root = null;
    public Profiler.Data.EventTree Root
    {
      get
      {
        if (root == null)
          Load();

        return root;
      }
    }

    private BinaryReader reader = null;

    public long Start { get { return Header.Start; } }
    public long Finish { get { return Header.Finish; } }

    public override double Duration
    {
      get
      {
        return Header.Duration;
      }
    }

    public override string Description
    {
      get
      {
        return String.Format("{0:0} ms", Header.Duration);
      }
    }

    public EventDescriptionBoard DescriptionBoard { get; private set; }
		public FrameGroup Group { get; private set; }

    private Board<EventBoardItem, EventDescription, EventNode> board;
    public Board<EventBoardItem, EventDescription, EventNode> Board
    {
      get
      {
        if (board == null)
          Load();
        
        return board;
      }
    }

    public List<Entry> Entries
    {
      get
      {
        if (entries == null)
          Load();

        return entries;
      }
    }

    public List<Entry> Categories { get; private set; }

		Object loading = new object();

    public override void Load()
    {
			lock (loading)
			{
				if (!IsLoaded)
				{
					entries = ReadEventList(reader, DescriptionBoard);

					root = new EventTree(this);
					board = new Board<EventBoardItem, EventDescription, EventNode>(root);

					reader = null;
					IsLoaded = true;
				}
			}
    }

    static List<Entry> ReadEventList(BinaryReader reader, EventDescriptionBoard board)
    {
      int count = reader.ReadInt32();
      List<Entry> result = new List<Entry>(count);

      for (int i = 0; i < count; ++i)
        result.Add(Entry.Read(reader, board));
      
      return result;
    }

    protected void ReadInternal(BinaryReader reader)
    {
      Header = FrameHeader.Read(reader);
      Categories = ReadEventList(reader, DescriptionBoard);
    }

		public double CalculateFilteredTime(HashSet<EventDescription> filter)
		{
			return Root.CalculateFilteredTime(filter);
		}

    public EventFrame(BinaryReader reader, FrameGroup group) : base(reader.BaseStream)
    {
      this.reader = reader;
			Group = group;
			DescriptionBoard = group.Board;
      ReadInternal(reader);
    }
  }
}
