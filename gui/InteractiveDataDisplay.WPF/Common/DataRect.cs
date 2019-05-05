// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System.Windows;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Describes a width, a height and location of a rectangle.
    /// This type is very similar to <see cref="System.Windows.Rect"/>, but has one important difference:
    /// while <see cref="System.Windows.Rect"/> describes a rectangle in screen coordinates, where y axis
    /// points to bottom (that's why <see cref="System.Windows.Rect"/>'s Bottom property is greater than Top).
    /// This type describes rectange in usual coordinates, and y axis point to top.
    /// </summary>
    public struct DataRect
    {
        private Range x;
        private Range y;

        /// <summary>
        /// Gets the empty <see cref="DataRect"/>.
        /// </summary>
        public static readonly DataRect Empty = 
            new DataRect(Range.Empty, Range.Empty);

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRect"/> struct.
        /// </summary>
        /// <param name="minX">Left value of DataRect.</param>
        /// <param name="minY">Bottom value of DataRect.</param>
        /// <param name="maxX">Right value of DataRect.</param>
        /// <param name="maxY">Top value of DataRect.</param>
        public DataRect(double minX, double minY, double maxX, double maxY)
            : this()
        {
            X = new Range(minX, maxX);
            Y = new Range(minY, maxY);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRect"/> struct.
        /// </summary>
        /// <param name="horizontal">Horizontal range</param>
        /// <param name="vertical">Vertical range</param>
        public DataRect(Range horizontal, Range vertical)
            : this()
        {
            X = horizontal; Y = vertical;
        }

        /// <summary>
        /// Gets of Sets horizontal range
        /// </summary>
        public Range X
        {
            get { return x; }
            set { x = value; }
        }

        /// <summary>
        /// Gets or sets vertical range
        /// </summary>
        public Range Y
        {
            get { return y; }
            set { y = value; }
        }

        /// <summary>
        /// Gets the width of <see cref="DataRect"/>.
        /// </summary>
        public double Width
        {
            get 
            { 
                return X.Max - X.Min; 
            }
        }

        /// <summary>
        /// Gets the height of <see cref="DataRect"/>.
        /// </summary>
        public double Height
        { 
            get 
            { 
                return Y.Max - Y.Min; 
            } 
        }

        /// <summary>
        /// Gets the left.
        /// </summary>
        /// <value>The left.</value>
        public double XMin
        { 
            get 
            { 
                return X.Min; 
            } 
        }

        /// <summary>
        /// Gets the right.
        /// </summary>
        /// <value>The right.</value>
        public double XMax 
        { 
            get 
            { 
                return X.Max; 
            } 
        }

        /// <summary>
        /// Gets the bottom.
        /// </summary>
        /// <value>The bottom.</value>
        public double YMin 
        { 
            get 
            { 
                return Y.Min; 
            } 
        }

        /// <summary>
        /// Gets the top.
        /// </summary>
        /// <value>The top.</value>
        public double YMax 
        { 
            get 
            { 
                return Y.Max; 
            } 
        }

        /// <summary>
        /// Gets a center point of a rectangle.
        /// </summary>
        public Point Center
        {
            get
            {
                return new Point((X.Max + X.Min) / 2.0, (Y.Max + Y.Min) / 2.0);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value><c>true</c> if this instance is empty; otherwise, <c>false</c>.</value>
        public bool IsEmpty 
        { 
            get 
            { 
                return X.IsEmpty || Y.IsEmpty; 
            } 
        }


        /// <summary>
        /// Updates current instance of <see cref="DataRect"/> with minimal DataRect 
        /// which vertical range will contain current vertical range and x value and 
        /// horizontal range will contain current horizontal range and y value
        /// </summary>
        /// <param name="x">Value, which will be used for surrond of horizontal range</param>
        /// <param name="y">Value, which will be used for surrond of vertical range</param>
        public void Surround(double x, double y)
        {
            this.x.Surround(x);
            this.y.Surround(y);
        }

        /// <summary>
        /// Updates current instance of <see cref="DataRect"/> with minimal DataRect which will contain current DataRect and specified DataRect
        /// </summary>
        /// <param name="rect">DataRect, which will be used for surrond of current instance of <see cref="DataRect"/></param>
        public void Surround(DataRect rect)
        {
            this.x.Surround(rect.X);
            this.y.Surround(rect.Y);
        }

        /// <summary>
        /// Updates horizontal range of current instance of <see cref="DataRect"/> with minimal range which will contain both horizontal range of current DataRect and specified value
        /// </summary>
        /// <param name="x">Value, which will be used for surrond of horizontal range of current instance of <see cref="DataRect"/></param>
        public void XSurround(double x)
        {
            this.x.Surround(x);
        }

        /// <summary>
        /// Updates horizontal range of current instance of <see cref="DataRect"/> with minimal range which will contain both horizontal range of current DataRect and specified range
        /// </summary>
        /// <param name="x">Range, which will be used for surrond of horizontal range of current instance of <see cref="DataRect"/></param>
        public void XSurround(Range x)
        {
            this.x.Surround(x);
        }

        /// <summary>
        /// Updates vertical range of current instance of <see cref="DataRect"/> with minimal range which will contain both vertical range of current DataRect and specified value
        /// </summary>
        /// <param name="y">Value, which will be used for surrond of vertical range of current instance of <see cref="DataRect"/></param>
        public void YSurround(double y)
        {
            this.y.Surround(y);
        }

        /// <summary>
        /// Updates vertical range of current instance of <see cref="DataRect"/> with minimal range which will contain both vertical range of current DataRect and specified range
        /// </summary>
        /// <param name="y">Range, which will be used for surrond of vertical range of current instance of <see cref="DataRect"/></param>
        public void YSurround(Range y)
        {
            this.y.Surround(y);
        }

        /// <summary>
        /// Returns a string that represents the current instance of <see cref="DataRect"/>.
        /// </summary>
        /// <returns>String that represents the current instance of <see cref="DataRect"/></returns>
        public override string ToString()
        {
            return "{" + X.ToString() + " " + Y.ToString() + "}";
        }
    }
}

