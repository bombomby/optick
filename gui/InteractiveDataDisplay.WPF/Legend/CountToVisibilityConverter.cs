// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System.Windows.Data;
using System;
using System.Windows;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Converts the count of collection to Visibility.
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        /// Converts any numeric value to Visibility.
        /// </summary>
        /// <param name="value">A value of any numeric type.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Visible if the value is positive. Collapsed if the value is negative or 0.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (int)value > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Converts Visibility to integer value.
        /// </summary>
        /// <param name="value">A value of Visibility enum.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>1 if the value is Visible. 0 if the value is Collapsed.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible ? 1 : 0;
        }

        #endregion
    }
}

