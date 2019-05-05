// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Collections.Generic;
using System.ComponentModel;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Facilitates drawing of vertical and/or horizontal grid lines
    /// </summary>
    [Description("Grid of vertical and horizontal lines")]
    public class AxisGrid : PlotBase
    {
        private Path path;

        /// <summary>
        /// Identifies the <see cref="VerticalTicks"/> dependency property
        /// </summary>
        public static readonly DependencyProperty VerticalTicksProperty =
            DependencyProperty.Register("VerticalTicks", typeof(IEnumerable<double>), typeof(AxisGrid), new PropertyMetadata(new double[0],
                (o, e) =>
                {
                    AxisGrid axisGrid = (AxisGrid)o;
                    if (axisGrid != null)
                    {
                        axisGrid.InvalidateMeasure();
                    }
                }));

        /// <summary>
        /// Identifies the <see cref="HorizontalTicks"/> dependency property
        /// </summary>
        public static readonly DependencyProperty HorizontalTicksProperty =
            DependencyProperty.Register("HorizontalTicks", typeof(IEnumerable<double>), typeof(AxisGrid), new PropertyMetadata(new double[0],
                (o, e) =>
                {
                    AxisGrid axisGrid = (AxisGrid)o;
                    if (axisGrid != null)
                    {
                        axisGrid.InvalidateMeasure();
                    }
                }));

        /// <summary>
        /// Identifies the <see cref="Stroke"/> dependency property
        /// </summary>
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(SolidColorBrush), typeof(AxisGrid), new PropertyMetadata(new SolidColorBrush(Colors.LightGray),
                (o, e) =>
                {
                    AxisGrid axisGrid = (AxisGrid)o;
                    if (axisGrid != null)
                    {
                        axisGrid.InvalidateMeasure();
                    }
                }));

        /// <summary>
        /// Gets or sets collection for horizontal lines coordinates
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Horizontal lines coordinates")]
        public IEnumerable<double> VerticalTicks
        {
            get { return (IEnumerable<double>)GetValue(VerticalTicksProperty); }
            set { SetValue(VerticalTicksProperty, value); }
        }

        /// <summary>
        /// Gets or sets collection for vertical lines coordinates
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Vertical lines coordinates")]
        public IEnumerable<double> HorizontalTicks
        {
            get { return (IEnumerable<double>)GetValue(HorizontalTicksProperty); }
            set { SetValue(HorizontalTicksProperty, value); }
        }

        /// <summary>
        /// Identifies <see cref="IsXAxisReversed"/> dependency property
        /// </summary>
        public static readonly DependencyProperty IsXAxisReversedProperty =
            DependencyProperty.Register("IsXAxisReversed", typeof(bool), typeof(AxisGrid), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a flag indicating whether the x-axis is reversed or not.
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public bool IsXAxisReversed
        {
            get { return (bool)GetValue(IsXAxisReversedProperty); }
            set { SetValue(IsXAxisReversedProperty, value); }
        }

        /// <summary>
        /// Identifies <see cref="IsYAxisReversed"/> dependency property
        /// </summary>
        public static readonly DependencyProperty IsYAxisReversedProperty =
            DependencyProperty.Register("IsYAxisReversed", typeof(bool), typeof(AxisGrid), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a flag indicating whether the y-axis is reversed or not.
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public bool IsYAxisReversed
        {
            get { return (bool)GetValue(IsYAxisReversedProperty); }
            set { SetValue(IsYAxisReversedProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Brush that specifies how the horizontal and vertical lines is painted
        /// </summary>
        [Category("Appearance")]
        public SolidColorBrush Stroke
        {
            get { return (SolidColorBrush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        /// <summary>
        /// Initializes new instance of <see cref="AxisGrid"/> class
        /// </summary>
        public AxisGrid()
        {
            HorizontalTicks = new double[0];
            VerticalTicks = new double[0];

            path = new Path();
            BindingOperations.SetBinding(path, Path.StrokeProperty, new Binding("Stroke") { Source = this, Mode = BindingMode.TwoWay });
            path.StrokeThickness = 1.0;
            Children.Add(path);
        }

        /// <summary>
        /// Measures the size in layout required for child elements and determines a size for the AxisGrid. 
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (Double.IsInfinity(availableSize.Width))
                availableSize.Width = 1024;
            if (Double.IsInfinity(availableSize.Height))
                availableSize.Height = 1024;

            
            GeometryGroup group = new GeometryGroup();

            double[] hTicks = HorizontalTicks.ToArray();
            double[] vTicks = VerticalTicks.ToArray();

            if (hTicks != null && hTicks.Length > 0)
            {
                double minY = 0;
                double maxY = availableSize.Height;

                int i = 0;
                if (hTicks[0] < ActualPlotRect.X.Min) i++;
                for (; i < hTicks.Length; i++)
                {
                    double screenX = GetHorizontalCoordinateFromTick(hTicks[i], availableSize.Width, ActualPlotRect.X);
                    LineGeometry line = new LineGeometry();
                    line.StartPoint = new Point(screenX, minY);
                    line.EndPoint = new Point(screenX, maxY);
                    group.Children.Add(line);
                }
            }

            if (vTicks != null && vTicks.Length > 0)
            {
                double minX = 0;
                double maxX = availableSize.Width;

                int i = 0;
                if (vTicks[0] < ActualPlotRect.Y.Min) i++;
                for (; i < vTicks.Length; i++)
                {
                    double screenY = GetVerticalCoordinateFromTick(vTicks[i], availableSize.Height, ActualPlotRect.Y);
                    LineGeometry line = new LineGeometry();
                    line.StartPoint = new Point(minX, screenY);
                    line.EndPoint = new Point(maxX, screenY);
                    group.Children.Add(line);
                }
            }

            path.Data = group;

            foreach (UIElement child in Children)
            {
                child.Measure(availableSize);
            }

            return availableSize;
        }

        /// <summary>
        /// Positions child elements and determines a size for a AxisGrid
        /// </summary>
        /// <param name="finalSize">The final area within the parent that AxisGrid should use to arrange itself and its children</param>
        /// <returns>The actual size used</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in Children)
            {
                child.Arrange(new Rect(new Point(0, 0), finalSize));
            }

            return finalSize;
        }

        private double GetHorizontalCoordinateFromTick(double tick, double screenSize, Range range)
        {
            return ValueToScreen(XDataTransform.DataToPlot(tick), screenSize, range, IsXAxisReversed);
        }

        private double GetVerticalCoordinateFromTick(double tick, double screenSize, Range range)
        {
            return  screenSize - ValueToScreen(YDataTransform.DataToPlot(tick), screenSize, range, IsYAxisReversed);
        }


        private static double ValueToScreen(double value, double dimensionSize, Range range, bool isReversed)
        {
            var bound1 = isReversed ? range.Max : range.Min;
            var bound2 = isReversed ? range.Min : range.Max;
            return (value - bound1) * dimensionSize / (bound2 - bound1);
        }
    }
}

