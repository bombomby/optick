// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Markup;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Subjects;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>Displays supplied data series as a collection of markers. Coordinates of marker and its visual properties
    /// are defined by different data series (sometimes called variables). Example is scatter plot where two data series 
    /// X and Y define cartesian coordinates of points on the screen.
    /// <para>Marker graph appearance is defined by three properties: <see cref="MarkerTemplate"/>, 
    /// <see cref="LegendTemplate"/> and <see cref="TooltipTemplate"/>. Data series are specified using <see cref="Sources"/>
    /// property.</para></summary>
    [ContentProperty("Sources")]
    [Description("Plots scattered points")]
    public partial class MarkerGraph : PlotBase
    {
        private List<DynamicMarkerViewModel> models = new List<DynamicMarkerViewModel>();
        private Type modelType;
        private List<Batch> batches = new List<Batch>();
        /// <summary>Dispatchers invocation of <see cref="IdleDraw"/> method in background (e.g. all operations
        /// with higher priority are complete.</summary>
        private DispatcherTimer idleTask = new DispatcherTimer();
        private long plotVersion = 0; // Each visible change will increase plot version
        private volatile bool isDrawing = false;
        private long currentTaskId;
        private long nextTaskId = 0;
        private int markersDrawn = 0; // Number of drawn markers
        /// <summary>ID of UI thread. Typically used to determine if BeginInvoke is required or not.</summary>
        protected int UIThreadID;
        /// <summary>
        /// Initializes a new instance of the <see cref="MarkerGraph"/> class.
        /// </summary>
        public MarkerGraph()
        {
            var rd = new ResourceDictionary();
            rd = (ResourceDictionary)Application.LoadComponent(new System.Uri("/InteractiveDataDisplay.WPF;component/Plots/Markers/MarkerTemplates.xaml", System.UriKind.Relative));
            Resources.MergedDictionaries.Add(rd);
            UIThreadID = Thread.CurrentThread.ManagedThreadId;

            LegendTemplate = Resources["DefaultLegendTemplate"] as DataTemplate;
            TooltipTemplate = Resources["DefaultTooltipTemplate"] as DataTemplate;

            idleTask.Interval = new TimeSpan(0);
            idleTask.Tick += new EventHandler(IdleDraw);

            Sources = new DataCollection();
        }
        #region MaxSnapshotSize
        /// <summary>Gets or sets maximum bitmap size in pixels used as placeholder when navigating over marker graph.
        /// Larger values result in crisper image when moving and scaling, 
        /// but higher memory consumption on marker graphs with a lot of batches. 
        /// <para>Default value is 1920 x 1080.</para></summary>
        [Category("InteractiveDataDisplay")]
        public Size MaxSnapshotSize
        {
            get { return (Size)GetValue(MaxSnapshotSizeProperty); }
            set { SetValue(MaxSnapshotSizeProperty, value); }
        }
        /// <summary>Identifies <see cref="MaxSnapshotSize"/> dependency property</summary>
        public static readonly DependencyProperty MaxSnapshotSizeProperty =
            DependencyProperty.Register("MaxSnapshotSize", typeof(Size), typeof(MarkerGraph), new PropertyMetadata(new Size(1920, 1080)));
        #endregion
        #region Templates
        #region Default Templates
        /// <summary>
        /// Gets default template for legend.
        /// </summary>
        public DataTemplate DefaultLegendTemplate
        {
            get { return Resources["DefaultLegendTemplate"] as DataTemplate; }
        }
        /// <summary>
        /// Gets default template for tooltip.
        /// </summary>
        public DataTemplate DefaultTooltipTemplate
        {
            get { return Resources["DefaultTooltipTemplate"] as DataTemplate; }
        }
        #endregion
        #region Circle
        /// <summary>
        /// Gets template for circle markers.
        /// </summary>
        public DataTemplate Circle
        {
            get { return Resources["Circle"] as DataTemplate; }
        }
        /// <summary>
        /// Gets simple legend template for circle markers.
        /// </summary>
        public DataTemplate CircleLegend
        {
            get { return Resources["CircleLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for circle markers with information about color.
        /// </summary>
        public DataTemplate CircleColorLegend
        {
            get { return Resources["CircleColorLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for circle markers with information about size.
        /// </summary>
        public DataTemplate CircleSizeLegend
        {
            get { return Resources["CircleSizeLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for circle markers with information about color and size.
        /// </summary>
        public DataTemplate CircleColorSizeLegend
        {
            get { return Resources["CircleColorSizeLegend"] as DataTemplate; }
        }
        #endregion
        #region Box
        /// <summary>
        /// Gets template for box markers.
        /// </summary>
        public DataTemplate Box
        {
            get { return Resources["Box"] as DataTemplate; }
        }
        /// <summary>
        /// Gets simple legend template for box markers.
        /// </summary>
        public DataTemplate BoxLegend
        {
            get { return Resources["BoxLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for box markers with information about color.
        /// </summary>
        public DataTemplate BoxColorLegend
        {
            get { return Resources["BoxColorLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for box markers with information about size.
        /// </summary>
        public DataTemplate BoxSizeLegend
        {
            get { return Resources["BoxSizeLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for box markers with information about color and size.
        /// </summary>
        public DataTemplate BoxColorSizeLegend
        {
            get { return Resources["BoxColorSizeLegend"] as DataTemplate; }
        }
        #endregion
        #region Diamond
        /// <summary>
        /// Gets template for diamond markers.
        /// </summary>
        public DataTemplate Diamond
        {
            get { return Resources["Diamond"] as DataTemplate; }
        }
        /// <summary>
        /// Gets simple legend template for diamond markers.
        /// </summary>
        public DataTemplate DiamondLegend
        {
            get { return Resources["DiamondLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for diamond markers with information about color.
        /// </summary>
        public DataTemplate DiamondColorLegend
        {
            get { return Resources["DiamondColorLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for diamond markers with information about size.
        /// </summary>
        public DataTemplate DiamondSizeLegend
        {
            get { return Resources["DiamondSizeLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for diamond markers with information about color and size.
        /// </summary>
        public DataTemplate DiamondColorSizeLegend
        {
            get { return Resources["DiamondColorSizeLegend"] as DataTemplate; }
        }
        #endregion
        #region Triangle
        /// <summary>
        /// Gets template for triangle markers.
        /// </summary>
        public DataTemplate Triangle
        {
            get { return Resources["Triangle"] as DataTemplate; }
        }
        /// <summary>
        /// Gets simple legend template for triangle markers.
        /// </summary>
        public DataTemplate TriangleLegend
        {
            get { return Resources["TriangleLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for triangle markers with information about color.
        /// </summary>
        public DataTemplate TriangleColorLegend
        {
            get { return Resources["TriangleColorLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for triangle markers with information about size.
        /// </summary>
        public DataTemplate TriangleSizeLegend
        {
            get { return Resources["TriangleSizeLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for triangle markers with information about color and size.
        /// </summary>
        public DataTemplate TriangleColorSizeLegend
        {
            get { return Resources["TriangleColorSizeLegend"] as DataTemplate; }
        }
        #endregion
        #region Cross
        /// <summary>
        /// Gets template for cross markers.
        /// </summary>
        public DataTemplate Cross
        {
            get { return Resources["Cross"] as DataTemplate; }
        }
        /// <summary>
        /// Gets simple legend template for cross markers.
        /// </summary>
        public DataTemplate CrossLegend
        {
            get { return Resources["CrossLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for cross markers with information about color.
        /// </summary>
        public DataTemplate CrossColorLegend
        {
            get { return Resources["CrossColorLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for cross markers with information about size.
        /// </summary>
        public DataTemplate CrossSizeLegend
        {
            get { return Resources["CrossSizeLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for cross markers with information about color and size.
        /// </summary>
        public DataTemplate CrossColorSizeLegend
        {
            get { return Resources["CrossColorSizeLegend"] as DataTemplate; }
        }
        #endregion
        #region ErrorBar
        /// <summary>
        /// Gets template for error bar markers.
        /// </summary>
        public DataTemplate ErrorBar
        {
            get { return Resources["ErrorBar"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for error bar markers.
        /// </summary>
        public DataTemplate ErrorBarLegend
        {
            get { return Resources["ErrorBarLegend"] as DataTemplate; }
        }
        #endregion
        #region VerticalInterval
        /// <summary>
        /// Gets template for VerticalInterval markers.
        /// </summary>
        public DataTemplate VerticalIntervalBar
        {
            get { return Resources["VerticalInterval"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for VerticalInterval markers.
        /// </summary>
        public DataTemplate VerticalIntervalLegend
        {
            get { return Resources["VerticalIntervalLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets tooltip template for VerticalInterval markers.
        /// </summary>
        public DataTemplate VerticalIntervalTooltip
        {
            get { return Resources["VerticalIntervalTooltip"] as DataTemplate; }
        }
        #endregion
        #region BarGraph
        /// <summary>
        /// Gets template for bar graph.
        /// </summary>
        public DataTemplate BarGraph
        {
            get { return Resources["BarGraph"] as DataTemplate; }
        }
        /// <summary>
        /// Gets legend template for bar graph.
        /// </summary>
        public DataTemplate BarGraphLegend
        {
            get { return Resources["BarGraphLegend"] as DataTemplate; }
        }
        /// <summary>
        /// Gets tooltip template for bar graph.
        /// </summary>
        public DataTemplate BarGraphTooltip
        {
            get { return Resources["BarGraphTooltip"] as DataTemplate; }
        }
        #endregion
        #region Tooltip Templates
        /// <summary>
        /// Gets tooltip template with information about color.
        /// </summary>
        public DataTemplate ColorTooltip
        {
            get { return Resources["ColorTooltip"] as DataTemplate; }
        }
        /// <summary>
        /// Gets tooltip template with information about size.
        /// </summary>
        public DataTemplate SizeTooltip
        {
            get { return Resources["SizeTooltip"] as DataTemplate; }
        }
        /// <summary>
        /// Gets tooltip template with information about color and size.
        /// </summary>
        public DataTemplate ColorSizeTooltip
        {
            get { return Resources["ColorSizeTooltip"] as DataTemplate; }
        }
        #endregion
        #endregion
        #region Sources
        /// <summary>
        /// Gets or sets the collection of <see cref="DataSeries"/> describing appearance and behavior of markers.
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("Appearance")]
        [Description("Collection of data series")]
        public DataCollection Sources
        {
            get { return (DataCollection)GetValue(SourcesProperty); }
            set { SetValue(SourcesProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Sources"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourcesProperty =
            DependencyProperty.Register("Sources", typeof(DataCollection), typeof(MarkerGraph), new PropertyMetadata(null, OnSourcesChanged));

        private static void OnSourcesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MarkerGraph me = (MarkerGraph)d;
            DataCollection col = (DataCollection)e.NewValue;
            if (e.OldValue != null)
            {
                ((DataCollection)e.OldValue).CollectionChanged -= me.OnSourceCollectionChanged;
                ((DataCollection)e.OldValue).DataSeriesUpdated -= me.OnSourceDataUpdated;
            }
            col.CollectionChanged += me.OnSourceCollectionChanged;
            col.DataSeriesUpdated += me.OnSourceDataUpdated;

            // Signal about total change of collection
            me.OnSourceCollectionChanged(me, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void OnSourceDataUpdated(object s, DataSeriesUpdatedEventArgs e)
        {
            StartRenderTask(true);
            foreach (var b in batches)
                b.AddChangedProperties(new string[] { e.Key });
            InvalidateContentBounds(); // This will cause measure and arrange pass
        }

        private void OnSourceCollectionChanged(object s, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (DataSeries ds in e.NewItems)
                {
                    ds.Owner = this;
                    //var pc = ds.Converter as IPlotValueConverter;
                    //if (pc != null)
                    //    pc.Plot = masterField;
                }
            }

            modelType = null;
            StartRenderTask(true);
            InvalidateContentBounds(); // This will cause measure and arrange pass
        }

        #endregion
        #region Sample marker model

        /// <summary>
        /// Identifies the <see cref="SampleMarkerModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SampleMarkerModelProperty =
            DependencyProperty.Register("SampleMarkerModel",
                typeof(DynamicMarkerViewModel),
                typeof(MarkerGraph),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the sample marker shown in default legend.
        /// <para>Default value is null.</para></summary>
        [Browsable(false)]
        public DynamicMarkerViewModel SampleMarkerModel
        {
            get
            {
                return (DynamicMarkerViewModel)GetValue(SampleMarkerModelProperty);
            }
            set
            {
                SetValue(SampleMarkerModelProperty, value);
            }
        }
        #endregion
        #region MarkersBatchSize

        /// <summary>
        /// Identifies the <see cref="MarkersBatchSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkersBatchSizeProperty =
            DependencyProperty.Register("MarkersBatchSize",
                typeof(int),
                typeof(MarkerGraph),
                new PropertyMetadata(500, OnMarkersBatchSizePropertyChanged));

        private static void OnMarkersBatchSizePropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MarkerGraph m = sender as MarkerGraph;
            m.Children.Clear();
            m.batches.Clear();
            m.StartRenderTask(false);
            m.InvalidateBounds(); // This will cause new measure cycle
        }

        /// <summary>
        /// Gets or sets the number of markers to draw as a single batch operation. Large values for 
        /// complex marker templates may decrease application responsiveness. Small values may result in
        /// longer times before marker graph fully updates.
        /// <para>Default value is 500.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public int MarkersBatchSize
        {
            get
            {
                return (int)GetValue(MarkersBatchSizeProperty);
            }
            set
            {
                SetValue(MarkersBatchSizeProperty, value);
            }
        }


        #endregion
        #region Stroke

        /// <summary>
        /// Identifies the <see cref="MarkersBatchSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke",
                typeof(SolidColorBrush),
                typeof(MarkerGraph),
                new PropertyMetadata(new SolidColorBrush(Colors.Black), OnStrokePropertyChanged));

        private static void OnStrokePropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MarkerGraph m = sender as MarkerGraph;
            m.StartRenderTask(false);
            foreach (var b in m.batches)
                b.AddChangedProperties(new string[] { "Stroke" });
            m.InvalidateMeasure();
        }

        /// <summary>
        /// Gets or sets the stroke for markers.
        /// <para>Default value is black color.</para>
        /// </summary>
        [Category("Appearance")]
        public SolidColorBrush Stroke
        {
            get { return (SolidColorBrush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }


        #endregion
        #region StrokeThickness

        /// <summary>
        /// Identifies the <see cref="MarkersBatchSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness",
                typeof(double),
                typeof(MarkerGraph),
                new PropertyMetadata(1.0, OnStrokeThicknessPropertyChanged));

        private static void OnStrokeThicknessPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MarkerGraph m = sender as MarkerGraph;
            m.StartRenderTask(false);
            foreach (var b in m.batches)
                b.AddChangedProperties(new string[] { "StrokeThickness" });
            m.InvalidateMeasure();
        }

        /// <summary>
        /// Gets or sets stroke thickness of markers.
        /// <para>Default value is 1.</para>
        /// </summary>
        [Category("Appearance")]
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }


        #endregion
        /// <summary>Ensures that models array is up to date according to current <see cref="Sources"/> collection</summary>
        private void SyncMarkerViewModels()
        {
            int markerCount = (Sources != null && Sources.IsValid) ? (Sources.MarkerCount) : (0);
            if (markerCount == 0)
            {
                if (models.Count > 0)
                    models.Clear();
                if (SampleMarkerModel != null)
                    SampleMarkerModel = null;
            }
            else
            {
                if (modelType == null)
                {
                    modelType = DynamicTypeGenerator.GenerateMarkerViewModelType(Sources);
                    models.Clear();
                }
                for (int i = models.Count; i < markerCount; i++)
                    models.Add(Activator.CreateInstance(modelType, i, Sources, true) as DynamicMarkerViewModel);
                if (models.Count > markerCount)
                    models.RemoveRange(markerCount, models.Count - markerCount);
                if (SampleMarkerModel != models[0])
                    SampleMarkerModel = models[0];
            }
        }
        /// <summary>
        /// Returns a converter used to transform specified marker graph to its data bounds.
        /// </summary>
        /// <param name="markerTemplate">A marker graph's template to search converter.</param>
        /// <returns></returns>
        protected static IValueConverter GetDataBoundsConverter(DataTemplate markerTemplate)
        {
            if (markerTemplate == null)
                return new DefaultDataBoundsConverter();

            var marker = (FrameworkElement)markerTemplate.LoadContent();

            IValueConverter conv = null;
            if (marker.Resources.Contains("DataBoundsFunc"))
                conv = marker.Resources["DataBoundsFunc"] as IValueConverter;
            if (conv == null)
                conv = new DefaultDataBoundsConverter();
            return conv;
        }
        /// <summary>
        /// Returns a converter used to transform specified marker graph to its screen thickness.
        /// </summary>
        /// <param name="markerTemplate">A marker graph's template to search converter.</param>
        /// <returns></returns>
        protected static IValueConverter GetScreenThicknessConverter(DataTemplate markerTemplate)
        {
            if (markerTemplate == null)
                return new DefaultScreenThicknessConverter();
            var marker = (FrameworkElement)markerTemplate.LoadContent();
            IValueConverter conv = null;
            if (marker.Resources.Contains("ScreenBoundsFunc"))
                conv = marker.Resources["ScreenBoundsFunc"] as IValueConverter;
            if (conv == null)
                conv = new DefaultScreenThicknessConverter();
            return conv;
        }
        /// <summary>This method is invoked when application is in idle state</summary>
        void IdleDraw(object sender, EventArgs e)
        {
            bool finished = true;
            SyncMarkerViewModels();
            // Remove extra batches. Such removals take a lot of time, so we do it at idle handlers
            if (batches.Count > Math.Ceiling(models.Count / (double)MarkersBatchSize))
            {
                var b = batches[batches.Count - 1];
                Children.Remove(b.Panel);
                Children.Remove(b.Image);
                batches.RemoveAt(batches.Count - 1);
                finished = false;
            }
            // Find first batch that is ready for Image updating
            var batch = batches.FirstOrDefault(b => b.PanelVersion == plotVersion &&
                b.Panel.Visibility == System.Windows.Visibility.Visible &&
                b.IsLayoutUpdated &&
                b.ImageVersion != plotVersion);
            if (batch != null)
            {
                batch.PlotRect = ActualPlotRect;
                var panelSize = new Size(Math.Max(1, batch.Panel.RenderSize.Width), Math.Max(1, batch.Panel.RenderSize.Height));
                var renderSize = new Size(
                    Math.Min(MaxSnapshotSize.Width, panelSize.Width),
                    Math.Min(MaxSnapshotSize.Height, panelSize.Height));
                if (batch.Content == null || batch.Content.PixelWidth != (int)Math.Ceiling(renderSize.Width) || batch.Content.PixelHeight != (int)Math.Ceiling(renderSize.Height))
                    batch.Content = new RenderTargetBitmap((int)renderSize.Width, (int)renderSize.Height, 96, 96, PixelFormats.Pbgra32);
                else
                    batch.Content.Clear();

                ScaleTransform transform = new ScaleTransform
                {
                    ScaleX = renderSize.Width < panelSize.Width ? renderSize.Width / panelSize.Width : 1.0,
                    ScaleY = renderSize.Height < panelSize.Height ? renderSize.Height / panelSize.Height : 1.0
                };
                var panel = batch.Panel;
                panel.RenderTransform = transform;
                batch.Content = new RenderTargetBitmap((int)renderSize.Width, (int)renderSize.Height, 96, 96, PixelFormats.Pbgra32);
                batch.Content.Render(panel);
                batch.ImageVersion = plotVersion;
                finished = false;
            }
            // Find first batch that should be rendered
            batch = batches.FirstOrDefault(b => b.PanelVersion != plotVersion);
            if (batch != null)
            {
                int idx = batches.IndexOf(batch);
                if (!batch.Panel.IsMaster && idx * MarkersBatchSize < models.Count)
                {
                    if (MarkerTemplate == null)
                    {
                        batch.Panel.Children.Clear();
                    }
                    else
                    {
                        int batchSize = Math.Min(MarkersBatchSize, models.Count - idx * MarkersBatchSize);
                        while (batch.Panel.Children.Count > batchSize)
                            batch.Panel.Children.RemoveAt(batch.Panel.Children.Count - 1);
                        for (int i = 0; i < batch.Panel.Children.Count; i++)
                        {
                            var mvm = models[i + idx * MarkersBatchSize];
                            var fe = batch.Panel.Children[i] as FrameworkElement;
                            if (fe.DataContext != mvm)
                                fe.DataContext = mvm;
                            else
                                mvm.Notify(batch.ChangedProperties);
                        }
                        for (int i = batch.Panel.Children.Count; i < batchSize; i++)
                        {
                            var fe = MarkerTemplate.LoadContent() as FrameworkElement;
                            fe.DataContext = models[i + idx * MarkersBatchSize];
                            if (TooltipTemplate != null)
                            {
                                var tc = new ContentControl
                                {
                                    Content = fe.DataContext,
                                    ContentTemplate = TooltipTemplate
                                };
                                ToolTipService.SetToolTip(fe, tc);
                            }
                            batch.Panel.Children.Add(fe);
                        }
                        markersDrawn += batchSize;
                    }
                    batch.IsLayoutUpdated = false;
                    batch.Image.Visibility = System.Windows.Visibility.Collapsed;
                    batch.Image.RenderTransform = null;
                    batch.Panel.Visibility = System.Windows.Visibility.Visible;
                    batch.Panel.InvalidateMeasure();
                    batch.PanelVersion = plotVersion;
                    batch.ClearChangedProperties();
                }
                finished = false;
            }
            if (finished)
            {
                idleTask.Stop();
                isDrawing = false;
                if (currentTaskId != -1)
                {
                    renderCompletion.OnNext(new MarkerGraphRenderCompletion(currentTaskId, markersDrawn));
                    currentTaskId = -1;
                }
            }
        }
        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is thread safe.
        /// </summary>
        /// <param name="list">List of new values for data series. Elements from <paramref name="list"/> are
        /// assigned to data series in the order in which data series were defined in XAML or were added
        /// to <see cref="Sources"/> collection. </param>
        /// <returns>ID of rendering task. You can subscribe to <see cref="RenderCompletion"/> to get notified when 
        /// marker graph is completely updated.</returns>
        /// <example>
        /// Next code will set two arrays as values of two first data series:
        /// <code>
        /// markerGraph.Plot(new int[] { 1,2,3 }, new double[] { -10.0, 0, 20.0 });
        /// </code>
        /// </example>
        public long Plot(params object[] list)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            long taskId = nextTaskId++;
            if (Thread.CurrentThread.ManagedThreadId == UIThreadID)
            {
                PlotSync(taskId, list);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PlotSync(taskId, list);
                }));
            }
            return taskId;
        }
        /// <summary>
        /// This method is similar to <see cref="Plot"/> method but can be called only from main thread.
        /// </summary>
        /// <param name="taskId">ID of rendering task.</param>
        /// <param name="list">List of new values for data series. Elements from <paramref name="list"/> are
        /// assigned to data series in the order in which data series were defined in XAML or were added
        /// to <see cref="Sources"/> collection.</param>
        protected void PlotSync(long taskId, params object[] list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            if (currentTaskId != -1)
                renderCompletion.OnNext(new MarkerGraphRenderCompletion(currentTaskId, markersDrawn));
            for (int i = 0; i < list.Length; i++)
            {
                if (Sources[i].Data == list[i])
                    Sources[i].Update();
                else
                    Sources[i].Data = list[i];
                Sources[i].Owner = this;
            }
            currentTaskId = taskId;
        }
        #region Render completion notifications
        private Subject<RenderCompletion> renderCompletion = new Subject<RenderCompletion>();
        /// <summary>
        /// Gets the source of notification about render completion.
        /// </summary>
        public IObservable<RenderCompletion> RenderCompletion
        {
            get
            {
                return renderCompletion;
            }
        }
        /// <summary>
        /// Gets the state indicating whether rendering is completed or not.
        /// </summary>
        public bool IsBusy
        {
            get
            {
                lock (this)
                {
                    return isDrawing;
                }
            }
        }
        private void StartRenderTask(bool completeCurrent)
        {
            if (completeCurrent && currentTaskId != -1)
            {
                renderCompletion.OnNext(new MarkerGraphRenderCompletion(currentTaskId, markersDrawn));
                currentTaskId = -1;
            }
            isDrawing = true;
            markersDrawn = 0;
            plotVersion++;
            idleTask.Start();
        }
        #endregion
        private bool isPaddingValid = false;
        private Thickness localPadding;
        private bool areContentBoundsValid = false;
        private DataRect localPlotBounds;
        /// <summary>
        /// Invalidates effective plot coordinate ranges. 
        /// This usually schedules recalculation of plot layout.
        /// </summary>
        public override void InvalidateBounds()
        {
            areContentBoundsValid = false;
            base.InvalidateBounds();
        }
        private void InvalidateContentBounds()
        {
            isPaddingValid = false;
            InvalidateBounds();
        }
        private void UpdateLocalPadding()
        {
            if (!isPaddingValid)
            {
                IValueConverter screenThicknessConverter = GetScreenThicknessConverter(MarkerTemplate);
                Thickness finalThickness = new Thickness();
                SyncMarkerViewModels();
                if (!(screenThicknessConverter is DefaultScreenThicknessConverter) || Sources.ContainsSeries("D"))
                    for (int i = 0; i < models.Count; i++)
                    {
                        Thickness thickness = (Thickness)screenThicknessConverter.Convert(models[i], typeof(Thickness), null, CultureInfo.InvariantCulture);
                        finalThickness = new Thickness(
                            Math.Max(finalThickness.Left, thickness.Left),
                            Math.Max(finalThickness.Top, thickness.Top),
                            Math.Max(finalThickness.Right, thickness.Right),
                            Math.Max(finalThickness.Bottom, thickness.Bottom)
                        );
                    }
                localPadding = finalThickness;
                isPaddingValid = true;
            }
        }
        /// <summary>
        /// Gets the range of {x, y} plot coordinates that corresponds to all the markers shown on the plot.
        /// </summary>
        /// <returns></returns>
        protected override DataRect ComputeBounds()
        {
            if (!areContentBoundsValid)
            {
                IValueConverter boundsConverter = GetDataBoundsConverter(MarkerTemplate);
                SyncMarkerViewModels();
                DataRect[] rects = new DataRect[models.Count];
                for (int i = 0; i < models.Count; i++)
                {
                    rects[i] = (DataRect)boundsConverter.Convert(models[i], typeof(DataRect), null, CultureInfo.InvariantCulture);
                }
                DataRect union = rects.Length > 0 ? rects[0] : new DataRect(-0.5, -0.5, 0.5, 0.5);
                for (int i = 1; i < rects.Length; i++)
                    union.Surround(rects[i]);
                if (union.XMax == union.XMin)
                    union = new DataRect(union.XMin - 0.5, union.YMin, union.XMin - 0.5 + union.Width + 1.0, union.YMin + union.Height);
                if (union.YMax == union.YMin)
                    union = new DataRect(union.XMin, union.YMin - 0.5, union.XMin + union.Width, union.YMin - 0.5 + union.Height + 1.0);
                localPlotBounds = union;
                areContentBoundsValid = true;
            }
            return localPlotBounds;
        }
        #region Marker Template
        /// <summary>
        /// Gets or sets the appearance of displaying markers.
        /// Predefined static resources for marker template are: 
        /// Box, Circle, Diamond, Triangle, Cross, ErrorBar, BarGraph and VerticalInterval.
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public DataTemplate MarkerTemplate
        {
            get { return (DataTemplate)GetValue(MarkerTemplateProperty); }
            set { SetValue(MarkerTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MarkerTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkerTemplateProperty =
            DependencyProperty.Register("MarkerTemplate", typeof(DataTemplate),
                typeof(MarkerGraph), new PropertyMetadata(null, OnMarkerTemplatePropertyChanged));

        private static void OnMarkerTemplatePropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MarkerGraph m = sender as MarkerGraph;
            foreach (var b in m.batches)
            {
                b.Panel.Children.Clear();
            }
            m.StartRenderTask(false);
            m.InvalidateBounds();
        }

        #endregion
        #region Legend Template
        /// <summary>
        /// Gets or sets the appearance of displaying legend.
        /// Predefined static resources for legend template for each type of marker templates are:
        /// DefaultLegendTemplate, simple Legend, ColorLegend, SizeLegend and ColorSizeLegend.
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public DataTemplate LegendTemplate
        {
            get { return (DataTemplate)GetValue(LegendTemplateProperty); }
            set { SetValue(LegendTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LegendTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LegendTemplateProperty =
            DependencyProperty.Register("LegendTemplate", typeof(DataTemplate),
                typeof(MarkerGraph), new PropertyMetadata(null));
        #endregion
        #region Tooltip Template
        /// <summary>
        /// Gets or sets the appearance of displaying tooltip.
        /// Predefined static resources for tooltip template are:
        /// DefaultTooltipTemplate, ColorTooltip, SizeTooltip and ColorSizeTooltip.
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public DataTemplate TooltipTemplate
        {
            get { return (DataTemplate)GetValue(TooltipTemplateProperty); }
            set { SetValue(TooltipTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TooltipTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TooltipTemplateProperty =
            DependencyProperty.Register("TooltipTemplate", typeof(DataTemplate),
                typeof(MarkerGraph), new PropertyMetadata(null, OnTooltipTemplatePropertyChanged));

        private static void OnTooltipTemplatePropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MarkerGraph m = sender as MarkerGraph;
            foreach (var b in m.batches)
            {
                b.Panel.Children.Clear();
            }
            m.StartRenderTask(false);
            m.InvalidateBounds();
        }

        #endregion
        /// <summary>
        /// Gets padding of marker graph.
        /// </summary>
        /// <returns></returns>
        protected override Thickness ComputePadding()
        {
            UpdateLocalPadding();
            return new Thickness(
                Math.Max(Padding.Left, localPadding.Left),
                Math.Max(Padding.Top, localPadding.Top),
                Math.Max(Padding.Right, localPadding.Right),
                Math.Max(Padding.Bottom, localPadding.Bottom));
        }
        private double prevScaleX = Double.NaN, prevScaleY = Double.NaN,
            prevOffsetX = Double.NaN, prevOffsetY = Double.NaN;
        /// <summary>
        /// Measures the size in layout required for child elements and determines a size for a <see cref="Figure"/>. 
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            // Update number of batches
            SyncMarkerViewModels();
            int batchCount = (int)Math.Ceiling(models.Count / (double)MarkersBatchSize);
            while (batches.Count < batchCount)
            {
                var b = new Batch();
                batches.Add(b);
                Children.Add(b.Image);
                Children.Add(b.Panel);
                // Ensure inner plots have the same data transforms
                b.Panel.SetBinding(PlotBase.XDataTransformProperty,
                    new Binding("XDataTransform")
                    {
                        Source = this
                    });
                b.Panel.SetBinding(PlotBase.YDataTransformProperty,
                    new Binding("YDataTransform")
                    {
                        Source = this
                    });
            }
            availableSize = PerformAsMaster(availableSize);
            // Request redraw of all batches if transform is changed
            if (ScaleX != prevScaleX || ScaleY != prevScaleY || OffsetX != prevOffsetX || OffsetY != prevOffsetY)
            {
                prevScaleX = ScaleX;
                prevScaleY = ScaleY;
                prevOffsetX = OffsetX;
                prevOffsetY = OffsetY;
                idleTask.Start();
                plotVersion++;
                foreach (var b in batches)
                {
                    b.ChangedProperties = null; // Update all properties 
                    b.Panel.Visibility = System.Windows.Visibility.Collapsed;
                    b.Image.Visibility = System.Windows.Visibility.Visible;
                }
            }
            foreach (UIElement elt in Children)
            {
                Image img = elt as Image;
                if (img != null)
                {
                    Batch batch = img.Tag as Batch;
                    if (batch != null && batch.Image == elt) // Special algorithm for snapshot image
                    {
                        DataRect plotRect = batch.PlotRect;
                        var newLT = new Point(LeftFromX(plotRect.XMin), TopFromY(plotRect.YMax));
                        var newRB = new Point(LeftFromX(plotRect.XMax), TopFromY(plotRect.YMin));
                        batch.Image.Measure(new Size(newRB.X - newLT.X, newRB.Y - newLT.Y));
                    }
                    else
                        elt.Measure(availableSize);
                }
                else
                    elt.Measure(availableSize);
            }
            return availableSize;
        }
        /// <summary>
        /// Positions child elements and determines a size for a <see cref="Figure"/>.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that Figure should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            finalSize.Width = Math.Min(finalSize.Width, DesiredSize.Width);
            finalSize.Height = Math.Min(finalSize.Height, DesiredSize.Height);
            // Arranging child elements
            foreach (UIElement elt in Children)
            {
                Image img = elt as Image;
                if (img != null)
                {
                    Batch batch = img.Tag as Batch;
                    if (batch != null && batch.Image == elt) // Special algorithm for snapshot image
                    {
                        DataRect plotRect = batch.PlotRect;
                        var newLT = new Point(LeftFromX(plotRect.XMin), TopFromY(plotRect.YMax));
                        var newRB = new Point(LeftFromX(plotRect.XMax), TopFromY(plotRect.YMin));
                        batch.Image.Arrange(new Rect(newLT.X, newLT.Y, newRB.X - newLT.X, newRB.Y - newLT.Y));
                    }
                    else
                        elt.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
                }
                else
                    elt.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            }
            if (ClipToBounds)
                Clip = new RectangleGeometry
                {
                    Rect = new Rect(new Point(0, 0), finalSize)
                };
            else
                Clip = null;
            return finalSize;
        }
    }
    /// <summary>
    /// Information about completed rendering pass for <see cref="MarkerGraph"/>.
    /// </summary>
    public class MarkerGraphRenderCompletion : RenderCompletion
    {
        private int markerCount;
        /// <summary>
        /// Initializes a new instance of <see cref="MarkerGraphRenderCompletion"/> class.
        /// </summary>
        /// <param name="taskId">ID of rendering task.</param>
        /// <param name="markerCount">Number of markers actually rendered.</param>
        public MarkerGraphRenderCompletion(long taskId, int markerCount)
        {
            TaskId = taskId;
            this.markerCount = markerCount;
        }
        /// <summary>
        /// Gets the number of actually rendered markers.
        /// </summary>
        public int MarkerCount
        {
            get { return markerCount; }
        }
    }
    internal class Batch
    {
        public Batch()
        {
            PanelVersion = -1;
            ImageVersion = -1;
            Image = new Image
            {
                Stretch = Stretch.Fill,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Tag = this
            };
            Content = new RenderTargetBitmap(1, 1, 96, 96, PixelFormats.Pbgra32);
            Panel = new SingleBatchPlot();
            Panel.LayoutUpdated += (s, a) => IsLayoutUpdated = true;
            ChangedProperties = emptyArray;
        }
        public SingleBatchPlot Panel { get; set; }
        public Image Image { get; set; }
        public DataRect PlotRect { get; set; }
        public RenderTargetBitmap Content
        {
            get { return (RenderTargetBitmap)Image.Source; }
            set { Image.Source = value; }
        }
        public long PanelVersion { get; set; }
        public long ImageVersion { get; set; }
        /// <summary>List of properties that needs to be updated for this batch. Empty array means no 
        /// properties, null array means all properties</summary>
        public string[] ChangedProperties { get; set; }
        public bool IsLayoutUpdated { get; set; }
        public void AddChangedProperties(IEnumerable<string> props)
        {
            if (ChangedProperties == null)
                ChangedProperties = props.ToArray();
            else
                ChangedProperties = ChangedProperties.Concat(props.Where(p => !ChangedProperties.Contains(p))).ToArray();
        }
        private static string[] emptyArray = new string[0];
        public void ClearChangedProperties()
        {
            ChangedProperties = emptyArray;
        }
    }

    /// <summary>Internal plotter with local plot rectangle control turned off</summary>
    internal class SingleBatchPlot : Plot
    {
        public SingleBatchPlot()
        {
            ClipToBounds = false;
        }
        protected override DataRect ComputeBounds()
        {
            return DataRect.Empty;
        }
        public override void InvalidateBounds()
        {
            // Do nothing 
        }
    }
}