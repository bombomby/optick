using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;

namespace Profiler
{
  public struct NetworkProtocol
  {
    public const UInt32 NETWORK_PROTOCOL_VERSION = 3;
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
			Handshake
    }

    public Type ResponseType { get; set; }
    public UInt32 Version { get; set; }
    public BinaryReader Reader { get; set; }

    public DataResponse(Type type, UInt32 version, BinaryReader reader)
    {
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
      Serialize(ResponseType, Reader.BaseStream, stream);
      stream.Position = 0;

      byte[] data = new byte[stream.Length];
      stream.Read(data, 0, (int)stream.Length);
      return Convert.ToBase64String(data);
    }

    public static void Serialize(DataResponse.Type type, Stream data, Stream result)
    {
      BinaryWriter writer = new BinaryWriter(result);
      writer.Write(NetworkProtocol.NETWORK_PROTOCOL_VERSION);
      writer.Write((UInt32)data.Length);
      writer.Write((UInt32)type);

      long position = data.Position;
      data.Seek(0, SeekOrigin.Begin);
      data.CopyTo(result);
      data.Seek(position, SeekOrigin.Begin);
    }

    public static DataResponse Create(Stream stream)
    {
      if (stream == null || !stream.CanRead)
        return null;

      var reader = new BinaryReader(stream);

      uint version = reader.ReadUInt32();
      uint length = reader.ReadUInt32();
      DataResponse.Type responseType = (DataResponse.Type)reader.ReadUInt32();
      byte[] bytes = reader.ReadBytes((int)length);

      return new DataResponse(responseType, version, new BinaryReader(new MemoryStream(bytes)));
    }

    public static DataResponse Create(String base64)
    {
      MemoryStream stream = new MemoryStream(Convert.FromBase64String(base64));
      return DataResponse.Create(stream);
    }
  }

  public enum MessageType
  {
    Start,
    Stop,
    TurnSampling,
		SetupHook,
		SetupWorkingThread,
  }

  public abstract class Message
  {
    public abstract Int32 GetMessageType();
    public virtual void Write(BinaryWriter writer)
    {
      writer.Write(GetMessageType());
    }
  }

  class StartMessage : Message
  {
    public StartMessage()
    {
    }

    public override Int32 GetMessageType()
    {
      return (Int32)MessageType.Start;
    }

    public override void Write(BinaryWriter writer)
    {
      base.Write(writer);
    }
  }

  class StopMessage : Message
  {
    public override Int32 GetMessageType()
    {
      return (Int32)MessageType.Stop;
    }
  }

  class TurnSamplingMessage : Message
  {
    Int32 eventID;
    bool isActive;

    public TurnSamplingMessage(Int32 eventID, bool isActive)
    {
      this.eventID = eventID;
      this.isActive = isActive;
    }

    public override Int32 GetMessageType()
    {
      return (Int32)MessageType.TurnSampling;
    }

    public override void Write(BinaryWriter writer)
    {
      base.Write(writer);
      writer.Write(eventID);
      writer.Write(isActive);
    }
  }

	class SetupHookMessage : Message
	{
		UInt64 address;
		bool isHooked;

		public SetupHookMessage(UInt64 address, bool isHooked)
		{
			this.address = address;
			this.isHooked = isHooked;
		}

		public override Int32 GetMessageType()
		{
			return (Int32)MessageType.SetupHook;
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(address);
			writer.Write(isHooked);
		}
	}

	class SetupWorkingThreadMessage : Message
	{
		UInt32 threadID;

		public SetupWorkingThreadMessage(UInt32 threadID)
		{
			this.threadID = threadID;
		}

		public override Int32 GetMessageType()
		{
			return (Int32)MessageType.SetupWorkingThread;
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(threadID);
		}
	}

}
