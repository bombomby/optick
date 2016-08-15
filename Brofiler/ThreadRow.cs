using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Profiler.Data;
using System.Windows.Media;
using Profiler.DirectX;
using System.Windows.Forms;

namespace Profiler
{
    public struct Interval
    {
        public double Left { get; set; }
        public double Width { get; set; }

        public double Right { get { return Left + Width; } }

        public void Normalize()
        {
            Width = Math.Max(0.0, Math.Min(Width, 1.0));
            Left = Math.Max(0.0, Math.Min(Left, 1.0 - Width));
        }

        public Interval(double left, double width)
        {
            Left = left;
            Width = width;
        }

        public static Interval Unit = new Interval(0.0, 1.0);
    }

    public class ThreadScroll
    {
        public Durable TimeSlice { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public Interval ViewUnit = Interval.Unit;

        public Durable ViewTime { get { return UnitToTime(ViewUnit); } }

        public double Zoom { get { return 1.0 / ViewUnit.Width; } }

        public Interval TimeToUnit(Durable d)
        {
            double durationTicks = TimeSlice.Finish - TimeSlice.Start;
            return new Interval((d.Start - TimeSlice.Start) / durationTicks, (d.Finish - d.Start) / durationTicks);
        }
        public Interval TimeToPixel(Durable d)
        {
            Interval unit = TimeToUnit(d);
            double scale = Width * Zoom;
            return new Interval((unit.Left - ViewUnit.Left) * scale, unit.Width * scale);
        }

        public double PixelToUnitLength(double pixel)
        {
            return (pixel / Width) * ViewUnit.Width;
        }

        public ITick PixelToTime(double pixel)
        {
            double unit = ViewUnit.Left + PixelToUnitLength(pixel);
            return new Tick() { Start = TimeSlice.Start + (long)(unit * (TimeSlice.Finish - TimeSlice.Start))};
        }

        public Durable UnitToTime(Interval unit)
        {
            long duration = TimeSlice.Finish - TimeSlice.Start;
            return new Durable(TimeSlice.Start + (long)(ViewUnit.Left * duration), TimeSlice.Start + (long)(ViewUnit.Right * duration));
        }

    }

    public abstract class ThreadRow
    {
        public enum RenderPriority
        {
            Background,
            Normal,
            Foreground,
            Selection,
        }

        public RenderPriority Priority = RenderPriority.Normal;

        public const double BaseHeight = 16.0;
        public const double BaseMargin = 0.75;

        public double Offset { get; set; }
        public abstract double Height { get; }
        public abstract String Name { get; }
        public FrameGroup Group { get; set; }

        public abstract void Render(DirectX.DirectXCanvas canvas, ThreadScroll scroll);
        public abstract void BuildMesh(DirectX.DirectXCanvas canvas, ThreadScroll scroll);

        public abstract void OnMouseMove(Point point, ThreadScroll scroll);
        public abstract void OnMouseClick(Point point, MouseEventArgs e, ThreadScroll scroll);
        public abstract void ApplyFilter(DirectX.DirectXCanvas canvas, ThreadScroll scroll, HashSet<EventDescription> descriptions);
    }
    public class HeaderThreadRow : ThreadRow
    {
        public override double Height { get { return BaseHeight; } }
        public override string Name { get { return String.Empty; } }

        public Color GradientTop { get; set; }
        public Color GradientBottom { get; set; }
        public Color SplitLines { get; set; }
        public Color TextColor { get; set; }
        public HeaderThreadRow(FrameGroup group)
        {
            Group = group;
        }

        DirectX.Mesh BackgroundMeshLines { get; set; }
        DirectX.Mesh BackgroundMeshTris { get; set; }

        public override void BuildMesh(DirectX.DirectXCanvas canvas, ThreadScroll scroll)
        {
            DirectX.DynamicMesh builder = canvas.CreateMesh();
            builder.Geometry = DirectX.Mesh.GeometryType.Lines;

            foreach (EventFrame frame in Group.MainThread.Events)
            {
                double x = scroll.TimeToUnit(frame.Header).Left;
                builder.AddLine(new Point(x, 0.0), new Point(x, 1.0), SplitLines);
            }
            BackgroundMeshLines = builder.Freeze(canvas.RenderDevice);

            DirectX.DynamicMesh builderHeader = canvas.CreateMesh();
            builderHeader.AddRect(new Rect(0.0, 0.0, 1.0, (Height - BaseMargin) / scroll.Height), new Color[] {GradientTop, GradientTop, GradientBottom, GradientBottom});
            BackgroundMeshTris = builderHeader.Freeze(canvas.RenderDevice);
        }

        public override void Render(DirectX.DirectXCanvas canvas, ThreadScroll scroll)
        {
            SharpDX.Matrix world = SharpDX.Matrix.Scaling((float)scroll.Zoom, 1.0f, 1.0f);
            world.TranslationVector = new SharpDX.Vector3(-(float)(scroll.ViewUnit.Left * scroll.Zoom), 0.0f, 0.0f);

            BackgroundMeshLines.World = world;
            BackgroundMeshTris.World = world;

            canvas.Draw(BackgroundMeshTris);
            canvas.Draw(BackgroundMeshLines);

            Data.Utils.ForEachInsideInterval(Group.MainThread.Events, scroll.ViewTime, frame =>
            {
                Interval interval = scroll.TimeToPixel(frame.Header);

                String text = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} ms", frame.Header.Duration);

                // 2 times to emulate "bold"
                for (int i = 0; i < 2; ++i)
                canvas.Text.Draw(new Point(interval.Left, Offset), text, TextColor, TextAlignment.Center, interval.Width);
            });
        }

        public override void OnMouseMove(Point point, ThreadScroll scroll) { }

        public override void OnMouseClick(Point point, MouseEventArgs e, ThreadScroll scroll) { }

        public override void ApplyFilter(DirectXCanvas canvas, ThreadScroll scroll, HashSet<EventDescription> descriptions) { }
    }

    public class EventsThreadRow : ThreadRow
    {
        ThreadDescription Description { get; set; }
        ThreadData EventData { get; set; }
        int MaxDepth { get; set; }

        Mesh Mesh { get; set; }
        Mesh SyncMesh { get; set; }

        const double SyncLineHeight = 4.0;
        static Color SynchronizationColor = Colors.OrangeRed;

        EventFilter Filter { get; set; }
        Mesh FilterMesh;

        public EventsThreadRow(FrameGroup group, ThreadDescription desc, ThreadData data)
        {
            Description = desc;
            EventData = data;
            Group = group;

            data.Events.ForEach(frame => MaxDepth = Math.Max(frame.CategoriesTree.Depth, MaxDepth));
        }

        void BuildMeshNode(DirectX.DynamicMesh builder, ThreadScroll scroll, EventNode node, int level)
        {
            if (level == MaxDepth)
                return;

            Interval interval = scroll.TimeToUnit(node.Entry);

            double y = (double)level / MaxDepth;
            double h = 1.0 / MaxDepth;

            builder.AddRect(new Rect(interval.Left, y, interval.Width, h), node.Description.Color);

            foreach (EventNode child in node.Children)
                BuildMeshNode(builder, scroll, child, level + 1);
        }

        public override void BuildMesh(DirectX.DirectXCanvas canvas, ThreadScroll scroll)
        {
            // Build Mesh
            DirectX.DynamicMesh builder = canvas.CreateMesh();
            DirectX.DynamicMesh syncBuilder = canvas.CreateMesh();

            foreach (EventFrame frame in EventData.Events)
            {
                Durable interval = Group.Board.TimeSlice;

                foreach (EventNode node in frame.CategoriesTree.Children)
                    BuildMeshNode(builder, scroll, node, 0);

                foreach (EventData sync in frame.Synchronization)
                {
                    Interval syncInterval = scroll.TimeToUnit(sync);
                    syncBuilder.AddRect(new Rect(syncInterval.Left, BaseMargin / Height, syncInterval.Width, SyncLineHeight / Height), SynchronizationColor);
                }
            }

            Mesh = builder.Freeze(canvas.RenderDevice);
            SyncMesh = syncBuilder.Freeze(canvas.RenderDevice);
        }

        public override double Height { get { return BaseHeight * MaxDepth; } }
        public override string Name { get { return Description.Name; } }

        const double TextDrawThreshold = 8.0;
        const double TextDrawOffset = 1.5;

        public override void Render(DirectX.DirectXCanvas canvas, ThreadScroll scroll)
        {
            SharpDX.Matrix world = SharpDX.Matrix.Scaling((float)scroll.Zoom, (float)((Height - 2.0 * BaseMargin) / scroll.Height), 1.0f);
            world.TranslationVector = new SharpDX.Vector3(-(float)(scroll.ViewUnit.Left * scroll.Zoom), (float)((Offset + 1.0 * BaseMargin) / scroll.Height), 0.0f);

            if (Mesh != null)
            {
                Mesh.World = world;
                canvas.Draw(Mesh);
            }

            if (FilterMesh != null)
            {
                FilterMesh.World = world;
                canvas.Draw(FilterMesh);
            }

            if (SyncMesh != null)
            {
                SyncMesh.World = world;
                canvas.Draw(SyncMesh);
            }

            Data.Utils.ForEachInsideInterval(EventData.Events, scroll.ViewTime, frame =>
            {
                frame.CategoriesTree.ForEachChild((node, level) =>
                {
                    Entry entry = (node as EventNode).Entry;
                    Interval intervalPx = scroll.TimeToPixel(entry);

                    if (intervalPx.Width < TextDrawThreshold)
                        return false;

                    if (intervalPx.Left < 0.0)
                    {
                        intervalPx.Width += intervalPx.Left;
                        intervalPx.Left = 0.0;
                    }

                    canvas.Text.Draw(new Point(intervalPx.Left + TextDrawOffset, Offset + level * BaseHeight), 
                                     entry.Description.Name, 
                                     System.Windows.Media.Colors.Black,
                                     TextAlignment.Left,
                                     intervalPx.Width - TextDrawOffset);

                    return true;
                });
            });
        }

        public delegate void EventNodeHoverHandler(Rect rect, ThreadRow row, EventNode node);
        public event EventNodeHoverHandler EventNodeHover;

        public delegate void EventNodeSelectedHandler(ThreadRow row, EventFrame frame, EventNode node);
        public event EventNodeSelectedHandler EventNodeSelected;

        public int FindNode(Point point, ThreadScroll scroll, out EventFrame eventFrame, out EventNode eventNode)
        {
            ITick tick = scroll.PixelToTime(point.X);

            int index = Data.Utils.BinarySearchExactIndex(EventData.Events, tick.Start);

            EventFrame resultFrame = null;
            EventNode resultNode = null;
            int resultLevel = -1;

            if (index >= 0)
            {
                EventFrame frame = EventData.Events[index];

                int desiredLevel = (int)(point.Y / BaseHeight);

                frame.CategoriesTree.ForEachChild((node, level) =>
                {
                    if (level > desiredLevel || resultFrame != null)
                        return false;

                    if (level == desiredLevel)
                    {
                        EventNode evNode = (node as EventNode);
                        if (evNode.Entry.Intersect(tick.Start))
                        {
                            resultFrame = frame;
                            resultNode = evNode;
                            resultLevel = level;
                            return false;
                        }
                    }

                    return true;
                });
            }

            eventFrame = resultFrame;
            eventNode = resultNode;

            return resultLevel;
        }

        public override void OnMouseMove(Point point, ThreadScroll scroll)
        {
            EventNode node = null;
            EventFrame frame = null;
            int level = FindNode(point, scroll, out frame, out node);
            if (level != -1)
            {
                Interval interval = scroll.TimeToPixel(node.Entry);
                Rect rect = new Rect(interval.Left, Offset + level * BaseHeight + BaseMargin, interval.Width, BaseHeight - BaseMargin);
                EventNodeHover(rect, this, node);
            }
            else
            {
                EventNodeHover(new Rect(), this, null);
            }
        }

        public override void OnMouseClick(Point point, MouseEventArgs e, ThreadScroll scroll)
        {
            EventNode node = null;
            EventFrame frame = null;
            int level = FindNode(point, scroll, out frame, out node);
            if (level != -1 && EventNodeSelected != null)
                EventNodeSelected(this, frame, node);
        }

        //static Color FilterFrameColor = Colors.LimeGreen;
        //static Color FilterEntryColor = Colors.Tomato;

        static Color FilterFrameColor = Colors.PaleGreen;
        static Color FilterEntryColor = Colors.Salmon;

        public override void ApplyFilter(DirectXCanvas canvas, ThreadScroll scroll, HashSet<EventDescription> descriptions)
        {
            Filter = EventFilter.Create(Description, EventData, descriptions);

            if (Filter != null)
            {
                DynamicMesh builder = canvas.CreateMesh();

                foreach (EventFrame frame in EventData.Events)
                {
                    Interval interval = scroll.TimeToUnit(frame.Header);
                    builder.AddRect(new Rect(interval.Left, 0.0, interval.Width, 1.0), FilterFrameColor);
                }

                foreach (Entry entry in Filter.Entries)
                {
                    Interval interval = scroll.TimeToUnit(entry);
                    builder.AddRect(new Rect(interval.Left, 0.0, interval.Width, 1.0), FilterEntryColor);
                }

                SharpDX.Utilities.Dispose(ref FilterMesh);
                FilterMesh = builder.Freeze(canvas.RenderDevice);
                //if (FilterMesh != null)
                //    FilterMesh.UseAlpha = true;
            }
            else
            {
                SharpDX.Utilities.Dispose(ref FilterMesh);
            }
        }
    }

    class EventFilter
    {
        public FrameGroup Group { get; set; }
        public HashSet<EventDescription> Descriptions { get; set; }

        public List<Entry> Entries = new List<Entry>();

        Object criticalSection = new Object();

        public delegate void LoadedEventHandler();

        void Load(ThreadDescription description, ThreadData data)
        {
            data.Events.ForEach(frame =>
            {
                foreach (EventDescription desc in Descriptions)
                {
                    List<Entry> filteredEntries = frame.ShortBoard.Get(desc);

                    if (filteredEntries != null)
                    {
                        lock (criticalSection)
                        {
                            Entries.AddRange(filteredEntries);
                        }
                    }
                        
                }
            });

            lock (criticalSection)
            {
                Entries.Sort();
            }
        }

        
        public static EventFilter Create(ThreadDescription description, ThreadData data, HashSet<EventDescription> descriptions)
        {
            if (descriptions == null)
                return null;

            EventFilter result = new EventFilter() { Descriptions = descriptions };
            result.Load(description, data);
            return result;
        }
    }
}
