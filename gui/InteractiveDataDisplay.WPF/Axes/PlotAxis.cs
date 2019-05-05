// Copyright © Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>Facilitates drawing of vertical or horizontal coordinate axis connected to parent <see cref="Plot"/> element</summary>
    [TemplatePart(Name = "PART_Axis", Type = typeof(Axis))]
    [Description("Figure's axis")]
    public sealed class PlotAxis : ContentControl
    {
        private PlotBase masterPlot = null;
        private Axis axis;

        /// <summary>
        /// Initializes new instance of <see cref="PlotAxis"/> class
        /// </summary>
        public PlotAxis()
        {
            XDataTransform = new IdentityDataTransform();
            YDataTransform = new IdentityDataTransform();
            Ticks = new double[0];
            DefaultStyleKey = typeof(PlotAxis);
            Loaded += PlotAxisLoaded;
            Unloaded += PlotAxisUnloaded;
        }

        private void PlotAxisUnloaded(object sender, RoutedEventArgs e)
        {
            masterPlot = null;
        }

        private void PlotAxisLoaded(object sender, RoutedEventArgs e)
        {
            masterPlot = PlotBase.FindMaster(this);
            if (masterPlot != null)
                InvalidateAxis();
        }

        /// <summary>
        /// Measures the size in layout required for child elements and determines a size for parent. 
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            InvalidateAxis();
            return base.MeasureOverride(availableSize);
        }

        private void InvalidateAxis()
        {
            if (axis != null)
            {
                if (axis.AxisOrientation == AxisOrientation.Left ||
                    axis.AxisOrientation == AxisOrientation.Right)
                {
                    if (masterPlot != null)
                    {
                        axis.Range = new Range(masterPlot.ActualPlotRect.YMin, masterPlot.ActualPlotRect.YMax);
                    }
                    axis.DataTransform = YDataTransform;
                }
                else
                {
                    if (masterPlot != null)
                    {
                        axis.Range = new Range(masterPlot.ActualPlotRect.XMin, masterPlot.ActualPlotRect.XMax);
                    }
                    axis.DataTransform = XDataTransform;
                }
            }
        }

        /// <summary>
        /// Invoked whenever application code or internal processes call ApplyTemplate
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            axis = GetTemplateChild("PART_Axis") as Axis;
            if (axis == null)
            {
                throw new InvalidOperationException("Invalid template!");
            }
            BindingOperations.SetBinding(this.axis, Axis.TicksProperty, new Binding("Ticks")
            {
                Source = this,
                Mode = BindingMode.TwoWay
            });
            InvalidateAxis();
        }

        /// <summary>
        /// Gets or sets a set of double values displayed as axis ticks.
        /// </summary>
        [Browsable(false)]
        public IEnumerable<double> Ticks
        {
            get { return (IEnumerable<double>)GetValue(TicksProperty); }
            set { SetValue(TicksProperty, value); }
        }

        /// <summary>Identify <see cref="Ticks"/> property</summary>
        public static readonly DependencyProperty TicksProperty =
            DependencyProperty.Register("Ticks", typeof(IEnumerable<double>), typeof(PlotAxis), new PropertyMetadata(new double[0]));

        /// <summary>
        /// Gets or Sets orientation of axis and location of labels
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Defines orientation of axis and location of labels")]
        public AxisOrientation AxisOrientation
        {
            get { return (AxisOrientation)GetValue(AxisOrientationProperty); }
            set { SetValue(AxisOrientationProperty, value); }
        }

        /// <summary>Identify <see cref="AxisOrientation"/> property</summary>
        public static readonly DependencyProperty AxisOrientationProperty =
            DependencyProperty.Register("AxisOrientation", typeof(AxisOrientation), typeof(PlotAxis), new PropertyMetadata(AxisOrientation.Bottom));

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
            DependencyProperty.Register("IsReversed", typeof(bool), typeof(PlotAxis), new PropertyMetadata(false));

        /// <summary>Gets or sets transform from user data to horizontal plot coordinate. 
        /// By default transform is <see cref="IdentityDataTransform"/>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public DataTransform XDataTransform
        {
            get { return (DataTransform)GetValue(XDataTransformProperty); }
            set { SetValue(XDataTransformProperty, value); }
        }

        /// <summary>Gets or sets transform from user data to vertical plot coordinate. 
        /// By default transform is <see cref="IdentityDataTransform"/>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public DataTransform YDataTransform
        {
            get { return (DataTransform)GetValue(YDataTransformProperty); }
            set { SetValue(YDataTransformProperty, value); }
        }

        /// <summary>Identify <see cref="XDataTransform"/> property</summary>
        public static readonly DependencyProperty XDataTransformProperty =
            DependencyProperty.Register("XDataTransform", typeof(DataTransform), typeof(PlotAxis), new PropertyMetadata(null,
                (o, e) =>
                {
                    PlotAxis plot = o as PlotAxis;
                    if (plot != null)
                    {
                        plot.InvalidateAxis();
                    }
                }));

        /// <summary>Identify <see cref="YDataTransform"/> property</summary>
        public static readonly DependencyProperty YDataTransformProperty =
            DependencyProperty.Register("YDataTransform", typeof(DataTransform), typeof(PlotAxis), new PropertyMetadata(null,
                (o, e) =>
                {
                    PlotAxis plot = o as PlotAxis;
                    if (plot != null)
                    {
                        plot.InvalidateAxis();
                    }
                }));
    }
}

