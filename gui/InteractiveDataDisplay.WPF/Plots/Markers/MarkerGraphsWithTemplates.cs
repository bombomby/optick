// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System.Windows;
using System.Windows.Media;
using System.ComponentModel;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// A particular case of <see cref="CartesianSizeColorMarkerGraph"/> with circle markers.
    /// </summary>
    [Description("Circle markers graph")]
    public class CircleMarkerGraph : CartesianSizeColorMarkerGraph
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CircleMarkerGraph"/> class.
        /// </summary>
        public CircleMarkerGraph()
        {
            MarkerType = new CircleMarker();
        }
    }
    /// <summary>
    /// A particular case of <see cref="CartesianSizeColorMarkerGraph"/> with box markers.
    /// </summary>
    [Description("Box markers graph")]
    public class BoxMarkerGraph : CartesianSizeColorMarkerGraph
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoxMarkerGraph"/> class.
        /// </summary>
        public BoxMarkerGraph()
        {
            MarkerType = new BoxMarker();
        }
    }
    /// <summary>
    /// A particular case of <see cref="CartesianSizeColorMarkerGraph"/> with diamond markers.
    /// </summary>
    [Description("Diamonds markers graph")]
    public class DiamondMarkerGraph : CartesianSizeColorMarkerGraph
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiamondMarkerGraph"/> class.
        /// </summary>
        public DiamondMarkerGraph()
        {
            MarkerType = new DiamondMarker();
        }
    }
    /// <summary>
    /// A particular case of <see cref="CartesianSizeColorMarkerGraph"/> with triangle markers.
    /// </summary>
    [Description("Triangles markers graph")]
    public class TriangleMarkerGraph : CartesianSizeColorMarkerGraph
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TriangleMarkerGraph"/> class.
        /// </summary>
        public TriangleMarkerGraph()
        {
            MarkerType = new TriangleMarker();
        }
    }
    /// <summary>
    /// A particular case of <see cref="CartesianSizeColorMarkerGraph"/> with cross markers.
    /// </summary>
    [Description("Cross markers graph")]
    public class CrossMarkerGraph : CartesianSizeColorMarkerGraph    
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CrossMarkerGraph"/> class.
        /// </summary>
        public CrossMarkerGraph()
        {
            MarkerType = new CrossMarker();
        }
    }
    /// <summary>
    /// Displays error bar marker graph as a particular case of <see cref="CartesianMarkerGraph"/>. 
    /// Has a set of additional data series: <see cref="ColorSeries"/> (key 'C') and <see cref="DataSeries"/> (keys 'W' and 'H') to define width 
    /// (in screen coordinates) and height (in plot coordinates) of each marker. 
    /// </summary>
    [Description("Marker graph showing verical intervals (center, error)")]
    public class ErrorBarGraph : CartesianMarkerGraph
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorBarGraph"/> class.
        /// </summary>
        public ErrorBarGraph()
        {
            Sources.Add(new DataSeries
            {
                Key = "H",
                Description = "Observation error",
            });
            Sources.Add(new DataSeries
            {
                Key = "W",
                Description = "Width",
                Data = 5
            });
            Sources.Add(new ColorSeries());
                        
            MarkerTemplate = ErrorBar;
            LegendTemplate = ErrorBarLegend;
            TooltipTemplate = DefaultTooltipTemplate;

            Description = "Observation error";
        }       
        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is thread safe.
        /// <para>This version does not need specification of X <see cref="DataSeries"/>. 
        /// Default value is a sequence of integers starting with zero.</para>
        /// </summary>
        /// <param name="y">
        /// Data for Y <see cref="DataSeries"/> defining y coordinates of the center of each bar. 
        /// <para>Can be a single value (to draw one marker or markers with the same y coordinates) 
        /// or an array or IEnumerable (to draw markers with different y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <param name="error">
        /// Data for <see cref="DataSeries"/> of observation error or the height of each bar. 
        /// <para>Can be a single value (to draw markers of one height) 
        /// or an array or IEnumerable (to draw markers of different heights) of any numeric type. 
        /// Should be defined in plot coordinates. Can be null then no markers will be drawn.</para>
        /// </param>
        /// <remarks>
        /// Note that all vector data for <see cref="DataSeries"/> should be of the same length. Otherwise no markers will be drawn.
        /// </remarks>
        /// <returns>ID of rendering task. You can subscribe to notification about rendering completion events
        /// as observer of RenderCompletion.
        /// </returns>
        public long PlotError(object y, object error)
        {
            return Plot(null, y, error);
        }
        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is thread safe.
        /// </summary>
        /// <param name="x">
        /// Data for X <see cref="DataSeries"/> defining x coordinates of the center of each bar. 
        /// <para>Can be a single value (to draw one marker or markers with the same x coordinates) 
        /// or an array or IEnumerable (to draw markers with different x coordinates) of any numeric type. 
        /// Can be null then x coordinates will be a sequence of integers starting with zero.</para>
        /// </param>
        /// <param name="y">
        /// Data for Y <see cref="DataSeries"/> defining y coordinates of the center of each bar. 
        /// <para>Can be a single value (to draw one marker or markers with the same y coordinates) 
        /// or an array or IEnumerable (to draw markers with different y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <param name="error">
        /// Data for <see cref="DataSeries"/> of observation error or the height of each bar. 
        /// <para>Can be a single value (to draw markers of one height) or an array or IEnumerable (to draw markers of different heights) of
        /// any numeric type. Should be defined in plot coordinates. Can be null then no markers will be drawn.</para>
        /// </param>
        /// <remarks>
        /// Note that all vector data for <see cref="DataSeries"/> should be of the same length. Otherwise no markers will be drawn.
        /// </remarks>
        /// <returns>ID of rendering task. You can subscribe to notification about rendering completion events
        /// as observer of RenderCompletion.
        /// </returns>
        public long PlotError(object x, object y, object error)
        {
            return Plot(x, y, error);
        }
        #region Size
        /// <summary>
        /// Identifies the <see cref="Size"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            "Size", typeof(object), typeof(ErrorBarGraph), new PropertyMetadata(5.0, OnSizeChanged));

        /// <summary>
        /// Gets or sets the data for width <see cref="DataSeries"/>.
        /// <para>Can be a single value (to draw markers of one width) or an array or IEnumerable (to draw markers of different widths) of
        /// any numeric type. Can be null then default value will be used for all markers.</para>
        /// <para>Default value is 5.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Screen width of intervals")]
        public object Size
        {
            get { return (object)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ErrorBarGraph mg = (ErrorBarGraph)d;
            mg.Sources["W"].Data = e.NewValue;
        }
        #endregion
        #region Color
        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            "Color", typeof(SolidColorBrush), typeof(ErrorBarGraph), new PropertyMetadata(new SolidColorBrush(Colors.Black), OnColorChanged));

        /// <summary>
        /// Gets or sets the data for <see cref="ColorSeries"/>.
        /// <para>Default value is black color.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Color of interval markers")]
        public SolidColorBrush Color
        {
            get { return (SolidColorBrush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ErrorBarGraph mg = (ErrorBarGraph)d;
            mg.Sources["C"].Data = e.NewValue;
        }
        #endregion
        #region Error
        /// <summary>
        /// Identifies the <see cref="Error"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ErrorProperty = DependencyProperty.Register(
            "Error", typeof(object), typeof(ErrorBarGraph), new PropertyMetadata(null, OnErrorChanged));

        /// <summary>
        /// Gets or sets the data for <see cref="DataSeries"/> of observation error or the height of each bar.
        /// <para>
        /// Can be a single value (to draw markers of one height) or an array or IEnumerable (to draw markers of different heigths) of
        /// any numeric type. Should be defined in plot coordinates. Can be null then no markers will be drawn.
        /// </para>
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Half height of interval")]
        public object Error
        {
            get { return (object)GetValue(ErrorProperty); }
            set { SetValue(ErrorProperty, value); }
        }

        private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ErrorBarGraph mg = (ErrorBarGraph)d;
            mg.Sources["H"].Data = e.NewValue;
        }
        #endregion
        #region ErrorDescription
        /// <summary>
        /// Identifies the <see cref="ErrorDescription"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ErrorDescriptionProperty = DependencyProperty.Register(
            "ErrorDescription", typeof(string), typeof(ErrorBarGraph),
            new PropertyMetadata(null, OnErrorDescriptionChanged));

        /// <summary>
        /// Gets or sets the description for <see cref="DataSeries"/> of the height of bars. 
        /// Is frequently used in legend and tooltip.
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Description of interval series")]
        public string ErrorDescription
        {
            get { return (string)GetValue(ErrorDescriptionProperty); }
            set { SetValue(ErrorDescriptionProperty, value); }
        }

        private static void OnErrorDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ErrorBarGraph mg = (ErrorBarGraph)d;
            mg.Sources["H"].Description = (string)e.NewValue;
            mg.Sources["Y"].Description = (string)e.NewValue;
        }
        #endregion
    }
    /// <summary>
    /// Displays bar chart graph as a particular case of marker graph. Has a set of data series: 
    /// X <see cref="DataSeries"/> defining x coordinates of the center of each bar,
    /// Y <see cref="DataSeries"/> defining the value for each bar,
    /// <see cref="ColorSeries"/> (key 'C') and <see cref="DataSeries"/> (key 'W') to set width of each bar. 
    /// </summary>
    [Description("Represents a bar chart")]
    public class BarGraph : MarkerGraph
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BarGraph"/> class.
        /// </summary>
        public BarGraph()
        {
            Sources.Add (new DataSeries
            {
                Key = "X",
                Description = "X"
            });
            Sources.Add (new DataSeries
            {
                Key = "Y",
                Description = "Y"
            });
            Sources.Add(new DataSeries
            {
                Key = "W",
                Description = "Width",
                Data = 1.0
            });
            Sources.Add(new ColorSeries());
            Sources["C"].Data = "Blue";    
        
            LegendTemplate = BarGraphLegend;
            TooltipTemplate = BarGraphTooltip;
            MarkerTemplate = BarGraph;

            StrokeThickness = 2; // To compensate layout subpixel rounding
            UseLayoutRounding = false;
        }
        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is thread safe.
        /// <para>Color and width of markers are getting from <see cref="Color"/> and <see cref="BarsWidth"/> dependency properties.</para>
        /// </summary>
        /// <param name="x">
        /// Data for X <see cref="DataSeries"/> defining x coordinates of the center of each bar. 
        /// <para>Can be a single value (to draw one marker or markers with the same x coordinates) 
        /// or an array or IEnumerable (to draw markers with different x coordinates) of any numeric type. 
        /// Can be null then x coordinates will be a sequence of integers starting with zero.</para>
        /// </param>
        /// <param name="y">
        /// Data for Y <see cref="DataSeries"/> defining the value of each bar. 
        /// <para>Can be a single value (to draw one marker or markers with the same y values) 
        /// or an array or IEnumerable (to draw markers with different y values) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <remarks>
        /// Note that all vector data for <see cref="DataSeries"/> should be of the same length. 
        /// Otherwise no markers will be drawn. 
        /// </remarks>
        /// <returns>ID of rendering task. You can subscribe to notification about rendering completion events
        /// as observer of RenderCompletion.
        /// </returns>
        public long PlotBars(object x, object y)
        {
            return Plot(x, y, BarsWidth, Color);
        }
        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is thread safe.
        /// <para>This version does not need specification of X <see cref="DataSeries"/>. Default value is a sequence of integers starting with zero.</para>
        /// <para>Color and width of markers are getting from <see cref="Color"/> and <see cref="BarsWidth"/> dependency properties.</para>
        /// </summary>
        /// <param name="y">
        /// Data for Y <see cref="DataSeries"/> defining the value of each bar. 
        /// <para>Can be a single value (to draw one marker or markers with the same y values) 
        /// or an array or IEnumerable (to draw markers with different y values) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <returns>ID of rendering task. You can subscribe to notification about rendering completion events
        /// as observer of RenderCompletion.
        /// </returns>
        public long PlotBars(object y)
        {
            return Plot(null, y, BarsWidth, Color);
        }
        #region X
        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XProperty = DependencyProperty.Register(
            "X", typeof(object), typeof(BarGraph), new PropertyMetadata(null, OnXChanged));

        /// <summary>
        /// Gets or sets the data of X <see cref="DataSeries"/> defining x coordinates of the center of bars.
        /// <para>Can be a single value (to draw one marker or markers with the same x coordinates) 
        /// or an array or IEnumerable (to draw a set of different markers) of any numeric type. 
        /// Can be null then x coordinates will be a sequence of integers starting with zero.</para>
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Centers of vertical bars")]
        public object X
        {
            get { return (object)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        private static void OnXChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BarGraph mg = (BarGraph)d;
            mg.Sources["X"].Data = e.NewValue;
        }
        #endregion
        #region Y
        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YProperty = DependencyProperty.Register(
            "Y", typeof(object), typeof(BarGraph), new PropertyMetadata(null, OnYChanged));

        /// <summary>
        /// Gets or sets the data for Y <see cref="DataSeries"/> defining the value of bars.
        /// <para>Can be a single value (to draw one marker or markers with the same y values) 
        /// or an array or IEnumerable (to draw markers with different y values) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Vertical sizes of vars")]
        public object Y
        {
            get { return (object)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }
        
        private static void OnYChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BarGraph mg = (BarGraph)d;
            mg.Sources["Y"].Data = e.NewValue;

        }
        #endregion
        #region Description
        /// <summary>
        /// Identifies the <see cref="Description"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            "Description", typeof(string), typeof(BarGraph), new PropertyMetadata(null, OnDescriptionChanged));

        /// <summary>
        /// Gets or sets the description of Y <see cref="DataSeries"/>. Is frequently used in legend and tooltip.
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Text for legend and tooltip")]
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BarGraph mg = (BarGraph)d;
            mg.Sources["Y"].Description = (string)e.NewValue;
        }
        #endregion    
        #region BarsWidth
        /// <summary>
        /// Identifies the <see cref="BarsWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BarsWidthProperty = DependencyProperty.Register(
            "BarsWidth", typeof(object), typeof(BarGraph), new PropertyMetadata(1.0, OnBarsWidthChanged));

        /// <summary>
        /// Gets or sets the data for <see cref="DataSeries"/> (key 'W') of width of bars in plot coordinates.
        /// <para>Can be a single value (to draw markers of one width) or an array or IEnumerable 
        /// (to draw markers with different widths) of any numeric type.</para>
        /// <para>Default value is 1.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Widths of bars in plot coordinates")]
        public object BarsWidth
        {
            get { return (object)GetValue(BarsWidthProperty); }
            set { SetValue(BarsWidthProperty, value); }
        }

        private static void OnBarsWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BarGraph mg = (BarGraph)d;
            mg.Sources["W"].Data = e.NewValue;
        }
        #endregion
        #region Color
        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            "Color", typeof(SolidColorBrush), typeof(BarGraph), new PropertyMetadata(new SolidColorBrush(Colors.Blue), OnColorChanged));

        /// <summary>
        /// Gets or sets the color of bars.
        /// <para>Default value is blue color.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Bars fill color")]
        public SolidColorBrush Color
        {
            get { return (SolidColorBrush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BarGraph mg = (BarGraph)d;
            mg.Sources["C"].Data = e.NewValue;
        }
        #endregion
    }
    /// <summary>
    /// A particular case of marker graph that enables visualizations of vertical intervals (bars)
    /// by defining top and bottom coordinates of each bar.
    /// </summary>
    [Description("Marker graph showing verical intervals (min,max)")]
    public class VerticalIntervalGraph : MarkerGraph
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VerticalIntervalGraph"/> class.
        /// </summary>
        public VerticalIntervalGraph()
        {
            Sources.Add(new DataSeries
            {
                Key = "X",
                Description = "X",
            });
            Sources.Add(new DataSeries
            {
                Key = "Y1",
                Description = "Interval bound"
            });
            Sources.Add(new DataSeries
            {
                Key = "Y2",
                Description = "Interval bound"
            });
            Sources.Add(new DataSeries
            {
                Key = "W",
                Description = "Width",
                Data = 5
            });
            Sources.Add(new ColorSeries());

            MarkerTemplate = VerticalIntervalBar;
            LegendTemplate = VerticalIntervalLegend;
            TooltipTemplate = VerticalIntervalTooltip;

            Description = "Interval";
        }
        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is thread safe.
        /// <para>This version does not need specification of X <see cref="DataSeries"/>. 
        /// Default value is a sequence of integers starting with zero.</para>
        /// <para>Color and width of markers are getting from <see cref="Color"/> and <see cref="Size"/> dependency properties.</para>
        /// </summary>
        /// <param name="y1">
        /// Data for <see cref="DataSeries"/> defining y coordinates of the bottom of each bar. 
        /// <para>Can be a single value (to draw one marker or markers with the same bottom y coordinates) 
        /// or an array or IEnumerable (to draw markers with different bottom y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <param name="y2">
        /// Data for <see cref="DataSeries"/> defining y coordinates of the top of each bar. 
        /// <para>Can be a single value (to draw one marker or markers with the same top y coordinates) 
        /// or an array or IEnumerable (to draw markers with different top y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <remarks>
        /// Note that all vector data for <see cref="DataSeries"/> should be of the same length. 
        /// Otherwise no markers will be drawn.
        /// </remarks>
        /// <returns>ID of rendering task. You can subscribe to notification about rendering completion events
        /// as observer of RenderCompletion.
        /// </returns>
        public long PlotIntervals(object y1, object y2)
        {
            return Plot(null, y1, y2);
        }
        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is thread safe.
        /// <para>Color and width of markers are getting from <see cref="Color"/> and <see cref="Size"/> dependency properties.</para>
        /// </summary>
        /// <param name="x">
        /// Data for X <see cref="DataSeries"/> defining x coordinates of the center of each bar. 
        /// <para>Can be a single value (to draw one marker or markers with the same x coordinates) 
        /// or an array or IEnumerable (to draw markers with different x coordinates) of any numeric type. 
        /// Can be null then x coordinates will be a sequence of integers starting with zero.</para>
        /// </param>
        /// <param name="y1">
        /// Data for <see cref="DataSeries"/> defining y coordinates of the bottom of each bar. 
        /// <para>Can be a single value (to draw one marker or markers with the same bottom y coordinates) 
        /// or an array or IEnumerable (to draw markers with different bottom y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <param name="y2">
        /// Data for <see cref="DataSeries"/> defining y coordinates of the top of each bar. 
        /// <para>Can be a single value (to draw one marker or markers with the same top y coordinates) 
        /// or an array or IEnumerable (to draw markers with different top y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <remarks>
        /// Note that all vector data for <see cref="DataSeries"/> should be of the same length. 
        /// Otherwise no markers will be drawn.
        /// </remarks>
        /// <returns>ID of rendering task. You can subscribe to notification about rendering completion events
        /// as observer of RenderCompletion.
        /// </returns>
        public long PlotIntervals(object x, object y1, object y2)
        {
            return Plot(x, y1, y2);
        }
        #region Size
        /// <summary>
        /// Identifies the <see cref="Size"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            "Size", typeof(object), typeof(VerticalIntervalGraph), new PropertyMetadata(5.0, OnSizeChanged));

        /// <summary>
        /// Gets or sets the data for width <see cref="DataSeries"/> (key 'W').
        /// <para>Can be a single value (to draw markers of one width) 
        /// or an array or IEnumerable (to draw markers of different widths) of
        /// any numeric type. Can be null then no markers will be drawn.</para>
        /// <para>Default value is 5.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Screen width of intervals")]
        public object Size
        {
            get { return (object)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VerticalIntervalGraph mg = (VerticalIntervalGraph)d;
            mg.Sources["W"].Data = e.NewValue;
        }
        #endregion
        #region Color
        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            "Color", typeof(SolidColorBrush), typeof(VerticalIntervalGraph), new PropertyMetadata(new SolidColorBrush(Colors.Black), OnColorChanged));

        /// <summary>
        /// Gets or sets the data for <see cref="ColorSeries"/>.
        /// <para>Default value is black color.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Color of interval markers")]
        public SolidColorBrush Color
        {
            get { return (SolidColorBrush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VerticalIntervalGraph mg = (VerticalIntervalGraph)d;
            mg.Sources["C"].Data = e.NewValue;
        }
        #endregion
        #region Y1
        /// <summary>
        /// Identifies the <see cref="Y1"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty Y1Property = DependencyProperty.Register(
            "Y1", typeof(object), typeof(VerticalIntervalGraph), new PropertyMetadata(null, OnY1Changed));

        /// <summary>
        /// Gets or sets the data for <see cref="DataSeries"/> (key 'Y1') defining y coordinates of the bottom of each bar. 
        /// <para>Can be a single value (to draw one marker or markers with the same bottom y coordinates) 
        /// or an array or IEnumerable (to draw markers with different bottom y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Bound of interval")]
        public object Y1
        {
            get { return (object)GetValue(Y2Property); }
            set { SetValue(Y2Property, value); }
        }

        private static void OnY1Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VerticalIntervalGraph mg = (VerticalIntervalGraph)d;
            mg.Sources["Y1"].Data = e.NewValue;
        }
        #endregion
        #region Y2
        /// <summary>
        /// Identifies the <see cref="Y2"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty Y2Property = DependencyProperty.Register(
            "Y2", typeof(object), typeof(VerticalIntervalGraph), new PropertyMetadata(null, OnY1Changed));

        /// <summary>
        /// Gets or sets the data for <see cref="DataSeries"/> (key 'Y2') defining y coordinates of the top of each bar. 
        /// <para>Can be a single value (to draw one marker or markers with the same top y coordinates) 
        /// or an array or IEnumerable (to draw markers with different top y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Bound of interval")]
        public object Y2
        {
            get { return (object)GetValue(Y2Property); }
            set { SetValue(Y2Property, value); }
        }

        private static void OnY2Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VerticalIntervalGraph mg = (VerticalIntervalGraph)d;
            mg.Sources["Y2"].Data = e.NewValue;
        }
        #endregion
        #region Description
        /// <summary>
        /// Identifies the <see cref="Description"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            "Description", typeof(string), typeof(VerticalIntervalGraph),
            new PropertyMetadata(null, OnDescriptionChanged));

        /// <summary>
        /// Gets or sets the description for <see cref="DataSeries"/> of bounds of intervals.
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Description of interval series")]
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VerticalIntervalGraph mg = (VerticalIntervalGraph)d;
            mg.Sources["Y1"].Description = (string)e.NewValue;
            mg.Sources["Y2"].Description = (string)e.NewValue;
        }
        #endregion
    }
}