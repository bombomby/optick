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
    public Stream BaseStream { get; private set; }

    public virtual String Description { get; set; }
    public virtual double Duration { get; set; }

    public bool IsLoaded { get; protected set; }
    public abstract void Load();

    public Frame(Stream baseStream)
    {
      BaseStream = baseStream;
    }

    public abstract DataResponse.Type ResponseType { get; }
  }
}
