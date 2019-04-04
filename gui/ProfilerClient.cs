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
using System.Diagnostics;

namespace Profiler
{
	public class ProfilerClient
	{
		private Object criticalSection = new Object();
		private static ProfilerClient profilerClient = new ProfilerClient();

		ProfilerClient()
		{

		}

		private void Reconnect()
		{
			if (client.Client.Connected)
				client.Client.Disconnect(true);

			client = new TcpClient();
		}

		public IPAddress IpAddress
		{
			get { return ipAddress; }
			set
			{
				if (!value.Equals(ipAddress))
				{
					ipAddress = value;
					Reconnect();
				}
			}
		}

		public UInt16 Port
		{
			get { return port; }
			set
			{
				if (port != value)
				{
					port = value;
					Reconnect();
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

				lock (criticalSection)
				{
					if (!client.Connected)
						return null;

					stream = client.GetStream();
				}

				return DataResponse.Create(stream, IpAddress, Port);
			}
			catch (System.IO.IOException ex)
			{
				lock (criticalSection)
				{
					Application.Current.Dispatcher.BeginInvoke(new Action(() =>
					{
						ConnectionChanged?.Invoke(IpAddress, Port, State.Disconnected, ex.Message);
					}));

					Reconnect();
				}
			}

			return null;
		}

		private IPAddress ipAddress;
		private UInt16 port = UInt16.MaxValue;

		const UInt16 PORT_RANGE = 3;

		private bool CheckConnection()
		{
			lock (criticalSection)
			{
				if (!client.Connected)
				{
					for (UInt16 currentPort = port; currentPort < port + PORT_RANGE; ++currentPort)
					{
						try
						{
							Application.Current.Dispatcher.BeginInvoke(new Action(() =>
							{
								ConnectionChanged?.Invoke(IpAddress, currentPort, State.Connecting, String.Empty);
							}));

							client.Connect(new IPEndPoint(ipAddress, currentPort));
							NetworkStream stream = client.GetStream();

							ConnectionChanged?.Invoke(ipAddress, currentPort, State.Connected, String.Empty);

							return true;
						}
						catch (SocketException ex)
						{
							Debug.Print(ex.Message);
						}
					}
				}
			}
			return false;
		}

		public enum State
		{
			Connecting,
			Connected,
			Disconnected,
		}
		public delegate void ConnectionStateEventHandler(IPAddress address, UInt16 port, State state, String message);
		public event ConnectionStateEventHandler ConnectionChanged;

		public bool SendMessage(Message message, bool autoconnect = false)
		{
			try
			{
				if (!client.Connected && !autoconnect)
					return false;

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
				lock (criticalSection)
				{
					Application.Current.Dispatcher.BeginInvoke(new Action(() =>
					{
						ConnectionChanged?.Invoke(IpAddress, Port, State.Disconnected, ex.Message);
					}));

					Reconnect();
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
