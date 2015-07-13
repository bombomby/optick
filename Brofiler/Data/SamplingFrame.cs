using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;

namespace Profiler.Data
{

	class SamplingSharedKey
	{
		public SamplingDescription Description { get; private set; }

		public override int GetHashCode()
		{
			return Description.Name.GetHashCode() ^ (Description.Path != null ? Description.Path.File.GetHashCode() : 0);
		}

		public SamplingSharedKey(SamplingDescription desc)
		{
			Description = desc;
		}

		public override bool Equals(Object obj)
		{
			if (obj is SamplingSharedKey)
			{
				SamplingSharedKey other = obj as SamplingSharedKey;
				return Description.Name == other.Description.Name && Description.Path.File == other.Description.Path.File;
			}
			return false;
		}
	}

	public class SamplingDescription : Description
  {
		private bool isHooked = false;
		public bool IsHooked
		{
			get { return isHooked; }
			set
			{
				if (isHooked != value)
				{
					ProfilerClient.Get().SendMessage(new SetupHookMessage(Address, value));
					isHooked = value;
				}
			}
		}

    public UInt64 Address { get; private set; }
    public String Module { get; private set; }

    static char[] slashDelimetr = { '\\', '/' };
    public String ModuleShortName
    {
      get
      {
        int index = Module.LastIndexOfAny(slashDelimetr);
        return index != -1 ? Module.Substring(index + 1) : Module;
      }
    }

    public static SamplingDescription UnresolvedDescription = new SamplingDescription() { Module = "Unresolved", FullName = "Unresolved", Address = 0, Path = new FileLine(String.Empty, 0) };

    public static SamplingDescription Create(BinaryReader reader)
    {
      SamplingDescription description = new SamplingDescription();
      description.Address = reader.ReadUInt64();
      description.Module = System.Text.Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32()));
      description.FullName = System.Text.Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32()));

      String file = System.Text.Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32()));
      description.Path = new FileLine(file, reader.ReadInt32());

      return description;
    }

		public override Object GetSharedKey()
		{
			return new SamplingSharedKey(this);
		}
	}

  public class SamplingDescriptionBoard
  {
    public Dictionary<UInt64, SamplingDescription> Descriptions { get; private set; }

    public static SamplingDescriptionBoard Create(BinaryReader reader)
    {
      SamplingDescriptionBoard board = new SamplingDescriptionBoard();
      board.Descriptions = new Dictionary<UInt64, SamplingDescription>();

      uint count = reader.ReadUInt32();
      for (uint i = 0; i < count; ++i)
      {
        SamplingDescription desc = SamplingDescription.Create(reader);
        board.Descriptions[desc.Address] = desc;
      }

      return board;
    }
  }

  public class SamplingNode : TreeNode<SamplingDescription>
  {
    public UInt64 Address { get; private set; }
    public override String Name { get { return Description.Name; } }
    
    // Participated in sampling process
    public uint Passed { get; private set; }

    // Stopped at this function
    public uint Sampled
    {
      get
      {
        uint total = Passed;
        foreach (SamplingNode child in Children)
        {
          total -= child.Passed;
        }
        return total;
      }
    }

    //public SamplingNode Parent { get; private set; }

    SamplingNode(SamplingNode root, SamplingDescription desc, UInt64 address, UInt32 passed)
      : base(root, desc, passed)
    {
      Passed = passed;
    }

    public static SamplingNode Create(BinaryReader reader, SamplingDescriptionBoard board, SamplingNode root)
    {
      UInt64 address = reader.ReadUInt64();

      SamplingDescription desc = null;
      if (!board.Descriptions.TryGetValue(address, out desc))
        desc = SamplingDescription.UnresolvedDescription;

      UInt32 passed = reader.ReadUInt32();

      SamplingNode node = new SamplingNode(root, desc, address, passed);

      UInt32 childrenCount = reader.ReadUInt32();
      for (UInt32 i = 0; i < childrenCount; ++i)
        node.AddChild(SamplingNode.Create(reader, board, root != null ? root : node));

      return node;
    }
  }

  public class SamplingFrame : Frame
  {
    public override DataResponse.Type ResponseType { get { return DataResponse.Type.SamplingFrame; } }

    public SamplingDescriptionBoard DescriptionBoard { get; private set; }
    public BinaryReader Reader { get; private set; }

    private Board<SamplingBoardItem, SamplingDescription, SamplingNode> board;
    public Board<SamplingBoardItem, SamplingDescription, SamplingNode> Board
    {
      get
      {
        Load();
        return board;
      }
    }

    private const int MAX_ROOT_SKIP_DEPTH = 3;
    private SamplingNode root = null;
    public SamplingNode Root
    {
      get
      {
        Load();

        SamplingNode result = root;
        int depth = 0;

        while (depth < MAX_ROOT_SKIP_DEPTH && result.Children.Count == 1 && (result.Children[0] as SamplingNode).Sampled == 0)
        {
          ++depth;
          result = result.Children[0] as SamplingNode;
        }

        return result;
      }
    }

		public uint SampleCount { get; private set; }

    public override string Description { get { return String.Format("{0} Sampling Data", SampleCount); } }
    public override double Duration { get { return 130.0; } }

    public override void Load()
    {
      if (!IsLoaded)
      {
        DescriptionBoard = SamplingDescriptionBoard.Create(Reader);
        root = SamplingNode.Create(Reader, DescriptionBoard, null);
        root.CalculateRecursiveExcludeFlag(new Dictionary<Object, int>());

        board = new Board<SamplingBoardItem, SamplingDescription, SamplingNode>(root);

        IsLoaded = true;
      }
    }

    public SamplingFrame(BinaryReader reader) : base(reader.BaseStream)
    {
      Reader = reader;
			SampleCount = reader.ReadUInt32();
    }
  }
}
