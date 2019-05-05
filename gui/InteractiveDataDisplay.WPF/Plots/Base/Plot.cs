// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;

namespace InteractiveDataDisplay.WPF { 
    /// <summary>
    /// Plot is a panel that supports positioning of child elements in plot coordinates by 
    /// providing attached properties X1,Y1,X2,Y2 and Points.
    /// Plot panel automatically computes bounding rectangle for all child elements.
    /// </summary>
    [Description("Position child elements in plot coordinates")]
    public class Plot : PlotBase
    {
        #region Attached Properties

        /// <summary>
        /// Identifies the Plot.X1 attached property. 
        /// </summary>
        public static readonly DependencyProperty X1Property =
            DependencyProperty.RegisterAttached("X1", typeof(double), typeof(Plot), new PropertyMetadata(double.NaN, DataCoordinatePropertyChangedHandler));

        /// <summary>
        /// Sets the value of the Plot.X1 attached property for a given dependency object. 
        /// </summary>
        /// <param name="element">The element to which the property value is written</param>
        /// <param name="value">Sets the Plot.X1 coordinate of the specified element</param>
        /// <remarks>
        /// Infinity values for Plot.X1 are handled is special way. 
        /// They do not participate in plot bounds computation, 
        /// “+Infinity” is translated to maximum visible value, “-Infinity” is translated to minimum visible coordinate. 
        /// </remarks>
        public static void SetX1(DependencyObject element, double value)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            element.SetValue(X1Property, value);
        }

        /// <summary>
        /// Returns the value of the Plot.X1 attached property for a given dependency object. 
        /// </summary>
        /// <param name="element">The element from which the property value is read</param>
        /// <returns>The Plot.X1 coordinate of the specified element</returns>
        /// <remarks>
        /// Default value of Plot.X1 property is Double.NaN. Element is horizontally arranged inside panel according
        /// to values of X1 and X2 attached property. X1 and X2 doesn't have to be ordered. If X1 is not
        /// specified (has NaN value) than Canvas.Left value is used.
        /// </remarks>
        public static double GetX1(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            return (double)element.GetValue(X1Property);
        }

        /// <summary>
        /// Identifies the Plot.X2 attached property. 
        /// </summary>
        public static readonly DependencyProperty X2Property =
            DependencyProperty.RegisterAttached("X2", typeof(double), typeof(Plot), new PropertyMetadata(double.NaN, DataCoordinatePropertyChangedHandler));

        /// <summary>
        /// Sets the value of the Plot.X2 attached property for a given dependency object. 
        /// </summary>
        /// <param name="element">The element to which the property value is written</param>
        /// <param name="value">Sets the Plot.X2 coordinate of the specified element</param>
        /// <remarks>
        /// Infinity values for Plot.X2 are handled is special way. 
        /// They do not participate in plot bounds computation, 
        /// “+Infinity” is translated to maximum visible value, “-Infinity” is translated to minimum visible coordinate. 
        /// </remarks>
        public static void SetX2(DependencyObject element, double value)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            element.SetValue(X2Property, value);
        }

        /// <summary>
        /// Returns the value of the Plot.X2 attached property for a given dependency object. 
        /// </summary>
        /// <param name="element">The element from which the property value is read</param>
        /// <returns>The Plot.X2 coordinate of the specified element</returns>
        /// <remarks>
        /// Default value of Plot.X2 property is Double.NaN.
        /// Element is horizontally arranged inside panel according
        /// to values of X1 and X2 attached property. X1 and X2 doesn't have to be ordered. If X2
        /// is not specified (has NaN value) then Width property is used to define element arrangement.
        /// </remarks>
        public static double GetX2(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            return (double)element.GetValue(X2Property);
        }

        /// <summary>
        /// Identifies the Plot.Y1 attached property. 
        /// </summary>
        public static readonly DependencyProperty Y1Property =
            DependencyProperty.RegisterAttached("Y1", typeof(double), typeof(Plot), new PropertyMetadata(double.NaN, DataCoordinatePropertyChangedHandler));

        /// <summary>
        /// Sets the value of the Plot.Y1 attached property for a given dependency object. 
        /// </summary>
        /// <param name="element">The element to which the property value is written</param>
        /// <param name="value">Sets the Plot.Y1 coordinate of the specified element</param>
        /// <remarks>
        /// Infinity values for Plot.Y1 are handled is special way. 
        /// They do not participate in plot bounds computation, 
        /// “+Infinity” is translated to maximum visible value, “-Infinity” is translated to minimum visible coordinate. 
        /// </remarks>
        public static void SetY1(DependencyObject element, double value)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            element.SetValue(Y1Property, value);
        }

        /// <summary>
        /// Returns the value of the Plot.Y1 attached property for a given dependency object. 
        /// </summary>
        /// <param name="element">The element from which the property value is read</param>
        /// <returns>The Plot.Y1 coordinate of the specified element</returns>
        /// <remarks>
        /// Default value of Plot.Y1 property is Double.NaN. Element is vertically arranged inside panel according
        /// to values of Y1 and Y2 attached property. Y1 and Y2 doesn't have to be ordered. 
        /// If Y1 is not specifed (has NaN value) then Canvas.Top property is used to define element position.
        /// </remarks>
        public static double GetY1(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            return (double)element.GetValue(Y1Property);
        }

        /// <summary>
        /// Identifies the Plot.Y2 attached property. 
        /// </summary>
        public static readonly DependencyProperty Y2Property =
            DependencyProperty.RegisterAttached("Y2", typeof(double), typeof(Plot), new PropertyMetadata(double.NaN, DataCoordinatePropertyChangedHandler));

        /// <summary>
        /// Sets the value of the Plot.Y2 attached property for a given dependency object. 
        /// </summary>
        /// <param name="element">The element to which the property value is written</param>
        /// <param name="value">Sets the Plot.Y2 coordinate of the specified element</param>
        /// <remarks>
        /// Infinity values for Plot.Y2 are handled is special way. 
        /// They do not participate in plot bounds computation, 
        /// “+Infinity” is translated to maximum visible value, “-Infinity” is translated to minimum visible coordinate. 
        /// </remarks>
        public static void SetY2(DependencyObject element, double value)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            element.SetValue(Y2Property, value);
        }

        /// <summary>
        /// Returns the value of the Plot.Y2 attached property for a given dependency object. 
        /// </summary>
        /// <param name="element">The element from which the property value is read</param>
        /// <returns>The Plot.Y2 coordinate of the specified element</returns>
        /// <remarks>
        /// Default value of Plot.Y2 property is Double.NaN. Element is vertically arranged inside panel according
        /// to values of Y1 and Y2 attached property. Y1 and Y2 doesn't have to be ordered. 
        /// If Y2 is not specifed (has NaN value) then Height property is used to define element arrangement.
        /// </remarks>
        public static double GetY2(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            return (double)element.GetValue(Y2Property);
        }

        /// <summary>
        /// Identifies the Plot.Points attached property. 
        /// </summary>
        public static readonly DependencyProperty PointsProperty =
            DependencyProperty.RegisterAttached("Points", typeof(PointCollection), typeof(Plot), new PropertyMetadata(null, DataCoordinatePropertyChangedHandler));

        /// <summary>
        /// Sets the value of the Plot.Points attached property for a given dependency object. 
        /// </summary>
        /// <param name="element">The element to which the property value is written</param>
        /// <param name="value">Sets the Plot.Points coollection of the specified element</param>
        public static void SetPoints(DependencyObject element, PointCollection value)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            element.SetValue(PointsProperty, value);
        }

        /// <summary>
        /// Returns the value of the Plot.Points attached property for a given dependency object. 
        /// </summary>
        /// <param name="element">The element from which the property value is read</param>
        /// <returns>The Plot.Points collection of the specified element</returns>
        /// <remarks>
        /// Default value of Plot.Points property is null
        /// </remarks>
        public static PointCollection GetPoints(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            return (PointCollection)element.GetValue(PointsProperty);
        }

        static void DataCoordinatePropertyChangedHandler(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var parent = VisualTreeHelper.GetParent(obj);
            if (parent is ContentPresenter) parent = VisualTreeHelper.GetParent(parent);
            var plotBase = parent as PlotBase;
            if (plotBase != null)
            {
                plotBase.InvalidateBounds();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Computes minimal plot rectangle, which contains all plot rectangles of child elements
        /// </summary>
        /// <returns>Minimal plot rectangle, which contains all plot rectangles of child elements</returns>
        protected override DataRect ComputeBounds()
        {
            var localPlotRect = DataRect.Empty;
            foreach (UIElement child in Children)
            {
                DependencyObject item = child ;
                if (item is ContentPresenter && VisualTreeHelper.GetChildrenCount(item) == 1)
                    item = VisualTreeHelper.GetChild(item, 0);
                double v;
                v = GetX1(item); 
                if (!double.IsNaN(v) && !double.IsInfinity(v)) localPlotRect.XSurround(XDataTransform.DataToPlot(v));
                v = GetX2(item);
                if (!double.IsNaN(v) && !double.IsInfinity(v)) localPlotRect.XSurround(XDataTransform.DataToPlot(v));
                v = GetY1(item);
                if (!double.IsNaN(v) && !double.IsInfinity(v)) localPlotRect.YSurround(YDataTransform.DataToPlot(v));
                v = GetY2(item);
                if (!double.IsNaN(v) && !double.IsInfinity(v)) localPlotRect.YSurround(YDataTransform.DataToPlot(v));
                var points = GetPoints(item);
                if (points != null)
                    foreach (var point in points)
                    {
                        localPlotRect.XSurround(XDataTransform.DataToPlot(point.X));
                        localPlotRect.YSurround(YDataTransform.DataToPlot(point.Y));
                    }
            }
            return localPlotRect;
        }

        /// <summary>
        /// Positions child elements and determines a size for a Plot
        /// </summary>
        /// <param name="finalSize">The final area within the parent that Plot should use to arrange itself and its children</param>
        /// <returns>The actual size used</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            ArrangeChildren(finalSize);

            if (ClipToBounds)
                Clip = new RectangleGeometry { Rect = new Rect(new Point(0, 0), finalSize) };
            else
                Clip = null;

            return finalSize;
        }

        /// <summary>
        /// Measures the size in layout required for child elements and determines a size for the Plot. 
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            availableSize = PerformAsMaster(availableSize);
            MeasureChildren(availableSize);
            return availableSize;
        }

        internal void MeasureChildren(Size availableSize)
        {
            foreach (UIElement child in Children)
            {
                DependencyObject item = child;
                if (item is ContentPresenter && VisualTreeHelper.GetChildrenCount(item) == 1)
                    item = VisualTreeHelper.GetChild(item, 0);

                var xy = GetPoints(item);
                if (xy != null)
                    if (item is Polyline)
                    {
                        var line = (Polyline)item;
                        var points = new PointCollection();
                        foreach (var point in xy) points.Add(new Point(LeftFromX(XDataTransform.DataToPlot(point.X)), TopFromY(YDataTransform.DataToPlot(point.Y))));
                        line.Points = points;
                    }
                    else if (item is Polygon)
                    {
                        var p = (Polygon)item;
                        var points = new PointCollection();
                        foreach (var point in xy) points.Add(new Point(LeftFromX(XDataTransform.DataToPlot(point.X)), TopFromY(YDataTransform.DataToPlot(point.Y))));
                        p.Points = points;
                    }
                if (item is Line)
                {
                    var line = (Line)item;
                    double v;
                    v = GetX1(line);
                    if (!double.IsNaN(v))
                    {
                        if (Double.IsNegativeInfinity(v))
                            line.X1 = 0;
                        else if (Double.IsPositiveInfinity(v))
                            line.X1 = availableSize.Width;
                        else
                            line.X1 = LeftFromX(XDataTransform.DataToPlot(v));
                    }
                    v = GetX2(line);
                    if (!double.IsNaN(v))
                    {
                        if (Double.IsNegativeInfinity(v))
                            line.X2 = 0;
                        else if (Double.IsPositiveInfinity(v))
                            line.X2 = availableSize.Width;
                        else
                            line.X2 = LeftFromX(XDataTransform.DataToPlot(v));
                    }
                    v = GetY1(line);
                    if (!double.IsNaN(v))
                    {
                        if (Double.IsNegativeInfinity(v))
                            line.Y1 = availableSize.Height;
                        else if (Double.IsPositiveInfinity(v))
                            line.Y1 = 0;
                        else
                            line.Y1 = TopFromY(YDataTransform.DataToPlot(v));
                    }
                    v = GetY2(line);
                    if (!double.IsNaN(v))
                    {
                        if (Double.IsNegativeInfinity(v))
                            line.Y2 = availableSize.Height;
                        else if (Double.IsPositiveInfinity(v))
                            line.Y2 = 0;
                        else
                            line.Y2 = TopFromY(YDataTransform.DataToPlot(v));
                    }
                }
                child.Measure(availableSize);
            }
        }

        internal void ArrangeChildren(Size finalSize)
        {
            foreach (UIElement child in Children)
            {
                DependencyObject item = child;
                if (item is ContentPresenter && VisualTreeHelper.GetChildrenCount(item) == 1)
                    item = VisualTreeHelper.GetChild(item, 0);

                if (item is Line || item is Polyline)
                {
                    child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
                }
                else
                {
                    double x1 = GetX1(item);
                    double x2 = GetX2(item);
                    double y1 = GetY1(item);
                    double y2 = GetY2(item);

                    Size desiredSize = Size.Empty;
                    if (double.IsNaN(x1) || double.IsNaN(x2) || double.IsNaN(y1) || double.IsNaN(y2))
                        desiredSize = child.DesiredSize;

                    double L = 0.0;
                    if (!double.IsNaN(x1))
                    {
                        if (double.IsNegativeInfinity(x1))
                            L = 0;
                        else if (double.IsPositiveInfinity(x1))
                            L = finalSize.Width;
                        else
                            L = LeftFromX(XDataTransform.DataToPlot(x1)); // x1 is not Nan and Inf here
                    }
                    else
                        L = (double)item.GetValue(Canvas.LeftProperty);

                    double W = 0.0;
                    var elem = item as FrameworkElement;
                    if (!double.IsNaN(x1) && !double.IsNaN(x2))
                    {
                        double L2 = 0.0;
                        if (double.IsNegativeInfinity(x2))
                            L2 = 0;
                        else if (double.IsPositiveInfinity(x2))
                            L2 = desiredSize.Width;
                        else
                            L2 = LeftFromX(XDataTransform.DataToPlot(x2)); // x2 is not Nan and Inf here

                        if (L2 >= L) W = L2 - L;
                        else
                        {
                            W = L - L2;
                            L = L2;
                        }
                    }
                    else if (elem != null || double.IsNaN(W = elem.Width) || double.IsInfinity(W))
                        W = desiredSize.Width;

                    double T = 0.0;
                    if (!double.IsNaN(y1))
                    {
                        if (double.IsNegativeInfinity(y1))
                            T = desiredSize.Height;
                        else if (double.IsPositiveInfinity(y1))
                            T = 0;
                        else
                            T = TopFromY(YDataTransform.DataToPlot(y1)); // y1 is not Nan and Inf here
                    }
                    else
                        T = (double)item.GetValue(Canvas.TopProperty);

                    double H = 0.0;
                    if (!double.IsNaN(y1) && !double.IsNaN(y2))
                    {
                        double T2 = 0.0;
                        if (double.IsNegativeInfinity(y2))
                            T2 = desiredSize.Height;
                        else if (double.IsPositiveInfinity(y2))
                            T2 = desiredSize.Width;
                        else
                            T2 = TopFromY(YDataTransform.DataToPlot(y2)); // y2 is not Nan and Inf here
                        if (T2 >= T) H = T2 - T;
                        else
                        {
                            H = T - T2;
                            T = T2;
                        }
                    }
                    else if (elem != null || double.IsNaN(H = elem.Height) || double.IsInfinity(H))
                        H = desiredSize.Height;

                    if (Double.IsNaN(L) || Double.IsInfinity(L) || Double.IsNaN(W) || Double.IsInfinity(W)) // Horizontal data to screen transform fails
                    {
                        L = 0;
                        W = desiredSize.Width;
                    }
                    if (Double.IsNaN(T) || Double.IsInfinity(T) || Double.IsNaN(H) || Double.IsInfinity(H)) // Vertical data to screen transform fails
                    {
                        T = 0;
                        H = desiredSize.Height;
                    }
                    child.Arrange(new Rect(L, T, W, H));
                }
            }
        }

        #endregion

    }
}

