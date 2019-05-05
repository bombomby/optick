// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Represents ranges for double type.
    /// </summary>
    public struct Range
    {
        double minimum;
        double maximum;

        /// <summary>
        /// Gets the minimum value of current range.
        /// </summary>
        public double Min
        {
            get { return minimum; }
            set { minimum = value; }
        }

        /// <summary>
        /// Gets the maximum value of current range.
        /// </summary>
        public double Max
        {
            get { return maximum; }
            set { maximum = value; }
        }

        /// <summary>
        /// Initializes new instance of range struct from given minimum and maximum values.
        /// </summary>
        /// <param name="minimum">Minimum value of the range.</param>
        /// <param name="maximum">Maximum value of the range.</param>
        public Range(double minimum, double maximum)
        {
            Debug.Assert(!double.IsNaN(minimum));
            Debug.Assert(!double.IsNaN(maximum));

            if (minimum < maximum)
            {
                this.minimum = minimum; 
                this.maximum = maximum;
            }
            else
            {
                this.minimum = maximum; 
                this.maximum = minimum;
            }
        }

        private Range(bool isEmpty)
        {
            if (isEmpty)
            {
                this.minimum = double.PositiveInfinity;
                this.maximum = double.NegativeInfinity;
            }
            else
            {
                minimum = 0;
                maximum = 0;
            }
        }

        /// <summary>
        /// Readonly instance of empty range
        /// </summary>
        public static readonly Range Empty = new Range(true);

        /// <summary>Returns true of this range contains no points (e.g. Min > Max).</summary>
        public bool IsEmpty { get { return Min > Max; } }

        /// <summary>Returns true of this range is a point (e.g. Min == Max).</summary>
        public bool IsPoint { get { return Max == Min; } }

        /// <summary>
        /// Updates current instance of <see cref="Range"/> with minimal range which contains current range and specified value
        /// </summary>
        /// <param name="value">Value, which will be used for for current instance of range surrond</param>
        public void Surround(double value)
        {
            if (value < minimum) minimum = value;
            if (value > maximum) maximum = value;
        }

        /// <summary>
        /// Updates current instance of <see cref="Range"/> with minimal range which contains current range and specified range
        /// </summary>
        /// <param name="range">Range, which will be used for current instance of range surrond</param>
        public void Surround(Range range)
        {
            if (range.IsEmpty) 
                return;

            Surround(range.minimum);
            Surround(range.maximum);
        }

        /// <summary>
        /// Returns a string that represents the current range.
        /// </summary>
        /// <returns>String that represents the current range</returns>
        public override string ToString()
        {
            return "[" + minimum.ToString(CultureInfo.InvariantCulture) + "," + maximum.ToString(CultureInfo.InvariantCulture) + "]";
        }

        /// <summary>
        /// Calculates range from current which will have the same center and which size will be larger in factor times
        /// </summary>
        /// <param name="factor">Zoom factor</param>
        /// <returns>Zoomed with specified factor range</returns>
        public Range Zoom(double factor)
        {
            if (IsEmpty)
                return new Range(true);
            
            double delta = (Max - Min) / 2;
            double center = (Max + Min) / 2;
            
            return new Range(center - delta * factor, center + delta * factor);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Range"/> is equal to the current range.
        /// </summary>
        /// <param name="obj">The range to compare with the current <see cref="Range"/>.</param>
        /// <returns>True if the specified range is equal to the current range, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            Range r = (Range)obj;
            return r.minimum == minimum && r.maximum == maximum;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for current instance</returns>
        public override int GetHashCode()
        {
            return minimum.GetHashCode() ^ maximum.GetHashCode();
        }

        /// <summary>
        /// Returns a value that indicates whether two specified range values are equal.
        /// </summary>
        /// <param name="first">The first value to compare.</param>
        /// <param name="second">The second value to compare.</param>
        /// <returns>True if values are equal, false otherwise.</returns>
        public static bool operator ==(Range first, Range second)
        {
            return first.Equals(second);
        }

        /// <summary>
        /// Returns a value that indicates whether two specified ranges values are not equal.
        /// </summary>
        /// <param name="first">The first value to compare.</param>
        /// <param name="second">The second value to compare.</param>
        /// <returns>True if values are not equal, false otherwise.</returns>
        public static bool operator !=(Range first, Range second)
        {
            return !first.Equals(second);
        }
    }
}

