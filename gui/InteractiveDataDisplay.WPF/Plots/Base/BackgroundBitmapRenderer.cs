// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;
using System.Reactive.Subjects;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Base class for renderers, which can prepare its render results in a separate thread.
    /// </summary>
    public abstract class BackgroundBitmapRenderer : Plot
    {
        private Image outputImage;

        /// <summary>Cartesian coordinates of image currently on the screen.</summary>
        private DataRect imageCartesianRect;

        /// <summary>Size of image currently on the screen.</summary>
        private Size imageSize;

        private int maxTasks;
        private Queue<long> tasks = new Queue<long>();
        private delegate void RenderFunc(RenderResult r, RenderTaskState state);
        private List<RenderTaskState> runningTasks;

        private long nextID = 0;

        /// <summary>
        /// Initializes new instance of <see cref="BackgroundBitmapRenderer"/> class, performing all basic preparings for inheriting classes.
        /// </summary>
        protected BackgroundBitmapRenderer()
        {
            maxTasks = Math.Max(1, System.Environment.ProcessorCount - 1);
            runningTasks = new List<RenderTaskState>();

            outputImage = new Image();
            outputImage.Stretch = Stretch.None;
            outputImage.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            outputImage.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

            Children.Add(outputImage);
            Unloaded += BackgroundBitmapRendererUnloaded;
        }

        void BackgroundBitmapRendererUnloaded(object sender, RoutedEventArgs e)
        {
            CancelAll();
        }

        private Size prevSize = new Size(Double.NaN, Double.NaN);
        private double prevScaleX = Double.NaN, prevScaleY = Double.NaN,
            prevOffsetX = Double.NaN, prevOffsetY = Double.NaN;

        /// <summary>
        /// Measures the size in layout required for child elements and determines a size for parent. 
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            availableSize = base.MeasureOverride(availableSize);

            outputImage.Measure(availableSize);
            
            // From resize or navigation?
            if (prevSize != availableSize ||
               prevOffsetX != masterField.OffsetX || prevOffsetY != masterField.OffsetY ||
               prevScaleX != masterField.ScaleX || prevScaleY != masterField.ScaleY)
            {
                prevSize = availableSize;
                prevOffsetX = masterField.OffsetX;
                prevOffsetY = masterField.OffsetY;
                prevScaleX = masterField.ScaleX;
                prevScaleY = masterField.ScaleY;

                CancelAll();

                if (imageSize.Width > 0 && imageSize.Height > 0)
                {
                    var newLT = new Point(LeftFromX(imageCartesianRect.XMin), TopFromY(imageCartesianRect.YMax));
                    var newRB = new Point(LeftFromX(imageCartesianRect.XMax), TopFromY(imageCartesianRect.YMin));
                    Canvas.SetLeft(outputImage, newLT.X);
                    Canvas.SetTop(outputImage, newLT.Y);
                    outputImage.RenderTransform = new ScaleTransform
                    {
                        ScaleX = (newRB.X - newLT.X) / imageSize.Width,
                        ScaleY = (newRB.Y - newLT.Y) / imageSize.Height
                    };
                }
                QueueRenderTask();
            }

            return availableSize;
        }

        private void EnqueueTask(long id/*Func<RenderTaskState, RenderResult> task*/)
        {
            if (runningTasks.Count < maxTasks)
            {
                Size screenSize = new Size(Math.Abs(LeftFromX(ActualPlotRect.XMax) - LeftFromX(ActualPlotRect.XMin)), Math.Abs(TopFromY(ActualPlotRect.YMax) - TopFromY(ActualPlotRect.YMin)));
                RenderTaskState state = new RenderTaskState(ActualPlotRect, screenSize);
                state.Id = id;
                state.Bounds = ComputeBounds();
                runningTasks.Add(state);

                if (!DesignerProperties.GetIsInDesignMode(this))
                {
                    ThreadPool.QueueUserWorkItem(s =>
                    {
                        var rr = RenderFrame((RenderTaskState)s);
                        Dispatcher.BeginInvoke(new RenderFunc(OnTaskCompleted), rr, s);

                    }, state);
                }
                else
                {
                    var rr = RenderFrame(state);
                    OnTaskCompleted(rr, state);
                }
            }
            else
                tasks.Enqueue(id);
        }

        /// <summary>
        /// Renders frame and returns it as a render result.
        /// </summary>
        /// <param name="state">Render task state for rendering frame.</param>
        /// <returns>Render result of rendered frame.</returns>
        protected virtual RenderResult RenderFrame(RenderTaskState state)
        {
            //if (!state.IsCancelled)
            //    return new RenderResult(HeatMap.BuildHeatMap(state.Transform.ScreenRect, new DataRect(state.Transform.ViewportRect), DataSource.X, DataSource.Y, DataSource.Data, 0));
            //else
            return null;
        }

        /// <summary>Creates new render task and puts it to queue.</summary>
        /// <returns>Async operation ID.</returns>
        protected long QueueRenderTask()
        {
            long id = nextID++;
            EnqueueTask(id);
            return id;
        }

        private void OnTaskCompleted(RenderResult r, RenderTaskState state)
        {
            if (r != null && !state.IsCanceled)
            {

                WriteableBitmap wr = new WriteableBitmap((int)r.Output.Width, (int)r.Output.Height, 96, 96, PixelFormats.Bgra32, null);
                // Calculate the number of bytes per pixel. 
                int bytesPerPixel = (wr.Format.BitsPerPixel + 7) / 8;
                // Stride is bytes per pixel times the number of pixels.
                // Stride is the byte width of a single rectangle row.
                int stride = wr.PixelWidth * bytesPerPixel;
                wr.WritePixels(new Int32Rect(0, 0, wr.PixelWidth, wr.PixelHeight), r.Image, stride, 0);


                outputImage.Source = wr;
                Canvas.SetLeft(outputImage, r.Output.Left);
                Canvas.SetTop(outputImage, r.Output.Top);
                imageCartesianRect = r.Visible;
                imageSize = new Size(r.Output.Width, r.Output.Height);
                outputImage.RenderTransform = null;
            }

            RaiseTaskCompletion(state.Id);

            runningTasks.Remove(state);

            while (tasks.Count > 1)
            {
                long id = tasks.Dequeue();
                RaiseTaskCompletion(id);
            }
            if (tasks.Count > 0 && runningTasks.Count < maxTasks)
            {
                EnqueueTask(tasks.Dequeue());
            }

            InvalidateMeasure();
        }

        /// <summary>
        /// Cancel all tasks, which are in quere.
        /// </summary>
        public void CancelAll()
        {
            foreach (var s in runningTasks)
                s.Stop();
        }

        private Subject<RenderCompletion> renderCompletion = new Subject<RenderCompletion>();

        /// <summary>
        /// Gets event which is occured when render task is finished
        /// </summary>
        public IObservable<RenderCompletion> RenderCompletion
        {
            get { return renderCompletion; }
        }

        /// <summary>
        /// Raises RenderCompletion event when task with the specified id is finished
        /// </summary>
        /// <param name="id">ID of finished task</param>
        protected void RaiseTaskCompletion(long id)
        {
            renderCompletion.OnNext(new RenderCompletion { TaskId = id });
        }
    }

    /// <summary>
    /// Represents contents of render result.
    /// </summary>
    public class RenderResult
    {
        private DataRect visible;
        private Rect output;
        private int[] image;

        /// <summary>
        /// Initializes new instance of RenderResult class form given parameters.
        /// </summary>
        /// <param name="image">Array of image pixels.</param>
        /// <param name="visible">Visible rect for graph.</param>
        /// <param name="offset">Image start offset.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">image height.</param>
        public RenderResult(int[] image, DataRect visible, Point offset, double width, double height)
        {
            this.visible = visible;
            this.output = new Rect(offset, new Size(width, height));
            this.image = image;
        }

        /// <summary>
        /// Gets the current visible rect.
        /// </summary>
        public DataRect Visible
        {
            get { return visible; }
        }

        /// <summary>
        /// Gets an array of image pixels.
        /// </summary>
        public int[] Image
        {
            get { return image; }
        }

        /// <summary>
        /// Gets the image output rect.
        /// </summary>
        public Rect Output
        {
            get { return output; }
        }
    }

    /// <summary>This class holds all information about rendering request.</summary>
    public class RenderTaskState
    {
        bool isCanceled = false;

        /// <summary>
        /// Initializes new instance of RenderTaskState class from given coordinate tranform.
        /// </summary>
        public RenderTaskState(DataRect actualPlotRect, Size screenSize)
        {
            ScreenSize = screenSize;
            ActualPlotRect = actualPlotRect;
        }

        /// <summary>
        /// Gets or sets the state Id.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets the screen size of the output image
        /// </summary>
        public Size ScreenSize { get; private set; }

        /// <summary>
        /// Gets plot rectangle of the visible area
        /// </summary>
        public DataRect ActualPlotRect { get; private set; }

        /// <summary>
        /// Gets or sets the current bounds.
        /// </summary>
        public DataRect Bounds { get; set; }

        /// <summary>
        /// Gets a value indicating whether the task is cancelled or not.
        /// </summary>
        public bool IsCanceled
        {
            get { return isCanceled; }
        }

        /// <summary>
        /// Sets state as canceled.
        /// </summary>
        public void Stop()
        {
            isCanceled = true;
        }
    }

    ///<summary>
    ///Contains reference to completed tasks.
    ///</summary>
    public class RenderCompletion
    {
        ///<summary>
        ///Gets or sets the Id of render task.
        ///</summary>
        public long TaskId { get; set; }
    }

}

