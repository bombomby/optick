// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

// Copyright © 2010 Microsoft Corporation, All Rights Reserved.
// This code released under the terms of the Microsoft Research License Agreement (MSR-LA, http://sds.codeplex.com/License)
using System.Windows.Media;

namespace InteractiveDataDisplay.WPF
{
	/// <summary>
	/// Represents a color palette, which can convert double values to colors.
	/// </summary>
	public interface IPalette
	{
        /// <summary>
        /// Gets a color for the specified value.
        /// </summary>
        /// <param name="value">A value from the <see cref="Range"/>.</param>
        /// <returns>A color.</returns>
        Color GetColor(double value);

        /// <summary>
        /// Gets the value indicating whether the <see cref="Range"/> contains absolute values 
        /// or it is relative ([0...1]).
        /// </summary>
        bool IsNormalized { get; }

        /// <summary>
        /// Gets the range on which palette is defined.
        /// </summary>
        Range Range { get; }
	}
}


