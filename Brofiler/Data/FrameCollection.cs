using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.IO;

namespace Profiler.Data
{
    public class FrameCollection : ObservableCollection<Frame>
    {
      Dictionary<int, EventDescriptionBoard> descriptions = new Dictionary<int, EventDescriptionBoard>();

      public void Flush()
      {
        descriptions.Clear();
      }

      public void Add(DataResponse.Type dataType, BinaryReader reader)
      {
        switch (dataType)
        {
          case DataResponse.Type.FrameDescriptionBoard:
            {
              EventDescriptionBoard board = EventDescriptionBoard.Read(reader);
              descriptions[board.ID] = board;
              break;
            }
          case DataResponse.Type.EventFrame:
            {
              int id = reader.ReadInt32();
              EventDescriptionBoard board = descriptions[id];
              Add(new EventFrame(reader, board));
              
              break;
            }

          case DataResponse.Type.SamplingFrame:
            {
              Add(new SamplingFrame(reader));
              break;
            }
        }
      }
    }

    public class TestFrameCollection : FrameCollection
    {
      // Encoded network stream with test frames
      static String descriptionBoard = "AgAAAKQCAAAAAAAAAQAAABgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAsAAABVcGRhdGVJbnB1dA4AAABUZXN0RW5naW5lLmNwcDkAAAC0gkb/ACYAAAB2b2lkIF9fY2RlY2wgVGVzdDo6U2xvd0Z1bmN0aW9uMih2b2lkKQ4AAABUZXN0RW5naW5lLmNwcBkAAAAAAAAAAA4AAABVcGRhdGVNZXNzYWdlcw4AAABUZXN0RW5naW5lLmNwcD4AAAAApf//AC4AAAB2b2lkIF9fY2RlY2wgVGVzdDo6U2xvd0Z1bmN0aW9uPDB4NDAwMDA+KHZvaWQpDgAAAFRlc3RFbmdpbmUuY3BwEAAAAAAAAAAACwAAAFVwZGF0ZUxvZ2ljDgAAAFRlc3RFbmdpbmUuY3BwQwAAANZw2v8ACwAAAFVwZGF0ZVNjZW5lDgAAAFRlc3RFbmdpbmUuY3BwSAAAAOvOh/8ADQAAAFVwZGF0ZVBoeXNpY3MOAAAAVGVzdEVuZ2luZS5jcHBSAAAAs971/wAEAAAARHJhdw4AAABUZXN0RW5naW5lLmNwcE0AAABygPr/AA==";
      static String eventFrame = "AgAAAHgBAAABAAAAAQAAAO8WLgAAAAAA4K/J9IwDAAAQ9Mz0jAMAAAYAAADir8n0jAMAAKUayvSMAwAAEAAAAKUayvSMAwAAy5bK9IwDAAASAAAAzJbK9IwDAAA5Esv0jAMAABQAAAA6Esv0jAMAAJKNy/SMAwAAFQAAAJONy/SMAwAAxHfM9IwDAAAWAAAAxXfM9IwDAACe8cz0jAMAABcAAAALAAAA4q/J9IwDAAClGsr0jAMAABAAAADir8n0jAMAAKQayvSMAwAAEQAAAKUayvSMAwAAy5bK9IwDAAASAAAAphrK9IwDAADLlsr0jAMAABMAAADMlsr0jAMAADkSy/SMAwAAFAAAAM2WyvSMAwAAORLL9IwDAAATAAAAOhLL9IwDAACSjcv0jAMAABUAAAA6Esv0jAMAAJGNy/SMAwAAEwAAAJONy/SMAwAAxHfM9IwDAAAWAAAAxXfM9IwDAACe8cz0jAMAABcAAADHd8z0jAMAAJ7xzPSMAwAAEwAAAA==";
      static String samplingFrame = "AgAAAGgFAAACAAAApQEAAAgAAACzEPQAAAAAAEgAAABaADoAXABQAHIAbwBmAGkAbABlAHIAXABSAGUAbABlAGEAcwBlAFwAUAByAG8AZgBpAGwAZQByAFQAZQBzAHQALgBlAHgAZQAIAAAAbQBhAGkAbgBSAAAAegA6AFwAcAByAG8AZgBpAGwAZQByAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXABwAHIAbwBmAGkAbABlAHIAdABlAHMAdAAuAGMAcABwAA4AAABFn+Z3AAAAADoAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAUwB5AHMAVwBPAFcANgA0AFwAbgB0AGQAbABsAC4AZABsAGwANgAAAFIAdABsAEkAbgBpAHQAaQBhAGwAaQB6AGUARQB4AGMAZQBwAHQAaQBvAG4AQwBoAGEAaQBuAAAAAAAAAAAAPSZHdAAAAABAAAAAQwA6AFwAVwBpAG4AZABvAHcAcwBcAHMAeQBzAHQAZQBtADMAMgBcAE0AUwBWAEMAUgAxADAAMAAuAGQAbABsABAAAABpAG4AaQB0AHQAZQByAG0AAAAAAAAAAACKM0B2AAAAAEAAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAcwB5AHMAdwBvAHcANgA0AFwAawBlAHIAbgBlAGwAMwAyAC4AZABsAGwAJgAAAEIAYQBzAGUAVABoAHIAZQBhAGQASQBuAGkAdABUAGgAdQBuAGsAAAAAAAAAAAByn+Z3AAAAADoAAABDADoAXABXAGkAbgBkAG8AdwBzAFwAUwB5AHMAVwBPAFcANgA0AFwAbgB0AGQAbABsAC4AZABsAGwANgAAAFIAdABsAEkAbgBpAHQAaQBhAGwAaQB6AGUARQB4AGMAZQBwAHQAaQBvAG4AQwBoAGEAaQBuAAAAAAAAAAAAcBj0AAAAAABIAAAAWgA6AFwAUAByAG8AZgBpAGwAZQByAFwAUgBlAGwAZQBhAHMAZQBcAFAAcgBvAGYAaQBsAGUAcgBUAGUAcwB0AC4AZQB4AGUAKAAAAFQAZQBzAHQAOgA6AEUAbgBnAGkAbgBlADoAOgBVAHAAZABhAHQAZQBOAAAAegA6AFwAcAByAG8AZgBpAGwAZQByAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAAMwAAABYc9AAAAAAASAAAAFoAOgBcAFAAcgBvAGYAaQBsAGUAcgBcAFIAZQBsAGUAYQBzAGUAXABQAHIAbwBmAGkAbABlAHIAVABlAHMAdAAuAGUAeABlADYAAABUAGUAcwB0ADoAOgBFAG4AZwBpAG4AZQA6ADoAVQBwAGQAYQB0AGUAUABoAHkAcwBpAGMAcwBOAAAAegA6AFwAcAByAG8AZgBpAGwAZQByAFwAcAByAG8AZgBpAGwAZQByAHQAZQBzAHQAXAB0AGUAcwB0AGUAbgBnAGkAbgBlAC4AYwBwAHAAVAAAAKVEO3YAAAAARAAAAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABzAHkAcwB3AG8AdwA2ADQAXABLAEUAUgBOAEUATABCAEEAUwBFAC4AZABsAGwACgAAAFMAbABlAGUAcAAAAAAAAAAAAAAAAAAAAAAApQEAAAEAAABFn+Z3AAAAAKUBAAABAAAAcp/mdwAAAAClAQAAAQAAAIozQHYAAAAApQEAAAEAAAA9Jkd0AAAAAKUBAAABAAAAsxD0AAAAAAClAQAAAQAAAHAY9AAAAAAApQEAAAEAAAAWHPQAAAAAAKUBAAABAAAApUQ7dgAAAAClAQAAAAAAAA==";

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
        Add(descriptionResponse.ResponseType, descriptionResponse.Reader);

        for (int i = 0; i < 32; ++i)
        {
          DataResponse eventResponse = DataResponse.Create(eventFrame);
          Add(eventResponse.ResponseType, eventResponse.Reader);
        }

        DataResponse samplingResponse = DataResponse.Create(samplingFrame);
        Add(samplingResponse.ResponseType, samplingResponse.Reader);
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
