using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Data
{
	public struct Vec3
	{
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
	}

	public class Tag : ITick, IComparable<Tag>
	{
		public EventDescription Description { get; set; }
		public Tick Time { get; set; }
		public String Name => Description.FullName;
		public virtual String FormattedValue { get; }

		public long Start => Time.Start;

		public int CompareTo(Tag other)
		{
			int result = Start.CompareTo(other.Start);
			return result == 0 ? Name.CompareTo(other.Name) : result;
		}

		public virtual void Read(BinaryReader reader, EventDescriptionBoard board)
		{
			Time = new Tick { Start = Durable.ReadTime(reader) };
			int descriptionID = reader.ReadInt32();
			Description = (0 <= descriptionID && descriptionID < board.Board.Count) ? board.Board[descriptionID] : null;
		}
	}

	public class TagFloat : Tag
	{
		public float Value { get; set; }
		public override void Read(BinaryReader reader, EventDescriptionBoard board)
		{
			base.Read(reader, board);
			Value = reader.ReadSingle();
		}

		public override String FormattedValue => Value.ToString("0.0##");
	}

	public class TagInt32 : Tag
	{
		public int Value { get; set; }
		public override void Read(BinaryReader reader, EventDescriptionBoard board)
		{
			base.Read(reader, board);
			Value = reader.ReadInt32();
		}

		public override String FormattedValue => Value.ToString("N0").Replace(',', ' ');
	}

	public class TagUInt32 : Tag
	{
		public UInt32 Value { get; set; }
		public override void Read(BinaryReader reader, EventDescriptionBoard board)
		{
			base.Read(reader, board);
			Value = reader.ReadUInt32();
		}

		public override String FormattedValue => Value.ToString("N0").Replace(',', ' ');
	}

	public class TagUInt64 : Tag
	{
		public UInt64 Value { get; set; }
		public override void Read(BinaryReader reader, EventDescriptionBoard board)
		{
			base.Read(reader, board);
			Value = reader.ReadUInt64();
		}

		public override String FormattedValue => Value.ToString("N0").Replace(',', ' ');
	}

	public class TagVec3 : Tag
	{
		public Vec3 Value { get; set; }
		public override void Read(BinaryReader reader, EventDescriptionBoard board)
		{
			base.Read(reader, board);
			Value = new Vec3 { X = reader.ReadSingle(), Y = reader.ReadSingle(), Z = reader.ReadSingle() };
		}

		public override String FormattedValue => String.Format("({0:0.0#}, {1:0.0#}, {2:0.0#})", Value.X, Value.Y, Value.Z);
	}

	public class TagString : Tag
	{
		public String Value { get; set; }
		public override void Read(BinaryReader reader, EventDescriptionBoard board)
		{
			base.Read(reader, board);
			Value = Utils.ReadBinaryString(reader);
		}

		public override String FormattedValue => Value;
	}



	public class TagsPack : IResponseHolder
	{
		public override DataResponse Response { get; set; }
		private FrameGroup Group { get; set; }
		public int ThreadIndex { get; private set; } = -1;
		public int CoreIndex { get; set; } = -1;

		List<Tag> tags = new List<Tag>();
		public List<Tag> Tags { get { return tags; } }

		bool IsLoaded { get; set; }

		public TagsPack(DataResponse response, FrameGroup group)
		{
			Response = response;
			Group = group;
			if (response != null)
			{
				ThreadIndex = response.Reader.ReadInt32();
				Load();
			}
		}

		public TagsPack(List<Tag> t)
		{
			tags = t;
		}

		void Load()
		{
			if (Response == null)
				return;

			lock (Response)
			{
				if (!IsLoaded)
				{
					tags = new List<Tag>();
					BinaryReader reader = Response.Reader;

					reader.ReadInt32(); // Skip 
					LoadTags<TagFloat>();
					LoadTags<TagUInt32>();
					LoadTags<TagInt32>();
					LoadTags<TagUInt64>();
					LoadTags<TagVec3>();
					reader.ReadInt32(); // Skip
					reader.ReadInt32(); // Skip
					LoadTags<TagString>();

					tags.Sort();

					IsLoaded = true;
				}

			}
		}

		void LoadTags<T>() where T : Tag, new()
		{
			BinaryReader reader = Response.Reader;

			int count = reader.ReadInt32();
			if (count == 0)
				return;

			for (int i = 0; i < count; ++i)
			{
				T val = new T();
				val.Read(reader, Group.Board);
				tags.Add(val);
			}
		}
	}
}
