using System;
using System.Collections.Generic;
using System.Windows;
using Profiler.Data;
using System.Windows.Media;
using Profiler.DirectX;

namespace Profiler.Controls
{
	public class EventsThreadRow : ThreadRow
	{
		public ThreadDescription Description { get; set; }
		public ThreadData EventData { get; set; }

		public bool LimitMaxDepth { get; set; } = true;
		int MaxThreadsDepth { get; set; }
		int MaxDepth { get; set; }

		List<Mesh> Blocks { get; set; }
		List<Mesh> SyncMesh { get; set; }
		List<Mesh> SyncWorkMesh { get; set; }
		DynamicMesh CallstackMeshPolys { get; set; }
		DynamicMesh CallstackMeshLines { get; set; }


		double SyncLineHeight = 4.0 * RenderSettings.dpiScaleY;
		static Color SynchronizationColor = Colors.Magenta;
		static Color SynchronizationColorUser = Colors.OrangeRed;
		static Color[] WorkColors = new Color[8]
			{   Colors.Lime,
				Colors.LimeGreen,
				Colors.ForestGreen,
				Colors.OliveDrab,

				Colors.RoyalBlue,
				Colors.Cyan,
				Colors.SlateBlue,
				Colors.LightBlue,
			};

		double CallstackMarkerRadius = 4.0 * RenderSettings.dpiScaleY;

		struct IntPair
		{
			public int count;
			public long duration;
		}


		bool IsUserInitiatedSync(SyncReason reason)
		{
			if (reason <= SyncReason.Win_Count)
			{
				return reason <= SyncReason.Win_UserRequest;
			}

			if (reason <= SyncReason.Pthread_Count)
			{
				// VS TODO: Find out proper user\kernel pre-emption mapping
				return true; // (reason == SyncReason.Pthread_InterruptibleSleep) || (reason == SyncReason.Pthread_UninterruptibleSleep);
			}

            if (reason <= SyncReason.SWT_COUNT)
            {
                if (reason == SyncReason.SWT_PREEMPT || reason == SyncReason.SWT_OWEPREEMPT || reason == SyncReason.SWT_NEEDRESCHED)
                    return false;
            }

			return true;
		}

		static Color CallstackColor = Colors.Red;
		static Color SystemCallstackColor = Colors.Yellow;

		EventFilter Filter { get; set; }
		Mesh FilterMesh;

		private ThreadViewSettings Settings { get; set; }

		public EventsThreadRow(FrameGroup group, ThreadDescription desc, ThreadData data, ThreadViewSettings settings)
		{
			Description = desc;
			EventData = data;
			Group = group;
			MaxDepth = 1;
			Settings = settings;

			Header = new ThreadNameView() { DataContext = this };

			UpdateThreadsDepth();

			switch (Settings?.ThreadExpandMode)
			{
				case ExpandMode.CollapseAll:
					_isExpanded = false;
					break;

				case ExpandMode.ExpandAll:
					_isExpanded = true;
					break;

				case ExpandMode.ExpandMain:
					_isExpanded = desc.ThreadIndex == group.Board.MainThreadIndex;
					break;
			}
		}

		private void UpdateThreadsDepth()
		{
			int depth = 1;

			foreach (EventFrame frame in EventData.Events)
			{
				depth = Math.Max(GetTree(frame).Depth, depth);
			}

			MaxThreadsDepth = depth;
		}

		private void UpdateDepth()
		{
			int targetDepth = IsExpanded ? Settings.ExpandedMaxThreadDepth : Settings.CollapsedMaxThreadDepth;
			MaxDepth = LimitMaxDepth ? Math.Min(Math.Max(targetDepth, 1), MaxThreadsDepth) : MaxThreadsDepth;
		}

		const float NodeGradientShade = 0.85f;

		void BuildMeshNode(DirectX.ComplexDynamicMesh builder, ThreadScroll scroll, EventNode node, int level)
		{
			if (level == MaxDepth)
				return;

			Interval interval = scroll.TimeToUnit(node.Entry);

			double y = (double)level / MaxDepth;
			double h = 1.0 / MaxDepth;

			Color nodeColor = node.Description.ForceColor;
			Color nodeGradColor = DirectX.Utils.MultiplyColor(nodeColor, NodeGradientShade);

			builder.AddRect(new Rect(interval.Left, y, interval.Width, h), new Color[] { nodeColor, nodeGradColor, nodeGradColor, nodeColor });

			foreach (EventNode child in node.Children)
			{
				BuildMeshNode(builder, scroll, child, level + 1);
			}
		}

		const int DIPSplitCount = 20;

		public override void BuildMesh(DirectX.DirectXCanvas canvas, ThreadScroll scroll)
		{
			SetBusy(true);			
			UpdateDepth();

			// Build Mesh
			DirectX.ComplexDynamicMesh builder = new ComplexDynamicMesh(canvas, DIPSplitCount);
			DirectX.ComplexDynamicMesh syncBuilder = new ComplexDynamicMesh(canvas, DIPSplitCount);
			DirectX.ComplexDynamicMesh syncWorkBuilder = new ComplexDynamicMesh(canvas, DIPSplitCount);

			if (EventData.Sync != null && EventData.Sync != null)
			{
				SyncReason stallReason = SyncReason.SyncReasonCount;
				long stallFrom = 0;
				int frameSyncIndex = 0;

				for (int i = 0; i < EventData.Sync.Count; i++)
				{
					SyncInterval sync = EventData.Sync[i];

					Interval workInterval = scroll.TimeToUnit(sync);

					//draw work
					int coreColorIndex = (int)sync.Core;
					coreColorIndex = coreColorIndex % WorkColors.Length;
					Color WorkColor = WorkColors[coreColorIndex];
					syncWorkBuilder.AddRect(new Rect(workInterval.Left, 0, workInterval.Right - workInterval.Left, SyncLineHeight / Height), WorkColor);

					if (i == 0)
					{
						stallReason = sync.Reason;
						stallFrom = sync.Finish;
						continue;
					}

					long workStart = sync.Start;
					long workFinish = sync.Finish;

					while (frameSyncIndex < EventData.Events.Count && EventData.Events[frameSyncIndex].Finish < stallFrom)
						++frameSyncIndex;

					//Ignoring all the waiting outside marked work to simplify the view
					if (frameSyncIndex < EventData.Events.Count && EventData.Events[frameSyncIndex].Start <= workStart)
					{
						Durable syncDurable = new Durable(stallFrom, workStart);
						Interval syncInterval = scroll.TimeToUnit(syncDurable);

						double syncWidth = syncInterval.Right - syncInterval.Left;
						if (syncWidth > 0)
						{
							// draw sleep
							Color waitColor = IsUserInitiatedSync(stallReason) ? SynchronizationColorUser : SynchronizationColor;
							syncBuilder.AddRect(new Rect(syncInterval.Left, 0, syncWidth, SyncLineHeight / Height), waitColor);
						}
					}

					stallFrom = workFinish;
					stallReason = sync.Reason;
				}
			}

			foreach (EventFrame frame in EventData.Events)
			{
				Durable interval = Group.Board.TimeSlice;
				EventTree tree = GetTree(frame);
				foreach (EventNode node in tree.Children)
				{
					BuildMeshNode(builder, scroll, node, 0);
				}
			}

			Blocks = builder.Freeze(canvas.RenderDevice);
			SyncMesh = syncBuilder.Freeze(canvas.RenderDevice);
			SyncWorkMesh = syncWorkBuilder.Freeze(canvas.RenderDevice);

			CallstackMeshPolys = canvas.CreateMesh();
			CallstackMeshPolys.Projection = Mesh.ProjectionType.Pixel;

			CallstackMeshLines = canvas.CreateMesh();
			CallstackMeshLines.Geometry = Mesh.GeometryType.Lines;
			CallstackMeshLines.Projection = Mesh.ProjectionType.Pixel;

			SetBusy(false);
		}

		public override double Height { get { return RenderParams.BaseHeight * MaxDepth; } }
		public override string Name { get { return Description.ThreadID != UInt64.MaxValue ? string.Format("{0} (0x{1:x})", Description.FullName, Description.ThreadID) : Description.Name; } }

		double TextDrawThreshold = 8.0 * RenderSettings.dpiScaleX;
		double TextDrawOffset = 1.5 * RenderSettings.dpiScaleY;

		public static void Draw(DirectX.DirectXCanvas canvas, List<Mesh> meshes, Matrix world)
		{
			meshes.ForEach(mesh =>
			{
				mesh.WorldTransform = world;
				canvas.Draw(mesh);
			});
		}

		EventTree GetTree(EventFrame frame)
		{
			return frame.Root;
		}

		public override void Render(DirectX.DirectXCanvas canvas, ThreadScroll scroll, DirectXCanvas.Layer layer, Rect box)
		{
			if (!IsVisible)
				return;

			Matrix world = GetWorldMatrix(scroll);

			if (layer == DirectXCanvas.Layer.Background)
			{
				Draw(canvas, Blocks, world);

				if (FilterMesh != null)
				{
					FilterMesh.WorldTransform = world;
					canvas.Draw(FilterMesh);
				}

				if (scroll.SyncDraw == ThreadScroll.SyncDrawType.Wait)
				{
					Draw(canvas, SyncMesh, world);
				}

				if (SyncWorkMesh != null && scroll.SyncDraw == ThreadScroll.SyncDrawType.Work)
				{
					Draw(canvas, SyncWorkMesh, world);
				}

				Data.Utils.ForEachInsideInterval(EventData.Events, scroll.ViewTime, frame =>
				{
					GetTree(frame).ForEachChild((node, level) =>
					{
						Entry entry = (node as EventNode).Entry;
						Interval intervalPx = scroll.TimeToPixel(entry);

						if (intervalPx.Width < TextDrawThreshold || intervalPx.Right < 0.0 || level >= MaxDepth)
							return false;

						if (intervalPx.Left < 0.0)
						{
							intervalPx.Width += intervalPx.Left;
							intervalPx.Left = 0.0;
						}

						double lum = Data.Utils.GetLuminance(entry.Description.ForceColor);
						Color color = lum < Data.Utils.LuminanceThreshold ? Colors.White : Colors.Black;

						canvas.Text.Draw(new Point(intervalPx.Left + TextDrawOffset, Offset + level * RenderParams.BaseHeight),
										 entry.Description.Name,
										 color,
										 TextAlignment.Left,
										 intervalPx.Width - TextDrawOffset);

						return true;
					});
				});
			}

			if (layer == DirectXCanvas.Layer.Foreground)
			{
				if (CallstackMeshPolys != null && CallstackMeshLines != null && (scroll.DrawCallstacks != 0 || scroll.DrawDataTags))
				{
					double width = CallstackMarkerRadius;
					double height = CallstackMarkerRadius;
					double offset = Offset + RenderParams.BaseHeight * 0.5;

					if (scroll.DrawCallstacks != 0)
					{
						Data.Utils.ForEachInsideInterval(EventData.Callstacks, scroll.ViewTime, callstack =>
						{
							if ((callstack.Reason & scroll.DrawCallstacks) != 0)
							{
								double center = scroll.TimeToPixel(callstack);

								Point[] points = new Point[] { new Point(center - width, offset), new Point(center, offset - height), new Point(center + width, offset), new Point(center, offset + height) };

								Color fillColor = (callstack.Reason == CallStackReason.AutoSample) ? CallstackColor : SystemCallstackColor;
								Color strokeColor = Colors.Black;

								CallstackMeshPolys.AddRect(points, fillColor);
								CallstackMeshLines.AddRect(points, strokeColor);
							}
						});
					}

					if (scroll.DrawDataTags && EventData.TagsPack != null)
					{
						Data.Utils.ForEachInsideInterval(EventData.TagsPack.Tags, scroll.ViewTime, tag =>
						{
							double center = scroll.TimeToPixel(tag);
							Point[] points = new Point[] { new Point(center - width, Offset), new Point(center + width, Offset), new Point(center, offset) };
							CallstackMeshPolys.AddTri(points, CallstackColor);
							CallstackMeshLines.AddTri(points, Colors.Black);
						});
					}

					CallstackMeshPolys.Update(canvas.RenderDevice);
					CallstackMeshLines.Update(canvas.RenderDevice);

					canvas.Draw(CallstackMeshPolys);
					canvas.Draw(CallstackMeshLines);
				}
			}


		}

		public delegate void EventNodeHoverHandler(Point mousePos, Rect rect, ThreadRow row, EventNode node);
		public event EventNodeHoverHandler EventNodeHover;

		public delegate void EventNodeSelectedHandler(ThreadRow row, EventFrame frame, EventNode node);
		public event EventNodeSelectedHandler EventNodeSelected;

		public EventFrame FindFrame(ITick tick)
		{
			int index = Data.Utils.BinarySearchExactIndex(EventData.Events, tick.Start);
			if (index >= 0)
			{
				return EventData.Events[index];
			}
			return null;
		}

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

				GetTree(frame).ForEachChild((node, level) =>
				{
					if (level > desiredLevel || resultFrame != null)
					{
						return false;
					}

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
				Rect rect = new Rect(interval.Left, Offset + level * RenderParams.BaseHeight + RenderParams.BaseMargin, interval.Width, RenderParams.BaseHeight - RenderParams.BaseMargin);
				EventNodeHover?.Invoke(point, rect, this, node);
			}
			else
			{
				EventNodeHover?.Invoke(point, new Rect(), this, null);
			}
		}

		public override void OnMouseHover(Point point, ThreadScroll scroll, List<object> dataContext)
		{
			EventNode node = null;
			EventFrame frame = null;

			ITick tick = scroll.PixelToTime(point.X);

			if (FindNode(point, scroll, out frame, out node) != -1)
			{
				dataContext.Add(node);
			}

			// show current sync info
			if (EventData.Sync != null && EventData.Sync != null)
			{
				int index = Data.Utils.BinarySearchClosestIndex(EventData.Sync, tick.Start);
				if (index != -1)
				{
					bool insideWaitInterval = false;
					WaitInterval interval = new WaitInterval() { Start = EventData.Sync[index].Finish, Reason = EventData.Sync[index].Reason };
					if (index + 1 < EventData.Sync.Count)
					{
						if (EventData.Sync[index].Finish < tick.Start && tick.Start < EventData.Sync[index + 1].Start)
						{
							UInt64 threadId = EventData.Sync[index].NewThreadId;

							ThreadDescription threadDesc = null;
							Group.Board.ThreadDescriptions.TryGetValue(threadId, out threadDesc);

							interval.newThreadDesc = threadDesc;
							interval.newThreadId = threadId;

							interval.Finish = EventData.Sync[index + 1].Start;
							dataContext.Add(interval);
							insideWaitInterval = true;
						}
					}

					if (!insideWaitInterval)
					{
						interval.Reason = SyncReason.SyncReasonActive;
						interval.Start = EventData.Sync[index].Start;
						interval.Finish = EventData.Sync[index].Finish;
						interval.core = (byte)EventData.Sync[index].Core;
						dataContext.Add(interval);
					}
				}
			}

			if (node != null)
			{
				// build all intervals inside selected node
				int from = Data.Utils.BinarySearchClosestIndex(frame.Synchronization, node.Entry.Start);
				int to = Data.Utils.BinarySearchClosestIndex(frame.Synchronization, node.Entry.Finish);

				if (from >= 0 && to >= from)
				{
					IntPair[] waitInfo = new IntPair[(int)SyncReason.SyncReasonCount];

					for (int index = from; index <= to; ++index)
					{
						SyncReason reason = frame.Synchronization[index].Reason;
						int reasonIndex = (int)reason;

						long idleStart = frame.Synchronization[index].Finish;
						long idleFinish = (index + 1 < frame.Synchronization.Count) ? frame.Synchronization[index + 1].Start : frame.Finish;

						if (idleStart > node.Entry.Finish)
						{
							continue;
						}

						long idleStartClamped = Math.Max(idleStart, node.Entry.Start);
						long idleFinishClamped = Math.Min(idleFinish, node.Entry.Finish);
						long durationInTicks = idleFinishClamped - idleStartClamped;
						waitInfo[reasonIndex].duration += durationInTicks;
						waitInfo[reasonIndex].count++;
					}

					NodeWaitIntervalList intervals = new NodeWaitIntervalList();

					for (int i = 0; i < waitInfo.Length; i++)
					{
						if (waitInfo[i].count > 0)
						{
							NodeWaitInterval interval = new NodeWaitInterval() { Start = 0, Finish = waitInfo[i].duration, Reason = (SyncReason)i, NodeInterval = node.Entry, Count = waitInfo[i].count };
							intervals.Add(interval);
						}
					}

					intervals.Sort((a, b) =>
				   {
					   return Comparer<long>.Default.Compare(b.Finish, a.Finish);
				   });

					if (intervals.Count > 0)
						dataContext.Add(intervals);
				}
			} // FindNode

			if (EventData.Callstacks != null && scroll.DrawCallstacks != 0)
			{
				int startIndex = Data.Utils.BinarySearchClosestIndex(EventData.Callstacks, tick.Start);

				for (int i = startIndex; (i <= startIndex + 1) && (i < EventData.Callstacks.Count) && (i != -1); ++i)
				{
					double pixelPos = scroll.TimeToPixel(EventData.Callstacks[i]);
					if (Math.Abs(pixelPos - point.X) < CallstackMarkerRadius * 1.2 && (EventData.Callstacks[i].Reason & scroll.DrawCallstacks) !=0 )
					{
						dataContext.Add(EventData.Callstacks[i]);
						break;
					}
				}
			}
		}

		public override void OnMouseClick(Point point, ThreadScroll scroll)
		{
			EventNode node = null;
			EventFrame frame = null;
			int level = FindNode(point, scroll, out frame, out node);
			if (EventNodeSelected != null)
			{
				ITick tick = scroll.PixelToTime(point.X);
				if (frame == null)
				{
					frame = FindFrame(tick);
				}
				if (frame != null)
				{
					EventNodeSelected(this, frame, node);
				}
			}
		}

		//static Color FilterFrameColor = Colors.LimeGreen;
		//static Color FilterEntryColor = Colors.Tomato;

		static Color FilterFrameColor = Colors.PaleGreen;
		static Color FilterEntryColor = Colors.Salmon;


		static Color GenerateColorFromString(string s)
		{
			int hash = s.GetHashCode();

			byte r = (byte)((hash & 0xFF0000) >> 16);
			byte g = (byte)((hash & 0x00FF00) >> 8);
			byte b = (byte)(hash & 0x0000FF);

			return Color.FromArgb(0xFF, r, g, b);
		}

		public override void ApplyFilter(DirectXCanvas canvas, ThreadScroll scroll, HashSet<EventDescription> descriptions)
		{
			Filter = EventFilter.Create(EventData, descriptions);

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