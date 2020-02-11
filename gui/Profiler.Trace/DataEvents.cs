using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Trace
{
    [DataContract]
    public class EventData : INotifyPropertyChanged
    {
        private DateTime start = DateTime.MaxValue;
        private DateTime finish = DateTime.MinValue;

        [DataMember]
        public DateTime Start
        {
            get { return start; }
            set
            {
                start = value;
                RaisePropertyChanged("Start");
                RaisePropertyChanged("Duration");
            }
        }

        [DataMember]
        public DateTime Finish
        {
            get { return finish; }
            set
            {
                finish = value;
                RaisePropertyChanged("Finish");
                RaisePropertyChanged("Duration");
            }
        }

        public bool IsValid
        {
            get
            {
                return Finish > Start && Start > DateTime.MinValue && Finish < DateTime.MaxValue;
            }
        }

        public double Duration => (Finish - Start).TotalSeconds;

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    [DataContract]
    public class SysCallData : EventData
    {
        [DataMember]
        public ulong Address { get; set; }
    }

    [DataContract]
    public class WorkIntervalData : EventData
    {
        [DataMember]
        public int WaitReason { get; set; }
        [DataMember]
        public int CpuID { get; set; }
    }

    [DataContract]
    public class CallstackData
    {
        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public UInt64[] Callstack { get; set; }
    }

    [DataContract]
    public class ThreadData : EventData
    {
        [DataMember]
        public int ThreadID { get; set; }

        [DataMember]
        public List<SysCallData> SysCalls { get; set; }

        [DataMember]
        public List<WorkIntervalData> WorkIntervals { get; set; }

        [DataMember]
        public List<CallstackData> Callstacks { get; set; }

        [DataMember]
        public List<IOData> IORequests { get; set; }

        public ThreadData()
        {
            SysCalls = new List<SysCallData>();
            WorkIntervals = new List<WorkIntervalData>();
            IORequests = new List<IOData>();
            Callstacks = new List<CallstackData>();
        }
    }

    [DataContract]
    public class IOData : EventData
    {
        [DataContract]
        public enum Type
        {
            [EnumMember]
            Read,
            [EnumMember]
            Write,
        }

        [DataMember]
        public Type IOType { get; set; }

        [DataMember]
        public int ThreadID { get; set; }

        [DataMember]
        public int Size { get; set; }

        [DataMember]
        public long Offset { get; set; }

        [DataMember]
        public String FileName { get; set; }
    }

    [DataContract]
    public class ImageData : IComparable<ImageData>
    {
        [DataMember]
        public String FileName { get; set; }

        [DataMember]
        public ulong DefaultBase { get; set; }

        [DataMember]
        public ulong ImageBase { get; set; }

        [DataMember]
        public int ImageSize { get; set; }

        [DataMember]
        public int ImageChecksum { get; set; }

        public int CompareTo(ImageData other)
        {
            return ImageBase.CompareTo(other.ImageBase);
        }
    }


    [DataContract]
    public class ProcessData : EventData
    {
        [DataMember]
        public UInt64 UniqueKey { get; set; }
        [DataMember]
        public String Name { get; set; }
        [DataMember]
        public String CommandLine { get; set; }
        [DataMember]
        public int ProcessID { get; set; }

        private int? result = null;
        [DataMember]
        public int? Result
        {
            get
            {
                return result;
            }
            set
            {
                result = value;
                RaisePropertyChanged("Result");
            }
        }

        [DataMember]
        public Dictionary<String, String> Artifacts { get; set; }

        [DataMember]
        public Dictionary<int, ThreadData> Threads { get; set; }

        [DataMember]
        public List<ImageData> Images { get; set; }

        public String Text { get { return Artifacts != null ? Artifacts.Values.First().Replace('\n',' ') : String.Empty; } }

        public void AddArtifact(String name, String val)
        {
            if (Artifacts == null)
                Artifacts = new Dictionary<string, string>();

            Artifacts.Add(name, val);

            RaisePropertyChanged("Artifacts");
            RaisePropertyChanged("Text");
        }

        public ProcessData()
        {
            Threads = new Dictionary<int, ThreadData>();
            Images = new List<ImageData>();
        }

        public ImageData GetImageData(ulong address)
        {
            foreach (ImageData image in Images)
                if (image.DefaultBase <= address && address < image.DefaultBase + (ulong)image.ImageSize)
                    return image;

            return null;
        }
    }

    [DataContract]
    public struct CounterSample : IComparable<CounterSample>
    {
        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public float[] Values { get; set; }

        public int CompareTo(CounterSample other)
        {
            return Timestamp.CompareTo(other.Timestamp);
        }
    }

    [DataContract]
    public struct CounterDescription
    {
        [DataMember]
        public String Name { get; set; }

    }

    [DataContract]
    public class CounterGroup
    {
        [DataMember]
        public List<CounterDescription> Descriptions { get; set; }

        [DataMember]
        public List<CounterSample> Samples { get; set; }
    }

	[DataContract]
	public struct SwitchContextData
	{
		[DataMember]
		public DateTime Timestamp;
		[DataMember]
		public UInt64 OldThreadID;
		[DataMember]
		public UInt64 NewThreadID;
		[DataMember]
		public byte CPUID;
	}

	[DataContract]
    public class ProcessGroup
    {
        [DataMember]
        public ObservableCollection<ProcessData> Processes { get; set; }

		[DataMember]
        public CounterGroup Counters { get; set; }

        public void Add(ProcessData process)
        {
            Processes.Add(process);
        }

        public void Clear()
        {
            Processes.Clear();
			Counters = null;
        }

        public ProcessGroup()
        {
            Processes = new ObservableCollection<ProcessData>();
        }
    }


	[DataContract]
	public class SwitchContextGroup
	{
		[DataMember]
		public List<SwitchContextData> Events { get; set; } = new List<SwitchContextData>();

		public void Add(SwitchContextData sc)
		{
			Events.Add(sc);
		}
	}

}
