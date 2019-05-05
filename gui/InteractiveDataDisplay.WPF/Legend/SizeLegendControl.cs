// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// A control to show information about size of drawing markers in a legend.
    /// </summary>
    [Description("Visually maps value to screen units")]
    public class SizeLegendControl : ContentControl
    {
        #region Fields

        private Canvas image;
        private Axis axis;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the range of axis values.
        /// Default value is [0, 1].
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Range of mapped values")]
        public Range Range
        {
            get { return (Range)GetValue(RangeProperty); }
            set { SetValue(RangeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the filling color.
        /// Default value is black color.
        /// </summary>
        [Category("Appearance")]
        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum value of displaying height. 
        /// Default value is 20.
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Maximum screen height of palette")]
        public new double MaxHeight
        {
            get { return (double)GetValue(MaxHeightProperty); }
            set { SetValue(MaxHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value indicating whether an axis should be rendered or not.
        /// Default value is true.
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public bool IsAxisVisible
        {
            get { return (bool)GetValue(IsAxisVisibleProperty); }
            set { SetValue(IsAxisVisibleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimum of size to display in a legend. This is screen size that corresponds to minimum value of data.
        /// Default value is Double.NaN.
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Screen size that corresponds to minimum value")]
        public double Min
        {
            get { return (double)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum of size to display in a legend. This is screen size that corresponds to maximum value of data.
        /// Default value is Double.NaN.
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Screen size that corresponds to maximum value")]
        public double Max
        {
            get { return (double)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }

        #endregion

        #region Dependency properties

        /// <summary>
        /// Identifies the <see cref="Range"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
            "Range",
            typeof(Range),
            typeof(SizeLegendControl),
            new PropertyMetadata(new Range(0, 1), OnRangeChanged));

        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            "Color",
            typeof(Color),
            typeof(SizeLegendControl),
            new PropertyMetadata(Colors.Black, OnColorChanged));

        /// <summary>
        /// Identifies the <see cref="MaxHeight"/> dependency property.
        /// </summary>
        public static new readonly DependencyProperty MaxHeightProperty = DependencyProperty.Register(
            "MaxHeight",
            typeof(double),
            typeof(SizeLegendControl),
            new PropertyMetadata(20.0, OnMinMaxChanged));

        /// <summary>
        /// Identifies the <see cref="IsAxisVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAxisVisibleProperty = DependencyProperty.Register(
            "IsAxisVisible",
            typeof(bool),
            typeof(SizeLegendControl),
            new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="Min"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinProperty = DependencyProperty.Register(
            "Min",
            typeof(double),
            typeof(SizeLegendControl),
            new PropertyMetadata(Double.NaN, OnMinMaxChanged));

        /// <summary>
        /// Identifies the <see cref="Max"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
            "Max",
            typeof(double),
            typeof(SizeLegendControl),
            new PropertyMetadata(Double.NaN, OnMinMaxChanged));

        #endregion

        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="SizeLegendControl"/> class.
        /// </summary>
        public SizeLegendControl()
        {
            StackPanel stackPanel = new StackPanel();

            image = new Canvas { HorizontalAlignment = HorizontalAlignment.Stretch };
            axis = new Axis { AxisOrientation = AxisOrientation.Bottom, HorizontalAlignment = HorizontalAlignment.Stretch };

            stackPanel.Children.Add(image);
            stackPanel.Children.Add(axis);

            Content = stackPanel;

            SizeChanged += (o, e) => 
            {
                if (e.PreviousSize.Width == 0 || e.PreviousSize.Height == 0 || Double.IsNaN(e.PreviousSize.Width) || Double.IsNaN(e.PreviousSize.Height))
                    UpdateCanvas();
            };

            IsTabStop = false;
        }

        #endregion

        #region Private methods

        private void UpdateCanvas()
        {
            if (Width == 0 || Double.IsNaN(Width))
                return;

            image.Children.Clear();

            double maxHeight = Double.IsNaN(Max) ? Range.Max : Max;
            double minHeight = Double.IsNaN(Min) ? Range.Min : Min;
            
            double visHeight = Math.Min(maxHeight, MaxHeight);

            image.Width = Width;
            axis.Width = Width;
            image.Height = visHeight;

            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = new Point(0, maxHeight);

            LineSegment lineSegment1 = new LineSegment();
            lineSegment1.Point = new Point(Width, maxHeight);
            LineSegment lineSegment2 = new LineSegment();
            lineSegment2.Point = new Point(Width, 0);
            LineSegment lineSegment3 = new LineSegment();
            lineSegment3.Point = new Point(0, maxHeight - minHeight);
            LineSegment lineSegment4 = new LineSegment();
            lineSegment4.Point = new Point(0, maxHeight);

            PathSegmentCollection pathSegmentCollection = new PathSegmentCollection();
            pathSegmentCollection.Add(lineSegment1);
            pathSegmentCollection.Add(lineSegment2);
            pathSegmentCollection.Add(lineSegment3);
            pathSegmentCollection.Add(lineSegment4);

            pathFigure.Segments = pathSegmentCollection;
            PathFigureCollection pathFigureCollection = new PathFigureCollection();
            pathFigureCollection.Add(pathFigure);
            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures = pathFigureCollection;

            Path path = new Path();
            // TODO: Make it dependency property of SizeControl
            path.Fill = new SolidColorBrush(Colors.LightGray);

            path.Data = pathGeometry;
            path.Stroke = new SolidColorBrush(Color);

            if (!Double.IsNaN(MaxHeight))
            {
                RectangleGeometry clip = new RectangleGeometry();
                clip.Rect = new Rect(0, maxHeight - this.MaxHeight, Width, this.MaxHeight);
                path.Clip = clip;
            }

            Canvas.SetTop(path, image.Height - maxHeight);
            image.Children.Add(path);

            if (visHeight < maxHeight)
            {
                Line top;
                if (minHeight >= visHeight)
                    top = new Line { X1 = 0, Y1 = 0, X2 = Width, Y2 = 0 };
                else
                    top = new Line
                    {
                        X1 = Width * (visHeight - minHeight) / (maxHeight - minHeight),
                        Y1 = 0,
                        X2 = Width,
                        Y2 = 0
                    };
                top.Stroke = new SolidColorBrush(Colors.Black);
                top.StrokeDashArray = new DoubleCollection();
                top.StrokeDashArray.Add(5);
                top.StrokeDashArray.Add(5);
                image.Children.Add(top);
            }
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SizeLegendControl control = (SizeLegendControl)d;
            control.OnRangeChanged();
        }

        private void OnRangeChanged()
        {
            axis.Range = Range;
            UpdateCanvas();
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SizeLegendControl control = (SizeLegendControl)d;
            control.OnColorChanged();
        }

        private void OnColorChanged()
        {
            if (image.Children.Count > 0)
                ((Path)image.Children[0]).Fill = new SolidColorBrush(Color);
        }

        private static void OnMinMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SizeLegendControl control = (SizeLegendControl)d;
            control.UpdateCanvas();
        }

        #endregion

    }
}

