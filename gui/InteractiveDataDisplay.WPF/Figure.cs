// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Media;
using System.Linq;
using System.ComponentModel;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Specifies the dock position of a child element that is inside a <see cref="Figure"/> 
    /// </summary>
    public enum Placement
    {
        /// <summary>
        /// A child element that is positioned on the left side of the <see cref="Figure"/> 
        /// </summary>
        Left, 
        
        /// <summary>
        /// A child element that is positioned at the top of the <see cref="Figure"/> 
        /// </summary>
        Top, 
        
        /// <summary>
        /// A child element that is positioned on the right side of the <see cref="Figure"/>
        /// </summary>
        Right, 
        
        /// <summary>
        /// A child element that is positioned at the bottom of the <see cref="Figure"/>
        /// </summary>
        Bottom, 
        
        /// <summary>
        /// A child element that is positioned at the center of the <see cref="Figure"/>
        /// </summary>
        Center
    }

    /// <summary>
    /// Figure class provides special layout options that are often found in charts. 
    /// It provides attached property Placement that allows to place child elements in center, left, top, right and bottom slots. 
    /// Figure override plot-to-screen transform, so that plot coordinates are mapped to the cental part of the Figure.
    /// </summary>
    /// <remarks>
    /// Figure class provides two-pass algorithm that prevents well-known loop occurring on resize of figure with fixed aspect ratio: figure resize forces update of plot-to-screen transform which adjusts labels on the axes. 
    /// Change of label size may result in change of central part size which again updates plot-to-screen transform which in turn leads to axes label updates and so on.
    /// </remarks>
    [Description("Plot with center, left, top, right and bottom slots")]
    public class Figure : PlotBase
    {
        private double topHeight = 0;
        private double topHeight2 = 0;
        private double bottomHeight = 0;
        private double bottomHeight2 = 0;
        private double leftWidth = 0;
        private double leftWidth2 = 0;
        private double rightWidth = 0;
        private double rightWidth2 = 0;

        private Size centerSize = Size.Empty;

        /// <summary>
        /// Identify <see cref="Placement"/> attached property
        /// </summary>
        public static readonly DependencyProperty PlacementProperty = DependencyProperty.RegisterAttached(
            "Placement",
            typeof(Placement),
            typeof(Figure),
            new PropertyMetadata(Placement.Center, OnPlacementPropertyChanged));

        private static void OnPlacementPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement elt = sender as FrameworkElement;
            if (elt != null) // TODO: What if sender is not a FrameworkElement
            {
                UIElement parent = elt.Parent as UIElement;
                if (parent != null)
                    parent.InvalidateMeasure();
            }
        }

        /// <summary>Gets or sets extra padding added to effective padding that is computed from all plots in composition</summary>
        [Category("InteractiveDataDisplay")]
        public Thickness ExtraPadding
        {
            get { return (Thickness)GetValue(ExtraPaddingProperty); }
            set { SetValue(ExtraPaddingProperty, value); }
        }

        /// <summary>Identifies <see cref="ExtraPadding"/> dependency property</summary>
        public static readonly DependencyProperty ExtraPaddingProperty =
            DependencyProperty.Register("ExtraPadding", typeof(Thickness), typeof(Figure), new PropertyMetadata(new Thickness()));

        /// <summary>
        /// Computes padding with maximum values for each side from padding of all children
        /// </summary>
        /// <returns>Padding with maximum values for each side from padding of all children</returns>
        protected override Thickness AggregatePadding()
        {
            var ep = base.AggregatePadding();
            return new Thickness(
                ep.Left + ExtraPadding.Left, 
                ep.Top + ExtraPadding.Top, 
                ep.Right + ExtraPadding.Right,
                ep.Bottom + ExtraPadding.Bottom);
        }

        /// <summary>
        /// Measures the size in layout required for child elements and determines a size for the Figure. 
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            // Assuming we are master panel
            var topElts = Children.Cast<UIElement>().Where(elt => GetPlacement(elt) == Placement.Top);
            var bottomElts = Children.Cast<UIElement>().Where(elt => GetPlacement(elt) == Placement.Bottom);
            var centerElts = Children.Cast<UIElement>().Where(elt => GetPlacement(elt) == Placement.Center);
            var rightElts = Children.Cast<UIElement>().Where(elt => GetPlacement(elt) == Placement.Right);
            var leftElts = Children.Cast<UIElement>().Where(elt => GetPlacement(elt) == Placement.Left);

            DataRect desiredRect;
            if (IsAutoFitEnabled)
            {
                desiredRect = AggregateBounds();
                if (desiredRect.IsEmpty)
                    desiredRect = new DataRect(0, 0, 1, 1);
                SetPlotRect(desiredRect, true);
            }
            else // resize
                desiredRect = PlotRect;

            //First Iteration: Measuring top and bottom slots, 
            //then meassuring left and right with top and bottom output values

            // Create transform for first iteration
            if (double.IsNaN(availableSize.Width) || double.IsNaN(availableSize.Height) ||
                double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
                availableSize = new Size(100, 100);

            Fit(desiredRect, availableSize);

            // Measure top and bottom slots
            double topBottomWidth = 0;
            double topBottomHeight = 0;
            topHeight = 0;
            bottomHeight = 0;

            foreach (var elt in topElts)
            {
                elt.Measure(availableSize);
                var ds = elt.DesiredSize;
                topBottomWidth = Math.Max(topBottomWidth, ds.Width);
                topHeight += ds.Height;
            }
            topBottomHeight += topHeight;

            foreach (var elt in bottomElts)
            {
                elt.Measure(availableSize);
                var ds = elt.DesiredSize;
                topBottomWidth = Math.Max(topBottomWidth, ds.Width);
                bottomHeight += ds.Height;
            }
            topBottomHeight += bottomHeight;

            // Measure left and right slots
            double leftRightWidth = 0;
            double leftRightHeight = 0;
            leftWidth = 0;
            rightWidth = 0;

            foreach (var elt in leftElts)
            {

                elt.Measure(availableSize);
                var ds = elt.DesiredSize;
                leftRightHeight = Math.Max(leftRightHeight, ds.Height);
                leftWidth += ds.Width;
            }
            leftRightWidth += leftWidth;

            foreach (var elt in rightElts)
            {
                elt.Measure(availableSize);
                var ds = elt.DesiredSize;
                leftRightHeight = Math.Max(leftRightHeight, ds.Height);
                rightWidth += ds.Width;
            }
            leftRightWidth += rightWidth;

            //Measure center elements
            Size availCenterSize = new Size(Math.Max(0, availableSize.Width - leftRightWidth), Math.Max(0, availableSize.Height - topBottomHeight));
            Fit(desiredRect, availCenterSize);

            foreach (var elt in centerElts)
            {
                elt.Measure(availCenterSize);
            }

            // Remeasure top and bottom slots
            double topBottomWidth2 = 0;
            double topBottomHeight2 = 0;
            topHeight2 = 0;
            bottomHeight2 = 0;

            foreach (var elt in topElts)
            {
                elt.Measure(new Size(availCenterSize.Width, elt.DesiredSize.Height));
                var ds = elt.DesiredSize;
                topBottomWidth2 = Math.Max(topBottomWidth2, ds.Width);
                topHeight2 += ds.Height;
            }
            topBottomHeight2 += topHeight2;

            foreach (var elt in bottomElts)
            {
                elt.Measure(new Size(availCenterSize.Width, elt.DesiredSize.Height));
                var ds = elt.DesiredSize;
                topBottomWidth2 = Math.Max(topBottomWidth2, ds.Width);
                bottomHeight2 += ds.Height;
            }
            topBottomHeight2 += bottomHeight2;


            //Scaling elements of their new meassured height it not equal to first
            if (bottomHeight2 > bottomHeight)
            {
                ScaleTransform transform = new ScaleTransform { ScaleY = bottomHeight / bottomHeight2 };
                foreach (var elt in bottomElts)
                {
                    elt.RenderTransform = transform;
                }
            }

            if (topHeight2 > topHeight)
            {
                ScaleTransform transform = new ScaleTransform { ScaleY = topHeight / topHeight2 };
                foreach (var elt in topElts)
                {
                    elt.RenderTransform = transform;
                }
            }

            // ReMeasure left and right slots
            double leftRightWidth2 = 0;
            double leftRightHeight2 = 0;
            leftWidth2 = 0;
            rightWidth2 = 0;

            foreach (var elt in leftElts)
            {
                elt.Measure(new Size(elt.DesiredSize.Width, availCenterSize.Height));
                var ds = elt.DesiredSize;
                leftRightHeight2 = Math.Max(leftRightHeight2, ds.Height);
                leftWidth2 += ds.Width;
            }
            leftRightWidth2 += leftWidth2;

            foreach (var elt in rightElts)
            {
                elt.Measure(new Size(elt.DesiredSize.Width, availCenterSize.Height));
                var ds = elt.DesiredSize;
                leftRightHeight2 = Math.Max(leftRightHeight2, ds.Height);
                rightWidth2 += ds.Width;
            }
            leftRightWidth2 += rightWidth2;

            //Scaling elements of their new meassured height it not equal to first
            if (leftWidth2 > leftWidth)
            {
                ScaleTransform transform = new ScaleTransform { ScaleX = leftWidth / leftWidth2 };
                foreach (var elt in leftElts)
                {
                    elt.RenderTransform = transform;
                }
            }

            if (rightWidth2 > rightWidth)
            {
                ScaleTransform transform = new ScaleTransform { ScaleX = rightWidth / rightWidth2 };
                foreach (var elt in rightElts)
                {
                    elt.RenderTransform = transform;
                }
            }

            centerSize = availCenterSize;
            return new Size(availCenterSize.Width + leftRightWidth, availCenterSize.Height + topBottomHeight);
        }

        /// <summary>
        /// Positions child elements and determines a size for a Figure
        /// </summary>
        /// <param name="finalSize">The final area within the parent that Figure should use to arrange itself and its children</param>
        /// <returns>The actual size used</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var topElts = Children.Cast<UIElement>().Where(elt => GetPlacement(elt) == Placement.Top).ToArray();
            var bottomElts = Children.Cast<UIElement>().Where(elt => GetPlacement(elt) == Placement.Bottom).ToArray();
            var centerElts = Children.Cast<UIElement>().Where(elt => GetPlacement(elt) == Placement.Center);
            var rightElts = Children.Cast<UIElement>().Where(elt => GetPlacement(elt) == Placement.Right).ToArray();
            var leftElts = Children.Cast<UIElement>().Where(elt => GetPlacement(elt) == Placement.Left).ToArray();

            double x = 0, y = 0;

            //Arranging top elements and setting clip bounds
            if (topHeight < topHeight2)
            {
                foreach (var elt in topElts)
                {
                    double finalHeight = elt.DesiredSize.Height * topHeight / topHeight2;
                    elt.Arrange(new Rect(leftWidth, y, centerSize.Width, finalHeight));
                    elt.Clip = new RectangleGeometry { Rect = new Rect(-leftWidth, 0, finalSize.Width, finalHeight) };
                    y += finalHeight;
                }
            }
            else
            {
                double iy = topHeight;
                for (int i = topElts.Length - 1; i > -1; i--)
                {
                    UIElement elt = topElts[i];
                    elt.Arrange(new Rect(leftWidth, iy - elt.DesiredSize.Height, centerSize.Width, elt.DesiredSize.Height));
                    elt.Clip = new RectangleGeometry { Rect = new Rect(-leftWidth, 0, finalSize.Width, elt.DesiredSize.Height) };
                    iy -= elt.DesiredSize.Height;
                }
                y = topHeight;
            }

            // Arranging left elements and setting clip bounds
            if (leftWidth < leftWidth2)
            {
                foreach (var elt in leftElts)
                {
                    double finalWidth = elt.DesiredSize.Width * leftWidth / leftWidth2;
                    elt.Arrange(new Rect(x, topHeight, finalWidth, centerSize.Height));
                    elt.Clip = new RectangleGeometry { Rect = new Rect(0, -topHeight, finalWidth, finalSize.Height) };
                    x += finalWidth;
                }
            }
            else
            {
                double ix = leftWidth;
                for (int i = leftElts.Length - 1; i > -1; i--)
                {
                    UIElement elt = leftElts[i];
                    elt.Arrange(new Rect(ix - elt.DesiredSize.Width, topHeight, elt.DesiredSize.Width, centerSize.Height));
                    elt.Clip = new RectangleGeometry { Rect = new Rect(0, -topHeight, elt.DesiredSize.Width, finalSize.Height) };
                    ix -= elt.DesiredSize.Width;
                }
                x = leftWidth;
            }

            // Arranging center elements
            foreach (var elt in centerElts)
            {
                elt.Arrange(new Rect(leftWidth, topHeight, centerSize.Width, centerSize.Height));
            }

            x += centerSize.Width;
            y += centerSize.Height;

            // Arranging bottom elements and setting clip bounds
            if (bottomHeight < bottomHeight2)
            {
                foreach (var elt in bottomElts)
                {
                    double finalHeight = elt.DesiredSize.Height * bottomHeight / bottomHeight2;
                    elt.Arrange(new Rect(leftWidth, y, centerSize.Width, finalHeight));
                    elt.Clip = new RectangleGeometry { Rect = new Rect(-leftWidth, 0, finalSize.Width, finalHeight) };
                    y += finalHeight;
                }
            }
            else
            {
                for (int i = bottomElts.Length - 1; i > -1; i--)
                {
                    UIElement elt = bottomElts[i];
                    elt.Arrange(new Rect(leftWidth, y, centerSize.Width, elt.DesiredSize.Height));
                    elt.Clip = new RectangleGeometry { Rect = new Rect(-leftWidth, 0, finalSize.Width, elt.DesiredSize.Height) };
                    y += elt.DesiredSize.Height;
                }
            }

            // Arranging right elements and setting clip bounds
            if (rightWidth < rightWidth2)
            {
                foreach (var elt in rightElts)
                {
                    double finalWidth = elt.DesiredSize.Width * rightWidth / rightWidth2;
                    elt.Arrange(new Rect(x, topHeight, finalWidth, centerSize.Height));
                    elt.Clip = new RectangleGeometry { Rect = new Rect(0, -topHeight, finalWidth, finalSize.Height) };
                    x += finalWidth;
                }
            }
            else
            {
                for (int i = rightElts.Length - 1; i > -1; i--)
                {
                    UIElement elt = rightElts[i];
                    elt.Arrange(new Rect(x, topHeight, elt.DesiredSize.Width, centerSize.Height));
                    elt.Clip = new RectangleGeometry { Rect = new Rect(0, -topHeight, elt.DesiredSize.Width, finalSize.Height) };
                    x += elt.DesiredSize.Width;
                }
            }

            return new Size(x, y);
        }

        /// <summary>
        /// Sets the value of the <see cref="Placement"/> attached property to a specified element. 
        /// </summary>
        /// <param name="element">The element to which the attached property is written.</param>
        /// <param name="placement">The needed <see cref="Placement"/> value.</param>
        public static void SetPlacement(DependencyObject element, Placement placement)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            element.SetValue(PlacementProperty, placement);
        }

        /// <summary>
        /// Gets the value of the <see cref="Placement"/> attached property to a specified element. 
        /// </summary>
        /// <param name="element">The element from which the property value is read.</param>
        /// <returns>The <see cref="Placement"/> property value for the element.</returns>
        /// <remarks>The default value of FigurePlacement property is Placement.Center</remarks>
        public static Placement GetPlacement(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            return (Placement)element.GetValue(PlacementProperty);
        }
    }

}

