// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// A control to show all the information about <see cref="Palette"/>.
    /// </summary>
    [Description("Visually maps value to color")]
    public class PaletteControl : ContentControl
    {
        #region Fields

        private Image image;
        private Axis axis;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the palette.
        /// Default value is black palette.
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [TypeConverter(typeof(StringToPaletteTypeConverter))] 
        public Palette Palette
        {
            get { return (Palette)GetValue(PaletteProperty); }
            set { SetValue(PaletteProperty, value); }
        }

        /// <summary>
        /// Gets or sets the range of axis values.
        /// Default value is [0, 1].
        /// </summary>
        [Category("InteractiveDataDisplay")]
        [Description("Range of values mapped to color")]
        public Range Range
        {
            get { return (Range)GetValue(RangeProperty); }
            set { SetValue(RangeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Palette"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register(
              "Palette",
              typeof(Palette),
              typeof(PaletteControl),
              new PropertyMetadata(Palette.Parse("Black"), OnPaletteChanged));

        private static void OnPaletteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PaletteControl control = (PaletteControl)d;
            control.OnPaletteChanged();
        }

        /// <summary>
        /// Identifies the <see cref="Range"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
          "Range",
          typeof(Range),
          typeof(PaletteControl),
          new PropertyMetadata(new Range(0, 1), OnRangeChanged));

        /// <summary>
        /// Gets or sets a value indicating whether an axis should be displayed.
        /// Default value is true.
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public bool IsAxisVisible
        {
            get { return (bool)GetValue(IsAxisVisibleProperty); }
            set { SetValue(IsAxisVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsAxisVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAxisVisibleProperty = DependencyProperty.Register(
          "IsAxisVisible",
          typeof(bool),
          typeof(PaletteControl),
          new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets the height of rendered palette.
        /// Default value is 20.
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public double PaletteHeight
        {
            get { return (double)GetValue(PaletteHeightProperty); }
            set { SetValue(PaletteHeightProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PaletteHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaletteHeightProperty = DependencyProperty.Register(
          "PaletteHeight",
          typeof(double),
          typeof(PaletteControl),
          new PropertyMetadata(20.0, OnPaletteHeightChanged));

        private static void OnPaletteHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PaletteControl control = (PaletteControl)d;
            control.image.Height = (double)e.NewValue;
            control.UpdateBitmap();
        }

        #endregion

        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteControl"/> class.
        /// </summary>
        public PaletteControl()
        {
            StackPanel stackPanel = new StackPanel();

            image = new Image { Height = 20, Stretch = Stretch.None, HorizontalAlignment = HorizontalAlignment.Stretch };
            axis = new Axis { AxisOrientation = AxisOrientation.Bottom, HorizontalAlignment = HorizontalAlignment.Stretch };

            stackPanel.Children.Add(image);
            stackPanel.Children.Add(axis);

            Content = stackPanel;

            SizeChanged += (o, e) =>
            {
                if (e.PreviousSize.Width == 0 || e.PreviousSize.Height == 0 || Double.IsNaN(e.PreviousSize.Width) || Double.IsNaN(e.PreviousSize.Height))
                    UpdateBitmap();
            };

            IsTabStop = false;
        }

        #endregion

        #region Private methods

        private void OnPaletteChanged()
        {
            UpdateBitmap();
        }

        private void UpdateBitmap()
        {
            if (Width == 0 || Double.IsNaN(Width))
            {
                image.Source = null;
                return;
            }
            if (Palette == null)
            {
                image.Source = null;
                return;
            }

            int width = (int)Width;
            int height = (int)image.Height;
            WriteableBitmap bmp2 = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            WriteableBitmap bmp = bmp2.Clone();
            bmp.Lock();
            unsafe
            {
                byte* pixels = (byte*)bmp.BackBuffer;
                int stride = bmp.BackBufferStride;
                int pixelWidth = bmp.PixelWidth;
                double min = Palette.Range.Min;
                double coeff = (Palette.Range.Max - min) / bmp.PixelWidth;
                for (int i = 0; i < pixelWidth; i++)
                {
                    double ratio = i * coeff + min;
                    Color color = Palette.GetColor(i * coeff + min);
                    for (int j = 0; j < height; j++)
                    {
                        pixels[(i << 2) + 3 + j * stride] = color.A;
                        pixels[(i << 2) + 2 + j * stride] = color.R;
                        pixels[(i << 2) + 1 + j * stride] = color.G;
                        pixels[(i << 2) + j * stride] = color.B;
                    }
                }
            }
            bmp.Unlock();
            image.Source = bmp;
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PaletteControl control = (PaletteControl)d;
            control.OnRangeChanged();
        }

        private void OnRangeChanged()
        {
            axis.Range = Range;
        }

        #endregion

    }
}

