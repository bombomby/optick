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

    public class Tag : ITick
    {
        public EventDescription Description { get; set; }
        public Timestamp Time { get; set; }

        public long Start => Time.Time;


        public virtual void Read(BinaryReader reader, EventDescriptionBoard board)
        {
            Time = new Timestamp { Time = Durable.ReadTime(reader) };
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
    }

    public class TagInt32 : Tag
    {
        public int Value { get; set; }
        public override void Read(BinaryReader reader, EventDescriptionBoard board)
        {
            base.Read(reader, board);
            Value = reader.ReadInt32();
        }
    }

    public class TagUInt64 : Tag
    {
        public UInt64 Value { get; set; }
        public override void Read(BinaryReader reader, EventDescriptionBoard board)
        {
            base.Read(reader, board);
            Value = reader.ReadUInt64();
        }
    }

    public class TagVec3 : Tag
    {
        public Vec3 Value { get; set; }
        public override void Read(BinaryReader reader, EventDescriptionBoard board)
        {
            base.Read(reader, board);
            Value = new Vec3 { X = reader.ReadSingle(), Y = reader.ReadSingle(), Z = reader.ReadSingle() };
        }
    }

    public class TagString : Tag
    {
        public String Value { get; set; }
        public override void Read(BinaryReader reader, EventDescriptionBoard board)
        {
            base.Read(reader, board);
            Value = Utils.ReadBinaryString(reader);
        }
    }



    public class TagsPack : IResponseHolder
    {
        public override DataResponse Response { get; set; }
        private FrameGroup Group { get; set; }
        public int ThreadIndex { get; private set; }

        List<Tag> tags;
        List<Tag> Tags { get { Load(); return tags; } }

        bool IsLoaded { get; set; }

        public TagsPack(DataResponse response, FrameGroup group)
        {
            Response = response;
            Group = group;
            ThreadIndex = response.Reader.ReadInt32();
            Load();
        }

        void Load()
        {
            lock (Response)
            {
                if (!IsLoaded)
                {
                    tags = new List<Tag>();
                    BinaryReader reader = Response.Reader;

                    reader.ReadInt32(); // Skip 
                    LoadTags<TagFloat>();
                    reader.ReadInt32(); // Skip 
                    LoadTags<TagInt32>();
                    LoadTags<TagUInt64>();
                    LoadTags<TagVec3>();
                    reader.ReadInt32(); // Skip
                    reader.ReadInt32(); // Skip
                    LoadTags<TagString>();

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
