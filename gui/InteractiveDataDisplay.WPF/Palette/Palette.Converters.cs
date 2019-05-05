// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows.Data;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Converts a string to a new instance of <see cref="Palette"/> class.
    /// </summary>
    public class StringToPaletteConverter : IValueConverter
    {
        /// <summary>
        /// Parses an string to <see cref="Palette"/>. For details see <see cref="Palette.Parse"/> method.
        /// </summary>
        /// <param name="value">A string to parse.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>A palette that this string describes.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                string str = (string)value;
                return Palette.Parse(str);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("StringToPaletteConverter: " + exc.Message);
                return Palette.Parse("Black");
            }
        }

        /// <summary>
        /// Returns a string describing an instance of <see cref="Palette"/> class.
        /// </summary>
        /// <param name="value">An instance of <see cref="Palette"/> class.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>A string describing specified palette.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Palette palette = (Palette)value;
            return palette.ToString();
        }
    }

    /// <summary>
    /// Provides a way to convert strings defined in xaml to a new instance of <see cref="Palette"/> class.
    /// </summary>
    public class StringToPaletteTypeConverter : TypeConverter
    {
        /// <summary>
        /// Gets whether a value can be converted to <see cref="Palette"/>.
        /// </summary>
        /// <param name="context">A format context.</param>
        /// <param name="sourceType">A type to convert from.</param>
        /// <returns>True if a value can be converted, false otherwise.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Parses an string to <see cref="Palette"/>. For details see <see cref="Palette.Parse"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value">A string to parse.</param>
        /// <returns>A palette that this string describes.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                try
                {
                    if (value == null)
                        throw new ArgumentNullException("value");

                    string str = value.ToString();
                    return Palette.Parse(str);
                }
                catch (Exception exc)
                {
                    Debug.WriteLine("StringToPaletteConverter: " + exc.Message);
                    return Palette.Parse("Black");
                }
            }
            else
                return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Returns whether a value can be converted to the specified type.
        /// </summary>
        /// <param name="context">A format context.</param>
        /// <param name="destinationType">A type to convert to.</param>
        /// <returns>True if the value can be converted, false otherwise</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(destinationType);
        }

        /// <summary>
        /// Returns a string describing an instance of <see cref="Palette"/> class.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value">An instance of <see cref="Palette"/> class.</param>
        /// <param name="destinationType"></param>
        /// <returns>A string describing specified palette.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            var palette = value as Palette;
            if (palette != null)
            {
                return palette.ToString();
            }
            else
                return base.ConvertTo(context, destinationType);
        }
    }
}

