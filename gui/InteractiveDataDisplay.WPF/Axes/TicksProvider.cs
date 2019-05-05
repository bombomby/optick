// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Provides mechanisms to generate ticks displayed on an axis. 
    /// </summary>
    public class TicksProvider
    {
        private int delta = 1;
        private int beta = 0;
                
        /// <summary>
        /// Initializes a new instance of <see cref="TicksProvider"/> class with default <see cref="MinorTicksProvider"/>.
        /// </summary>
        public TicksProvider()
        {
            minorProvider = new MinorTicksProvider();
        }

        private readonly MinorTicksProvider minorProvider;
        /// <summary>
        /// Gets the <see cref="MinorTicksProvider"/>.
        /// </summary>
        public MinorTicksProvider MinorProvider
        {
            get { return minorProvider; }
        }

        private Range range = new Range(0, 1);
        /// <summary>
        /// Gets or sets the range of axis.
        /// </summary>
        public Range Range
        {
            get { return range; }
            set
            {
                range = value;
                delta = 1;
                beta = (int)Math.Round(Math.Log10(range.Max - range.Min)) - 1;
            }
        }
        
        /// <summary>
        /// Gets an array of ticks for specified axis range.
        /// </summary>
        public double[] GetTicks()
        {
            double start = Range.Min;
            double finish = Range.Max;
            double d = finish - start;

            if (d == 0)
                return new double[] { start, finish };

            double temp = delta * Math.Pow(10, beta);
            double min = Math.Floor(start / temp);
            double max = Math.Floor(finish / temp);
            int count = (int)(max - min + 1);
            List<double> res = new List<double>();
            double x0 = min * temp;
            for (int i = 0; i < count + 1; i++)
            {
                double v = RoundHelper.Round(x0 + i * temp, beta);
                if(v >= start && v <= finish)
                    res.Add(v);
            }
            return res.ToArray();
        }
        
        /// <summary>
        /// Decreases the tick count.
        /// </summary>
        public void DecreaseTickCount()
        {
            if (delta == 1)
            {
                delta = 2;
            }
            else if (delta == 2)
            {
                delta = 5;
            }
            else if (delta == 5)
            {
                delta = 1;
                beta++;
            }
        }

        /// <summary>
        /// Increases the tick count.
        /// </summary>
        public void IncreaseTickCount()
        {
            if (delta == 1)
            {
                delta = 5;
                beta--;
            }
            else if (delta == 2)
            {
                delta = 1;
            }
            else if (delta == 5)
            {
                delta = 2;
            }
        }

        /// <summary>
        /// Generates minor ticks in specified range.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <returns>An array of minor ticks.</returns>
        public double[] GetMinorTicks(Range range)
        {
            var ticks = new List<double>(GetTicks());
            double temp = delta * Math.Pow(10, beta);
            ticks.Insert(0, RoundHelper.Round(ticks[0] - temp, beta));
            ticks.Add(RoundHelper.Round(ticks[ticks.Count - 1] + temp, beta));
            return minorProvider.CreateTicks(range, ticks.ToArray());
        }
    }
}

