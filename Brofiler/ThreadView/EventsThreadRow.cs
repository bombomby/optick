using System;
using System.Collections.Generic;
using System.Windows;
using Profiler.Data;
using System.Windows.Media;
using Profiler.DirectX;
using System.Windows.Forms;

namespace Profiler
{
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
            {
                BuildMeshNode(builder, scroll, child, level + 1);
            }
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
                {
                    BuildMeshNode(builder, scroll, node, 0);
                }

                Interval frameInterval = scroll.TimeToUnit(frame.Header);

                double start = frameInterval.Left;

                if (frame.Synchronization != null)
                {
                    foreach (Durable sync in frame.Synchronization)
                    {
                        Interval syncInterval = scroll.TimeToUnit(sync);

                        if (start < syncInterval.Left)
                        {
                            syncBuilder.AddRect(new Rect(start, RenderParams.BaseMargin / Height, syncInterval.Left - start, SyncLineHeight / Height), SynchronizationColor);
                        }

                        start = Math.Max(syncInterval.Right, start);
                    }
                }
            }

            Mesh = builder.Freeze(canvas.RenderDevice);
            SyncMesh = syncBuilder.Freeze(canvas.RenderDevice);
        }

        public override double Height { get { return RenderParams.BaseHeight * MaxDepth; } }
        public override string Name { get { return Description.Name; } }

        const double TextDrawThreshold = 8.0;
        const double TextDrawOffset = 1.5;

        public override void Render(DirectX.DirectXCanvas canvas, ThreadScroll scroll)
        {
            SharpDX.Matrix world = SharpDX.Matrix.Scaling((float)scroll.Zoom, (float)((Height - 2.0 * RenderParams.BaseMargin) / scroll.Height), 1.0f);
            world.TranslationVector = new SharpDX.Vector3(-(float)(scroll.ViewUnit.Left * scroll.Zoom), (float)((Offset + 1.0 * RenderParams.BaseMargin) / scroll.Height), 0.0f);

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

                    double lum = DirectX.Utils.GetLuminance(entry.Description.Color);
                    Color color = lum < 0.33 ? Colors.White : Colors.Black;

                    canvas.Text.Draw(new Point(intervalPx.Left + TextDrawOffset, (Offset + level * RenderParams.BaseHeight) * RenderParams.dpiScaleY), 
                                     entry.Description.Name, 
                                     color,
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

                int desiredLevel = (int)(point.Y / RenderParams.BaseHeight);

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
                Rect rect = new Rect(interval.Left, RenderParams.dpiScaleY * (Offset + level * RenderParams.BaseHeight + RenderParams.BaseMargin), interval.Width, (RenderParams.BaseHeight - RenderParams.BaseMargin) * RenderParams.dpiScaleY);
                EventNodeHover(rect, this, node);
            }
            else
            {
                EventNodeHover(new Rect(), this, null);
            }
        }

        public override void OnMouseHover(Point point, ThreadScroll scroll, List<object> dataContext)
        {
            EventNode node = null;
            EventFrame frame = null;
            if (FindNode(point, scroll, out frame, out node) != -1)
            {
                dataContext.Add(node);

                ITick tick = scroll.PixelToTime(point.X);
                int index = Data.Utils.BinarySearchClosestIndex(frame.Synchronization, tick.Start);
                if (index != -1)
                {
					WaitInterval interval = new WaitInterval() { Start = frame.Synchronization[index].Finish, Reason = frame.Synchronization[index].Reason };
					if (index + 1 < frame.Synchronization.Count)
					{
						if (frame.Synchronization[index].Finish < tick.Start && frame.Tick < frame.Synchronization[index + 1].Start)
						{
							interval.Finish = frame.Synchronization[index + 1].Start;
							dataContext.Add(interval);
						}
					}
					else
					{
						if (frame.Synchronization[index].Finish < tick.Start)
						{
							interval.Finish = frame.Finish;
							dataContext.Add(interval);
						}
					}
				}
            }
        }

        public override void OnMouseClick(Point point, ThreadScroll scroll)
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
}