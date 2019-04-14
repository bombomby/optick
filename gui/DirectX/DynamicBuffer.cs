using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.DirectX
{
	public class Utilites
	{
		public static int SizeOf<T>() where T : struct
		{
			return Marshal.SizeOf(default(T));
		}
	}

	class DynamicBuffer<T> : List<T>, IDisposable where T : struct
	{
		public SharpDX.Direct3D11.Buffer Buffer;
		public BindFlags Type { get; private set; }

		public int BufferCapacity { get; private set; }

		public DynamicBuffer(Device device, BindFlags type)
		{
			Type = type;
		}

		void Init(Device device, int count)
		{
			SharpDX.Utilities.Dispose(ref Buffer);
			Buffer = new SharpDX.Direct3D11.Buffer(device, Utilites.SizeOf<T>() * count, ResourceUsage.Dynamic, Type, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
			BufferCapacity = count;
		}

		public void Update(Device device, bool autoclear = true)
		{
			if (BufferCapacity < Count)
				Init(device, Count);

			if (Count > 0)
			{
				DataStream stream;
				device.ImmediateContext.MapSubresource(Buffer, 0, MapMode.WriteDiscard, MapFlags.None, out stream);
				stream.WriteRange(ToArray(), 0, Count);
				device.ImmediateContext.UnmapSubresource(Buffer, 0);

				if (autoclear)
					Clear();
			}
		}

		public SharpDX.Direct3D11.Buffer Freeze(Device device)
		{
			return SharpDX.Direct3D11.Buffer.Create(device, Type, ToArray());
		}

		public void Dispose()
		{
			SharpDX.Utilities.Dispose(ref Buffer);
		}
	}
}
