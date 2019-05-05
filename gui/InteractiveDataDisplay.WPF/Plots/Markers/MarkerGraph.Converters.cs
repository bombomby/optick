// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Diagnostics;
using System.Globalization;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Converts a value to <see cref="SolidColorBrush"/> (see <see cref="Convert"/> method for details).
    /// </summary>
    public class PaletteConverter : IValueConverter
    {
        Palette palette = InteractiveDataDisplay.WPF.Palette.Heat;

        /// <summary>
        /// Gets or sets the palette for convertion.
        /// </summary>
        public Palette Palette
        {
            set { palette = value; }
            get { return palette; }
        }

        /// <summary>
        /// Converts values to <see cref="SolidColorBrush"/>.
        /// </summary>
        /// <param name="value">Value to convert. It can be one of the following types: 
        /// <see cref="SolidColorBrush"/>, <see cref="Color"/>, 
        /// <see cref="string"/> defining system name or hexadecimal representation (#AARRGGBB) of color
        /// or any numeric. If the value is of numeric type then <see cref="Palette"/> is used for convertion.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">The instance of <see cref="DataSeries"/> representing colors.</param>
        /// <param name="culture"></param>
        /// <returns><see cref="SolidColorBrush"/> from specified value.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                DataSeries data = parameter as DataSeries;

                if (value is double)
                {
                    if (palette.IsNormalized)
                        palette = new Palette(false, new Range(data.MinValue, data.MaxValue), palette);
                    return new SolidColorBrush(Palette.GetColor((double)value));
                }
                else
                {
                    SolidColorBrush solidColorBrush = value as SolidColorBrush;
                    if (solidColorBrush != null)
                    {
                        return solidColorBrush;
                    }
                    else if (value is Color)
                    {
                        return new SolidColorBrush((Color)value);
                    }
                    else
                    {
                        string str = value as string;
                        if (str != null)
                        {
                            Color color = new Color();
                            if (Char.IsLetter(str[0]))
                            {
                                bool isNamed = false;
                                Type colors = typeof(Colors);
                                foreach (var property in colors.GetProperties())
                                {
                                    if (property.Name == str)
                                    {
                                        color = (Color)property.GetValue(null, null);
                                        isNamed = true;
                                    }
                                }
                                if (!isNamed)
                                    throw new ArgumentException("Wrong name of color");
                            }
                            else if (str[0] == '#')
                            {
                                if (str.Length == 7)
                                {
                                    color.A = 255;
                                    color.R = System.Convert.ToByte(str.Substring(1, 2), 16);
                                    color.G = System.Convert.ToByte(str.Substring(3, 2), 16);
                                    color.B = System.Convert.ToByte(str.Substring(5, 2), 16);
                                }
                                else if (str.Length == 9)
                                {
                                    color.A = System.Convert.ToByte(str.Substring(1, 2), 16);
                                    color.R = System.Convert.ToByte(str.Substring(3, 2), 16);
                                    color.G = System.Convert.ToByte(str.Substring(5, 2), 16);
                                    color.B = System.Convert.ToByte(str.Substring(7, 2), 16);
                                }
                                else throw new ArgumentException("Wrong name of color");
                            }
                            else throw new ArgumentException("Wrong name of color");
                            return new SolidColorBrush(color);
                        }
                        else
                        {
                            double d = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                            if (palette.IsNormalized)
                            {
                                if (data.MinValue == data.MaxValue)
                                    return new SolidColorBrush(new Palette(false, 
                                                new Range(data.MinValue - 0.5, data.MaxValue + 0.5), palette).GetColor(d));
                                else
                                    return new SolidColorBrush(new Palette(false, 
                                                new Range(data.MinValue, data.MaxValue), palette).GetColor(d));
                            }
                            return new SolidColorBrush(palette.GetColor(d));
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value: " + exc.Message);
                return new SolidColorBrush(Colors.Black);
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// A converter to translate a value into the half of another value of opposite sign.
    /// </summary>
    public class TranslateConverter : IValueConverter
    {
        /// <summary>
        /// Returns an opposite signed result calculated from specified value and parameter.
        /// </summary>
        /// <param name="value">A value to convert.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">A value to add before convertion.</param>
        /// <param name="culture"></param>
        /// <returns>Half of a sum of value and parameter with opposite sign. Value if value or parameter is null.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null || parameter == null)
                    return value;
                return -(System.Convert.ToDouble(value, CultureInfo.InvariantCulture) +
                    System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture)) / 2;
            }
            catch (InvalidCastException exc)
            {
                Debug.WriteLine("Cannot convert value: " + exc.Message);
                return 0;
            }
        }
        
        /// <summary>
        /// Returns an opposite signed result calculated from specified value and parameter.
        /// </summary>
        /// <param name="value">A value to convert.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">A value to add before convertion.</param>
        /// <param name="culture"></param>
        /// <returns>Sum of value and parameter with opposite sign multiplied by 2. Value if value or parameter is null.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null || parameter == null)
                    return value;
                return -(System.Convert.ToDouble(value, CultureInfo.InvariantCulture)
                    + System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture)) * 2;
            }
            catch (InvalidCastException exc)
            {
                Debug.WriteLine("Cannot convert value: " + exc.Message);
                return 0;
            }
        }
    }

    /// <summary>
    /// A converter to multiply two values.
    /// </summary>
    public class ValueScaleConverter : IValueConverter
    {
        /// <summary>
        /// Multiplies a value by another. The second value should be passed as a parameter.
        /// </summary>
        /// <param name="value">First multiplier.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Second multiplier.</param>
        /// <param name="culture"></param>
        /// <returns>Product of value and parameter. Value if value or parameter is null.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null || parameter == null)
                    return value;
                return System.Convert.ToDouble(value, CultureInfo.InvariantCulture) *
                    System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value: " + exc.Message);
                return 0;
            }
        }

        /// <summary>
        /// Devides a value by another. The second value should be passed as a parameter.
        /// </summary>
        /// <param name="value">A dividend.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">A divisor.</param>
        /// <param name="culture"></param>
        /// <returns>Quotient of value and parameter. Value if value or parameter is null.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null || parameter == null)
                    return value;
                return System.Convert.ToDouble(value, CultureInfo.InvariantCulture) /
                    System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value back: " + exc.Message);
                return 0;
            }
        }
    }

    /// <summary>
    /// A converter to transform a value from one range to another saving the ratio.
    /// </summary>
    public class ResizeConverter : IValueConverter
    {
        private Range origin;
        private Range resized;

        /// <summary>
        /// Initializes a new instance of <see cref="ResizeConverter"/> class.
        /// </summary>
        /// <param name="origin">The range for origin value.</param>
        /// <param name="resized">The range for result value.</param>
        public ResizeConverter(Range origin, Range resized)
        {
            this.origin = origin;
            this.resized = resized;
        }

        /// <summary>
        /// Gets or sets the range for origin value.
        /// </summary>
        public Range Origin
        {
            get { return origin; }
            set { origin = value; }
        }
        /// <summary>
        /// Gets or sets the range for result value.
        /// </summary>
        public Range Resized
        {
            get { return resized; }
            set { resized = value; }
        }

        /// <summary>
        /// Convertes a value from <see cref="Origin"/> range to the value in <see cref="Resized"/> range saving the ratio.
        /// </summary>
        /// <param name="value">A value to convert. Should be of numeric type.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Converted value from resized range.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                double x = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                double res = 0;
                if (Double.IsNaN(origin.Min) || Double.IsNaN(origin.Max))
                    res = x;
                else if (origin.Min == origin.Max)
                    res = (resized.Max - resized.Min) / 2;
                else
                    res = resized.Min + (resized.Max - resized.Min) * (x - origin.Min) / (origin.Max - origin.Min);
                return res;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value: " + exc.Message);
                return 0;
            }
        }

        /// <summary>
        /// Convertes a value from <see cref="Resized"/> range to the value in <see cref="Origin"/> range.
        /// </summary>
        /// <param name="value">A value to convert. Should be of numeric type.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Converted value from origin range.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                double x = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                double res = 0;
                if (Double.IsNaN(resized.Min) || Double.IsNaN(resized.Max))
                    res = x;
                else if (resized.Min == resized.Max)
                    res = (origin.Max - origin.Min) / 2;
                else
                    res = origin.Min + (origin.Max - origin.Min) * (x - resized.Min) / (resized.Max - resized.Min);
                return res;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value back: " + exc.Message);
                return 0;
            }
        }
    }

    /// <summary>
    /// Converter, which returns input value if it is not null, parameter otherwise 
    /// </summary>
    public class NullToDefaultConverter : IValueConverter
    {
        /// <summary>
        /// Returns the same value if it is not null, parameter otherwise.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return parameter;
            else
                return value;
        }

        /// <summary>
        /// Returns the same value if value is not equal to parameter, null otherwise.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Object.Equals(value, parameter))
                return null;
            else
                return value;
        }
    }

    /// <summary>
    /// Default converter to transform <see cref="DynamicMarkerViewModel"/> instances of <see cref="MarkerGraph"/> to their data bounds.
    /// Default data bounds rect is point defined by X and Y data series.
    /// </summary>
    public class DefaultDataBoundsConverter : IValueConverter
    {
        /// <summary>
        /// Gets data bounds of a marker of <see cref="MarkerGraph"/> by its <see cref="DynamicMarkerViewModel"/>.
        /// </summary>
        /// <param name="value">An instance of <see cref="DynamicMarkerViewModel"/> class describing specified marker.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Data bounds of specified marker of marker graph.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                DynamicMarkerViewModel model = value as DynamicMarkerViewModel;
                double x = System.Convert.ToDouble(model.Sources["X"], CultureInfo.InvariantCulture);
                double y = System.Convert.ToDouble(model.Sources["Y"], CultureInfo.InvariantCulture);
                return new DataRect(x, y, x, y);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value to DataRect: " + exc.Message);
                return new DataRect();
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Convert back is impossible");
        }
    }

    /// <summary>
    /// A converter to transform <see cref="DynamicMarkerViewModel"/> instances of <see cref="ErrorBarGraph"/> to their data bounds.
    /// </summary>
    public class ErrorBarDataBoundsConverter : IValueConverter
    {
        /// <summary>
        /// Gets data bounds of a marker of <see cref="ErrorBarGraph"/> by its <see cref="DynamicMarkerViewModel"/>.
        /// </summary>
        /// <param name="value">An instance of <see cref="DynamicMarkerViewModel"/> class describing specified marker.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Data bounds of specified marker of error bar.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                DynamicMarkerViewModel model = value as DynamicMarkerViewModel;
                double y = System.Convert.ToDouble(model.Sources["Y"], CultureInfo.InvariantCulture);
                double h = System.Convert.ToDouble(model.Sources["H"], CultureInfo.InvariantCulture);
                double x = System.Convert.ToDouble(model.Sources["X"], CultureInfo.InvariantCulture);
                return new DataRect(x, y - h / 2, x, y + h / 2);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value to DataRect: " + exc.Message);
                return new DataRect();
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert Data Rect to Marker View Model");
        }
    }


    /// <summary>
    /// A converter to transform <see cref="DynamicMarkerViewModel"/> instances of <see cref="VerticalIntervalGraph"/> to their data bounds.
    /// </summary>
    public class VerticalIntervalDataBoundsConverter : IValueConverter
    {
        /// <summary>
        /// Gets data bounds of a marker of <see cref="VerticalIntervalGraph"/> by its <see cref="DynamicMarkerViewModel"/>.
        /// </summary>
        /// <param name="value">An instance of <see cref="DynamicMarkerViewModel"/> class describing specified marker.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Data bounds of specified marker of graph.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                DynamicMarkerViewModel model = value as DynamicMarkerViewModel;
                double y1 = System.Convert.ToDouble(model.Sources["Y1"], CultureInfo.InvariantCulture);
                double y2 = System.Convert.ToDouble(model.Sources["Y2"], CultureInfo.InvariantCulture);
                double x = System.Convert.ToDouble(model.Sources["X"], CultureInfo.InvariantCulture);
                return new DataRect(x, Math.Min(y1,y2), x, Math.Max(y1,y2));
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value to DataRect: " + exc.Message);
                return new DataRect();
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert Data Rect to Marker View Model");
        }
    }

    /// <summary>
    /// A converter to transform <see cref="DynamicMarkerViewModel"/> instances of <see cref="BarGraph"/> to their data bounds.
    /// </summary>
    public class BarGraphDataBoundsConverter : IValueConverter
    {
        /// <summary>
        /// Gets data bounds of a marker of <see cref="BarGraph"/> by its <see cref="DynamicMarkerViewModel"/>.
        /// </summary>
        /// <param name="value">An instance of <see cref="DynamicMarkerViewModel"/> class describing specified marker.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Data bounds of specified marker of bar graph.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                DynamicMarkerViewModel model = value as DynamicMarkerViewModel;
                double y = Math.Max(System.Convert.ToDouble(model.Sources["Y"], CultureInfo.InvariantCulture), 0);
                double h = Math.Abs(System.Convert.ToDouble(model.Sources["Y"], CultureInfo.InvariantCulture));
                double w = System.Convert.ToDouble(model.Sources["W"], CultureInfo.InvariantCulture);
                double x = System.Convert.ToDouble(model.Sources["X"], CultureInfo.InvariantCulture);
                return new DataRect(x - w / 2, y - h, x + w / 2, h);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value to DataRect: " + exc.Message);
                return new DataRect();
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert Data Rect to Marker View Model");
        }
    }

    /// <summary>
    /// Default converter to transform <see cref="DynamicMarkerViewModel"/> instances of <see cref="MarkerGraph"/> to their screen thicknesses.
    /// Default screen thickness is defined as a half of D series value.
    /// </summary>
    public class DefaultScreenThicknessConverter : IValueConverter
    {
        private string seriesName;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultScreenThicknessConverter"/> class.
        /// </summary>
        /// <param name="seriesName">
        /// A <see cref="DataSeries.Key"/> of <see cref="DataSeries"/> used to calculate screen thickness.
        /// </param>
        protected DefaultScreenThicknessConverter(string seriesName)
        {
            this.seriesName = seriesName;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultScreenThicknessConverter"/> class.
        /// The name of <see cref="DataSeries"/> used to calculate screen thickness is "D".
        /// </summary>
        public DefaultScreenThicknessConverter()
            : this("D"){}

        /// <summary>
        /// Gets screen thickness of a marker of <see cref="MarkerGraph"/> by its <see cref="DynamicMarkerViewModel"/>.
        /// </summary>
        /// <param name="value">An instance of <see cref="DynamicMarkerViewModel"/> class describing specified marker.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Screen thickness of specified marker of marker graph. If it is 0 than returns 5.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                DynamicMarkerViewModel model = value as DynamicMarkerViewModel;
                double d = System.Convert.ToDouble(model[seriesName], CultureInfo.InvariantCulture);
                if (d == 0)
                    d = 10;
                return new Thickness(d / 2.0);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value to Thickness: " + exc.Message);
                return new Thickness();
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert Thickness to Marker View Model");
        }
    }

    /// <summary>
    /// A converter to transform <see cref="DynamicMarkerViewModel"/> instances of <see cref="ErrorBarGraph"/> to their screen thicknesses.
    /// Screen thickness of each bar is defined as a half of W series value.
    /// </summary>
    public class ErrorBarScreenThicknessConverter : DefaultScreenThicknessConverter
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ErrorBarScreenThicknessConverter"/> class.
        /// The name of <see cref="DataSeries"/> used to calculate screen thickness is "W".
        /// </summary>
        public ErrorBarScreenThicknessConverter()
            : base("W"){}
    }

    #region Bar graph converters

    /// <summary>
    /// A converter to get maximum between the value and 0.
    /// Is used in template of <see cref="BarGraph"/> to get the top y coordinate of each bar.
    /// </summary>
    public class BarGraphTopConverter : IValueConverter
    {
        /// <summary>
        /// Finds maximum between the value and 0.
        /// </summary>
        /// <param name="value">A value to convert.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Value if it is positive, 0 otherwise. Null if value is null.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return value;
                return Math.Max(System.Convert.ToDouble(value, CultureInfo.InvariantCulture), 0);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value: " + exc.Message);
                return 0;
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// A converter to get minimum between the value and 0.
    /// Is used in template of <see cref="BarGraph"/> to get the bottom y coordinate of each bar.
    /// </summary>
    public class BarGraphBottomConverter : IValueConverter
    {
        /// <summary>
        /// Finds minimum between the value and 0.
        /// </summary>
        /// <param name="value">A value to convert.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Value if it is negative, 0 otherwise. Null if value is null.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return value;
                return Math.Min(0, System.Convert.ToDouble(value, CultureInfo.InvariantCulture));
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value: " + exc.Message);
                return 0;
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// A converter to get x coordinate of left side of specified marker of <see cref="BarGraph"/>.
    /// </summary>
    public class BarGraphLeftConverter : IValueConverter
    {
        /// <summary>
        /// Gets the value of x coordinate of left side of specified marker of <see cref="BarGraph"/> by its <see cref="DynamicMarkerViewModel"/>.
        /// </summary>
        /// <param name="value">An instance of <see cref="DynamicMarkerViewModel"/> class describing specified marker.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>X coordinate of left side of bar. Null if the value is null.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return value;
                DynamicMarkerViewModel model = value as DynamicMarkerViewModel;
                if (model != null)
                {
                    return System.Convert.ToDouble(model.Sources["X"], CultureInfo.InvariantCulture) -
                        System.Convert.ToDouble(model.Sources["W"], CultureInfo.InvariantCulture) / 2;
                }
                else
                    return 0;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value: " + exc.Message);
                return 0;
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// A converter to get x coordinate of right side of specified marker of <see cref="BarGraph"/>.
    /// </summary>
    public class BarGraphRightConverter : IValueConverter
    {
        /// <summary>
        /// Gets the value of x coordinate of right side of specified marker of <see cref="BarGraph"/> by its <see cref="DynamicMarkerViewModel"/>.
        /// </summary>
        /// <param name="value">An instance of <see cref="DynamicMarkerViewModel"/> class describing specified marker.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>X coordinate of left side of bar. Null if the value is null.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return value;
                DynamicMarkerViewModel model = value as DynamicMarkerViewModel;
                if (model != null)
                {
                    return System.Convert.ToDouble(model.Sources["X"], CultureInfo.InvariantCulture) +
                        System.Convert.ToDouble(model.Sources["W"], CultureInfo.InvariantCulture) / 2;
                }
                else
                    return 0;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value: " + exc.Message);
                return 0;
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    #endregion

    #region ErrorBar converters

    /// <summary>
    /// A converter to get the bottom y coordinate of each marker of <see cref="ErrorBarGraph"/>.
    /// </summary>
    public class ErrorBarBottomConverter : IValueConverter
    {
        /// <summary>
        /// Gets the value of bottom y coordinate specified marker of <see cref="ErrorBarGraph"/> by its <see cref="DynamicMarkerViewModel"/>.
        /// </summary>
        /// <param name="value">An instance of <see cref="DynamicMarkerViewModel"/> class describing specified marker.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Bottom y coordinate of bar. Null if the value is null.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return value;
                DynamicMarkerViewModel model = value as DynamicMarkerViewModel;
                if (model != null)
                {
                    return System.Convert.ToDouble(model.Sources["Y"], CultureInfo.InvariantCulture) -
                        System.Convert.ToDouble(model.Sources["H"], CultureInfo.InvariantCulture) / 2;
                }
                else
                    return 0;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value: " + exc.Message);
                return 0;
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// A converter to get the top y coordinate of each marker of <see cref="ErrorBarGraph"/>.
    /// </summary>
    public class ErrorBarTopConverter : IValueConverter
    {
        /// <summary>
        /// Gets the value of top y coordinate specified marker of <see cref="ErrorBarGraph"/> by its <see cref="DynamicMarkerViewModel"/>.
        /// </summary>
        /// <param name="value">An instance of <see cref="DynamicMarkerViewModel"/> class describing specified marker.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Top y coordinate of bar. Null if the value is null.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return value;
                DynamicMarkerViewModel model = value as DynamicMarkerViewModel;
                if (model != null)
                {
                    return System.Convert.ToDouble(model.Sources["Y"], CultureInfo.InvariantCulture) +
                        System.Convert.ToDouble(model.Sources["H"], CultureInfo.InvariantCulture) / 2;
                }
                else
                    return 0;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value: " + exc.Message);
                return 0;
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    #endregion

    #region VerticalIntervalTopConverter

    /// <summary>
    /// A converter to get the top y coordinate of each marker of <see cref="VerticalIntervalGraph"/>.
    /// </summary>
    public class VerticalIntervalTopConverter : IValueConverter
    {
        /// <summary>
        /// Gets the value of top y coordinate specified marker of <see cref="VerticalIntervalGraph"/> by its <see cref="DynamicMarkerViewModel"/>.
        /// </summary>
        /// <param name="value">An instance of <see cref="DynamicMarkerViewModel"/> class describing specified marker.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Top y coordinate of bar. Null if the value is null.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return value;
                DynamicMarkerViewModel model = value as DynamicMarkerViewModel;
                if (model != null)
                {
                    return Math.Max(System.Convert.ToDouble(model.Sources["Y1"], CultureInfo.InvariantCulture),
                                    System.Convert.ToDouble(model.Sources["Y2"], CultureInfo.InvariantCulture));
                }
                else
                    return 0;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value: " + exc.Message);
                return 0;
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    #endregion

    #region VerticalIntervalBottomConverter

    /// <summary>
    /// A converter to get the bottom y coordinate of each marker of <see cref="VerticalIntervalGraph"/>.
    /// </summary>
    public class VerticalIntervalBottomConverter : IValueConverter
    {
        /// <summary>
        /// Gets the value of bottom y coordinate specified marker of <see cref="VerticalIntervalGraph"/> by its <see cref="DynamicMarkerViewModel"/>.
        /// </summary>
        /// <param name="value">An instance of <see cref="DynamicMarkerViewModel"/> class describing specified marker.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Bottom y coordinate of bar. Null if the value is null.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return value;
                DynamicMarkerViewModel model = value as DynamicMarkerViewModel;
                if (model != null)
                {
                    return Math.Min(System.Convert.ToDouble(model.Sources["Y1"], CultureInfo.InvariantCulture),
                                    System.Convert.ToDouble(model.Sources["Y2"], CultureInfo.InvariantCulture));
                }
                else
                    return 0;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot convert value: " + exc.Message);
                return 0;
            }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    #endregion

    /// <summary>
    /// Gets triangle points by its given height.
    /// Is used in templates with triangle markers.
    /// </summary>
    public class DoubleToTrianglePointsConverter : IValueConverter
    {
        /// <summary>
        /// Constructs a <see cref="PointCollection"/> with triangle vertexes from a given height.
        /// </summary>
        /// <param name="value">A height of triangle.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>A collection of triangle vertexes.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            PointCollection pc = new PointCollection();
            pc.Add(new Point(-0.5 * d, 0.288675 * d));
            pc.Add(new Point(0, -0.711324865 * d));
            pc.Add(new Point(0.5 * d, 0.288675 * d));
            return pc;
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert point collection to double");
        }
    }
}


