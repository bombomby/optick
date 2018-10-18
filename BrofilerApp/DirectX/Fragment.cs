using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace Profiler.DirectX
{
	public class Fragment : IDisposable
	{
		public PixelShader PS { get; set; }
		public VertexShader VS { get; set; }
		public InputLayout Layout { get; set; }

		public void Dispose()
		{
			PS.Dispose();
			VS.Dispose();
			Layout.Dispose();
		}
	}
}
