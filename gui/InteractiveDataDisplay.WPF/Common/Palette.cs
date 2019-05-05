// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

// Copyright © 2010 Microsoft Corporation, All Rights Reserved.
// This code released under the terms of the Microsoft Research License Agreement (MSR-LA, http://sds.codeplex.com/License)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Globalization;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Represents a point within the range of a palette, containing colors on the left and rigth sides.
    /// </summary>
    public class PalettePoint
    {
        private Color rightColor, leftColor;
        private double x;

        internal PalettePoint()
        {}

        /// <summary>
        /// Initializes a new instance of <see cref="PalettePoint"/> class.
        /// </summary>
        /// <param name="x">Coordinate of a point.</param>
        /// <param name="right">Color on a right side.</param>
        /// <param name="left">Color on a left side.</param>
        public PalettePoint(double x, Color right, Color left)
        {
            this.rightColor = right;
            this.leftColor = left;
            this.x = x;
        }

        /// <summary>
        /// Gets the color on a right side of a point.
        /// </summary>
        public Color RightColor
        {
            internal set { rightColor = value; }
            get { return rightColor; }
        }
        /// <summary>
        /// Gets the color on a left side of a point.
        /// </summary>
        public Color LeftColor
        {
            internal set { leftColor = value; }
            get { return leftColor; }
        }
        /// <summary>
        /// Gets the coordinate of a point.
        /// </summary>
        public double X
        {
            internal set { x = value; }
            get { return x; }
        }

        /// <summary>
        /// Returns a string that represents the current palette point with information of coordinate and colors.
        /// </summary>
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} - left: {1}, right: {2}", x, leftColor, rightColor);
        }
    }

    /// <summary>
    /// Defines mapping between color and double value.
    /// </summary>
    public class Palette : IPalette
    {
        private bool isNormalized;
        private Range range;
        private PalettePoint[] points;

        /// <summary>
        /// Predefined palette "Blue, Green, Red".
        /// </summary>
        public static Palette Heat = new Palette(true, new Range(0, 1),
            new PalettePoint[3]{
                new PalettePoint(0.0, Colors.Blue, Colors.Blue),
                new PalettePoint(0.5, Color.FromArgb(255, 0, 255, 0), Color.FromArgb(255, 0, 255, 0)),
                new PalettePoint(1.0, Colors.Red, Colors.Red)
            });

        // See http://www.geos.ed.ac.uk/it/howto/GMT/CPT/palettes.html
        // public readonly static Palette Topo = Palette.Parse("#C977D9,#A18AE6,#8AA2E6,#8BD1E7,#8AF3CF,#85F38E,#EDE485,#F0B086,#DE9F8B,#74A3B3=0.5,#99CC70,#DCD68E,#BDF385,#EDDFAD,#F7E8CA,#FFF9F3,#FFF9F6,#FFFBF9,#FFFCFA,White");

        private Palette()
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="Palette"/> class.
        /// </summary>
        /// <param name="isNormalized">Indicated whether palette is absolute or normalized. 
        /// If true, the actual range of palette is always [0...1].</param>
        /// <param name="range">Range of palette.</param>
        /// <param name="points">An array of color points.</param>
        /// <remarks>
        /// <para>If <paramref name="isNormalized"/> is true, value of the <paramref name="range"/>
        /// is ignored and actual range is always [0...1].</para>
        /// <para>The array <paramref name="points"/> is cloned before use and therefore can be modified 
        /// after the constructor is completed without any effect on the palette instance.
        /// </para>
        /// </remarks>
        public Palette(bool isNormalized, Range range, PalettePoint[] points)
        {
            this.isNormalized = isNormalized;
            if (isNormalized)
                this.range = new Range(0, 1);
            else
                this.range = new Range(range.Min, range.Max);

            if (points == null) throw new ArgumentNullException("points");
            if (points.Length < 2) throw new ArgumentException("Palette should have at least two points");
            this.points = (PalettePoint[])points.Clone();
        }

        /// <summary>
        /// Initializes new instance of <see cref="Palette"/> class on a basis of existing palette's colors.
        /// </summary>
        /// <param name="isNormalized">Indicated whether palette is absolute or normalized. 
        /// If true, the actual range of palette is always [0...1].</param>
        /// <param name="range">Range of palette.</param>
        /// <param name="palette">A palette to construct a new one.</param>
        public Palette(bool isNormalized, Range range, Palette palette)
        {
            if (palette == null) throw new ArgumentNullException("palette");
            this.isNormalized = isNormalized;
            if (isNormalized)
                this.range = new Range(0, 1);
            else
                this.range = range;

            if (palette.IsNormalized == isNormalized)
            {
                points = (PalettePoint[])palette.points.Clone();
            }
            else
            {
                var srcRange = palette.Range;
                double alpha, beta;
                if (isNormalized) // from absolute to normalized
                {
                    alpha = 1.0 / (srcRange.Max - srcRange.Min);
                    beta = -srcRange.Min * alpha;
                }
                else // from normalized to absolute
                {
                    alpha = range.Max - range.Min;
                    beta = range.Min;
                }
                points = palette.points.Select(pp => new PalettePoint(pp.X * alpha + beta, pp.RightColor, pp.LeftColor)).ToArray();
            }
        }

        /// <summary>
        /// Gets the points with colors describing the palette.
        /// </summary>
        public PalettePoint[] Points
        {
            get { return points.Clone() as PalettePoint[]; }
        }

        /// <summary>
        /// Parses the palette from the string. See remarks for details.
        /// </summary>
        /// <param name="value">A string to parse from.</param>
        /// <remarks>
        /// <para>String can contain: names of colors (<see cref="System.Windows.Media.Color"/> or hexadecimal representation #AARRGGBB),
        /// double values and separators (equal symbol or comma) to separate colors and values from each other. </para>
        /// <para>Comma is used for gradient colors, equal symbol - for initializing a color of specified value.</para>
        /// <para>Palette can be normalized or absolute.</para>
        /// <para>Absolute palette maps entire numbers to colors exactly as numbers are specified. For examples,
        /// palette '-10=Blue,Green,Yellow,Red=10' maps values -10 and below to Blue, 10 and above to red.</para>
        /// <para>Normalized palette maps minimum value to left palette color, maximum value to right palette color. In normalized palette
        /// no numbers on left and right sides are specified.</para>
        /// <para>Palettes can contain regions of constant color. For example, palette 'Blue=0.25=Yellow=0.5=#00FF00=0.75=Orange' maps first quarter of
        /// values to blue, second quarter to yellow and so on.</para>
        /// </remarks>
        /// <returns>A palette that specified string describes.</returns>
        public static Palette Parse(string value)
        {
            bool isNormalized = true;
            Range range;
            List<PalettePoint> points = new List<PalettePoint>();

            if (value == null) value = String.Empty;
            if (String.IsNullOrEmpty(value))
                return new Palette(true, new Range(0, 1), new PalettePoint[2]{
                                      new PalettePoint(0.0, Colors.White, Colors.White),
                                      new PalettePoint(1.0, Colors.Black, Colors.Black)});
            Lexer lexer = new Lexer(value);
            int state = -1;
            double lastNumber;
            if (lexer.ReadNext())
            {
                points.Add(new PalettePoint(0.0, Colors.White, Colors.White));
                if (lexer.CurrentLexeme == Lexer.LexemeType.Number)
                {
                    points[points.Count - 1].X = lexer.ValueNumber;
                    isNormalized = false;
                    if (lexer.ReadNext() && lexer.CurrentLexeme != Lexer.LexemeType.Separator)
                        throw new PaletteFormatException(lexer.Position, "separator expected");
                    if (lexer.ReadNext() && lexer.CurrentLexeme != Lexer.LexemeType.Color)
                        throw new PaletteFormatException(lexer.Position, "color expected");
                }
                if (lexer.CurrentLexeme == Lexer.LexemeType.Color)
                {
                    points[points.Count - 1].RightColor = lexer.ValueColor;
                    points[points.Count - 1].LeftColor = lexer.ValueColor;
                    points.Add(new PalettePoint(points[0].X, lexer.ValueColor, lexer.ValueColor));
                }
                else
                    throw new PaletteFormatException(lexer.Position, "wrong lexeme");
            }
            lastNumber = points[0].X;
            while (lexer.ReadNext())
            {
                if (lexer.CurrentLexeme == Lexer.LexemeType.Separator)
                {
                    if (lexer.ValueSeparator == Lexer.Separator.Equal)
                    {
                        if (lexer.ReadNext())
                        {
                            if (lexer.CurrentLexeme == Lexer.LexemeType.Number)
                            {
                                if (lexer.ValueNumber < lastNumber)
                                    throw new PaletteFormatException(lexer.Position, "number is less than previous");
                                lastNumber = lexer.ValueNumber;
                                if (state == -1)
                                {
                                    //x1 = color = x2
                                    points[points.Count - 1].X = lexer.ValueNumber;
                                    state = 1;
                                }
                                else if (state == 0)
                                {
                                    //color = x
                                    points[points.Count - 1].X = lexer.ValueNumber;
                                    state = 2;
                                }
                                else
                                    throw new PaletteFormatException(lexer.Position, "wrong lexeme");
                            }
                            else if (lexer.CurrentLexeme == Lexer.LexemeType.Color)
                            {
                                if (state == 1 || state == 2)
                                {
                                    //x = color (,x=color || color1=x=color2)
                                    points[points.Count - 1].RightColor = lexer.ValueColor;
                                    state = -1;
                                }
                                else if (state == 0 || state == -1)
                                {
                                    //color1 = color2
                                    points[points.Count - 1].X = points[0].X - 1;
                                    points[points.Count - 1].RightColor = lexer.ValueColor;
                                    state = -1;
                                }
                                else
                                    throw new PaletteFormatException(lexer.Position, "wrong lexeme");
                            }
                            else
                                throw new PaletteFormatException(lexer.Position, "wrong lexeme");
                        }
                    }
                    else if (lexer.ValueSeparator == Lexer.Separator.Comma)
                    {
                        if (lexer.ReadNext())
                        {
                            if (state == 1 || state == -1 || state == 2)
                            {
                                if (lexer.CurrentLexeme == Lexer.LexemeType.Number)
                                {
                                    if (lexer.ValueNumber <= lastNumber)
                                        throw new PaletteFormatException(lexer.Position, "number is less than previous");
                                    lastNumber = lexer.ValueNumber;
                                    //x1 = color, x2
                                    if (lexer.ReadNext() && lexer.CurrentLexeme == Lexer.LexemeType.Separator && lexer.ValueSeparator == Lexer.Separator.Equal)
                                    {
                                        if (lexer.ReadNext() && lexer.CurrentLexeme == Lexer.LexemeType.Color)
                                        {
                                            if (state != -1)
                                                points.Add(new PalettePoint(lexer.ValueNumber, lexer.ValueColor, lexer.ValueColor));
                                            else
                                            {
                                                points[points.Count - 1].X = lexer.ValueNumber;
                                                points[points.Count - 1].RightColor = lexer.ValueColor;
                                                points[points.Count - 1].LeftColor = lexer.ValueColor;
                                            }
                                            state = -1;
                                        }
                                        else
                                            throw new PaletteFormatException(lexer.Position, "color expected");
                                    }
                                    else
                                        throw new PaletteFormatException(lexer.Position, "wrong lexeme");
                                }
                                else if (lexer.CurrentLexeme == Lexer.LexemeType.Color)
                                {
                                    // x = color1, color2
                                    if (state == -1)
                                        points.RemoveAt(points.Count - 1);
                                    state = 0;
                                }
                                else
                                    throw new PaletteFormatException(lexer.Position, "wrong lexeme");
                            }

                            else if (state == 0)
                            {
                                if (lexer.CurrentLexeme == Lexer.LexemeType.Number)
                                {
                                    if (lexer.ValueNumber <= lastNumber)
                                        throw new PaletteFormatException(lexer.Position, "number is less than previous");
                                    lastNumber = lexer.ValueNumber;
                                    //color, x
                                    points[points.Count - 1].X = points[0].X - 1;
                                    if (lexer.ReadNext() && lexer.CurrentLexeme == Lexer.LexemeType.Separator && lexer.ValueSeparator == Lexer.Separator.Equal)
                                    {
                                        if (lexer.ReadNext() && lexer.CurrentLexeme == Lexer.LexemeType.Color)
                                        {
                                            points.Add(new PalettePoint(lexer.ValueNumber, lexer.ValueColor, lexer.ValueColor));
                                            state = -1;
                                        }
                                        else
                                            throw new PaletteFormatException(lexer.Position, "color expected");
                                    }
                                    else
                                        throw new PaletteFormatException(lexer.Position, "wrong lexeme");
                                }
                                else if (lexer.CurrentLexeme == Lexer.LexemeType.Color)
                                {
                                    //color1, color2
                                    points[points.Count - 1].X = points[0].X - 1;
                                    state = 0;
                                }
                                else
                                    throw new PaletteFormatException(lexer.Position, "wrong lexeme");
                            }
                        }
                    }
                    if (state == -1)
                        points.Add(new PalettePoint(points[0].X, lexer.ValueColor, lexer.ValueColor));
                    else if (state == 0)
                        points.Add(new PalettePoint(points[0].X, lexer.ValueColor, lexer.ValueColor));
                }
                else
                    throw new PaletteFormatException(lexer.Position, "separator expected");
            }

            if (lexer.CurrentLexeme == Lexer.LexemeType.Separator)
                throw new PaletteFormatException(lexer.Position, "wrong lexeme");
            if ((lexer.CurrentLexeme == Lexer.LexemeType.Number && isNormalized) ||
               (lexer.CurrentLexeme == Lexer.LexemeType.Color && !isNormalized))
                throw new PaletteFormatException(lexer.Position, "wrong ending");
            if (isNormalized)
            {
                points[points.Count - 1].X = 1.0;
                if (points[points.Count - 1].X < points[points.Count - 2].X)
                    throw new PaletteFormatException(lexer.Position, "number is less than previous");
            }
            points[points.Count - 1].RightColor = points[points.Count - 1].LeftColor;
            if (points[0].X >= points[points.Count - 1].X)
                throw new PaletteFormatException(lexer.Position, "wrong range of palette");
            range = new Range(points[0].X, points[points.Count - 1].X);

            int start = 1;
            int count = 0;
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].X == points[0].X - 1)
                {
                    if (count == 0) start = i;
                    count++;
                }
                else if (count != 0)
                {
                    double res_x = (points[start + count].X - points[start - 1].X) / (count + 1);
                    for (int j = 0; j < count; j++)
                        points[start + j].X = points[start - 1].X + res_x * (j + 1);
                    count = 0;
                    start = 1;
                }
            }

            return new Palette(isNormalized, range, points.ToArray());
        }

        private static string ColorToString(Color color)
        {
            Type colors = typeof(Colors);
            foreach (var property in colors.GetProperties())
            {
                if (((Color)property.GetValue(null, null)) == color)
                    return property.Name;
            }
            if (color.A == 0xff)
                return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B);
        }

        private static string PositionToString(double x)
        {
            return x.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets a string that describes current palette.
        /// </summary>
        /// <returns>String that describes palette.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            int k = 0;

            if (!IsNormalized)
                sb.Append(PositionToString(points[k].X)).Append('=');
            sb.Append(ColorToString(points[k].RightColor));
            k++;
            for (; k < points.Length - 1; k++)
            {
                if (points[k].LeftColor == points[k - 1].RightColor)
                {
                    sb.Append('=').Append(PositionToString(points[k].X));
                    if (points[k].RightColor != points[k].LeftColor ||
                       (points[k].RightColor == points[k].LeftColor && points[k + 1].LeftColor == points[k].RightColor))
                    {
                        sb.Append('=');
                        sb.Append(ColorToString(points[k].RightColor));
                    }
                }
                else
                {
                    sb.Append(',');
                    if (points[k].RightColor == points[k].LeftColor && points[k].LeftColor == points[k + 1].LeftColor)
                    {
                        sb.Append(PositionToString(points[k].X)).Append('=');
                        sb.Append(ColorToString(points[k].LeftColor));
                    }
                    else
                    {
                        sb.Append(ColorToString(points[k].LeftColor)).Append('=').Append(PositionToString(points[k].X));
                        if (points[k].RightColor == points[k + 1].LeftColor ||
                            points[k].RightColor != points[k].LeftColor)
                        {
                            sb.Append('=');
                            sb.Append(ColorToString(points[k].RightColor));
                        }
                    }
                }
            }
            k = points.Length - 1;
            if (points[k].LeftColor != points[k - 1].RightColor)
            {
                sb.Append(',');
                sb.Append(ColorToString(points[k].LeftColor));
            }
            if (!IsNormalized)
                sb.Append('=').Append(PositionToString(points[k].X));
            return sb.ToString();
        }

        #region IPalette Members

        /// <summary>
        /// Gets a color for the specified value. Note that palette uses HSL interpolation of colors.
        /// </summary>
        /// <param name="value">A double value from <see cref="Range"/>.</param>
        /// <returns>A color for specific double value.</returns>
        public Color GetColor(double value)
        {
            Color color = new Color();
            if (IsNormalized && (value > 1 || value < 0))
                throw new ArgumentException("Wrong value for normalized palette");
            else
            {
                if (value <= points[0].X)
                {
                    color.A = points[0].LeftColor.A;
                    color.R = points[0].LeftColor.R;
                    color.G = points[0].LeftColor.G;
                    color.B = points[0].LeftColor.B;
                }
                else if (value >= points[points.Length - 1].X)
                {
                    color.A = points[points.Length - 1].RightColor.A;
                    color.R = points[points.Length - 1].RightColor.R;
                    color.G = points[points.Length - 1].RightColor.G;
                    color.B = points[points.Length - 1].RightColor.B;
                }
                else
                    for (int i = 0; i < points.Length - 1; i++)
                    {
                        var p1 = points[i];
                        var p2 = points[i + 1];
                        if (value >= p1.X && value <= p2.X)
                        {
                            var c1 = p1.RightColor;
                            var c2 = p2.LeftColor;
                            double alpha = (value - points[i].X) / (p2.X - p1.X);

                            HSLColor cHSL1 = new HSLColor(c1);
                            HSLColor cHSL2 = new HSLColor(c2);

                            if (Math.Abs(cHSL2.H - cHSL1.H) > 3)
                            {
                                if (cHSL1.H < cHSL2.H)
                                    cHSL1 = new HSLColor(cHSL1.A, cHSL1.H + 6, cHSL1.S, cHSL1.L);
                                else if (cHSL1.H > cHSL2.H)
                                    cHSL2 = new HSLColor(cHSL2.A, cHSL2.H + 6, cHSL2.S, cHSL2.L);
                            }

                            HSLColor cHSL = new HSLColor(cHSL1.A + ((cHSL2.A - cHSL1.A) * alpha),
                                                         cHSL1.H + ((cHSL2.H - cHSL1.H) * alpha),
                                                         cHSL1.S + ((cHSL2.S - cHSL1.S) * alpha),
                                                         cHSL1.L + ((cHSL2.L - cHSL1.L) * alpha));

                            if (cHSL.H >= 6)
                                cHSL = new HSLColor(cHSL.A, cHSL.H - 6, cHSL.S, cHSL.L);
                            color = cHSL.ConvertToRGB();
                            break;
                        }
                    }
            }
            return color;
        }

        /// <summary>
        /// Gets the value indicating whether the <see cref="Range"/> is absolute or relative ([0, 1]).
        /// </summary>
        public bool IsNormalized
        {
            get { return isNormalized; }
        }

        /// <summary>
        /// Gets the range on which palette is defined.
        /// </summary>
        public Range Range
        {
            get { return range; }
        }
        #endregion

        /// <summary>
        /// Determines whether the specified palette is equal to the current instance.
        /// </summary>
        /// <param name="obj">The palette to compare with current.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Palette p = obj as Palette;
            if (p == null) return false;
            if (isNormalized != p.isNormalized) return false;
            if (range.Min != p.range.Min || range.Max != p.range.Max) return false;
            int n = this.points.Length;
            if (n != p.points.Length) return false;
            for (int i = 0; i < n; i++)
            {
                var l = points[i];
                var r = p.points[i];
                if (l.X != r.X || l.LeftColor != r.LeftColor || l.RightColor != r.RightColor)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Serves as a hash function for a palette type.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = isNormalized.GetHashCode() ^ range.GetHashCode();
            for (int i = 0; i < points.Length; i++)
            {
                var l = points[i];
                hash ^= l.X.GetHashCode() ^ l.LeftColor.GetHashCode() ^ l.RightColor.GetHashCode();
            }
            return hash;
        }
    }

    internal class Lexer
    {
        public enum Separator { Equal, Comma };
        public enum LexemeType { Color, Separator, Number };

        LexemeType currentLexem;
        Color valueColor;
        double valueNumber;
        Separator valueSeparator;

        string paletteString;
        int position;

        public LexemeType CurrentLexeme
        {
            get { return currentLexem; }
        }
        public Color ValueColor
        {
            get { return valueColor; }
        }
        public double ValueNumber
        {
            get { return valueNumber; }
        }
        public Separator ValueSeparator
        {
            get { return valueSeparator; }
        }
        public int Position
        {
            get { return position; }
        }

        public Lexer(string value)
        {
            paletteString = value;
            position = 0;
        }

        public bool ReadNext()
        {
            if (position >= paletteString.Length)
                return false;
            while (paletteString[position] == ' ') position++;

            if (paletteString[position] == '#' || Char.IsLetter(paletteString[position]))
            {
                currentLexem = LexemeType.Color;
                int start = position;
                while (position < paletteString.Length && paletteString[position] != ' ' &&
                       paletteString[position] != '=' && paletteString[position] != ',')
                {
                    position++;
                }
                string color = paletteString.Substring(start, position - start);
                valueColor = GetColorFromString(color);
            }
            else if (paletteString[position] == '=' || paletteString[position] == ',')
            {
                currentLexem = LexemeType.Separator;
                if (paletteString[position] == '=') valueSeparator = Separator.Equal;
                else valueSeparator = Separator.Comma;
                position++;
            }
            else
            {
                currentLexem = LexemeType.Number;
                int start = position;
                while (position < paletteString.Length && paletteString[position] != ' ' &&
                       paletteString[position] != '=' && paletteString[position] != ',')
                {
                    position++;
                }
                string number = paletteString.Substring(start, position - start);
                valueNumber = Double.Parse(number, CultureInfo.InvariantCulture);
            }
            return true;
        }

        private Color GetColorFromString(string str)
        {
            Color color = new Color();

            if (Char.IsLetter(str[0]))
            {
                bool isNamed = false;
                Type colors = typeof(Colors);
                foreach (var property in colors.GetProperties())
                {
                    if (property.Name == str)
                    {
                        color = (Color)property.GetValue(null, null);
                        isNamed = true;
                    }
                }
                if (!isNamed)
                    throw new PaletteFormatException(Position, "wrong name of color");
            }
            else if (str[0] == '#')
            {
                if (str.Length == 7)
                {
                    color.A = 255;
                    color.R = Convert.ToByte(str.Substring(1, 2), 16);
                    color.G = Convert.ToByte(str.Substring(3, 2), 16);
                    color.B = Convert.ToByte(str.Substring(5, 2), 16);
                }
                else if (str.Length == 9)
                {
                    color.A = Convert.ToByte(str.Substring(1, 2), 16);
                    color.R = Convert.ToByte(str.Substring(3, 2), 16);
                    color.G = Convert.ToByte(str.Substring(5, 2), 16);
                    color.B = Convert.ToByte(str.Substring(7, 2), 16);
                }
                else throw new PaletteFormatException(Position, "wrong name of color");
            }
            else throw new PaletteFormatException(Position, "wrong name of color");

            return color;
        }
    }

    /// <summary>
    /// An exception that is thrown when an argument does not meet the parameter specifications 
    /// of <see cref="Palette"/> class methods.
    /// </summary>
    public class PaletteFormatException : FormatException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PaletteFormatException"/> class.
        /// </summary>
        /// <param name="position">A position in a string in which an exception occured.</param>
        /// <param name="message">A message to show.</param>
        public PaletteFormatException(int position, string message) :
            base(String.Format(CultureInfo.InvariantCulture, "Incorrect palette string at char {0}: {1}", position, message)) { }

        /// <summary>
        /// Initializes a new instance of <see cref="PaletteFormatException"/> class with default message.
        /// </summary>
        public PaletteFormatException() { }

        /// <summary>
        /// Initializes a new instance of <see cref="PaletteFormatException"/> class.
        /// </summary>
        /// <param name="message">A message to show.</param>
        public PaletteFormatException(string message) 
            : base(message) { }

        /// <summary>
        /// Initializes a new instance of <see cref="PaletteFormatException"/> class.
        /// </summary>
        /// <param name="message">A message to show.</param>
        /// <param name="innerException">An occured exception.</param>
        public PaletteFormatException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    internal class HSLColor
    {
        private double h;
        private double s;
        private double l;
        private double a;

        public HSLColor(double a, double h, double s, double l)
        {
            this.a = a;
            this.h = h;
            this.s = s;
            this.l = l;
        }

        public HSLColor(Color color)
        {
            this.a = color.A / 255.0;
            
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double maxcolor = Math.Max(r, g);
            maxcolor = Math.Max(maxcolor, b);
            double mincolor = Math.Min(r, g);
            mincolor = Math.Min(mincolor, b);

            this.l = (maxcolor + mincolor) / 2.0;

            if (maxcolor == mincolor)
                this.s = 0.0;
            else
            {
                if (this.l < 0.5)
                    s = (maxcolor - mincolor) / (maxcolor + mincolor);
                else
                    s = (maxcolor - mincolor) / (2.0 - maxcolor - mincolor);
            }
            if (maxcolor == mincolor)
                this.h = 0;
            else if (maxcolor == r)
            {
                if (g >= b)
                    this.h = (g - b) / (maxcolor - mincolor);
                else
                    this.h = (g - b) / (maxcolor - mincolor) + 6.0;
            }
            else if (maxcolor == g)
                this.h = 2.0 + (b - r) / (maxcolor - mincolor);
            else if (maxcolor == b)
                this.h = 4.0 + (r - g) / (maxcolor - mincolor);
        }

        public Color ConvertToRGB()
        {
            Color color = new Color();
            color.A = (byte)(this.a * 255.0);

            double c = (1.0 - Math.Abs(2.0 * this.l - 1.0)) * this.s;
            double x = c * (1.0 - Math.Abs(this.h % 2.0 - 1.0));

            if (this.h < 1 && this.h >= 0)
            {
                color.R = Convert.ToByte(c * 255.0);
                color.G = Convert.ToByte(x * 255.0);
                color.B = 0;
            }
            if (this.h < 2 && this.h >= 1)
            {
                color.R = Convert.ToByte(x * 255.0);
                color.G = Convert.ToByte(c * 255.0);
                color.B = 0;
            }
            if (this.h < 3 && this.h >= 2)
            {
                color.R = 0;
                color.G = Convert.ToByte(c * 255.0);
                color.B = Convert.ToByte(x * 255.0);
            }
            if (this.h < 4 && this.h >= 3)
            {
                color.R = 0;
                color.G = Convert.ToByte(x * 255.0);
                color.B = Convert.ToByte(c * 255.0);
            }
            if (this.h < 5 && this.h >= 4)
            {
                color.R = Convert.ToByte(x * 255.0);
                color.G = 0;
                color.B = Convert.ToByte(c * 255.0);
            }
            if (this.h < 6 && this.h >= 5)
            {
                color.R = Convert.ToByte(c * 255.0);
                color.G = 0;
                color.B = Convert.ToByte(x * 255.0);
            }

            double m = (this.l - c / 2.0) * 255.0;
            double temp = color.R + m;
            if (temp > 255)
                color.R = 255;
            else if (temp < 0)
                color.R = 0;
            else
                color.R = Convert.ToByte(temp);

            temp = color.G + m;
            if (temp > 255)
                color.G = 255;
            else if (temp < 0)
                color.G = 0;
            else
                color.G = Convert.ToByte(color.G + m);

            temp = color.B + m;
            if (temp > 255)
                color.B = 255;
            else if (temp < 0)
                color.B = 0;
            else
                color.B = Convert.ToByte(color.B + m);

            return color;
        }

        public double A
        {
            get { return a; }
        }
        public double H
        {
            get { return h; }
        }
        public double S
        {
            get { return s; }
        }
        public double L
        {
            get { return l; }
        }
    }
}

