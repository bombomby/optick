// Copyright © Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Globalization;
using System.ComponentModel;
using System.Collections;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// A base class for all <see cref="MarkerGraph"/> instances in cartesian coordinates.
    /// Has predefined X and Y <see cref="DataSeries"/>.
    /// </summary>
    [Description("Marker graph in cartesian coordinates")]
    public class CartesianMarkerGraph : MarkerGraph
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CartesianMarkerGraph"/> class.
        /// </summary>
        public CartesianMarkerGraph()
        {
            Sources.Add(new DataSeries { Key = "X", Description = "X" });
            Sources.Add(new DataSeries { Key = "Y", Description = "Y" });
        }
        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is thread safe.
        /// <para>This version does not need specification of X <see cref="DataSeries"/>. Default value is a sequence of integers starting with zero. </para>
        /// </summary>
        /// <param name="y">Data for Y <see cref="DataSeries"/> defining y coordinates of markers. 
        /// <para>Can be a single value (to draw one marker or markers with the same y coordinates) 
        /// or an array or IEnumerable (to draw markers with different y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <returns>ID of rendering task. You can subscribe to notification about rendering completion events
        /// as observer of RenderCompletion.</returns>
        public virtual long PlotY(object y)
        {
            return Plot(null, y);
        }
        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is thread safe.
        /// </summary>
        /// <param name="x">Data for X <see cref="DataSeries"/> defining x coordinates of markers. 
        /// <para>Can be a single value (to draw one marker or markers with the same x coordinates) 
        /// or an array or IEnumerable (to draw markers with different x coordinates) of any numeric type. 
        /// Can be null then x coordinates will be a sequence of integers starting with zero.</para>
        /// </param>
        /// <param name="y">Data for Y <see cref="DataSeries"/> defining y coordinates of markers. 
        /// <para>Can be a single value (to draw one marker or markers with the same y coordinates) 
        /// or an array or IEnumerable (to draw markers with different y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <remarks>
        /// Note that all vector data for <see cref="DataSeries"/> should be of the same length. Otherwise no markers will be drawn.
        /// </remarks>
        /// <returns>ID of rendering task. You can subscribe to notification about rendering completion events
        /// as observer of RenderCompletion.</returns>
        public virtual long PlotXY(object x, object y)
        {
            return Plot(x, y);
        }
        #region X
        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XProperty = DependencyProperty.Register(
            "X", typeof(object), typeof(CartesianMarkerGraph), new PropertyMetadata(null, OnXChanged));
        /// <summary>
        /// Gets or sets the data for X <see cref="DataSeries"/> defining x coordinates of markers.
        /// <para>Can be a single value (to draw one marker or markers with the same x coordinates) 
        /// or an array or IEnumerable (to draw markers with different x coordinates) of any numeric type. 
        /// Can be null then x coordinates will be a sequence of integers starting with zero.</para>
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("X coordinates")]
        public object X
        {
            get { return (object)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }
        private static void OnXChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CartesianMarkerGraph mg = (CartesianMarkerGraph)d;
            mg.Sources["X"].Data = e.NewValue;
        }
        #endregion
        #region Y
        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YProperty = DependencyProperty.Register(
            "Y", typeof(object), typeof(CartesianMarkerGraph), new PropertyMetadata(null, OnYChanged));

        /// <summary>
        /// Gets or sets the data for Y <see cref="DataSeries"/> defining y coordinates of markers.
        /// <para>Can be a single value (to draw one marker or markers with the same y coordinates) 
        /// or an array or IEnumerable (to draw markers with different y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Y coordinates")]
        public object Y
        {
            get { return (object)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        /// <summary>
        /// Called when property <see cref="Y"/> changes.
        /// </summary>
        /// <param name="oldValue">Old value of propery <see cref="Y"/>.</param>
        /// <param name="newValue">New value of propery <see cref="Y"/>.</param>
        protected virtual void OnYChanged(object oldValue, object newValue)
        {
            Sources["Y"].Data = newValue;
        }

        private static void OnYChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CartesianMarkerGraph mg = (CartesianMarkerGraph)d;
            mg.OnYChanged(e.OldValue, e.NewValue);
        }
        #endregion
        #region Description
        /// <summary>
        /// Identifies the <see cref="Description"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            "Description", typeof(string), typeof(CartesianMarkerGraph), new PropertyMetadata(null, OnDescriptionChanged));

        /// <summary>
        /// Gets or sets the description of Y <see cref="DataSeries"/>. Is frequently used in legend and tooltip.
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Text for tooltip and legend")]
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CartesianMarkerGraph mg = (CartesianMarkerGraph)d;
            mg.Sources["Y"].Description = (string)e.NewValue;
        }
        #endregion
    }
    /// <summary>
    /// Is a <see cref="CartesianMarkerGraph"/> with defined <see cref="ColorSeries"/> and <see cref="SizeSeries"/> 
    /// to draw markers in cartesian coordinates in color and of different size.
    /// <remarks>
    /// Note that legend and tooltip templates are choosing automaticaly depending on a using version of Plot method.
    /// </remarks>
    /// </summary>
    [Description("Marker graph in cartesian space with color and size as additional series")]
    public class CartesianSizeColorMarkerGraph : CartesianMarkerGraph
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CartesianSizeColorMarkerGraph"/> class.
        /// </summary>
        public CartesianSizeColorMarkerGraph()
        {
            Sources.Add(new ColorSeries());
            Sources.Add(new SizeSeries());
        }

        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is not thread safe.
        /// <para>This version does not need specification of X <see cref="DataSeries"/>. Default value is a sequence of integers starting with zero.</para>
        /// <para>Color and size of markers are taken from <see cref="Color"/> and <see cref="Size"/> dependency properties.</para>
        /// </summary>
        /// <param name="y">Data for Y <see cref="DataSeries"/> defining y coordinates of markers. 
        /// <para>Can be a single value (to draw one marker or markers with the same y coordinates) 
        /// or an array or IEnumerable (to draw markers with different y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <returns>ID of rendering task. You can subscribe to notification about rendering completion events
        /// as observer of RenderCompletion.</returns>
        public override long PlotY(object y)
        {
            LegendTemplate = GetLegendTemplate(Color, Size);
            TooltipTemplate = GetTooltipTemplate(Color, Size);
            return Plot(null, y, Color, Size);
        }
        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is not thread safe.
        /// <para>Color and size of markers are getting from <see cref="Color"/> and <see cref="Size"/> dependency properties.</para>
        /// </summary>
        /// <param name="x">Data for X <see cref="DataSeries"/> defining x coordinates of markers. 
        /// <para>Can be a single value (to draw one marker or markers with the same x coordinates) 
        /// or an array or IEnumerable (to draw markers with different x coordinates) of any numeric type.
        /// Can be null then x coordinates will be a sequence of integers starting with zero.</para>
        /// </param>
        /// <param name="y">Data for Y <see cref="DataSeries"/> defining y coordinates of markers. 
        /// <para>Can be a single value (to draw one marker or markers with the same y coordinates) 
        /// or an array or IEnumerable (to draw markers with different y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <remarks>
        /// Note that all vector data for <see cref="DataSeries"/> should be of the same length. Otherwise no markers will be drawn.
        /// </remarks>
        /// <returns>ID of rendering task. You can subscribe to notification about rendering completion events
        /// as observer of RenderCompletion.</returns>
        public override long PlotXY(object x, object y)
        {
            LegendTemplate = GetLegendTemplate(Color, Size);
            TooltipTemplate = GetTooltipTemplate(Color, Size);

            return Plot(x, y, Color, Size);
        }
        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is not thread safe.
        /// <para>Size of markers is getting from <see cref="Size"/> dependency property.</para>
        /// </summary>
        /// <param name="x">Data for X <see cref="DataSeries"/> defining x coordinates of markers. 
        /// <para>Can be a single value (to draw one marker or markers with the same x coordinates) 
        /// or an array or IEnumerable (to draw markers with different x coordinates) of any numeric type.
        /// Can be null then x coordinates will be a sequence of integers starting with zero.</para>
        /// </param>
        /// <param name="y">Data for Y <see cref="DataSeries"/> defining y coordinates of markers. 
        /// <para>Can be a single value (to draw one marker or markers with the same y coordinates) 
        /// or an array or IEnumerable (to draw markers with different y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <param name="c">Data for <see cref="ColorSeries"/> defining color of markers. 
        /// <para>Can be a single value (to draw markers of one color) or an array or IEnumerable 
        /// (to draw markers of different colors) of any numeric type, System.Windows.Media.Color, 
        /// or string defining system name of a color. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <remarks>
        /// Note that all vector data for <see cref="DataSeries"/> should be of the same length. Otherwise no markers will be drawn.
        /// </remarks>
        /// <returns>ID of rendering task. You can subscribe to notification about rendering completion events
        /// as observer of RenderCompletion.</returns>
        public long PlotColor(object x, object y, object c)
        {
            LegendTemplate = GetLegendTemplate(c, Size);
            TooltipTemplate = GetTooltipTemplate(c, Size);

            return Plot(x, y, c, Size);
        }
        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is thread safe.
        /// <para>Color of markers is getting from <see cref="Color"/> dependency property.</para>
        /// </summary>
        /// <param name="x">Data for X <see cref="DataSeries"/> defining x coordinates of markers. 
        /// <para>Can be a single value (to draw one marker or markers with the same x coordinates) 
        /// or an array or IEnumerable (to draw markers with different x coordinates) of any numeric type.
        /// Can be null then x coordinates will be a sequence of integers starting with zero.</para>
        /// </param>
        /// <param name="y">Data for Y <see cref="DataSeries"/> defining y coordinates of markers. 
        /// <para>Can be a single value (to draw one marker or markers with the same y coordinates) 
        /// or an array or IEnumerable (to draw markers with different y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <param name="d">Data for <see cref="SizeSeries"/> defining size of markers in screen coordinates. 
        /// <para>Can be a single value (to draw markers of one size) or an array or IEnumerable (to draw markers of different sizes) of
        /// any numeric type. Can be null then no markers will be drawn.</para>
        /// </param>
        /// <remarks>
        /// Note that all vector data for <see cref="DataSeries"/> should be of the same length. Otherwise no markers will be drawn.
        /// </remarks>
        /// <returns>ID of rendering task. You can subscribe to notification about rendering completion events
        /// as observer of RenderCompletion.</returns>
        public long PlotSize(object x, object y, object d)
        {
            LegendTemplate = GetLegendTemplate(Color, d);
            TooltipTemplate = GetTooltipTemplate(Color, d);

            return Plot(x, y, Color, d);
        }
        /// <summary>
        /// Updates data in data series and starts redrawing marker graph. This method returns before
        /// marker graph is redrawn. This method is not thread safe.
        /// </summary>
        /// <param name="x">Data for X <see cref="DataSeries"/> defining x coordinates of markers. 
        /// <para>Can be a single value (to draw one marker or markers with the same x coordinates) 
        /// or an array or IEnumerable (to draw markers with different x coordinates) of any numeric type. 
        /// Can be null then x coordinates will be a sequence of integers starting with zero.</para>
        /// </param>
        /// <param name="y">Data for Y <see cref="DataSeries"/> defining y coordinates of markers. 
        /// <para>Can be a single value (to draw one marker or markers with the same y coordinates) 
        /// or an array or IEnumerable (to draw markers with different y coordinates) of any numeric type. 
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <param name="c">Data for <see cref="ColorSeries"/> defining color of markers. 
        /// <para>Can be a single value (to draw markers of one color) or an array or IEnumerable (to draw markers of different colors) of
        /// any numeric type, System.Windows.Media.Color, or string defining system name of a color.
        /// Can be null then no markers will be drawn.</para>
        /// </param>
        /// <param name="d">Data for <see cref="SizeSeries"/> defining size of markers in screen coordinates. 
        /// <para>Can be a single value (to draw markers of one size) or an array or IEnumerable (to draw markers of different sizes) of
        /// any numeric type. Can be null then no markers will be drawn.</para>
        /// </param>
        /// <remarks>
        /// Note that all vector data for <see cref="DataSeries"/> should be of the same length. Otherwise no markers will be drawn.
        /// </remarks>
        /// <returns>ID of rendering task. You can subscribe to notification about rendering completion events
        /// as observer of RenderCompletion.</returns>
        public long PlotColorSize(object x, object y, object c, object d)
        {
            LegendTemplate = GetLegendTemplate(c, d);
            TooltipTemplate = GetTooltipTemplate(c, d);
            
            return Plot(x, y, c, d);
        }
        private static bool IsScalarOrNull(object obj)
        {
            if (obj == null || obj is string)
            {
                return true;
            }
            IEnumerable ienum = obj as IEnumerable;
            if (ienum != null)
            {
                int n = DataSeries.GetArrayFromEnumerable(ienum).Length;
                if (n == 0 || n == 1)
                    return true;
                return false;
            }
            Array array = obj as Array;
            if (array != null)
            {
                int n = array.Length;
                if (n == 0 || n == 1)
                    return true;
                return array.Rank == 1;
            }
            return true;
        }
        private DataTemplate GetLegendTemplate(object c, object d)
        {
            if (!IsScalarOrNull(c) && !IsScalarOrNull(d))
                return MarkerType.GetColorSizeLegendTemplate(this);
            else if (!IsScalarOrNull(c))
                return MarkerType.GetColorLegendTemplate(this);
            else if (!IsScalarOrNull(d))
                return MarkerType.GetSizeLegendTemplate(this);
            else
                return MarkerType.GetYLegendTemplate(this);
        }
        private DataTemplate GetTooltipTemplate(object c, object d)
        {
            if (!IsScalarOrNull(c) && !IsScalarOrNull(d))
                return ColorSizeMarker.GetColorSizeTooltipTemplate(this);
            else if (!IsScalarOrNull(c))
                return ColorSizeMarker.GetColorTooltipTemplate(this);
            else if (!IsScalarOrNull(d))
                return ColorSizeMarker.GetSizeTooltipTemplate(this);
            else
                return ColorSizeMarker.GetYTooltipTemplate(this);
        }
        /// <summary>
        /// Called when property Y changes.
        /// </summary>
        /// <param name="oldValue">Old value of propery Y.</param>
        /// <param name="newValue">New value of propery Y.</param>
        protected override void OnYChanged(object oldValue, object newValue)
        {
            LegendTemplate = GetLegendTemplate(Color, Size);
            TooltipTemplate = GetTooltipTemplate(Color, Size);
            base.OnYChanged(oldValue, newValue);
        }
        #region Size
        /// <summary>
        /// Identifies the <see cref="Size"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            "Size", typeof(object), typeof(CartesianSizeColorMarkerGraph), new PropertyMetadata(10.0, OnSizeChanged));

        /// <summary>
        /// Gets or sets the data for <see cref="SizeSeries"/> (key 'D').
        /// <para>Can be a single value (to draw markers of one size) or an array or IEnumerable (to draw markers of different sizes) of
        /// any numeric type. Can be null then no markers will be drawn.</para>
        /// <para>If properties <see cref="Min"/> and <see cref="Max"/> are not Double.NaN then <see cref="ResizeConverter"/>
        /// is used to transform values from original range of data to the range created from <see cref="Min"/> and <see cref="Max"/>.</para>
        /// <para>Default value is 10.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Series to define marker sizes")]
        public object Size
        {
            get { return (object)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CartesianSizeColorMarkerGraph mg = (CartesianSizeColorMarkerGraph)d;
            mg.LegendTemplate = mg.GetLegendTemplate(mg.Color, e.NewValue);
            mg.TooltipTemplate = mg.GetTooltipTemplate(mg.Color, e.NewValue);
            mg.Sources["D"].Data = e.NewValue;
        }
        #endregion
        #region Color
        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            "Color", typeof(object), typeof(CartesianSizeColorMarkerGraph), new PropertyMetadata("Black", OnColorChanged));

        /// <summary>
        /// Gets or sets the data for <see cref="ColorSeries"/> (key 'C').
        /// <para>Can be a single value (to draw markers of one color) or an array or IEnumerable (to draw markers of different colors) of
        /// any numeric type, System.Windows.Media.Color, or string defining system name or hexadecimal representation (#AARRGGBB) of a color. 
        /// Can be null then no markers will be drawn.</para>
        /// <para>Default value is black color.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Series to define marker colors")]
        public object Color
        {
            get { return (object)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CartesianSizeColorMarkerGraph mg = (CartesianSizeColorMarkerGraph)d;
            mg.LegendTemplate = mg.GetLegendTemplate(e.NewValue, mg.Size);
            mg.TooltipTemplate = mg.GetTooltipTemplate(e.NewValue, mg.Size);
            mg.Sources["C"].Data = e.NewValue;
        }
        #endregion
        #region SizeDescription
        /// <summary>
        /// Identifies the <see cref="SizeDescription"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SizeDescriptionProperty = DependencyProperty.Register(
            "SizeDescription", typeof(string), typeof(CartesianSizeColorMarkerGraph),
            new PropertyMetadata(null, OnSizeDescriptionChanged));

        /// <summary>
        /// Gets or sets the description of <see cref="SizeSeries"/>.
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Size series description")]
        public string SizeDescription
        {
            get { return (string)GetValue(SizeDescriptionProperty); }
            set { SetValue(SizeDescriptionProperty, value); }
        }

        private static void OnSizeDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CartesianSizeColorMarkerGraph mg = (CartesianSizeColorMarkerGraph)d;
            mg.Sources["D"].Description = (string)e.NewValue;
        }
        #endregion
        #region ColorDescription
        /// <summary>
        /// Identifies the <see cref="ColorDescription"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorDescriptionProperty = DependencyProperty.Register(
            "ColorDescription", typeof(string), typeof(CartesianSizeColorMarkerGraph),
            new PropertyMetadata(null, OnColorDescriptionChanged));

        /// <summary>
        /// Gets or sets the description of <see cref="ColorSeries"/>.
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Color series description")]
        public string ColorDescription
        {
            get { return (string)GetValue(ColorDescriptionProperty); }
            set { SetValue(ColorDescriptionProperty, value); }
        }

        private static void OnColorDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CartesianSizeColorMarkerGraph mg = (CartesianSizeColorMarkerGraph)d;
            mg.Sources["C"].Description = (string)e.NewValue;
        }
        #endregion
        #region Min
        /// <summary>
        /// Identifies the <see cref="Min"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinProperty = DependencyProperty.Register(
            "Min", typeof(double), typeof(CartesianSizeColorMarkerGraph), new PropertyMetadata(Double.NaN, OnMinChanged));

        /// <summary>
        /// Gets or sets screen size corresponding to minimal value in size series.
        /// <para>Default value is Double.NaN.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Minimal screen size of marker")]
        public double Min
        {
            get { return (double)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }

        private static void OnMinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CartesianSizeColorMarkerGraph mg = (CartesianSizeColorMarkerGraph)d;
            (mg.Sources["D"] as SizeSeries).Min = Convert.ToDouble(e.NewValue, CultureInfo.InvariantCulture); ;
        }
        #endregion
        #region Max
        /// <summary>
        /// Identifies the <see cref="Max"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
            "Max", typeof(double), typeof(CartesianSizeColorMarkerGraph), new PropertyMetadata(Double.NaN, OnMaxChanged));

        /// <summary>
        /// Gets or sets screen size corresponding to maximum value in size series.
        /// <para>Default value is Double.NaN.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Maximum screen size of marker")]
        public double Max
        {
            get { return (double)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }

        private static void OnMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CartesianSizeColorMarkerGraph mg = (CartesianSizeColorMarkerGraph)d;
            (mg.Sources["D"] as SizeSeries).Max = Convert.ToDouble(e.NewValue, CultureInfo.InvariantCulture); ;
        }
        #endregion
        #region Palette
        /// <summary>
        /// Identifies the <see cref="Palette"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register(
            "Palette", typeof(IPalette), typeof(CartesianSizeColorMarkerGraph), new PropertyMetadata(null, OnPaletteChanged));

        /// <summary>
        /// Gets or sets the color palette for markers. Defines mapping of values to colors. 
        /// Is used only if the data of <see cref="ColorSeries"/> is of numeric type.
        /// <para>Default value is null.</para>
        /// </summary>
        [TypeConverter(typeof(StringToPaletteTypeConverter))]
        [Category("InteractiveDataDisplay")]
        [Description("Defines mapping of values to colors")]
        public IPalette Palette
        {
            get { return (IPalette)GetValue(PaletteProperty); }
            set { SetValue(PaletteProperty, value); }
        }

        private static void OnPaletteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CartesianSizeColorMarkerGraph mg = (CartesianSizeColorMarkerGraph)d;
            (mg.Sources["C"] as ColorSeries).Palette = e.NewValue as IPalette;
        }
        #endregion
        #region MarkerType
        /// <summary>
        /// Identifies the <see cref="MarkerType"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkerTypeProperty = DependencyProperty.Register(
            "MarkerType", typeof(ColorSizeMarker), typeof(CartesianSizeColorMarkerGraph), new PropertyMetadata(null, OnMarkerTypeChanged));

        /// <summary>
        /// Gets or sets one of predefined types for markers.
        /// <para>Default value is null.</para>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("One of predefined marker types")]
        public ColorSizeMarker MarkerType
        {
            get { return (ColorSizeMarker)GetValue(MarkerTypeProperty); }
            set { SetValue(MarkerTypeProperty, value); }
        }

        private static void OnMarkerTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CartesianSizeColorMarkerGraph mg = (CartesianSizeColorMarkerGraph)d;
            mg.MarkerTemplate = mg.MarkerType.GetMarkerTemplate(mg);
        }
        #endregion
    }
    /// <summary>
    /// An abstract class providing methods to get templates for markers that use color and size.
    /// </summary>
    public abstract class ColorSizeMarker
    {
        /// <summary>
        /// Gets a template for markers.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for markers of particular marker graph.</returns>
        public abstract DataTemplate GetMarkerTemplate(MarkerGraph mg);
        /// <summary>
        /// Gets template for marker legend with information about Y <see cref="DataSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph.</returns>
        public abstract DataTemplate GetYLegendTemplate(MarkerGraph mg);
        /// <summary>
        /// Gets template for marker legend with information about Y <see cref="DataSeries"/> and <see cref="ColorSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph.</returns>
        public abstract DataTemplate GetColorLegendTemplate(MarkerGraph mg);
        /// <summary>
        /// Gets template for marker legend with information about Y <see cref="DataSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph.</returns>
        public abstract DataTemplate GetSizeLegendTemplate(MarkerGraph mg);
        /// <summary>
        /// Gets template for marker legend with information about Y <see cref="DataSeries"/>, 
        /// <see cref="ColorSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph.</returns>
        public abstract DataTemplate GetColorSizeLegendTemplate(MarkerGraph mg);
        /// <summary>
        /// Gets template for marker tooltip with information about Y <see cref="DataSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for tooltip of particular marker graph or null if marker graph is null.</returns>
        public static DataTemplate GetYTooltipTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.DefaultTooltipTemplate;
        }
        /// <summary>
        /// Gets template for marker tooltip with information about Y <see cref="DataSeries"/> and <see cref="ColorSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for tooltip of particular marker graph or null if marker graph is null.</returns>
        public static DataTemplate GetColorTooltipTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.ColorTooltip;
        }
        /// <summary>
        /// Gets template for marker tooltip with information about Y <see cref="DataSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for tooltip of particular marker graph or null if marker graph is null.</returns>
        public static DataTemplate GetSizeTooltipTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.SizeTooltip;
        }
        /// <summary>
        /// Gets template for marker tooltip with information about Y <see cref="DataSeries"/>, <see cref="ColorSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for tooltip of particular marker graph or null if marker graph is null.</returns>
        public static DataTemplate GetColorSizeTooltipTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.ColorSizeTooltip;
        }
    }
    /// <summary>
    /// Provides methods to get predefined templates for circle markers.
    /// </summary>
    public class CircleMarker : ColorSizeMarker
    {
        /// <summary>
        /// Gets template for circle markers.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for markers of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetMarkerTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.Circle;
        }
        /// <summary>
        /// Gets template for circle marker legend with information about Y <see cref="DataSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetYLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.CircleLegend;
        }
        /// <summary>
        /// Gets template for circle marker legend with information about Y <see cref="DataSeries"/> and <see cref="ColorSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetColorLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.CircleColorLegend;
        }

        /// <summary>
        /// Gets template for circle marker legend with information about Y <see cref="DataSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetSizeLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.CircleSizeLegend;
        }

        /// <summary>
        /// Gets template for circle marker legend with information about Y <see cref="DataSeries"/>, <see cref="ColorSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetColorSizeLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.CircleColorSizeLegend;
        }
    }
    /// <summary>
    /// Provides methods to get predefined templates for box markers.
    /// </summary>
    public class BoxMarker : ColorSizeMarker
    {
        /// <summary>
        /// Gets template for box markers.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for markers of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetMarkerTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.Box;
        }

        /// <summary>
        /// Gets template for box marker legend with information about Y <see cref="DataSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetYLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.BoxLegend;
        }

        /// <summary>
        /// Gets template for box marker legend with information about Y <see cref="DataSeries"/> and <see cref="ColorSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetColorLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.BoxColorLegend;
        }

        /// <summary>
        /// Gets template for box marker legend with information about Y <see cref="DataSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetSizeLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.BoxSizeLegend;
        }

        /// <summary>
        /// Gets template for box marker legend with information about Y <see cref="DataSeries"/>, <see cref="ColorSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetColorSizeLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.BoxColorSizeLegend;
        }
    }

    /// <summary>
    /// Provides methods to get predefined templates for cross markers.
    /// </summary>
    public class CrossMarker : ColorSizeMarker
    {
        /// <summary>
        /// Gets template for cross markers.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for markers of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetMarkerTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.Cross;
        }

        /// <summary>
        /// Gets template for cross marker legend with information about Y <see cref="DataSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetYLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.CrossLegend;
        }

        /// <summary>
        /// Gets template for cross marker legend with information about Y <see cref="DataSeries"/> and <see cref="ColorSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetColorLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.CrossColorLegend;
        }

        /// <summary>
        /// Gets template for cross marker legend with information about Y <see cref="DataSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetSizeLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.CrossSizeLegend;
        }

        /// <summary>
        /// Gets template for cross marker legend with information about Y <see cref="DataSeries"/>, <see cref="ColorSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetColorSizeLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.CrossColorSizeLegend;
        }
    }

    /// <summary>
    /// Provides methods to get predefined templates for diamond markers.
    /// </summary>
    public class DiamondMarker : ColorSizeMarker
    {
        /// <summary>
        /// Gets template for diamond markers.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for markers of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetMarkerTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.Diamond;
        }

        /// <summary>
        /// Gets template for diamond marker legend with information about Y <see cref="DataSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetYLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.DiamondLegend;
        }

        /// <summary>
        /// Gets template for diamond marker legend with information about Y <see cref="DataSeries"/> and <see cref="ColorSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetColorLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.DiamondColorLegend;
        }

        /// <summary>
        /// Gets template for diamond marker legend with information about Y <see cref="DataSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetSizeLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.DiamondSizeLegend;
        }

        /// <summary>
        /// Gets template for diamond marker legend with information about Y <see cref="DataSeries"/>, <see cref="ColorSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetColorSizeLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.DiamondColorSizeLegend;
        }
    }

    /// <summary>
    /// Provides methods to get predefined templates for triangle markers.
    /// </summary>
    public class TriangleMarker : ColorSizeMarker
    {
        /// <summary>
        /// Gets template for triangle markers.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for markers of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetMarkerTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.Triangle;
        }

        /// <summary>
        /// Gets template for triangle marker legend with information about Y <see cref="DataSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetYLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.TriangleLegend;
        }

        /// <summary>
        /// Gets template for triangle marker legend with information about Y <see cref="DataSeries"/> and <see cref="ColorSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetColorLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.TriangleColorLegend;
        }

        /// <summary>
        /// Gets template for triangle marker legend with information about Y <see cref="DataSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetSizeLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.TriangleSizeLegend;
        }

        /// <summary>
        /// Gets template for triangle marker legend with information about Y <see cref="DataSeries"/>, <see cref="ColorSeries"/> and <see cref="SizeSeries"/>.
        /// </summary>
        /// <param name="mg">Marker graph to get template for.</param>
        /// <returns>A template used for legend of particular marker graph or null if marker graph is null.</returns>
        public override DataTemplate GetColorSizeLegendTemplate(MarkerGraph mg)
        {
            if (mg == null)
                return null;
            return mg.TriangleColorSizeLegend;
        }
    }
}

