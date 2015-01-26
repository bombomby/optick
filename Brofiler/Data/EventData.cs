using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Profiler.Data
{
	public class EventData : Durable
	{
		private String customData;
		public String CustomData
		{
			get { return customData; }
		}

		public void ReadEventData(BinaryReader reader)
		{
			ReadDurable(reader);
		}

		public EventData(long s, long f, String customData) : base(s, f)
		{
			this.customData = customData;
		}

		public EventData() { }
	}

	public abstract class Description
	{
		public abstract Object GetSharedKey();

		private String name;
		public String Name { get { return name; } }

		private String fullName;
		public String FullName
		{
			get { return fullName; }
			set
			{
				fullName = value;
				name = fullName;

				int lastNameSymbol = fullName.IndexOf('(');
				if (lastNameSymbol != -1)
				{
					String leftPart = fullName.Substring(0, lastNameSymbol);
					int startIndex = leftPart.LastIndexOf(' ');
					name = leftPart.Substring(startIndex + 1);
				}
			}
		}

		private FileLine path = FileLine.Empty;
		public FileLine Path { 
			get { return path; } 
			set { path = value != null ? value : FileLine.Empty; } 
		}
	}
}
