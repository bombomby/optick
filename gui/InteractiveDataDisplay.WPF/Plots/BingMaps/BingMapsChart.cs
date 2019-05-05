// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Maps.MapControl.WPF;
using System.Windows.Input;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Control for plotting data in geographical coordinates over Bing Maps.
    /// This control should be put into Children collection of Bing Maps MapControl. Plot coordinates
    /// (x,y) are treated as longitude and latitude.
    /// </summary>
    public class BingMapsPlot : Plot
    {
        Map parentMap = null;

        /// <summary>Canvas spreading from (-85,-180) to (85, 180)</summary>
        Canvas entireWorld = new Canvas();

        /// <summary>
        /// Initializes a new instance of <see cref="BingMapsPlot"/> class. Assigns
        /// instance of <see cref="MercatorTransform"/> to <see cref="DataTransform"/> property.
        /// </summary>
        public BingMapsPlot()
        {
            YDataTransform = new MercatorTransform();
            IsAutoFitEnabled = false;
            ClipToBounds = false;
            Loaded += new RoutedEventHandler(MapPlotter2D_Loaded);
            Unloaded += new RoutedEventHandler(MapPlotter2D_Unloaded);
        }
        
        void MapPlotter2D_Loaded(object sender, RoutedEventArgs e)
        {
            parentMap = GetParentMap();
            if (parentMap != null)
            {
                parentMap.Children.Add(entireWorld);
                MapLayer.SetPositionRectangle(entireWorld,
                    new LocationRect(new Location(-85, -180), new Location(85, 180)));
                parentMap.ViewChangeEnd += new EventHandler<MapEventArgs>(parentMap_ViewChangeEnd);
                MapLayer.SetPositionRectangle(this,
                    new LocationRect(new Location(-85, -180), new Location(85, 180)));
                SetPlotRect(new DataRect(-180, YDataTransform.DataToPlot(-85), 180, YDataTransform.DataToPlot(85)));
                entireWorld.LayoutUpdated += WorldLayoutUpdated;
            }
        }

        void WorldLayoutUpdated(object sender, EventArgs e)
        {
            UpdatePlotRect();
            entireWorld.LayoutUpdated -= WorldLayoutUpdated;
        }

        void MapPlotter2D_Unloaded(object sender, RoutedEventArgs e)
        {
            if (parentMap != null)
            {
                parentMap.Children.Remove(entireWorld);
                parentMap.ViewChangeEnd -= parentMap_ViewChangeEnd;
            }
        }

        void parentMap_ViewChangeEnd(object sender, MapEventArgs e)
        {
            UpdatePlotRect();
        }

        private void UpdatePlotRect()
        {
            parentMap = GetParentMap();
            var transform = entireWorld.TransformToVisual(parentMap);
            var lt = transform.Transform(new Point(0, 0));
            var rb = transform.Transform(new Point(entireWorld.RenderSize.Width, entireWorld.RenderSize.Height));

            var sw = parentMap.ViewportPointToLocation(new Point(Math.Max(0, lt.X), Math.Min(parentMap.RenderSize.Height, rb.Y)));
            var ne = parentMap.ViewportPointToLocation(new Point(Math.Min(parentMap.RenderSize.Width, rb.X), Math.Max(0, lt.Y)));
            if (lt.X > 0)
                sw.Longitude = -180;
            if (rb.X < parentMap.RenderSize.Width)
                ne.Longitude = 180;
            var newPlotRect = new DataRect(sw.Longitude, YDataTransform.DataToPlot(sw.Latitude),
                ne.Longitude, YDataTransform.DataToPlot(ne.Latitude));
            if(Math.Abs(newPlotRect.XMin - PlotOriginX) > 1e-10 ||
               Math.Abs(newPlotRect.YMin - PlotOriginY) > 1e-10 ||
               Math.Abs(newPlotRect.XMax - PlotOriginX - PlotWidth) > 1e-10 ||
               Math.Abs(newPlotRect.YMax - PlotOriginY - PlotHeight) > 1e-10)
            {
                 MapLayer.SetPositionRectangle(this, new LocationRect(sw, ne));
                 SetPlotRect(newPlotRect);
            }
        }

        [CLSCompliantAttribute(false)]
        public Map GetParentMap ()
        {
            return this.Tag as Map;
        }
        protected override Thickness AggregatePadding()
        {
            return new Thickness();
        }
    }  
}

