using System.Collections.Generic;
using System.IO;

namespace Profiler.Data
{
    public class RawCounterData
    {
        public EventDescription Description { get; set; }
        public Tick Time { get; set; }

        public float Value { get; set; }

        public void Read(BinaryReader reader, EventDescriptionBoard board)
        {
            Time = new Tick { Start = Durable.ReadTime(reader) };
            int descriptionId = reader.ReadInt32();
            Value = reader.ReadSingle();
            Description = (0 <= descriptionId && descriptionId < board.Board.Count) ? board.Board[descriptionId] : null;
        }
    }
    
    public class CounterModel
    {
        public CounterModel(string name, string displayName, List<Measurement> measurements)
        {
            Name = name;
            DisplayName = displayName;
            Measurements = measurements;
        }
        
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public List<Measurement> Measurements { get; private set; }

        public readonly struct Measurement
        {
            public Measurement(double value, double relativeMSec)
            {
                Value = value;
                RelativeMSec = relativeMSec;
            }

            public double Value { get; }
            public double RelativeMSec { get; }
        }
    }

    public class RawCountersPack : IResponseHolder
    {
        public RawCountersPack(DataResponse response, FrameGroup group)
        {
            Response = response;
            Group = group;
            if (response != null)
            {
                ThreadIndex = response.Reader.ReadInt32();
                Load();
            }
        }
        
        public override DataResponse Response { get; set; }
        
        private FrameGroup Group { get; set; }
        public int ThreadIndex { get; private set; } = -1;
        public int CoreIndex { get; set; } = -1;

        public List<RawCounterData> Counters { get; set; }
        
        bool IsLoaded { get; set; }

        private void Load()
        {
            if (Response == null)
                return;

            lock (Response)
            {
                if (!IsLoaded)
                {
                    Counters = new List<RawCounterData>();
                    BinaryReader reader = Response.Reader;

                    var count = reader.ReadInt32();
                    for (int i = 0; i < count; ++i)
                    {
                        var counter = new RawCounterData();
                        counter.Read(reader, Group.Board);
                        Counters.Add(counter);
                    }

                    IsLoaded = true;
                }

            }
        }
    }
}