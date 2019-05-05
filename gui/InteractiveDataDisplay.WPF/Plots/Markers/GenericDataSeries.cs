// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Diagnostics;
using System.ComponentModel;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Data series to define the color of markers. 
    /// Converts value to brush (using <see cref="PaletteConverter"/>) and provides properties to control this convertion.
    /// </summary>
    public class ColorSeries : DataSeries
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorSeries"/> class.
        /// <para>The key for this data series is "C", default converter is <see cref="PaletteConverter"/>,
        /// default value is black color.</para>
        /// </summary>
        public ColorSeries()
        {
            this.Key = "C";
            this.Description = "Color of the points";
            this.Data = "Black";
            this.Converter = new PaletteConverter();
            this.DataChanged += new EventHandler(ColorSeries_DataChanged);
        }

        void ColorSeries_DataChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.Data != null)
                {
                    if (Palette.IsNormalized)
                    {
                        if (double.IsNaN(this.MinValue) || double.IsNaN(this.MaxValue))
                            PaletteRange = Range.Empty;
                        else
                            PaletteRange = new Range(this.MinValue, this.MaxValue);
                    }
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Wrong value in ColorSeries: " + exc.Message);
            }
        }

        #region Palette
        /// <summary>
        /// Identifies the <see cref="Palette"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaletteProperty =
            DependencyProperty.Register("Palette",
                typeof(IPalette),
                typeof(ColorSeries),
                new PropertyMetadata(InteractiveDataDisplay.WPF.Palette.Heat, (s,a) => ((ColorSeries)s).OnPalettePropertyChanged(a)));

        private void OnPalettePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            Palette newPalette = (Palette)e.NewValue;
            (Converter as PaletteConverter).Palette = newPalette;
            if (newPalette.IsNormalized && Data != null)
            {
                if (double.IsNaN(MinValue) || double.IsNaN(MaxValue))
                    PaletteRange = Range.Empty;
                else
                    PaletteRange = new Range(MinValue, MaxValue);
            }
            else if (!newPalette.IsNormalized)
                PaletteRange = new Range(newPalette.Range.Min, newPalette.Range.Max);
            RaiseDataChanged();
        }

        /// <summary>
        /// Gets or sets the color palette for markers. Defines mapping of values to colors.
        /// Is used only if the data is of any numeric type.
        /// <para>Default value is heat palette.</para>
        /// </summary>
        [TypeConverter(typeof(StringToPaletteTypeConverter))]
        public IPalette Palette
        {
            get { return (IPalette)GetValue(PaletteProperty); }
            set { SetValue(PaletteProperty, value); }
        }
        #endregion
        #region PaletteRange
        /// <summary>
        /// Identifies the <see cref="PaletteRange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaletteRangeProperty =
            DependencyProperty.Register("PaletteRange",
                typeof(Range),
                typeof(ColorSeries),
                new PropertyMetadata(new Range(0, 1), null));     
        /// <summary>
        /// Gets or sets the range which is displayed in legend if <see cref="ColorSeries.Palette"/> is normalized.
        /// Otherwise this property is ignored.
        /// <para>Default value is (0, 1).</para>
        /// </summary>
        public Range PaletteRange
        {
            get { return (Range)GetValue(PaletteRangeProperty); }
            set { SetValue(PaletteRangeProperty, value); }
        }
        #endregion
    }
    /// <summary>
    /// Data series to define the size of markers. 
    /// Provides properties to set minimum and maximum sizes of markers to display.
    /// </summary>
    public class SizeSeries : DataSeries
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SizeSeries"/> class.
        /// The key for this data series is "D", default value is 10.
        /// </summary>
        public SizeSeries()
        {
            this.Key = "D";
            this.Description = "Size of the points";
            this.Data = 10;
            this.DataChanged += new EventHandler(SizeSeries_DataChanged);
        }
        void SizeSeries_DataChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.Data != null)
                {
                    Range range = Range.Empty;
                    if (!double.IsNaN(this.MinValue) && !double.IsNaN(this.MaxValue))
                        range = new Range(this.MinValue, this.MaxValue);

                    if (this.Converter != null)
                        (this.Converter as ResizeConverter).Origin = range;
                    Range = range;
                    First = FindFirstDataItem();
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Wrong value in SizeSeries: " + exc.Message);
            }
        }
        #region Range
        /// <summary>
        /// Identifies the <see cref="Range"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RangeProperty =
            DependencyProperty.Register("Range",
                typeof(Range),
                typeof(SizeSeries),
                new PropertyMetadata(new Range(0, 1), null));

        /// <summary>
        /// Gets the actual size range of markers.
        /// </summary>
        public Range Range
        {
            get { return (Range)GetValue(RangeProperty); }
            internal set { SetValue(RangeProperty, value); }
        }
        #endregion
        #region Min, Max
        /// <summary>
        /// Identifies the <see cref="Min"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinProperty =
            DependencyProperty.Register("Min",
                typeof(double),
                typeof(SizeSeries),
                new PropertyMetadata(Double.NaN, OnMinMaxPropertyChanged));
        /// <summary>
        /// Gets or sets the minimum of size of markers to draw.
        /// <para>Default value is Double.NaN.</para>
        /// </summary>
        public double Min
        {
            get { return (double)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="Max"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxProperty =
            DependencyProperty.Register("Max",
                typeof(double),
                typeof(SizeSeries),
                new PropertyMetadata(Double.NaN, OnMinMaxPropertyChanged));
        /// <summary>
        /// Gets or sets the maximum of size of markers to draw.
        /// <para>Default value is Double.NaN.</para>
        /// </summary>
        public double Max
        {
            get { return (double)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }
        private static void OnMinMaxPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SizeSeries m = sender as SizeSeries;
            if (Double.IsNaN(m.Min) || Double.IsNaN(m.Max))
                m.Converter = null;
            else
            {
                if (m.Converter == null)
                {
                    if (m.Data != null)
                        m.Converter = new ResizeConverter(new Range(m.MinValue, m.MaxValue),
                                                          new Range(m.Min, m.Max));
                    else
                        m.Converter = new ResizeConverter(new Range(0, 1),
                                                          new Range(m.Min, m.Max));
                }
                else
                    (m.Converter as ResizeConverter).Resized = new Range(m.Min, m.Max);
            }
            m.RaiseDataChanged();
        }
        #endregion
    }
}