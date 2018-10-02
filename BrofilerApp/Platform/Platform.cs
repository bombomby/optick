using System.Net;
using System.Net.Sockets;

namespace Profiler
{
    public class Platform
    {
        public static IPAddress GetPS4Address()
        {
            return IPAddress.None;
        }

        public static IPAddress GetXONEAddress()
        {
            return IPAddress.None;
        }

        public static IPAddress GetPCAddress()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip;

            return IPAddress.Parse("127.0.0.1");
        }
    }
}
