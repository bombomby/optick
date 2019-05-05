// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;

namespace InteractiveDataDisplay.WPF { 
    /// <summary>
    /// Performs transformations between data values and plot coordinates.
    /// </summary>
    public abstract class DataTransform : DependencyObject
    {
        /// <summary>Gets range of valid data values. <see cref="DataToPlot"/> method returns NaN for
        /// values outside this range. 
        /// </summary>
        public Range Domain { get; private set; }
        
        /// <summary>
        /// Initializes a new instance of <see cref="DataTransform"/> class.
        /// </summary>
        /// <param name="domain">A range of valid data.</param>
        protected DataTransform(Range domain)
        {
            Domain = domain;
        }

        /// <summary>
        /// Converts value from data to plot coordinates.
        /// </summary>
        /// <param name="dataValue">A value in data coordinates.</param>
        /// <returns>Value converted to plot coordinates or NaN if <paramref name="dataValue"/>
        /// falls outside of <see cref="Domain"/>.</returns>
        public abstract double DataToPlot(double dataValue);

        /// <summary>
        /// Converts value from plot coordinates to data.
        /// </summary>
        /// <param name="plotValue">A value in plot coordinates.</param>
        /// <returns>Value converted to data coordinates or NaN if no value in data coordinates
        /// matches <paramref name="plotValue"/>.</returns>
        public abstract double PlotToData(double plotValue);

        /// <summary>Identity transformation</summary>
        public static readonly DataTransform Identity = new IdentityDataTransform();
    }

    /// <summary>
    /// Provides identity transformation between data and plot coordinates.
    /// </summary>
    public class IdentityDataTransform : DataTransform
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IdentityDataTransform"/> class.
        /// </summary>
        public IdentityDataTransform()
            : base(new Range(double.MinValue, double.MaxValue))
        {
        }

        /// <summary>
        /// Returns a value in data coordinates without convertion.
        /// </summary>
        /// <param name="dataValue">A value in data coordinates.</param>
        /// <returns></returns>
        public override double DataToPlot(double dataValue)
        {
            return dataValue;
        }

        /// <summary>
        /// Returns a value in plot coordinates without convertion.
        /// </summary>
        /// <param name="plotValue">A value in plot coordinates.</param>
        /// <returns></returns>
        public override double PlotToData(double plotValue)
        {
            return plotValue;
        }
    }

    /// <summary>
    /// Represents a mercator transform, used in maps.
    /// Transforms y coordinates.
    /// </summary>
    public sealed class MercatorTransform : DataTransform
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MercatorTransform"/> class.
        /// </summary>
        public MercatorTransform()
            : base(new Range(-85, 85))
        {
            CalcScale(maxLatitude);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MercatorTransform"/> class.
        /// </summary>
        /// <param name="maxLatitude">The maximal latitude.</param>
        public MercatorTransform(double maxLatitude)
            : base(new Range(-maxLatitude, maxLatitude))
        {
            this.maxLatitude = maxLatitude;
            CalcScale(maxLatitude);
        }

        private void CalcScale(double inputMaxLatitude)
        {
            double maxLatDeg = inputMaxLatitude;
            double maxLatRad = maxLatDeg * Math.PI / 180;
            scale = maxLatDeg / Math.Log(Math.Tan(maxLatRad / 2 + Math.PI / 4));
        }

        private double scale;
        /// <summary>
        /// Gets the scale.
        /// </summary>
        /// <value>The scale.</value>
        public double Scale
        {
            get { return scale; }
        }

        private double maxLatitude = 85;
        /// <summary>
        /// Gets the maximal latitude.
        /// </summary>
        /// <value>The max latitude.</value>
        public double MaxLatitude
        {
            get { return maxLatitude; }
        }

        /// <summary>
        /// Converts value from mercator to plot coordinates.
        /// </summary>
        /// <param name="dataValue">A value in mercator coordinates.</param>
        /// <returns>Value converted to plot coordinates.</returns>
        public override double DataToPlot(double dataValue)
        {
            if (-maxLatitude <= dataValue && dataValue <= maxLatitude)
            {
                dataValue = scale * Math.Log(Math.Tan(Math.PI * (dataValue + 90) / 360));
            }
            return dataValue;
        }

        /// <summary>
        /// Converts value from plot to mercator coordinates.
        /// </summary>
        /// <param name="plotValue">A value in plot coordinates.</param>
        /// <returns>Value converted to mercator coordinates.</returns>
        public override double PlotToData(double plotValue)
        {
            if (-maxLatitude <= plotValue && plotValue <= maxLatitude)
            {
                double e = Math.Exp(plotValue / scale);
                plotValue = 360 * Math.Atan(e) / Math.PI - 90;
            }
            return plotValue;
        }
    }

    /// <summary>
    /// Provides linear transform u = <see cref="Scale"/> * d + <see cref="Offset"/> from data value d to plot coordinate u.
    /// </summary>
    public sealed class LinearDataTransform : DataTransform
    {
        /// <summary>
        /// Gets or sets the scale factor.
        /// </summary>
        public double Scale
        {
            get { return (double)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="Scale"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof(double), typeof(LinearDataTransform), new PropertyMetadata(1.0));

        /// <summary>
        /// Gets or sets the distance to translate an value.
        /// </summary>
        public double Offset
        {
            get { return (double)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="Offset"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.Register("Offset", typeof(double), typeof(LinearDataTransform), new PropertyMetadata(0.0));

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearDataTransform"/> class.
        /// </summary>
        public LinearDataTransform()
            : base(new Range(double.MinValue, double.MaxValue))
        {
        }

        /// <summary>
        /// Transforms a value according to defined <see cref="Scale"/> and <see cref="Offset"/>.
        /// </summary>
        /// <param name="dataValue">A value in data coordinates.</param>
        /// <returns>Transformed value.</returns>
        public override double DataToPlot(double dataValue)
        {
            return dataValue * Scale + Offset;
        }

        /// <summary>
        /// Returns a value in data coordinates from its transformed value.
        /// </summary>
        /// <param name="plotValue">Transformed value.</param>
        /// <returns>Original value or NaN if <see cref="Scale"/> is 0.</returns>
        public override double PlotToData(double plotValue)
        {
            if (Scale != 0)
            {
                return (plotValue - Offset) / Scale;
            }
            else
                return double.NaN;
        }
    }
}

