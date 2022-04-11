using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Data
{
	public class Capture
	{
		public class OptickHeader
		{
			const UInt32 OPTICK_MAGIC = 0xB50FB50F;
			const UInt16 OPTICK_VERSION = 0;
			public enum Flags : UInt16
			{
				IsZip = 1 << 0,
				IsMiniz = 1 << 1,
			}

			public UInt32 Magic { get; set; }
			public UInt16 Version { get; set; }
			public Flags Settings { get; set; }

			public OptickHeader(Stream stream)
			{
				BinaryReader reader = new BinaryReader(stream);
				Magic = reader.ReadUInt32();
				Version = reader.ReadUInt16();
				Settings = (Flags)reader.ReadUInt16();
			}

			public OptickHeader()
			{
				Magic = OPTICK_MAGIC;
				Version = OPTICK_VERSION;
				Settings = Flags.IsZip;
			}

			public bool IsValid
			{
				get { return Magic == OPTICK_MAGIC; }
			}

			public bool IsZip
			{
				get { return (Settings & Flags.IsZip) != 0; }
			}

			public bool IsMiniz
			{
				get { return (Settings & Flags.IsMiniz) != 0; }
			}

			public void Write(Stream stream)
			{
				BinaryWriter writer = new BinaryWriter(stream);
				writer.Write(Magic);
				writer.Write(Version);
				writer.Write((UInt16)Settings);
			}
		}

		public static Stream Open(String path)
		{
			if (File.Exists(path))
			{
				FileStream stream = new FileStream(path, FileMode.Open);
				OptickHeader header = new OptickHeader(stream);
				if (header.IsValid)
				{
					if (header.IsZip)
						return new GZipStream(stream, CompressionMode.Decompress, false);

					if (header.IsMiniz)
					{
						// Workaround for RFC 1950 vs RFC 1951 mismatch
						// http://george.chiramattel.com/blog/2007/09/deflatestream-block-length-does-not-match.html
						stream.ReadByte();
						stream.ReadByte();

						return new DeflateStream(stream, CompressionMode.Decompress);
					}

					return stream;

				}
				else
				{
					stream.Close();
				}
			}
			return null;
		}

		public static Stream Create(string fileName)
		{
			return Create(new FileStream(fileName, FileMode.Create));
		}

		public static Stream Create(Stream stream, bool leaveStreamOpen = false)
		{
			OptickHeader header = new OptickHeader();
			header.Write(stream);
			if (header.IsZip)
				return new GZipStream(stream, CompressionLevel.Fastest, leaveStreamOpen);
			else
				return stream;
		}
	}
}
