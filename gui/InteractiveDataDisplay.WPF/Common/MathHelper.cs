// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Helper class for mathematical calculations.
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// Verifies whether the value is NaN of Infinity.
        /// </summary>
        /// <param name="value">The value to varify.</param>
        /// <returns>True if the value is not NaN and is Infinity, false otherwise.</returns>
        public static bool IsFinite(this double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value);
        }

        /// <summary>
        /// Clamps specified long value to specified interval.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum of the interval.</param>
        /// <param name="max">Maximum of the interval.</param>
        /// <returns>Long value in range [min, max].</returns>
        public static long Clamp(long value, long min, long max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        /// <summary>
        /// Clamps specified double value to specified interval.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum of the interval.</param>
        /// <param name="max">Maximum of the interval.</param>
        /// <returns>Double value in range [min, max].</returns>
        public static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        /// <summary>Clamps specified double value to [0,1].</summary>
        /// <param name="value">Value to clamp.</param>
        /// <returns>Double value in range [0, 1].</returns>
        public static double Clamp(double value)
        {
            return Math.Max(0, Math.Min(value, 1));
        }

        /// <summary>
        /// Clamps specified integer value to specified interval.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum of the interval.</param>
        /// <param name="max">Maximum of the interval.</param>
        /// <returns>Integer value in range [min, max].</returns>
        public static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        /// <summary>
        /// Returns <see cref="Rect"/> by four given coordinates of corner points.
        /// </summary>
        /// <param name="minX">X coordinate on a left-top corner.</param>
        /// <param name="minY">Y coordinate on a left-top corner.</param>
        /// <param name="maxX">X coordinate on a right-bottom corner.</param>
        /// <param name="maxY">Y coordinate on a right-bottom corner.</param>
        /// <returns></returns>
        public static Rect CreateRectByPoints(double minX, double minY, double maxX, double maxY)
        {
            return new Rect(new Point(minX, minY), new Point(maxX, maxY));
        }

        /// <summary>
        /// Returns <see cref="Rect"/> by given point of a center and <see cref="Size"/>.
        /// </summary>
        /// <param name="center">Point of a center of a rectangle.</param>
        /// <param name="size">Size of a rectangle.</param>
        /// <returns>Rect.</returns>
        public static Rect CreateRectFromCenterSize(Point center, Size size)
        {
            return new Rect(center.X - size.Width / 2, center.Y - size.Height / 2, size.Width, size.Height);
        }

        /// <summary>
        /// Converts an angle in radians to the angle in degrees.
        /// </summary>
        /// <param name="angleInRadians">Angle in radians.</param>
        /// <returns>Angle in degrees.</returns>
        public static double ToDegrees(this double angleInRadians)
        {
            return angleInRadians * 180 / Math.PI;
        }
    }
}

