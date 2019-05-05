// Copyright © Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Globalization;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Provides mechanisms to generate labels displayed on an axis by double ticks. 
    /// </summary>
    public class LabelProvider : ILabelProvider
    {
        /// <summary>
        /// Generates an array of labels from an array of double.
        /// </summary>
        /// <param name="ticks">An array of double ticks.</param>
        /// <returns>An array of <see cref="FrameworkElement"/>.</returns>
        public FrameworkElement[] GetLabels(double[] ticks)
        {
            if (ticks == null)
                throw new ArgumentNullException("ticks");

            List<TextBlock> Labels = new List<TextBlock>();
            foreach (double tick in ticks)
            {
                TextBlock text = new TextBlock();
                text.Text = tick.ToString(CultureInfo.InvariantCulture);
                Labels.Add(text);
            }
            return Labels.ToArray();
        }
    }
}

