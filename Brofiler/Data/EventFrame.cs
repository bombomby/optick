using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media;
using System.Threading;
using System.ComponentModel;

namespace Profiler.Data
{
    public class FrameHeader : EventData
    {
        public int ThreadIndex { get; private set; }
		public int FiberIndex { get; private set; }

        public static FrameHeader Read(DataResponse response)
        {
            FrameHeader header = new FrameHeader();
            header.ThreadIndex = response.Reader.ReadInt32();
            if (response.ApplicationID == NetworkProtocol.BROFILER_APP_ID)
            {
                header.FiberIndex = response.Reader.ReadInt32();
            }

            header.ReadEventData(response.Reader);
            return header;
        }

        public FrameHeader() { }

        public FrameHeader(int threadIndex, int fiberIndex, IDurable duration)
        {
			ThreadIndex = threadIndex;
			FiberIndex = fiberIndex;
            Start = duration.Start;
            Finish = duration.Finish;
        }
    }


    public class ShortBoard : Dictionary<EventDescription, List<Entry>>
    {
        public ShortBoard(List<Entry> entries)
        {
            for (int i = 0; i < entries.Count; ++i)
            {
                Entry entry = entries[i];

                List<Entry> sharedEntries = null;
                if (!TryGetValue(entry.Description, out sharedEntries))
                {
                    sharedEntries = new List<Entry>();
                    Add(entry.Description, sharedEntries);
                }
                sharedEntries.Add(entry);
            }
        }

        public List<Entry> Get(EventDescription description)
        {
            List<Entry> result = null;
            TryGetValue(description, out result);
            return result;
        }
    }


    public class EventFrame : Frame, IDurable, IComparable<EventFrame>, INotifyPropertyChanged
    {
        public override DataResponse.Type ResponseType { get { return DataResponse.Type.EventFrame; } }

        public FrameHeader Header { get; private set; }
        public long Tick { get { return Header.Start; } }

		private EventTree categoriesTree;

        private EventTree root = null;
        public Profiler.Data.EventTree Root
        {
            get
            {
                if (root == null)
				{
					lock ( loading )
					{
						if ( root == null )
							root = new EventTree( this, Entries );
					}
				}

                return root;
            }
        }

        public long Start { get { return Header.Start; } }
        public long Finish { get { return Header.Finish; } }

		string filteredDescription = "";

        public override double Duration
        {
            get
            {
                return Header.Duration;
            }
        }

        public double SynchronizationDuration { get; set; }

        public override string Description
        {
            get
            {
                return String.Format("{0:0} ms", Header.Duration);
            }
        }

		public override string FilteredDescription
		{
			get
			{
				return filteredDescription;
			}

			set
			{
				filteredDescription = value;
				OnPropertyChanged("FilteredDescription"); 
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}


        public string DeatiledDescription
        {
            get
            {
                return String.Format("Work: {0:0.###}ms   Wait: {1:0.###}ms", Duration - SynchronizationDuration, SynchronizationDuration).Replace(',', '.');
            }
        }

        public EventDescriptionBoard DescriptionBoard { get { return Group.Board; } }
        public FrameGroup Group { get; private set; }

        private Board<EventBoardItem, EventDescription, EventNode> board;
        public Board<EventBoardItem, EventDescription, EventNode> Board
        {
            get
            {
                if (board == null)
				{
					lock ( loading )
					{
						if ( board == null )
							board = new Board<EventBoardItem, EventDescription, EventNode>( Root );
					}
				}

                return board;
            }
        }

        private ShortBoard shortBoard;
        public ShortBoard ShortBoard
        {
            get
            {
                if (shortBoard == null)
                    shortBoard = new ShortBoard(Entries);

                return shortBoard;
            }
        }

	    public List<Entry> Entries { get; private set; }
	    public List<Entry> Categories { get; private set; }

	    public EventTree CategoriesTree
	    {
		    get
		    {
				if ( categoriesTree == null )
				{
					lock ( loading )
					{
						if ( categoriesTree == null )
							categoriesTree = new EventTree( this, Categories );
					}
				}

			    return categoriesTree;
		    }
	    }

        public List<Data.SyncInterval> Synchronization { get; private set; }
		public List<Data.FiberSyncInterval> FiberSync { get; private set; }

        long IDurable.Finish
        {
            get
            {
                return Header.Finish;
            }

        }

        long ITick.Start
        {
            get
            {
                return Header.Start;
            }
        }

        Object loading = new object();

	    public override void Load()
	    {
		    // invoke lazy init;
		    IsLoaded = CategoriesTree != null &&
		               Root != null &&
		               Board != null;
	    }

        static List<EventData> ReadEventTimeList(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            List<EventData> result = new List<EventData>(count);

            for (int i = 0; i < count; ++i)
                result.Add(EventData.Create(reader));

            return result;
        }

        static List<Entry> ReadEventList(BinaryReader reader, EventDescriptionBoard board)
        {
            int count = reader.ReadInt32();
            List<Entry> result = new List<Entry>(count);

            for (int i = 0; i < count; ++i)
                result.Add(Entry.Read(reader, board));

            return result;
        }

        List<EventData> LinearizeEventList(List<EventData> events)
        {
            List<EventData> result = new List<EventData>(events.Count);
            EventData currentRoot = null;

            foreach (EventData entry in events)
            {
                if (currentRoot == null)
                {
                    currentRoot = entry;
                    result.Add(entry);
                }
                else if (entry.Finish <= currentRoot.Finish)
                {
                    continue;
                }
                else if (Durable.TicksToMs(Math.Abs(entry.Start - currentRoot.Finish)) < 0.005)
                {
                    currentRoot.Finish = entry.Finish;
                }
                else
                {
                    currentRoot = entry;
                    result.Add(entry);
                }
            }

            return result;
        }

        public void MergeWith(EventFrame frame)
        {
            Categories.AddRange(frame.Categories);
            Categories.Sort();

            Entries.AddRange(frame.Entries);
            Entries.Sort();

			if ( root != null )
				root = new EventTree( this, Entries );
			if ( board != null )
				board = new Board<EventBoardItem, EventDescription, EventNode>( root );
			if ( categoriesTree != null )
				categoriesTree = new EventTree( this, Categories );
        }

        protected void ReadInternal(DataResponse response)
        {
            Header = FrameHeader.Read(response);
            Categories = ReadEventList(response.Reader, DescriptionBoard);
			Entries = ReadEventList(response.Reader, DescriptionBoard );

            Synchronization = new List<SyncInterval>();
			FiberSync = new List<FiberSyncInterval>();
        }

        public double CalculateFilteredTime(HashSet<Object> filter)
        {
            return Root.CalculateFilteredTime(filter);
        }

        public int CompareTo(EventFrame other)
        {
            return Start.CompareTo(other.Start);
        }

        public EventFrame(DataResponse response, FrameGroup group) : base(response)
        {
            BinaryReader reader = response.Reader;
            Group = group;
            ReadInternal(response);
        }

        private void Init(FrameHeader header, List<Entry> entries, FrameGroup group)
        {
            Header = header;
            Group = group;
            Categories = new List<Entry>();

            Entries = entries;
            foreach (Entry entry in entries)
            {
                if (entry.Description.Color.A != 0)
                {
                    Categories.Add(entry);
                }
            }

            categoriesTree = new EventTree(this, Categories);

            root = new EventTree(this, Entries);
            board = new Board<EventBoardItem, EventDescription, EventNode>(root);

            Synchronization = new List<SyncInterval>();
            FiberSync = new List<FiberSyncInterval>();

            IsLoaded = true;
        }

        public EventFrame(FrameHeader header, List<Entry> entries, FrameGroup group) : base(null)
        {
            Init(header, entries, group);
        }

        public EventFrame(EventFrame frame, EventNode node) : base(null)
        {
            List<Entry> entries = new List<Entry>();
            node.ForEach((n, level) => { entries.Add((n as EventNode).Entry); return true; });
            Init(new FrameHeader(frame.Header.ThreadIndex, frame.Header.FiberIndex, new Durable(node.Entry.Start, node.Entry.Finish)), entries, frame.Group);
        }
    }
}
