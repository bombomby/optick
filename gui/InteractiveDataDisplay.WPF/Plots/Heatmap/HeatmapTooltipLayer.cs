// Copyright © Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

//// Copyright © Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.
//
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Threading;
using System.ComponentModel;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Control for displaying tooltips for <see cref="HeatmapGraph"/>. 
    /// Shows a value of heatmap in specified point.
    /// </summary>
    [Description("Displays value of heatmap under cursor")]
    public class HeatmapTooltipLayer : Panel
    {
        private Point location;
        private Object content;
        private DispatcherTimer dispatcherTimer;
        private ToolTip toolTip = new ToolTip();
        private TimeSpan dueTime = new TimeSpan(0, 0, 1);
        private TimeSpan durationInterval = new TimeSpan(0, 0, 7);
        private IDisposable subscription;
        private Dictionary<HeatmapGraph, IDisposable> heatmapSubscriptions = new Dictionary<HeatmapGraph,IDisposable>();
        private PlotBase parent = null;

        /// <summary>
        /// Initializes new instance of <see cref="HeatmapTooltipLayer"/> class.
        /// </summary>
        public HeatmapTooltipLayer()
        {
            ContentFunc = DefaultContentFunc;
            this.toolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            this.toolTip.VerticalOffset = 10;
            this.toolTip.HorizontalOffset = 5;
            this.MouseMove += new MouseEventHandler(OnMouseMove);
            this.MouseLeave += new MouseEventHandler(TooltipLayer_MouseLeave);
            this.toolTip.IsOpen = false;
            
            this.dispatcherTimer = new DispatcherTimer();
            this.dispatcherTimer.Interval = dueTime;
            this.dispatcherTimer.Tick += OnTick;

            this.Loaded += new RoutedEventHandler(HeatmapTooltipLayer_Loaded);
            this.Unloaded += new RoutedEventHandler(HeatmapTooltipLayer_Unloaded);
        }

        void HeatmapTooltipLayer_Loaded(object sender, RoutedEventArgs e)
        {
            var visualParent = VisualTreeHelper.GetParent(this);
            parent = visualParent as PlotBase;
            while(visualParent != null && parent == null)
            {
                visualParent = VisualTreeHelper.GetParent(visualParent);
                parent = visualParent as PlotBase;
            }
            if (parent != null)
            {
                parent.MouseMove += new MouseEventHandler(OnMouseMove);
                parent.MouseLeave += new MouseEventHandler(TooltipLayer_MouseLeave);
                subscription = parent.CompositionChange.Subscribe(
                    next =>
                    {
                        RefreshTooltip();
                        heatmapSubscriptions.Clear();
                        foreach (UIElement elem in parent.RelatedPlots)
                        {
                            var heat = elem as HeatmapGraph;
                            if (heat != null)
                            {
                                heatmapSubscriptions.Add(heat, heat.RenderCompletion.Subscribe(
                                    render =>
                                    {
                                        RefreshTooltip();
                                    }));
                            }
                        }
                    });
            }
        }

        void HeatmapTooltipLayer_Unloaded(object sender, RoutedEventArgs e)
        {
            subscription.Dispose();
            foreach (IDisposable s in heatmapSubscriptions.Values)
            {
                s.Dispose();
            }
            heatmapSubscriptions.Clear();
        }

        private void RefreshTooltip()
        {
            object result = ContentFunc(this.location);
            if (result == null)
            {
                toolTip.Content = null;
                toolTip.IsOpen = false;
                return;
            }
            toolTip.Content = result;
        }

        private void TooltipLayer_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Hide();
        }

        /// <summary>
        /// Gets or sets the tooltip content.
        /// </summary>
        [Browsable(false)]
        public Object Content
        {
            get { return content; }
            set { content = value; }
        }

        /// <summary>
        /// Tooltip content function.
        /// </summary>
        [Browsable(false)]
        public Func<Point, Object> ContentFunc
        {
            get;
            set;
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            this.location = e.GetPosition(this);
            if (toolTip.IsOpen)
            {
                this.Hide();
            }

            if (dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
            }

            dispatcherTimer.Interval = dueTime;
            dispatcherTimer.Start();
        }

        void OnTick(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();

            bool show = !toolTip.IsOpen;
            object result = ContentFunc(location);
            if (result == null)
            {
                return;
            }
            toolTip.Content = result;
            toolTip.IsOpen = show;
            if (show)
                HideDelayed(durationInterval);
        }

        /// <summary>
        /// Default function for building tooltip content.
        /// </summary>
        /// <param name="pt">Point in which the value is requied.</param>
        /// <returns>A content for tooltip for specified point.</returns>
        public virtual object DefaultContentFunc(Point pt)
        {
            if (parent == null)
                return null;
            StackPanel contentPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
            };
            foreach (UIElement elem in parent.RelatedPlots)
            {
                if (elem is HeatmapGraph)
                {
                    ITooltipProvider el = elem as ITooltipProvider;
                    if (el != null)
                    {
                        var control = new ContentControl();
                        control.IsTabStop = false;
                        if (el.TooltipContentFunc != null)
                        {
                            var internalContent = el.TooltipContentFunc(pt);
                            if (internalContent != null)
                            {
                                control.Content = internalContent;
                                contentPanel.Children.Add(control);
                            }
                        }
                    }
                }
            }
            return contentPanel.Children.Count > 0 ? contentPanel : null;
        }

        private void Hide()
        {
            if (dispatcherTimer.IsEnabled)
                dispatcherTimer.Stop();
            toolTip.IsOpen = false;
        }


        private void HideDelayed(TimeSpan delay)
        {
            if (dispatcherTimer.IsEnabled)
                dispatcherTimer.Stop();

            dispatcherTimer.Interval = delay;
            dispatcherTimer.Start();
        }
    }

    /// <summary>
    /// Interface for elements which can provide content for tooltips.
    /// </summary>
    public interface ITooltipProvider
    {
        /// <summary>
        /// Tooltip content generation function.
        /// </summary>
        Func<Point, object> TooltipContentFunc { get; }
    }
}


