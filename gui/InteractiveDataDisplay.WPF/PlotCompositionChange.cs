// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

namespace InteractiveDataDisplay.WPF
{
    /// <summary>Identifies event when collection of related plots in composition change</summary>
    public struct PlotCompositionChange
    {
        private PlotBase master;

        /// <summary>
        /// Initializes a new instance of <see cref="PlotCompositionChange"/>
        /// </summary>
        /// <param name="master">Master <see cref="PlotBase"/> of composition that change</param>
        public PlotCompositionChange(PlotBase master)
        {
            this.master = master;
        }

        /// <summary>Gets master plot of composition that change</summary>
        public PlotBase Master
        {
            get { return master; }
        }

        /// <summary>
        /// Determines whether the specified <see cref="PlotCompositionChange"/> is equal to the current <see cref="PlotCompositionChange"/>.
        /// </summary>
        /// <param name="obj">The <see cref="PlotCompositionChange"/> to compare with the current <see cref="PlotCompositionChange"/>.</param>
        /// <returns>True if the specified <see cref="PlotCompositionChange"/> is equal to the current <see cref="PlotCompositionChange"/>, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            PlotCompositionChange operand = (PlotCompositionChange)obj;
            return master == operand.master;
        }

        /// <summary>
        /// Returns a value that indicates whether two specified <see cref="PlotCompositionChange"/> values are equal.
        /// </summary>
        /// <param name="plotCompositionChange1">The first value to compare.</param>
        /// <param name="plotCompositionChange2">The second value to compare.</param>
        /// <returns>True if values are equal, false otherwise.</returns>
        public static bool operator ==(PlotCompositionChange plotCompositionChange1, PlotCompositionChange plotCompositionChange2)
        {
            return plotCompositionChange1.Equals(plotCompositionChange2);
        }

        /// <summary>
        /// Returns a value that indicates whether two specified <see cref="PlotCompositionChange"/> values are not equal.
        /// </summary>
        /// <param name="plotCompositionChange1">The first value to compare.</param>
        /// <param name="plotCompositionChange2">The second value to compare.</param>
        /// <returns>True if values are not equal, false otherwise.</returns>
        public static bool operator !=(PlotCompositionChange plotCompositionChange1, PlotCompositionChange plotCompositionChange2)
        {
            return !plotCompositionChange1.Equals(plotCompositionChange2);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for current instance</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

