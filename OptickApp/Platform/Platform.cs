using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;

namespace Profiler
{
	public class Platform
	{
		public enum Type
		{
			Unknown,
			Windows,
			Linux,
			MacOS,
			XBox,
			Playstation,
		}

		public class Connection
		{
			public Platform.Type Target { get; set; }
			public string Name { get; set; }
            [XmlIgnore]
            public IPAddress Address { get; set; }
            [XmlElement("Address")]
            public string AddressForXml
            {
                get { return Address.ToString(); }
                set{ Address = string.IsNullOrEmpty(value) ? null : IPAddress.Parse(value);}
            }
            public int Port { get; set; }
		}

		public static IPAddress GetPS4Address()
		{
			return IPAddress.None;
		}

		public static IPAddress GetXONEAddress()
		{
			return IPAddress.None;
		}

		public static List<IPAddress> GetPCAddresses()
		{
			List<IPAddress> result = new List<IPAddress>();
			foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
				if (ip.AddressFamily == AddressFamily.InterNetwork)
					result.Add(ip);

			if (result.Count == 0)
				result.Add(IPAddress.Parse("127.0.0.1"));

			return result;
		}
	}
}
