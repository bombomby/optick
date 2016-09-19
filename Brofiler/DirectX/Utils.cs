using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.DirectX
{
    class Utils
    {
        public static SharpDX.Color Convert(System.Windows.Media.Color color)
        {
            return new SharpDX.Color(color.R, color.G, color.B, color.A);
        }

        public static double GetLuminance(System.Windows.Media.Color color)
        {
            return 0.2126 * color.ScR + 0.7152 * color.ScG + 0.0722 * color.ScB;
        }
    }
}
