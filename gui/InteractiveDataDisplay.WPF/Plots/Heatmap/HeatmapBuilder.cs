// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Helper class, which provides various methods for building heatmaps.
    /// </summary>
    public static class HeatmapBuilder
    {
        /// <summary>
        /// Builds heatmap from specified data, grid, missing value, visibleRect and palette.
        /// </summary>
        /// <param name="data">2D array with data to plot</param>
        /// <param name="missingValue">Missing value. Heatmap will have transparent regions at missing value.</param>
        /// <param name="palette">Palette to translate numeric values to colors</param>
        /// <param name="range">Min and max values in <paramref name="data"/> array</param>
        /// <param name="rect">Heatmap rectangle screen coordinates</param>
        /// <param name="screen">Plot rectaneg screen coordinates</param>
        static public int[] BuildHeatMap(Rect screen, DataRect rect, double[] x, double[] y, double[,] data,
            double missingValue, IPalette palette, Range range)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (x == null)
                throw new ArgumentNullException("x");
            if (y == null)
                throw new ArgumentNullException("y");
            if (palette == null)
                throw new ArgumentNullException("palette");

            int pixelWidth = (int)screen.Width;
            int pixelHeight = (int)screen.Height;
            int[] pixels = new int[pixelWidth * pixelHeight];
            double doubleMissingValue = missingValue;
            UInt32[] paletteColors = new uint[512];

            if (!palette.IsNormalized)
                range = palette.Range;

            for (int i = 0; i < 512; i++)
            {
                Color c;
                if (palette.IsNormalized)
                    c = palette.GetColor((double)i / 511.0);
                else
                    c = palette.GetColor(range.Min + i * (range.Max - range.Min) / 511.0);

                paletteColors[i] = (((uint)(c.A)) << 24) | (((uint)c.R) << 16) | (((uint)c.G) << 8) | c.B;
            }

            int xdimRank = x.Length;
            int ydimRank = y.Length;

            int xdataRank = data.GetLength(0);
            int ydataRank = data.GetLength(1);

            double factor = (range.Max != range.Min) ? (1.0 / (range.Max - range.Min)) : 0.0;

            if (xdataRank == xdimRank && ydataRank == ydimRank)
            {
                double[,] v = null;
                v = Filter(pixelWidth, pixelHeight, rect, x, y, data, doubleMissingValue);
                if (palette.IsNormalized)
                {
                    if (Double.IsNaN(doubleMissingValue))
                    {
                        for (int j = 0, k = 0; j < pixelHeight; j++)
                        {
                            for (int i = 0; i < pixelWidth; i++, k++)
                            {
                                double vv = v[i, pixelHeight - j - 1];
                                if (!Double.IsNaN(vv))
                                {
                                    double v01 = (vv - range.Min) * factor;
                                    pixels[k] = (int)paletteColors[((uint)(511 * v01)) & 0x1FF];
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0, k = 0; j < pixelHeight; j++)
                        {
                            for (int i = 0; i < pixelWidth; i++, k++)
                            {
                                double vv = v[i, pixelHeight - j - 1];
                                if (vv != doubleMissingValue)
                                {
                                    double v01 = (vv - range.Min) * factor;
                                    pixels[k] = (int)paletteColors[((uint)(511 * v01)) & 0x1FF];
                                }
                            }
                        }
                    }
                }
                else // Palette is absolute
                {
                    if (Double.IsNaN(doubleMissingValue))
                    {
                        for (int j = 0, k = 0; j < pixelHeight; j++)
                        {
                            for (int i = 0; i < pixelWidth; i++, k++)
                            {
                                double vv = v[i, pixelHeight - j - 1];
                                if (!Double.IsNaN(vv))
                                {
                                    double v01 = (Math.Max(range.Min, Math.Min(range.Max, vv)) - range.Min) * factor;
                                    pixels[k] = (int)paletteColors[((uint)(511 * v01)) & 0x1FF];
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0, k = 0; j < pixelHeight; j++)
                        {
                            for (int i = 0; i < pixelWidth; i++, k++)
                            {
                                double vv = v[i, pixelHeight - j - 1];
                                if (vv != doubleMissingValue)
                                {
                                    double v01 = (Math.Max(range.Min, Math.Min(range.Max, vv)) - range.Min) * factor;
                                    pixels[k] = (int)paletteColors[((uint)(511 * v01)) & 0x1FF];
                                }
                            }
                        }
                    }
                }
            }
            else if ((xdataRank + 1) == xdimRank && (ydataRank + 1) == ydimRank)
            {

                // Prepare arrays
                int[] xPixelDistrib;
                int[] yPixelDistrib;
                
                xPixelDistrib = CreatePixelToDataMap(pixelWidth, Math.Max(x[0], rect.XMin), Math.Min(x[x.Length - 1], rect.XMax), x);
                yPixelDistrib = CreatePixelToDataMap(pixelHeight, Math.Max(y[0], rect.YMin), Math.Min(y[y.Length - 1], rect.YMax), y);

                if (palette.IsNormalized)
                {
                    if (Double.IsNaN(doubleMissingValue))
                    {
                        for (int j = 0, k = 0; j < pixelHeight; j++)
                        {
                            for (int i = 0; i < pixelWidth; i++, k++)
                            {
                                double vv = data[xPixelDistrib[i], yPixelDistrib[pixelHeight - j - 1]];
                                if (!Double.IsNaN(vv))
                                {
                                    double v01 = (vv - range.Min) * factor;
                                    pixels[k] = (int)paletteColors[((uint)(511 * v01)) & 0x1FF];
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0, k = 0; j < pixelHeight; j++)
                        {
                            for (int i = 0; i < pixelWidth; i++, k++)
                            {
                                double vv = data[xPixelDistrib[i], yPixelDistrib[pixelHeight - j - 1]];
                                if (vv != doubleMissingValue)
                                {
                                    double v01 = (vv - range.Min) * factor;
                                    pixels[k] = (int)paletteColors[((uint)(511 * v01)) & 0x1FF];
                                }
                            }
                        }
                    }
                }
                else // Palette is absolute
                {
                    if (Double.IsNaN(doubleMissingValue))
                    {
                        for (int j = 0, k = 0; j < pixelHeight; j++)
                        {
                            for (int i = 0; i < pixelWidth; i++, k++)
                            {
                                double vv = data[xPixelDistrib[i], yPixelDistrib[pixelHeight - j - 1]];
                                if (!Double.IsNaN(vv))
                                {
                                    double v01 = (Math.Max(range.Min, Math.Min(range.Max, vv)) - range.Min) * factor;
                                    pixels[k] = (int)paletteColors[((uint)(511 * v01)) & 0x1FF];
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0, k = 0; j < pixelHeight; j++)
                        {
                            for (int i = 0; i < pixelWidth; i++, k++)
                            {
                                double vv = data[xPixelDistrib[i], yPixelDistrib[pixelHeight - j - 1]];
                                if (vv != doubleMissingValue)
                                {
                                    double v01 = (Math.Max(range.Min, Math.Min(range.Max, vv)) - range.Min) * factor;
                                    pixels[k] = (int)paletteColors[((uint)(511 * v01)) & 0x1FF];
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                throw new ArgumentException("Size of x,y and data arrays does not match conditions");
            }

            return pixels;
        }


        /// <summary>Prepares linear interpolation filter tables</summary>
        /// <param name="width">Size of heatmap in pixels</param>
        /// <param name="minX">Plot coordinate corresponding to left pixel</param>
        /// <param name="maxX">Plot coordinate corresponding to right pixel</param>
        /// <param name="x">Plot coordinates of data points</param>
        /// <param name="pixels">Indices in target array (pixels)</param>
        /// <param name="indexes">Indices in source array</param>
        /// <param name="alphas">Linear transform coefficient</param>
        /// <param name="weights">Weights</param>
        /// <remarks>
        /// <example>
        /// Following code performs linear interpolation without taking missing values into account. 
        /// Names of variables correspond to parameter names
        /// <code><![CDATA[
        /// double[] target = new double[pixels.Length];
        /// for(int i = 0;i<pixels.Length)
        ///     target[pixels[i]] += weights[i]*(alphas[i]*source[indexes[i]]+(1-alphas[i])*source[indexes[i]+1]);
        /// ]]>
        /// </code>
        /// </example>
        /// </remarks>
        public static void PrepareFilterTables(int width, double minX, double maxX, double[] x,
            out int[] pixels, out int[] indexes, out double[] alphas, out double[] weights)
        {
            if (x == null)
                throw new ArgumentNullException("x");

            int length = x.Length;
            if (length <= 1)
                throw new ArgumentException("Knots array should contain at least 2 knots");
            if (maxX > x[length - 1])
                maxX = x[length - 1];
            else if (minX < x[0])
                minX = x[0];

            List<int> p = new List<int>(length);
            List<int> idx = new List<int>(length);
            List<double> k = new List<double>(length);
            List<double> w = new List<double>(length);


            int i = 0;
            double delta = (maxX - minX) / width;
            for (int pixel = 0; pixel < width; pixel++)
            {
                double pixmin = minX + pixel * delta; // Current pixel is segment [pixmin, pixmin + delta]
                while (x[i + 1] <= pixmin)
                    i++; // No out of range here because of prerequisites
                while (i < length - 1)
                {
                    double min = Math.Max(pixmin, x[i]);
                    double max = Math.Min(pixmin + delta, x[i + 1]);
                    double center = (min + max) / 2;
                    idx.Add(i);
                    p.Add(pixel);
                    k.Add((x[i + 1] - center) / (x[i + 1] - x[i]));
                    w.Add((max - min) / delta);
                    if (x[i + 1] >= pixmin + delta)
                        break;
                    else
                        i++;
                }
            }
            pixels = p.ToArray();
            indexes = idx.ToArray();
            alphas = k.ToArray();
            weights = w.ToArray();
        }

        /// <summary>Performs array resize and subset with linear interpolation</summary>
        /// <param name="width">Result width</param>
        /// <param name="height">Result height</param>
        /// <param name="rect">Range of coordinates for entire data</param>
        /// <param name="x">Coordinates of x points (length of array must be <paramref name="width"/></param>
        /// <param name="y">Coordinates of y points (length of array must be <paramref name="height"/></param>
        /// <param name="data">Source data array</param>
        /// <param name="missingValue">Missing value or NaN if no missing value</param>
        /// <returns>Resulting array with size <paramref name="width"/> * <paramref name="height"/></returns>
        public static double[,] Filter(int width, int height, DataRect rect,
            double[] x, double[] y, double[,] data, double missingValue)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (x == null)
                throw new ArgumentNullException("x");
            if (y == null)
                throw new ArgumentNullException("y");

            // Check preconditions
            if (x.Length != data.GetLength(0))
                throw new ArgumentException("Size of x and data arrays does not match");
            if (y.Length != data.GetLength(1))
                throw new ArgumentException("Size of y and data arrays does not match");

            // Prepare filters
            int[] hp, vp, hi, vi;
            double[] ha, va, hw, vw;
            PrepareFilterTables(width, rect.XMin, rect.XMax, x, out hp, out hi, out ha, out hw);
            PrepareFilterTables(height, rect.YMin, rect.YMax, y, out vp, out vi, out va, out vw);

            // Prepare arrays
            double[,] r = new double[width, height];
            bool hasMissingValue = !Double.IsNaN(missingValue);
            double offset = 0;
            if (hasMissingValue && missingValue == 0)
            {
                offset = -1;
                for (int i = 0; i < width; i++)
                    for (int j = 0; j < height; j++)
                        r[i, j] = offset;
            }

            // Do filtering
            int hpLen = hp.Length;
            int vpLen = vp.Length;
            for (int i = 0; i < hpLen; i++)
            {
                int px = hp[i];
                int i0 = hi[i];

                for (int j = 0; j < vpLen; j++)
                {
                    int py = vp[j];
                    if (hasMissingValue && r[px, py] == missingValue)
                        continue;
                    int j0 = vi[j];
                    if (hasMissingValue &&
                        (data[i0, j0] == missingValue ||
                         data[i0 + 1, j0] == missingValue ||
                         data[i0, j0 + 1] == missingValue ||
                         data[i0 + 1, j0 + 1] == missingValue))
                        r[px, py] = missingValue;
                    else
                    {
                        double v0 = ha[i] * data[i0, j0] + (1 - ha[i]) * data[i0 + 1, j0];
                        double v1 = ha[i] * data[i0, j0 + 1] + (1 - ha[i]) * data[i0 + 1, j0 + 1];
                        double v = va[j] * v0 + (1 - va[j]) * v1;
                        r[px, py] += v * hw[i] * vw[j];
                    }
                }
            }
            // Offset data if needed
            if (offset != 0)
                for (int i = 0; i < width; i++)
                    for (int j = 0; j < height; j++)
                        if (r[i, j] != missingValue)
                            r[i, j] -= offset;

            return r;
        }

        /// <summary>Computes array of indices in data array for rendering heatmap with given width in colormap mode</summary>
        /// <param name="width">Width of heatmap in pixels</param>
        /// <param name="xmin">Data value corresponding to left heatmap edge</param>
        /// <param name="xmax">Data value corresponding to right heatmap edge</param>
        /// <param name="x">Grid array. Note that <paramref name="xmin"/> and <paramref name="xmax"/> should be
        /// inside grid range. Otherwise exception will occur.</param>
        /// <returns>Array of length <paramref name="width"/> where value of each element is index in data
        /// array</returns>
        private static int[] CreatePixelToDataMap(int width, double xmin, double xmax, double[] x)
        {
            int length = x.Length;
            if (length <= 1)
                throw new ArgumentException("Knots array should contain at least 2 knots");
            if (xmax > x[length - 1] || xmin < x[0])
                throw new ArgumentException("Cannot interpolate beyond bounds");

            int[] pixels = new int[width];
            double delta = (xmax - xmin) / width;
            int i = 0;
            for (int pixel = 0; pixel < width; pixel++)
            {
                double pixCenter = xmin + (pixel + 0.5) * delta; // Pixel center
                while (x[i+1] < pixCenter)
                    i++; // No out of range here because of prerequisites. Now x[i+1] > pixCenter 
                pixels[pixel] = i;
            }

            return pixels;
        }

        /// <summary>
        /// Calculates data range from specified 2d array of data.
        /// </summary>
        public static Range GetMaxMin(double[,] data)
        {
            double Max, Min;
            if (data != null)
            {
                int N = data.GetLength(0);
                int M = data.GetLength(1);
                Max = data[0, 0];
                Min = data[0, 0];

                for (int i = 0; i < N; i++)
                    for (int j = 0; j < M; j++)
                    {
                        if (!double.IsNaN(data[i, j]))
                        {
                            if (data[i, j] > Max) Max = data[i, j];
                            if (data[i, j] < Min) Min = data[i, j];
                        }
                    }
                if (double.IsNaN(Max) || double.IsNaN(Min))
                {
                    Max = 0;
                    Min = 0;
                }
            }
            else
            {
                Max = Double.NaN;
                Min = Double.NaN;
            }
            return new Range(Min, Max); ;
        }

        /// <summary>
        /// Calculates data range from specified 2d array of data and missing value.
        /// </summary>
        public static Range GetMaxMin(double[,] data, double missingValue)
        {
            double Max, Min;
            if (data != null)
            {
                int N = data.GetLength(0);
                int M = data.GetLength(1);

                Max = data[0, 0];
                Min = data[0, 0];

                for (int i = 0; i < N; i++)
                    for (int j = 0; j < M; j++)
                    {
                        if (data[i, j] != missingValue)
                        {
                            if (data[i, j] > Max) Max = data[i, j];
                            if (data[i, j] < Min) Min = data[i, j];
                        }
                    }
                if (Max == missingValue || Min == missingValue)
                {
                    Max = 0;
                    Min = 0;
                }
            }
            else
            {
                Max = missingValue;
                Min = missingValue;
            }
            return new Range(Min, Max); ;
        }

    }


}

