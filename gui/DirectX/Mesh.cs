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

		public System.Windows.Media.Matrix WorldTransform { get; set; }

		private System.Windows.Media.Matrix localTransform = System.Windows.Media.Matrix.Identity;
		private System.Windows.Media.Matrix inverseLocalTransform = System.Windows.Media.Matrix.Identity;

		public System.Windows.Rect AABB { get; set; }

		public System.Windows.Media.Matrix LocalTransform
		{
			get
			{
				return localTransform;
			}
			set
			{
				localTransform = value;
				inverseLocalTransform = value;
				inverseLocalTransform.Invert();
			}
		}

		public System.Windows.Media.Matrix InverseLocalTransform
		{
			get { return inverseLocalTransform; }
		}

		public Mesh()
		{
			WorldTransform = System.Windows.Media.Matrix.Identity;
			LocalTransform = System.Windows.Media.Matrix.Identity;
			AABB = new System.Windows.Rect();
		}

		public void Dispose()
		{
			Utilities.Dispose(ref VertexBuffer);
			Utilities.Dispose(ref IndexBuffer);
		}
	}
}
