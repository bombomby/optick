// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Helper class for performing manipulations with arrays
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>Gets index of item that is nearest to <paramref name="value"/>.</summary>
        /// <param name="array">Array in ascending order.</param>
        /// <param name="value">Value to look for.</param>
        /// <returns>Index of closest element or -1 if <paramref name="value"/>
        /// is less than first element or greater than last.</returns>
        public static int GetNearestIndex(double[] array, double value)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            int i = Array.BinarySearch<double>(array, value);
            if (i >= 0)
                return i;
            i = ~i;
            if (i == array.Length)
                return -1; // greater than last value
            if (i == 0)
                return -1; // less than first value
            return (value - array[i - 1] > array[i] - value) ? (i) : (i - 1);
        }

        #region 1D array conversion utils

        /// <summary>Converts array of floats to array of doubles</summary>
        /// <param name="array">Array of float</param>
        /// <returns>Array of same length but with double elements</returns>
        public static double[] ToDoubleArray(this float[] array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            double[] result = new double[array.Length];
            for (int i = 0; i < array.Length; i++)
                    result[i] = array[i];
            return result;
        }

        /// <summary>Converts array of 32-bit integers to array of doubles</summary>
        /// <param name="array">Array of 32-bit integers</param>
        /// <returns>Array of same length but with double elements</returns>
        public static double[] ToDoubleArray(this int[] array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            double[] result = new double[array.Length];
            for (int i = 0; i < array.Length; i++)
                result[i] = array[i];
            return result;
        }

        /// <summary>Converts array of booleans to array of doubles where true maps to 1, false maps to 0</summary>
        /// <param name="array">Array of booleans</param>
        /// <returns>Array of same dimensions but with double elements</returns>
        public static double[] ToDoubleArray(this bool[] array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            double[] result = new double[array.Length];
            for (int i = 0; i < array.Length; i++)
                result[i] = array[i] ? 1 : 0 ;
            return result;
        }

        /// <summary>Converts array of doubles, floats, integers or booleans to array of doubles where true maps to 1, false maps to 0</summary>
        /// <param name="array">Source array</param>
        /// <returns>Array of same length but with double elements</returns>
        public static double[] ToDoubleArray(Array array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new InvalidOperationException("Source array is not 1D");
            var elemType = array.GetType().GetElementType();
            if (elemType == typeof(double))
                return (double[])array;
            if (elemType == typeof(float))
                return ArrayExtensions.ToDoubleArray((float[])array);
            else if (elemType == typeof(int))
                return ArrayExtensions.ToDoubleArray((int[])array);
            else if (elemType == typeof(bool))
                return ArrayExtensions.ToDoubleArray((bool[])array);
            else
                throw new NotSupportedException("Conversions of 1D arrays of " + elemType.Name + " to double[] is not supported");
        }

        #endregion

        #region 2D array conversion utils

        /// <summary>Converts 2D array of floats to 2D array of doubles</summary>
        /// <param name="array">2D array of float</param>
        /// <returns>2D array of same dimensions but with double elements</returns>
        [CLSCompliantAttribute(false)]
        public static double[,] ToDoubleArray(this float[,] array) 
        {
            if (array == null)
                throw new ArgumentNullException("array");
            int n = array.GetLength(0);
            int m = array.GetLength(1);
            double[,] result = new double[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    result[i, j] = array[i, j];
            return result;
        }

        /// <summary>Converts 2D array of 32-bit integers to 2D array of doubles</summary>
        /// <param name="array">2D array of 32-bit integers</param>
        /// <returns>2D array of same dimensions but with double elements</returns>
        [CLSCompliantAttribute(false)]
        public static double[,] ToDoubleArray(this int[,] array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            int n = array.GetLength(0);
            int m = array.GetLength(1);
            double[,] result = new double[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    result[i, j] = array[i, j];
            return result;
        }

        /// <summary>Converts 2D array of booleans to 2D array of doubles where true maps to 1, false maps to 0</summary>
        /// <param name="array">2D array of booleans</param>
        /// <returns>2D array of same dimensions but with double elements</returns>
        [CLSCompliantAttribute(false)]
        public static double[,] ToDoubleArray(this bool[,] array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            int n = array.GetLength(0);
            int m = array.GetLength(1);
            double[,] result = new double[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    result[i, j] = array[i, j] ? 1 : 0;
            return result;
        }

        /// <summary>Converts 2D array of doubles, floats, integers or booleans to 2D array of doubles where true maps to 1, false maps to 0</summary>
        /// <param name="array">2D array</param>
        /// <returns>2D array of same dimensions but with double elements</returns>
        public static double[,] ToDoubleArray2D(Array array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 2)
                throw new InvalidOperationException("Source array is not 2D");
            var elemType = array.GetType().GetElementType();
            if (elemType == typeof(double))
                return (double[,])array;
            if (elemType == typeof(float))
                return ArrayExtensions.ToDoubleArray((float[,])array);
            else if (elemType == typeof(int))
                return ArrayExtensions.ToDoubleArray((int[,])array);
            else if (elemType == typeof(bool))
                return ArrayExtensions.ToDoubleArray((bool[,])array);
            else
                throw new NotSupportedException("Conversions of 2D arrays of " + elemType.Name + " to double[,] is not supported");
        }

        #endregion
    }
}

