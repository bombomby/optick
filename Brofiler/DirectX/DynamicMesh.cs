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

        public static int[] BoxTriIndices = new int[]{ 0, 1, 2, 2, 3, 0 };
        public static int[] BoxLineIndices = new int[] { 0, 1, 1, 2, 2, 3, 3, 0 };

        int[] GetBoxIndicesList()
        {
            return Geometry == Mesh.GeometryType.Polygons ? BoxTriIndices : BoxLineIndices;
        }

        public void AddRect(Rect rect, System.Windows.Media.Color color)
        {
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

        public void AddRect(Rect rect, System.Windows.Media.Color[] colors)
        {
            int index = Vertices.Count;
            Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)rect.Left, (float)rect.Top), Color = Utils.Convert(colors[0]) });
            Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)rect.Right, (float)rect.Top), Color = Utils.Convert(colors[1]) });
            Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)rect.Right, (float)rect.Bottom), Color = Utils.Convert(colors[2]) });
            Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)rect.Left, (float)rect.Bottom), Color = Utils.Convert(colors[3]) });
            foreach (int i in GetBoxIndicesList())
                Indices.Add(index + i);

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
            mesh.VertexBuffer = Vertices.Freeze(device);
            mesh.IndexBuffer = Indices.Freeze(device);
            mesh.VertexBufferBinding = new VertexBufferBinding(mesh.VertexBuffer, Marshal.SizeOf(typeof(Mesh.Vertex)), 0);
            mesh.PrimitiveCount = Geometry == Mesh.GeometryType.Polygons ? Indices.Count / 3 : Indices.Count / 2;
            mesh.Geometry = Geometry;
            mesh.Projection = Projection;
            mesh.World = World;
            mesh.Fragment = Fragment;
            return mesh;
        }

        public void AddLine(System.Windows.Point start, System.Windows.Point finish, System.Windows.Media.Color color)
        {
            Debug.Assert(Geometry == Mesh.GeometryType.Lines);

            int index = Vertices.Count;
            SharpDX.Color c = Utils.Convert(color);
            Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)start.X, (float)start.Y), Color = c });
            Vertices.Add(new Mesh.Vertex() { Position = new Vector2((float)finish.X, (float)finish.Y), Color = c });
            Indices.Add(index + 0);
            Indices.Add(index + 1);
        }

        public DynamicMesh(Device device)
        {
            Vertices = new DynamicBuffer<Vertex>(device, BindFlags.VertexBuffer);
            Indices = new DynamicBuffer<int>(device, BindFlags.IndexBuffer);
        }
    }
}
