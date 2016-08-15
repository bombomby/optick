using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace Profiler.DirectX
{
    public class Mesh : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Vertex
        {
            public Vector2 Position;
            public SharpDX.Color Color;
        }

        public enum ProjectionType
        {
            Unit,
            Pixel,
        }

        public enum GeometryType
        {
            Polygons,
            Lines
        }

        public ProjectionType Projection { get; set; }
        public GeometryType Geometry { get; set; }

        public Fragment Fragment { get; set; }

        public bool UseAlpha { get; set; }

        public int PrimitiveCount { get; set; }
        public SharpDX.Direct3D11.Buffer VertexBuffer;
        public SharpDX.Direct3D11.Buffer IndexBuffer;

        public SharpDX.Direct3D11.VertexBufferBinding VertexBufferBinding;

        public SharpDX.Matrix World { get; set; }

        public Mesh()
        {
            World = SharpDX.Matrix.Identity;
        }

        public void Dispose()
        {
            Utilities.Dispose(ref VertexBuffer);
            Utilities.Dispose(ref IndexBuffer);
        }
    }
}
