using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using Profiler.Data;
using System.Security;

namespace Profiler.Data
{
	public struct NetworkProtocol
	{
		/*
                public const UInt32 NETWORK_PROTOCOL_VERSION_6  = 6; 
                public const UInt32 NETWORK_PROTOCOL_VERSION_7  = 7; // Changed ThreadID - uint32 => uint64
                public const UInt32 NETWORK_PROTOCOL_VERSION_8  = 8; // Changed CoreID in SyncData - uint32 => uint64
                public const UInt32 NETWORK_PROTOCOL_VERSION_9  = 9; // Added thread synchronization wait reason
                public const UInt32 NETWORK_PROTOCOL_VERSION_10 = 10; // Added StackWalk event
                public const UInt32 NETWORK_PROTOCOL_VERSION_11 = 11; // Added thread synchronization switch to thread ID
                public const UInt32 NETWORK_PROTOCOL_VERSION_12 = 12; // Added separate fiber sync data stream
				public const UInt32 NETWORK_PROTOCOL_VERSION_18 = 18; // Bumped version
				public const UInt32 NETWORK_PROTOCOL_VERSION_20 = 20; // Added Summary                
				...
         */
		public const UInt32 NETWORK_PROTOCOL_VERSION_18 = 18; // Bumped version
		public const UInt32 NETWORK_PROTOCOL_VERSION_20 = 20; // Added Summary   
		public const UInt32 NETWORK_PROTOCOL_VERSION_23 = 23; // Added Support for Target Platform and Computer name in Handshake response
		public const UInt32 NETWORK_PROTOCOL_VERSION_24 = 24; // Adding Modules
		public const UInt32 NETWORK_PROTOCOL_VERSION_25 = 25; // Adding ThreadID to the frame list
		public const UInt32 NETWORK_PROTOCOL_VERSION_26 = 26; // Adding FrameType to the FrameHeader

		public const UInt32 NETWORK_PROTOCOL_VERSION = NETWORK_PROTOCOL_VERSION_26;
		public const UInt32 NETWORK_PROTOCOL_MIN_VERSION = NETWORK_PROTOCOL_VERSION_18;

		public const UInt16 OPTICK_APP_ID = 0xB50F;
	}

	public class DataResponse
	{
		public enum Type
		{
			FrameDescriptionBoard,
			EventFrame,
			SamplingFrame,
			NullFrame,
			ReportProgress,
			Handshake,
			Reserved_0,
			SynchronizationData,
			TagsPack,
			CallstackDescriptionBoard,
			CallstackPack,
			Reserved_1,
			Reserved_2,
			Reserved_3,
			Reserved_4,

			FiberSynchronizationData = 1 << 8,
			SyscallPack,
			SummaryPack,
			FramesPack,
		}
		public UInt16 ApplicationID { get; set; }
		public Type ResponseType { get; set; }
		public UInt32 Version { get; set; }
		public BinaryReader Reader { get; set; }

		public struct ConnectionSource
		{
			public IPAddress Address { get; set; }
			public UInt16 Port { get; set; }
		}
		public ConnectionSource Source;

		public DataResponse(UInt16 appID, Type type, UInt32 version, BinaryReader reader)
		{
			ApplicationID = appID;
			ResponseType = type;
			Version = version;
			Reader = reader;
		}

		public DataResponse(Type type, Stream stream)
		{
			ResponseType = type;
			Version = NetworkProtocol.NETWORK_PROTOCOL_VERSION;
			Reader = new BinaryReader(stream);
		}

		public String SerializeToBase64()
		{
			MemoryStream stream = new MemoryStream();
			Serialize(ApplicationID, ResponseType, Reader.BaseStream, stream);
			stream.Position = 0;

			byte[] data = new byte[stream.Length];
			stream.Read(data, 0, (int)stream.Length);
			return Convert.ToBase64String(data);
		}

		public static void Serialize(UInt16 appID, DataResponse.Type type, Stream data, Stream result)
		{
			BinaryWriter writer = new BinaryWriter(result);
			writer.Write(NetworkProtocol.NETWORK_PROTOCOL_VERSION);
			writer.Write((UInt32)data.Length);
			writer.Write((UInt16)type);
			writer.Write((UInt16)appID);

			long position = data.Position;
			data.Seek(0, SeekOrigin.Begin);
			data.CopyTo(result);
			data.Seek(position, SeekOrigin.Begin);
		}

		public void Serialize(Stream result)
		{
			BinaryWriter writer = new BinaryWriter(result);
			writer.Write((UInt32)Version);
			writer.Write((UInt32)Reader.BaseStream.Length);
			writer.Write((UInt16)ResponseType);
			writer.Write((UInt16)ApplicationID);

			long position = Reader.BaseStream.Position;
			Reader.BaseStream.Seek(0, SeekOrigin.Begin);
			Reader.BaseStream.CopyTo(result);
			Reader.BaseStream.Seek(position, SeekOrigin.Begin);
		}

		public static DataResponse Create(Stream stream)
		{
			if (stream == null || !stream.CanRead)
				return null;

			var reader = new BinaryReader(stream);

			try
			{
				uint version = reader.ReadUInt32();
				uint length = reader.ReadUInt32();
				UInt16 responseType = reader.ReadUInt16();
				UInt16 applicationId = reader.ReadUInt16();
				byte[] bytes = reader.ReadBytes((int)length);

				return new DataResponse(applicationId, (DataResponse.Type)responseType, version, new BinaryReader(new MemoryStream(bytes)));
			}
			catch (EndOfStreamException) { }

			return null;
		}

		public static DataResponse Create(String base64)
		{
			MemoryStream stream = new MemoryStream(Convert.FromBase64String(base64));
			return DataResponse.Create(stream);
		}

		public static DataResponse Create(NetworkStream stream, IPAddress ipAddress, UInt16 port)
		{
			DataResponse response = Create(stream);
			if (response != null)
			{
				response.Source.Address = ipAddress;
				response.Source.Port = port;
			}
			return response;
		}
	}

	public enum MessageType
	{
		Start,
		Stop,
		Cancel,
		TurnSampling,
	}

	public abstract class Message
	{
		public static UInt32 MESSAGE_MARK = 0xB50FB50F;
		public static UInt16 APPLICATION_ID = 0;

		public abstract Int16 GetMessageType();
		public virtual void Write(BinaryWriter writer)
		{
			writer.Write(APPLICATION_ID);
			writer.Write(GetMessageType());
		}
	}

	public class StartMessage : Message
	{
		public CaptureSettings Settings {get;set;}
		public SecureString Password { get; set; }

        public StartMessage()
		{
		}

		public override Int16 GetMessageType()
		{
			return (Int32)MessageType.Start;
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
            writer.Write((UInt32)Settings.Mode);
            writer.Write(Settings.CategoryMask);
            writer.Write(Settings.SamplingFrequencyHz);
            writer.Write(Settings.FrameLimit);
			writer.Write(Settings.TimeLimitUs);
			writer.Write(Settings.MaxSpikeLimitUs);
			writer.Write(Settings.MemoryLimitMb);
			String pwd = Utils.GetUnsecureBase64String(Password);
            Utils.WriteBinaryString(writer, pwd);
		}
	}

	public class StopMessage : Message
	{
		public override Int16 GetMessageType()
		{
			return (Int16)MessageType.Stop;
		}
	}

	public class CancelMessage : Message
	{
		public override Int16 GetMessageType()
		{
			return (Int16)MessageType.Cancel;
		}
	}

	public class TurnSamplingMessage : Message
	{
		Int32 eventID;
		bool isActive;

		public TurnSamplingMessage(Int32 eventID, bool isActive)
		{
			this.eventID = eventID;
			this.isActive = isActive;
		}

		public override Int16 GetMessageType()
		{
			return (Int16)MessageType.TurnSampling;
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(eventID);
			writer.Write(isActive);
		}
	}
}
