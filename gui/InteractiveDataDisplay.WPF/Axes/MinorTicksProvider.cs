// Copyright © Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Provides mechanisms to generate minor ticks displayed on an axis. 
    /// </summary>
    public class MinorTicksProvider
    {
        /// <summary>
        /// Default ticks count for provider.
        /// </summary>
        public static readonly int DefaultTicksCount = 3;
        private int ticksCount;

        /// <summary>
        /// Initializes a new instance of <see cref="MinorTicksProvider"/> class with default ticks count.
        /// </summary>
        public MinorTicksProvider()
        {
            ticksCount = DefaultTicksCount;
        }

        /// <summary>
        /// Gets or sets the count of generated ticks.
        /// </summary>
        public int TicksCount
        {
            get { return ticksCount; }
            set { ticksCount = value; }
        }
        
        /// <summary>
        /// Generates minor ticks for given array of major ticks and range.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <param name="ticks">An array of major ticks.</param>
        /// <returns></returns>
        public double[] CreateTicks(Range range, double[] ticks)
        {
            if (ticks == null)
                throw new ArgumentNullException("ticks");

            if (ticks.Count() < 2)
                return null;

            List<double> res = new List<double>();
            double step = (ticks[1] - ticks[0]) / (ticksCount + 1);
            int i = 1;
            if (range.Min > ticks[0])
            {
                double x0 = ticks[1] - i * step;
                res.Add(x0);
                while (res[res.Count - 1] >= range.Min)
                {
                    res.Add(x0 - i * step);
                    i++;
                }
                i++;
            }

            int fin = ticks.Length - 1;
            if (ticks[fin] > range.Max)
                fin--;
            for (i = 0; i < fin; i++)
            {
                double x0 = ticks[i] + step;
                for (int j = 0; j < ticksCount; j++)
                    res.Add(x0 + j * step);
            }

            if (fin != ticks.Length - 1)
            {
                double x0 = ticks[fin] + step;
                res.Add(x0);
                i = 1;
                while (res[res.Count - 1] <= range.Max)
                {
                    res.Add(x0 + i * step);
                    i++;
                }
            }
            return res.ToArray();
        }
    }
}

