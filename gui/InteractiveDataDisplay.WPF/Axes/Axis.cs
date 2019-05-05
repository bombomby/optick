// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using System.ComponentModel;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>Facilitates drawing of vertical or horizontal coordinate axis</summary>
    [Description("Vertical or horizontal coordinate axis")]
    public class Axis : Panel
    {
        private ILabelProvider labelProvider;
        private TicksProvider ticksProvider;
        private Path majorTicksPath;
        private Path minorTicksPath;

        private FrameworkElement[] labels;

        private const int maxTickArrangeIterations = 12;
        private int maxTicks = 20;
        private const double increaseRatio = 8.0;
        private const double decreaseRatio = 8.0;
        private const double tickLength = 10;

        private bool drawTicks = true;
        private bool drawMinorTicks = true;

        /// <summary>
        /// Initializes a new instance of <see cref="Axis"/> class with double <see cref="LabelProvider"/>
        /// and <see cref="TicksProvider"/>.
        /// </summary>
        public Axis()
        {
            DataTransform = new IdentityDataTransform();
            Ticks = new double[0];

            majorTicksPath = new Path();
            minorTicksPath = new Path();
            Children.Add(majorTicksPath);
            Children.Add(minorTicksPath);

            BindingOperations.SetBinding(majorTicksPath, Path.StrokeProperty, new Binding("Foreground") { Source = this, Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(minorTicksPath, Path.StrokeProperty, new Binding("Foreground") { Source = this, Mode = BindingMode.TwoWay });

            if (labelProvider == null)
                this.labelProvider = new LabelProvider();
            if (ticksProvider == null)
                this.ticksProvider = new TicksProvider();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Axis"/> class.
        /// </summary>
        /// <param name="labelProvider">A <see cref="LabelProvider"/> to create labels.</param>
        /// <param name="ticksProvider">A <see cref="TicksProvider"/> to create ticks.</param>
        public Axis(ILabelProvider labelProvider, TicksProvider ticksProvider)
            : this()
        {
            this.labelProvider = labelProvider;
            this.ticksProvider = ticksProvider;
        }

        /// <summary>
        /// Gets or sets a set of double values displayed as axis ticks in data values.
        /// </summary>
        /// <remarks>
        /// Ticks are calculated from current Range of axis in plot coordinates and current DataTransform of axis.
        /// Example: for range [-1,1] int plot coordinates and DataTransform as y = 0.001 * x + 0.5 values of Ticks will be in [-1000,1000] range
        /// It is not recommended to set this property manually.
        /// </remarks>
        [Browsable(false)]
        public IEnumerable<double> Ticks
        {
            get { return (IEnumerable<double>)GetValue(TicksProperty); }
            set { SetValue(TicksProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="Ticks"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TicksProperty =
            DependencyProperty.Register("Ticks", typeof(IEnumerable<double>), typeof(Axis), new PropertyMetadata(new double[0]));

        /// <summary>
        /// Gets or sets the range of values on axis in plot coordinates.
        /// </summary>
        ///<remarks>
        /// <see cref="Range"/> should be inside Domain of <see cref="DataTransform"/>, otherwise exception will be thrown.
        ///</remarks>
        [Category("InteractiveDataDisplay")]
        [Description("Range of values on axis")]
        public Range Range
        {
            get { return (Range)GetValue(RangeProperty); }
            set { SetValue(RangeProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="Range"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RangeProperty =
            DependencyProperty.Register("Range", typeof(Range), typeof(Axis), new PropertyMetadata(new Range(0, 1),
                (o, e) =>
                {
                    Axis axis = (Axis)o;
                    if (axis != null)
                    {
                        axis.InvalidateMeasure();
                    }
                }));


        /// <summary>
        /// Gets or sets orientation of the axis and location of labels
        /// </summary>
        /// <remarks>The default AxisOrientation is AxisOrientation.Bottom</remarks>
        [Category("InteractiveDataDisplay")]
        [Description("Defines orientation of axis and location of labels")]
        public AxisOrientation AxisOrientation
        {
            get { return (AxisOrientation)GetValue(AxisOrientationProperty); }
            set { SetValue(AxisOrientationProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="AxisOrientation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AxisOrientationProperty =
            DependencyProperty.Register("AxisOrientation", typeof(AxisOrientation), typeof(Axis), new PropertyMetadata(AxisOrientation.Bottom,
                (o, e) =>
                {
                    Axis axis = (Axis)o;
                    if (axis != null)
                    {
                        axis.InvalidateMeasure();
                    }
                }));

        /// <summary>
        /// Gets or sets a flag indicating whether the axis is reversed or not.
        /// </summary>
        /// <remarks>Axis is not reversed by default.</remarks>
        [Category("InteractiveDataDisplay")]
        [Description("Defines orientation of axis and location of labels")]
        public bool IsReversed
        {
            get { return (bool)GetValue(IsReversedProperty); }
            set { SetValue(IsReversedProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="IsReversed"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReversedProperty =
            DependencyProperty.Register("IsReversed", typeof(bool), typeof(Axis), new PropertyMetadata(false,
                (o, e) =>
                {
                    Axis axis = (Axis)o;
                    if (axis != null)
                    {
                        axis.InvalidateMeasure();
                    }
                }));

        /// <summary>
        /// Gets or sets <see cref="DataTransform"/> for an axis.
        /// </summary>
        /// <remarks>
        /// The default transform is <see cref="IdentityDataTransform"/>
        /// DataTransform is used for transform plot coordinates from Range property to data values, which will be displayed on ticks
        /// </remarks>
        [Description("Transform from data to plot coordinates")]
        [Category("InteractiveDataDisplay")]
        public DataTransform DataTransform
        {
            get { return (DataTransform)GetValue(DataTransformProperty); }
            set { SetValue(DataTransformProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="DataTransform"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataTransformProperty =
            DependencyProperty.Register("DataTransform", typeof(DataTransform), typeof(Axis), new PropertyMetadata(null,
                (o, e) =>
                {
                    Axis axis = (Axis)o;
                    if (axis != null)
                    {
                        axis.InvalidateMeasure();
                    }
                }));

        /// <summary>
        /// Gets or sets the brush for labels and ticks of axis
        /// </summary>
        /// <remarks>The default foreground is black</remarks>
        [Category("Appearance")]
        [Description("Brush for labels and ticks")]
        public SolidColorBrush Foreground
        {
            get { return (SolidColorBrush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="Foreground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register("Foreground", typeof(SolidColorBrush), typeof(Axis), new PropertyMetadata(new SolidColorBrush(Colors.Black)));

        /// <summary>
        /// Gets or sets the maximum possible count of ticks.
        /// </summary>
        /// <remarks>The defalut count is 20</remarks>
        [Category("InteractiveDataDisplay")]
        [Description("Maximum number of major ticks")]
        public int MaxTicks
        {
            set
            {
                maxTicks = value;
                InvalidateMeasure();
            }
            get
            {
                return maxTicks;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the ticks should be rendered or not.
        /// </summary>
        /// <remarks>The default value is true.</remarks>
        [Category("InteractiveDataDisplay")]
        public bool AreTicksVisible
        {
            set
            {
                drawTicks = value;
                drawMinorTicks = value;
                if (drawTicks)
                    InvalidateMeasure();
            }
            get { return drawTicks; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the minor ticks should be rendered or not.
        /// </summary>
        /// <remarks>The default value is true.</remarks>
        [Category("InteractiveDataDisplay")]
        public bool AreMinorTicksVisible
        {
            set
            {
                drawMinorTicks = value;
                if (drawMinorTicks)
                    InvalidateMeasure();
            }
            get { return drawMinorTicks; }
        }

        private void CreateTicks(Size axisSize)
        {
            Range range = new Range(
                    DataTransform.PlotToData(Double.IsInfinity(Range.Min) || Double.IsNaN(Range.Min) ? 0 : Range.Min),
                    DataTransform.PlotToData(Double.IsInfinity(Range.Max) || Double.IsNaN(Range.Max) ? 1 : Range.Max));

            // One tick if range is point
            if (range.IsPoint)
            {
                var t = new double[] { range.Min };
                Ticks = t;
                labels = labelProvider.GetLabels(t);
                return;
            }

            // Do first pass of ticks arrangement
            ticksProvider.Range = range;
            double[] ticks = ticksProvider.GetTicks();
            labels = labelProvider.GetLabels(ticks);

            TickChange result;
            if (ticks.Length > MaxTicks)
                result = TickChange.Decrease;
            else if (ticks.Length < 2)
                result = TickChange.Increase;
            else
                result = CheckLabelsArrangement(axisSize, labels, ticks);

            int iterations = 0;
            int prevLength = ticks.Length;
            while (result != TickChange.OK && iterations++ < maxTickArrangeIterations)
            {
                if (result == TickChange.Increase)
                    ticksProvider.IncreaseTickCount();
                else
                    ticksProvider.DecreaseTickCount();
                double[] newTicks = ticksProvider.GetTicks();
                if (newTicks.Length > MaxTicks && result == TickChange.Increase)
                {
                    ticksProvider.DecreaseTickCount(); // Step back and stop to not get more than MaxTicks
                    break;
                }
                else if (newTicks.Length < 2 && result == TickChange.Decrease)
                {
                    ticksProvider.IncreaseTickCount(); // Step back and stop to not get less than 2
                    break;
                }
                var prevTicks = ticks;
                ticks = newTicks;
                var prevLabels = labels;
                labels = labelProvider.GetLabels(newTicks);
                var newResult = CheckLabelsArrangement(axisSize, labels, ticks);
                if (newResult == result) // Continue in the same direction
                {
                    prevLength = newTicks.Length;
                }
                else // Direction changed or layout OK - stop the loop
                {
                    if (newResult != TickChange.OK) // Direction changed - time to stop
                    {
                        if (result == TickChange.Decrease)
                        {
                            if (prevLength < MaxTicks)
                            {
                                ticks = prevTicks;
                                labels = prevLabels;
                                ticksProvider.IncreaseTickCount();
                            }
                        }
                        else
                        {
                            if (prevLength >= 2)
                            {
                                ticks = prevTicks;
                                labels = prevLabels;
                                ticksProvider.DecreaseTickCount();
                            }
                        }
                        break;
                    }
                    break;
                }
            }

            Ticks = ticks;
        }

        private void DrawCanvas(Size axisSize, double[] cTicks)
        {
            double length = IsHorizontal ? axisSize.Width : axisSize.Height;

            GeometryGroup majorTicksGeometry = new GeometryGroup();
            GeometryGroup minorTicksGeometry = new GeometryGroup();

            if (!Double.IsNaN(length) && length != 0)
            {
                int start = 0;
                if (cTicks.Length > 0 && cTicks[0] < Range.Min) start++;

                if (Range.IsPoint)
                    drawMinorTicks = false;

                for (int i = start; i < cTicks.Length; i++)
                {
                    LineGeometry line = new LineGeometry();
                    majorTicksGeometry.Children.Add(line);

                    if (IsHorizontal)
                    {
                        line.StartPoint = new Point(GetCoordinateFromTick(cTicks[i], axisSize), 0);
                        line.EndPoint = new Point(GetCoordinateFromTick(cTicks[i], axisSize), tickLength);
                    }
                    else
                    {
                        line.StartPoint = new Point(0, GetCoordinateFromTick(cTicks[i], axisSize));
                        line.EndPoint = new Point(tickLength, GetCoordinateFromTick(cTicks[i], axisSize));
                    }

                    if (labels[i] is TextBlock)
                    {
                        (labels[i] as TextBlock).Foreground = Foreground;
                    }
                    else if (labels[i] is Control)
                    {
                        (labels[i] as Control).Foreground = Foreground;
                    }

                    Children.Add(labels[i]);
                }

                if (drawMinorTicks)
                {
                    double[] minorTicks = ticksProvider.GetMinorTicks(Range);
                    if (minorTicks != null)
                    {
                        for (int j = 0; j < minorTicks.Length; j++)
                        {
                            LineGeometry line = new LineGeometry();
                            minorTicksGeometry.Children.Add(line);

                            if (IsHorizontal)
                            {
                                line.StartPoint = new Point(GetCoordinateFromTick(minorTicks[j], axisSize), 0);
                                line.EndPoint = new Point(GetCoordinateFromTick(minorTicks[j], axisSize), tickLength / 2.0);
                            }
                            else
                            {
                                line.StartPoint = new Point(0, GetCoordinateFromTick(minorTicks[j], axisSize));
                                line.EndPoint = new Point(tickLength / 2.0, GetCoordinateFromTick(minorTicks[j], axisSize));
                            }
                        }
                    }
                }

                majorTicksPath.Data = majorTicksGeometry;
                minorTicksPath.Data = minorTicksGeometry;

                if (!drawMinorTicks)
                    drawMinorTicks = true;
            }
        }

        /// <summary>
        /// Positions child elements and determines a size for a Figure.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that Figure should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            finalSize.Width = Math.Min(DesiredSize.Width, finalSize.Width);
            finalSize.Height = Math.Min(DesiredSize.Height, finalSize.Height);

            double labelArrangeOriginX = 0;
            double labelArrangeOriginY = 0;


            switch (AxisOrientation)
            {
                case AxisOrientation.Top:
                    majorTicksPath.Arrange(new Rect(0, finalSize.Height - tickLength, finalSize.Width, tickLength));
                    minorTicksPath.Arrange(new Rect(0, finalSize.Height - tickLength / 2.0, finalSize.Width, tickLength / 2.0));
                    break;
                case AxisOrientation.Bottom:
                    majorTicksPath.Arrange(new Rect(0, 0, finalSize.Width, tickLength));
                    minorTicksPath.Arrange(new Rect(0, 0, finalSize.Width, tickLength / 2.0));
                    break;
                case AxisOrientation.Right:
                    majorTicksPath.Arrange(new Rect(0, 0, tickLength, finalSize.Height));
                    minorTicksPath.Arrange(new Rect(0, 0, tickLength / 2.0, finalSize.Height));
                    break;
                case AxisOrientation.Left:
                    majorTicksPath.Arrange(new Rect(Math.Max(0, finalSize.Width - tickLength), 0, tickLength, finalSize.Height));
                    minorTicksPath.Arrange(new Rect(Math.Max(0, finalSize.Width - tickLength / 2.0), 0, tickLength / 2.0, finalSize.Height));
                    labelArrangeOriginX = finalSize.Width - tickLength - CalculateMaxLabelWidth();
                    break;
            }

            foreach (var label in labels)
            {
                label.Arrange(new Rect(labelArrangeOriginX, labelArrangeOriginY, label.DesiredSize.Width, label.DesiredSize.Height));
            }

            return finalSize;
        }

        /// <summary>
        /// Measures the size in layout required for child elements and determines a size for the Figure. 
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (Double.IsInfinity(availableSize.Width))
                availableSize.Width = 128;
            if (Double.IsInfinity(availableSize.Height))
                availableSize.Height = 128;


            Size effectiveSize = availableSize;


            ClearLabels();
            CreateTicks(effectiveSize);

            double[] cTicks = Ticks.ToArray();
            DrawCanvas(effectiveSize, cTicks);

            foreach (UIElement child in Children)
            {
                child.Measure(availableSize);
            }

            double maxLabelWidth = CalculateMaxLabelWidth();
            double maxLabelHeight = CalculateMaxLabelHeight();

            switch (AxisOrientation)
            {
                case AxisOrientation.Top:

                    for (int i = 0; i < labels.Length; i++)
                    {
                        labels[i].RenderTransform = new TranslateTransform { X = GetCoordinateFromTick(cTicks[i], effectiveSize) - labels[i].DesiredSize.Width / 2, Y = 0 };
                    }
                    break;
                case AxisOrientation.Bottom:
                    for (int i = 0; i < labels.Length; i++)
                    {
                        labels[i].RenderTransform = new TranslateTransform { X = GetCoordinateFromTick(cTicks[i], effectiveSize) - labels[i].DesiredSize.Width / 2, Y = majorTicksPath.DesiredSize.Height };
                    }
                    break;
                case AxisOrientation.Right:
                    for (int i = 0; i < labels.Length; i++)
                    {
                        labels[i].RenderTransform = new TranslateTransform { X = majorTicksPath.DesiredSize.Width, Y = GetCoordinateFromTick(cTicks[i], effectiveSize) - labels[i].DesiredSize.Height / 2 };
                    }
                    break;
                case AxisOrientation.Left:
                    for (int i = 0; i < labels.Length; i++)
                    {
                        labels[i].RenderTransform = new TranslateTransform { X = maxLabelWidth - labels[i].DesiredSize.Width, Y = GetCoordinateFromTick(cTicks[i], effectiveSize) - labels[i].DesiredSize.Height / 2 };
                    }
                    break;
            }

            if (IsHorizontal)
            {
                availableSize.Height = majorTicksPath.DesiredSize.Height + maxLabelHeight;
            }
            else
            {
                availableSize.Width = majorTicksPath.DesiredSize.Width + maxLabelWidth;
            }

            return availableSize;
        }

        private void ClearLabels()
        {
            if (labels != null)
            {
                foreach (var elt in labels)
                {
                    if (Children.Contains(elt))
                        Children.Remove(elt);
                }
            }
        }

        private double CalculateMaxLabelHeight()
        {
            if (labels != null)
            {
                double max = 0;
                for (int i = 0; i < labels.Length; i++)
                    if (Children.Contains(labels[i]) && labels[i].DesiredSize.Height > max)
                        max = labels[i].DesiredSize.Height;
                return max;
            }
            return 0;
        }

        private double CalculateMaxLabelWidth()
        {
            if (labels != null)
            {
                double max = 0;
                for (int i = 0; i < labels.Length; i++)
                    if (Children.Contains(labels[i]) && labels[i].DesiredSize.Width > max)
                        max = labels[i].DesiredSize.Width;
                return max;
            }
            return 0;
        }

        private TickChange CheckLabelsArrangement(Size axisSize, IEnumerable<FrameworkElement> inputLabels, double[] inputTicks)
        {
            var actualLabels1 = inputLabels.Select((label, i) => new { Label = label, Index = i });
            var actualLabels2 = actualLabels1.Where(el => el.Label != null);
            var actualLabels3 = actualLabels2.Select(el => new { Label = el.Label, Tick = inputTicks[el.Index] });
            var actualLabels = actualLabels3.ToList();

            actualLabels.ForEach(item => item.Label.Measure(axisSize));

            var sizeInfos = actualLabels.Select(item =>
                new { Offset = GetCoordinateFromTick(item.Tick, axisSize), Size = IsHorizontal ? item.Label.DesiredSize.Width : item.Label.DesiredSize.Height })
                .OrderBy(item => item.Offset).ToArray();

            // If distance between labels if smaller than threshold for any of the labels - decrease
            for (int i = 0; i < sizeInfos.Length - 1; i++)
                if ((sizeInfos[i].Offset + sizeInfos[i].Size * decreaseRatio / 2) > sizeInfos[i + 1].Offset)
                    return TickChange.Decrease;

            // If distance between labels if larger than threshold for all of the labels - increase
            TickChange res = TickChange.Increase;
            for (int i = 0; i < sizeInfos.Length - 1; i++)
            {
                if ((sizeInfos[i].Offset + sizeInfos[i].Size * increaseRatio / 2) > sizeInfos[i + 1].Offset)
                {
                    res = TickChange.OK;
                    break;
                }
            }

            return res;
        }

        private double GetCoordinateFromTick(double tick, Size screenSize)
        {
            return ValueToScreen(DataTransform.DataToPlot(tick), screenSize, Range);
        }

        private double ValueToScreen(double value, Size screenSize, Range range)
        {
            double leftBound, rightBound, topBound, bottomBound;
            leftBound = bottomBound = IsReversed ? range.Max : range.Min;
            rightBound = topBound = IsReversed ? range.Min : range.Max;

            if (IsHorizontal)
            {
                return range.IsPoint ?
                    (screenSize.Width / 2) :
                    ((value - leftBound) * screenSize.Width / (rightBound - leftBound));
            }
            else
            {
                return range.IsPoint ?
                    (screenSize.Height / 2) :
                    (screenSize.Height - (value - leftBound) * screenSize.Height / (rightBound - leftBound));
            }
        }

        /// <summary>
        /// Gets the value indcating whether the axis is horizontal or not.
        /// </summary>
        private bool IsHorizontal
        {
            get
            {
                return (AxisOrientation == AxisOrientation.Bottom || AxisOrientation == AxisOrientation.Top);
            }
        }
    }

    internal enum TickChange
    {
        Increase = -1,
        OK = 0,
        Decrease = 1
    }
}

