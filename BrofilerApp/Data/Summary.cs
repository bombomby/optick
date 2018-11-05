using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Data
{
	public class SummaryPack : IResponseHolder
	{
		public override DataResponse Response { get; set; }

		public class Item
		{
			public String Name { get; set; }
			public String Value { get; set; }
		}

		public class Attachment
		{
			public enum Type
			{
				BRO_IMAGE,
				BRO_TEXT,
				BRO_OTHER,
			}
			public Type FileType { get; set; }
			public String Name { get; set; }
			public Stream Data { get; set; }

			public override string ToString()
			{
				return Name;
			}
		}

		public List<double> Frames { get; set; }
		public List<Item> SummaryTable { get; set; }
		public List<Attachment> Attachments { get; set; }

		public int BoardID { get; set; }

		public static SummaryPack Create(String path)
		{
			if (File.Exists(path))
			{
				using (Stream stream = Capture.Create(path))
				{
					DataResponse response = DataResponse.Create(stream);
					if (response != null)
					{
						if (response.ResponseType == DataResponse.Type.SummaryPack)
						{
							return new SummaryPack(response);
						}
					}
				}
			}

			return null;
		}

		public SummaryPack(DataResponse response)
		{
			Response = response;

			BoardID = response.Reader.ReadInt32();

			int frameCount = response.Reader.ReadInt32();
			Frames = new List<double>(frameCount);
			for (int i = 0; i < frameCount; ++i)
				Frames.Add(response.Reader.ReadSingle());

			int itemCount = response.Reader.ReadInt32();
			SummaryTable = new List<Item>(itemCount);
			for (int i = 0; i < itemCount; ++i)
				SummaryTable.Add(new Item() { Name = Utils.ReadBinaryString(response.Reader), Value = Utils.ReadBinaryString(response.Reader) });

			int attachmentCount = response.Reader.ReadInt32();
			Attachments = new List<Attachment>(attachmentCount);
			for (int i = 0; i < attachmentCount; ++i)
			{
				Attachment attachment = new Attachment()
				{
					FileType = (Attachment.Type)response.Reader.ReadInt32(),
					Name = Utils.ReadBinaryString(response.Reader)
				};

				int fileSize = response.Reader.ReadInt32();
				attachment.Data = new MemoryStream(response.Reader.ReadBytes(fileSize));

				Attachments.Add(attachment);
			}
		}
	}
}
