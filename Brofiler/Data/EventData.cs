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
				name = StripFunctionArguments(fullName);
				name = StripReturnValue(name);
			}
		}

		private FileLine path = FileLine.Empty;
		public FileLine Path { 
			get { return path; } 
			set { path = value != null ? value : FileLine.Empty; } 
		}

		static char startBracket = '(';
		static char endBracket = ')';
		static char[] brackets = new char[] { startBracket, endBracket };
		static String StripFunctionArguments(String name)
		{
			int counter = 0;
			
			int index = name.Length - 1;

			while (index > 0)
			{
				index = name.LastIndexOfAny(brackets, index);
				if (index != -1)
				{
					counter = counter + (name[index] == endBracket ? 1 : -1);
					if (counter == 0)
						return name.Substring(0, index);

					--index;
				}
			}

			return name;
		}

		static String[] callConventions = { "__thiscall", "__fastcall", "__cdecl", "__clrcall", "__stdcall", "__vectorcall" };
		static String StripReturnValue(String name)
		{
			if (name.Length == 0)
				return name;

			foreach (String functionCall in callConventions)
			{
				int index = name.IndexOf(functionCall);
				if (index != -1)
				{
					int cutIndex = index + functionCall.Length + 1;
					if (cutIndex < name.Length)
						return name.Substring(cutIndex);
				}
			}

			return name;
		}
	}
}
