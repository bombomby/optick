using Profiler.Data;
using Profiler.InfrastructureMvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.ViewModels
{
	class MemoryStatsViewModel : BaseViewModel
	{
		public class StatsItem : BaseViewModel
		{
			public DataResponse.Type ResponseType { get; set; }
			public UInt64 TotalMemory { get; set; }
			public UInt64 TotalCount { get; set; }

			public void Add(UInt64 size)
			{
				TotalMemory = TotalMemory + size;
				TotalCount = TotalCount + 1;
			}
		}

		Dictionary<DataResponse.Type, StatsItem> StatsDictionary { get; set; } = new Dictionary<DataResponse.Type, StatsItem>();

		private List<StatsItem> _stats = null;
		public List<StatsItem> Stats
		{
			get { return _stats; }
			set { SetProperty(ref _stats, value); OnPropertyChanged("TotalMemory"); }
		}

		public UInt64 TotalMemory
		{
			get
			{
				UInt64 total = 0;
				Stats.ForEach(s => total += s.TotalMemory);
				return total;
			}
		}

		public void Load(DataResponse response)
		{
			StatsItem item = null;
			if (!StatsDictionary.TryGetValue(response.ResponseType, out item))
			{
				item = new StatsItem() { ResponseType = response.ResponseType };
				StatsDictionary.Add(response.ResponseType, item);
			}
			item.Add((UInt64)response.Reader.BaseStream.Length);
		}

		public void Update()
		{
			List<StatsItem> items = new List<StatsItem>(StatsDictionary.Values);
			items.Sort((a, b) => -a.TotalMemory.CompareTo(b.TotalMemory));
			Stats = items;
		}
	}
}
