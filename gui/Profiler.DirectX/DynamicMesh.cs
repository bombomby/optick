using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SharpDX;
using SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Diagnostics;

namespace Profiler.DirectX
{
	public class DynamicMesh : Mesh
	{
		DynamicBuffer<Mesh.Vertex> Vertices;
		DynamicBuffer<int> Indices;

		bool IsDirty { get; set; }

		public static int[] BoxTriIndices = new int[] { 0, 1, 2, 2, 3, 0 };
		public static int[] BoxLineIndices = new int[] { 0, 1, 1, 2, 2, 3, 3, 0 };
		public static int[] TriIndices = new int[] { 0, 1, 2 };
		public static int[] TriLineIndices = new int[] { 0, 1, 1, 2, 2, 0 };

		int[] GetBoxIndicesList()
		{
			return Geometry == Mesh.GeometryType.Polygons ? BoxTriIndices : BoxLineIndices;
		}

		int[] GetTriIndicesList()
		{
			return Geometry == Mesh.GeometryType.Polygons ? TriIndices : TriLineIndices;
		}

		public virtual void AddRect(Rect rect, System.Windows.Media.Color color)
		{
			rect = new Rect(InverseLocalTransform.Transform(rect.Location), new Size(InverseLocalTransform.M11 * rect.Size.Width, InverseLocalTransform.M22 * rect.Size.Height));

			int index = Vertices.Count;
			SharpDX.Color c = Utils.Convert(color);
			Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)rect.Left, (float)rect.Top), Color = c });
			Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)rect.Right, (float)rect.Top), Color = c });
			Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)rect.Right, (float)rect.Bottom), Color = c });
			Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)rect.Left, (float)rect.Bottom), Color = c });
			foreach (int i in GetBoxIndicesList())
				Indices.Add(index + i);

			IsDirty = true;
		}

		public virtual void AddRect(System.Windows.Point[] rect, System.Windows.Media.Color color)
		{
			int index = Vertices.Count;
			SharpDX.Color c = Utils.Convert(color);
			for (int i = 0; i < 4; ++i)
			{
				System.Windows.Point p = InverseLocalTransform.Transform(rect[i]);
				Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)p.X, (float)p.Y), Color = c });
			}

			foreach (int i in GetBoxIndicesList())
				Indices.Add(index + i);

			IsDirty = true;
		}


		public virtual void AddRect(Rect rect, System.Windows.Media.Color[] colors)
		{
			rect = new Rect(InverseLocalTransform.Transform(rect.Location), new Size(InverseLocalTransform.M11 * rect.Size.Width, InverseLocalTransform.M22 * rect.Size.Height));

			int index = Vertices.Count;
			Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)rect.Left, (float)rect.Top), Color = Utils.Convert(colors[0]) });
			Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)rect.Right, (float)rect.Top), Color = Utils.Convert(colors[1]) });
			Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)rect.Right, (float)rect.Bottom), Color = Utils.Convert(colors[2]) });
			Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)rect.Left, (float)rect.Bottom), Color = Utils.Convert(colors[3]) });
			foreach (int i in GetBoxIndicesList())
				Indices.Add(index + i);

			IsDirty = true;
		}

		public virtual void AddTri(System.Windows.Point[] points, System.Windows.Media.Color color)
		{
			AddTri(points[0], points[1], points[2], color);
		}

		public virtual void AddTri(System.Windows.Point a, System.Windows.Point b, System.Windows.Point c, System.Windows.Media.Color color)
		{
			a = InverseLocalTransform.Transform(a);
			b = InverseLocalTransform.Transform(b);
			c = InverseLocalTransform.Transform(c);

			int index = Vertices.Count;
			SharpDX.Color vertexColor = Utils.Convert(color);
			Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)a.X, (float)a.Y), Color = vertexColor });
			Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)b.X, (float)b.Y), Color = vertexColor });
			Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)c.X, (float)c.Y), Color = vertexColor });

			foreach (int i in GetTriIndicesList())
				Indices.Add(index + i);

			IsDirty = true;
		}

		public virtual void AddLine(System.Windows.Point start, System.Windows.Point finish, System.Windows.Media.Color color)
		{
			start = InverseLocalTransform.Transform(start);
			finish = InverseLocalTransform.Transform(finish);

			Debug.Assert(Geometry == Mesh.GeometryType.Lines);

			int index = Vertices.Count;
			SharpDX.Color c = Utils.Convert(color);
			Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)start.X, (float)start.Y), Color = c });
			Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)finish.X, (float)finish.Y), Color = c });
			Indices.Add(index + 0);
			Indices.Add(index + 1);

			IsDirty = true;
		}

		public void Update(Device device, bool autoclear = true)
		{
			if (IsDirty || (Vertices.Count == 0 && PrimitiveCount > 0))
			{
				PrimitiveCount = Geometry == Mesh.GeometryType.Polygons ? Indices.Count / 3 : Indices.Count / 2;

				Vertices.Update(device, autoclear);
				Indices.Update(device, autoclear);

				VertexBuffer = Vertices.Buffer;
				IndexBuffer = Indices.Buffer;

				VertexBufferBinding = new VertexBufferBinding(Vertices.Buffer, Marshal.SizeOf(typeof(Mesh.Vertex)), 0);
				IsDirty = false;
			}
		}

		public Mesh Freeze(Device device)
		{
			if (Indices.Count == 0 || Vertices.Count == 0)
				return null;

			Mesh mesh = new Mesh();

			System.Windows.Point minP = Utils.Convert(Vertices[0].Position);
			System.Windows.Point maxP = Utils.Convert(Vertices[0].Position);

			mesh.AABB = new Rect(Utils.Convert(Vertices[0].Position), new Size());
			foreach (var v in Vertices)
			{
				System.Windows.Point p = Utils.Convert(v.Position);
				minP.X = Math.Min(minP.X, p.X);
				minP.Y = Math.Min(minP.Y, p.Y);
				maxP.X = Math.Max(maxP.X, p.X);
				maxP.Y = Math.Max(maxP.Y, p.Y);
			}

			mesh.AABB = new Rect(minP, maxP);

			mesh.VertexBuffer = Vertices.Freeze(device);
			mesh.IndexBuffer = Indices.Freeze(device);
			mesh.VertexBufferBinding = new VertexBufferBinding(mesh.VertexBuffer, Marshal.SizeOf(typeof(Mesh.Vertex)), 0);
			mesh.PrimitiveCount = Geometry == Mesh.GeometryType.Polygons ? Indices.Count / 3 : Indices.Count / 2;
			mesh.Geometry = Geometry;
			mesh.Projection = Projection;
			mesh.WorldTransform = WorldTransform;
			mesh.LocalTransform = LocalTransform;
			mesh.Fragment = Fragment;
			return mesh;
		}

		public DynamicMesh(Device device)
		{
			Vertices = new DynamicBuffer<Vertex>(device, BindFlags.VertexBuffer);
			Indices = new DynamicBuffer<int>(device, BindFlags.IndexBuffer);
		}
	}
}
