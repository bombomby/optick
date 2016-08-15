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
    }
}
