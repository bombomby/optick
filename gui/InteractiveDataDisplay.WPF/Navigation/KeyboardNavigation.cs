// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Provides keyboard navigation around viewport of parent <see cref="PlotBase"/>.
    /// </summary>
    /// <remarks>
    /// Place instance of <see cref="KeyboardNavigation"/> inside your <see cref="PlotBase"/> element to enable keyboard navigation over it.
    /// </remarks>
    [Description("Navigates parent plot by keyboard commands")]
    public class KeyboardNavigation : Control
    {
        private PlotBase masterPlot = null;

        /// <summary>
        /// Initializes a new instance of <see cref="KeyboardNavigation"/> class.
        /// </summary>
        public KeyboardNavigation()
        {
            Loaded += KeyboardNavigationLoaded;
            Unloaded += KeyboardNavigationUnloaded;

            KeyDown += KeyboardNavigationKeyDown;
            KeyUp += KeyboardNavigationKeyUp;
        }

        /// <summary>Gets or sets vertical navigation status. True means that user can navigate along Y axis</summary>
        /// <remarks>The default value is true</remarks>
        [Category("InteractiveDataDisplay")]
        public bool IsVerticalNavigationEnabled
        {
            get { return (bool)GetValue(IsVerticalNavigationEnabledProperty); }
            set { SetValue(IsVerticalNavigationEnabledProperty, value); }
        }

        /// <summary>Identifies <see cref="IsVerticalNavigationEnabled"/> property</summary>
        public static readonly DependencyProperty IsVerticalNavigationEnabledProperty =
            DependencyProperty.Register("IsVerticalNavigationEnabled", typeof(bool), typeof(KeyboardNavigation), new PropertyMetadata(true));

        /// <summary>Gets or sets horizontal navigation status. True means that user can navigate along X axis</summary>
        /// <remarks>The default value is true</remarks>
        [Category("InteractiveDataDisplay")]
        public bool IsHorizontalNavigationEnabled
        {
            get { return (bool)GetValue(IsHorizontalNavigationEnabledProperty); }
            set { SetValue(IsHorizontalNavigationEnabledProperty, value); }
        }

        /// <summary>Identifies <see cref="IsHorizontalNavigationEnabled"/> property</summary>
        public static readonly DependencyProperty IsHorizontalNavigationEnabledProperty =
            DependencyProperty.Register("IsHorizontalNavigationEnabled", typeof(bool), typeof(KeyboardNavigation), new PropertyMetadata(true));

        void KeyboardNavigationUnloaded(object sender, RoutedEventArgs e)
        {
            masterPlot = null;
        }

        void KeyboardNavigationLoaded(object sender, RoutedEventArgs e)
        {
            masterPlot = PlotBase.FindMaster(this);
        }

        private void KeyboardNavigationKeyDown(object sender, KeyEventArgs e)
        {
            if (masterPlot != null)
            {

                if (e.Key == Key.Up && IsVerticalNavigationEnabled)
                {
                    var rect = masterPlot.PlotRect;
                    double dy = rect.Height / 200;

                    masterPlot.SetPlotRect(new DataRect(
                        rect.XMin,
                        rect.YMin - dy,
                        rect.XMin + rect.Width,
                        rect.YMin - dy + rect.Height));

                    masterPlot.IsAutoFitEnabled = false;
                    e.Handled = true;
                }
                if (e.Key == Key.Down && IsVerticalNavigationEnabled)
                {
                    var rect = masterPlot.PlotRect;
                    double dy = - rect.Height / 200;

                    masterPlot.SetPlotRect(new DataRect(
                        rect.XMin,
                        rect.YMin - dy,
                        rect.XMin + rect.Width,
                        rect.YMin - dy + rect.Height));

                    masterPlot.IsAutoFitEnabled = false;
                    e.Handled = true;
                }
                if (e.Key == Key.Right && IsHorizontalNavigationEnabled)
                {
                    var rect = masterPlot.PlotRect;
                    double dx = - rect.Width / 200;

                    masterPlot.SetPlotRect(new DataRect(
                        rect.XMin + dx,
                        rect.YMin,
                        rect.XMin + dx + rect.Width,
                        rect.YMin + rect.Height));

                    masterPlot.IsAutoFitEnabled = false;
                    e.Handled = true;
                }
                if (e.Key == Key.Left && IsHorizontalNavigationEnabled)
                {
                    var rect = masterPlot.PlotRect;
                    double dx = rect.Width / 200;

                    masterPlot.SetPlotRect(new DataRect(
                        rect.XMin + dx,
                        rect.YMin,
                        rect.XMin + dx + rect.Width,
                        rect.YMin + rect.Height));

                    masterPlot.IsAutoFitEnabled = false;
                    e.Handled = true;
                }
                if (e.Key == Key.Subtract)
                {
                    DoZoom(1.2);
                    masterPlot.IsAutoFitEnabled = false;
                    e.Handled = true;
                }
                if (e.Key == Key.Add)
                {
                    DoZoom(1 / 1.2);
                    masterPlot.IsAutoFitEnabled = false;
                    e.Handled = true;
                }
                if (e.Key == Key.Home)
                {
                    masterPlot.IsAutoFitEnabled = true;
                    e.Handled = true;
                }
            }
        }

        private void DoZoom(double factor)
        {
            if (masterPlot != null)
            {
                var rect = masterPlot.PlotRect;

                if (IsHorizontalNavigationEnabled)
                    rect.X = rect.X.Zoom(factor);
                if (IsVerticalNavigationEnabled)
                    rect.Y = rect.Y.Zoom(factor);

                if (IsZoomEnable(rect))
                {
                    masterPlot.SetPlotRect(rect);
                    masterPlot.IsAutoFitEnabled = false;
                }
            }
        }

        private bool IsZoomEnable(DataRect rect)
        {
            bool res = true;
            if (IsHorizontalNavigationEnabled)
            {
                double e_max = Math.Log(Math.Max(Math.Abs(rect.XMax), Math.Abs(rect.XMin)), 2);
                double log = Math.Log(rect.Width, 2);
                res = log > e_max - 40;
            }
            if (IsVerticalNavigationEnabled)
            {
                double e_max = Math.Log(Math.Max(Math.Abs(rect.YMax), Math.Abs(rect.YMin)), 2);
                double log = Math.Log(rect.Height, 2);
                res = res && log > e_max - 40;
            }
            return res;
        }

        void KeyboardNavigationKeyUp(object sender, KeyEventArgs e)
        {
        }       
     }
}


