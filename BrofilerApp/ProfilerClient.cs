using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Profiler.Data;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;
using System.Windows;
using System.Runtime.CompilerServices;

namespace Profiler
{
  public class ProfilerClient
  {
		private Object criticalSection = new Object();
    private static ProfilerClient profilerClient = new ProfilerClient();

    public IPAddress IpAddress
    {
      get { return ipAddress; }
      set
        {
            if (ipAddress != value)
            {
               ipAddress = value;
                if (client.Client.Connected)
                    client.Client.Disconnect(true);
            }
        }
    }

    public int Port
    {
      get { return port; }
      set
            {
                if (port != value)
                {
                    port = value;
                    if (client.Client.Connected)
                        client.Client.Disconnect(true);
                }
            }
    }

    public static ProfilerClient Get() { return profilerClient; }

    TcpClient client = new TcpClient();

    #region SocketWork

    public DataResponse RecieveMessage()
    {
			try
			{
				NetworkStream stream = null;

				lock(criticalSection)
				{
					if (!client.Connected)
						return null;

					stream = client.GetStream();
				}

        return DataResponse.Create(stream);
			}
			catch (System.IO.IOException ex)
			{
				if (MessageBox.Show(ex.Message) == MessageBoxResult.OK)
				{
					lock (criticalSection)
					{
						client = new TcpClient();
					}
				}
			}

			return null;
    }

    private IPAddress ipAddress;
    private int port = -1;

		const int PORT_RANGE = 4;

		private bool CheckConnection()
		{
			lock (criticalSection)
			{
				if (!client.Connected)
				{
					for (int currentPort = port + PORT_RANGE - 1; currentPort >= port; --currentPort)
					{
						try
						{
							client.Connect(new IPEndPoint(ipAddress, currentPort));
                            NetworkStream stream = client.GetStream();
 
                            return true;
						}
						catch (SocketException) { }
					}
				}
			}
			return false;
		}

    public bool SendMessage(Message message)
    {
			try
			{
				CheckConnection();

				lock (criticalSection)
				{
					MemoryStream buffer = new MemoryStream();
					message.Write(new BinaryWriter(buffer));
					buffer.Flush();

					UInt32 length = (UInt32)buffer.Length;

					NetworkStream stream = client.GetStream();

					BinaryWriter writer = new BinaryWriter(stream);
					writer.Write(Message.MESSAGE_MARK);
					writer.Write(length);

					buffer.WriteTo(stream);
					stream.Flush();
				}

				return true;
			}
			catch (Exception ex)
			{
				if (MessageBox.Show(ex.Message) == MessageBoxResult.OK)
				{
					lock (criticalSection)
					{
						//client.Client.Shutdown(SocketShutdown.Both);
						client = new TcpClient();
					}
				}
			}

			return false;
    }

    public void Close()
    {
			lock (criticalSection)
			{
				if (client != null)
				{
					client.Close();
					client = null;
				}
			}
    }

    #endregion
  }
}
