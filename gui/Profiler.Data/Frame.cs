using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media;
using System.Windows;

namespace Profiler.Data
{
	public abstract class Frame
	{
		public DataResponse Response { get; private set; }

		public FrameGroup Group { get; set; }
		public virtual String Description { get; set; }
		public virtual String FilteredDescription { get; set; }
		public virtual double Duration { get; set; }

		public bool IsLoaded { get; protected set; }
		public abstract void Load();

		public Frame(DataResponse response, FrameGroup group)
		{
			Group = group;
			Response = response;
		}

		public abstract DataResponse.Type ResponseType { get; }
	}
}
