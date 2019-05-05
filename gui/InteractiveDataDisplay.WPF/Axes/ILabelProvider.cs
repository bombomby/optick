// Copyright © Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System.Windows;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Defines a method to create labels as an array of <see cref="FrameworkElement"/> from an array of double values.
    /// </summary>
    public interface ILabelProvider
    {
        /// <summary>
        /// Generates an array of labels from an array of double.
        /// </summary>
        /// <param name="ticks">An array of double ticks.</param>
        /// <returns></returns>
        FrameworkElement[] GetLabels(double[] ticks);
    }
}

