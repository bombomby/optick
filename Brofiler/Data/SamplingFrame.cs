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

        public static SamplingDescription UnresolvedDescription = new SamplingDescription() { Module = "Unresolved", FullName = "Unresolved", Address = 0, Path = FileLine.Empty };

        public static SamplingDescription Create(BinaryReader reader)
        {
            SamplingDescription description = new SamplingDescription();
            description.Address = reader.ReadUInt64();
            description.Module = Utils.ReadBinaryString(reader);
            description.FullName = Utils.ReadBinaryString(reader);

            String file = Utils.ReadBinaryString(reader);
            description.Path = new FileLine(file, reader.ReadInt32());

            return description;
        }

        public static SamplingDescription Create(UInt64 address)
        {
            return new SamplingDescription() { Module = "Unresolved", FullName = String.Format("0x{0:x}", address), Address = address, Path = FileLine.Empty };
        }

        public override Object GetSharedKey()
        {
            return new SamplingSharedKey(this);
        }
    }

    public abstract class ISamplingBoard
    {
        public abstract SamplingDescription GetDescription(UInt64 address);
    }

    public class SamplingDescriptionBoard : ISamplingBoard
    {
        public Dictionary<UInt64, SamplingDescription> Descriptions { get; private set; }

        protected void Read(BinaryReader reader)
        {
            Descriptions = new Dictionary<UInt64, SamplingDescription>();

            uint count = reader.ReadUInt32();
            for (uint i = 0; i < count; ++i)
            {
                SamplingDescription desc = SamplingDescription.Create(reader);
                Descriptions[desc.Address] = desc;
            }
        }

        public static SamplingDescriptionBoard Create(BinaryReader reader)
        {
            SamplingDescriptionBoard board = new SamplingDescriptionBoard();
            board.Read(reader);
            return board;
        }

        public override SamplingDescription GetDescription(ulong address)
        {
            SamplingDescription result = null;
            return Descriptions.TryGetValue(address, out result) ? result : SamplingDescription.UnresolvedDescription;
        }
    }

    public class DummySamplingBoard : ISamplingBoard
    {
        public override SamplingDescription GetDescription(ulong address)
        {
            return SamplingDescription.Create(address);
        }

        public static DummySamplingBoard Instance = new DummySamplingBoard();
    }

    public class SamplingDescriptionPack : SamplingDescriptionBoard
    {
        public DataResponse Response { get; set; }
        public static SamplingDescriptionPack Create(DataResponse response)
        {
            SamplingDescriptionPack pack = new SamplingDescriptionPack() { Response = response };
            pack.Read(response.Reader);
            return pack;
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


        bool IsSimilar(SamplingDescription a, SamplingDescription b)
        {
            return a.Address == b.Address || (a.Path.Line == b.Path.Line && a.Name == b.Name);
        }

        void AppendMerge(Callstack callstack, int index, SamplingNode root)
        {
            if (callstack.Count == index)
                return;

            SamplingDescription desc = callstack[index];
            SamplingNode rootNode = root == null ? this : root;

            foreach (SamplingNode node in Children)
            {
                if (IsSimilar(node.Description, desc))
                {
                    ++node.Passed;
                    node.AppendMerge(callstack, index + 1, rootNode);
                    return;
                }
            }

            SamplingNode child = new SamplingNode(rootNode, desc, desc.Address, 1);
            AddChild(child);
            child.AppendMerge(callstack, index + 1, rootNode);
        }

        void Update()
        {
            ForEach((node, level) => 
            {
                SamplingNode sNode = (node as SamplingNode);
                sNode.Duration = sNode.Passed;

                uint passedChildren = 0;
                sNode.Children.ForEach(child => passedChildren += (child as SamplingNode).Passed);
                sNode.ChildrenDuration = passedChildren;

                return true;
            });
        }

        public static SamplingNode Create(List<Callstack> callstacks)
        {
            SamplingNode node = new SamplingNode(null, null, 0, (uint)callstacks.Count);
            callstacks.ForEach(c => node.AppendMerge(c, 0, node));
            node.Update();
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

            set
            {
                root = value;
                root.CalculateRecursiveExcludeFlag(new Dictionary<Object, int>());
                board = new Board<SamplingBoardItem, SamplingDescription, SamplingNode>(root);
                IsLoaded = true;
            }
		}

		public int SampleCount { get; private set; }

		public override string Description { get { return String.Format("{0} Sampling Data", SampleCount); } }

		public override string FilteredDescription
		{
			get
			{
				return "";
			}
		}

		public override double Duration { get { return 130.0; } }

		public override void Load()
		{
			if (!IsLoaded)
			{
				DescriptionBoard = SamplingDescriptionBoard.Create(Reader);
                Root = SamplingNode.Create(Reader, DescriptionBoard, null);
            }
		}

		public SamplingFrame(DataResponse response) : base(response)
		{
			Reader = response.Reader;
			SampleCount = Reader.ReadInt32();
		}

        public SamplingFrame(List<Callstack> callstacks) : base(null)
        {
            SampleCount = callstacks.Count;
            Root = SamplingNode.Create(callstacks);
        }
	}
}
