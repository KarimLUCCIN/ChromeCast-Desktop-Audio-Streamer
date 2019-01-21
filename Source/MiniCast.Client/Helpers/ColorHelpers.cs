using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MiniCast.Client.Helpers
{
    public static class ColorHelpers
    {
        public static Color Lerp(Color a, Color b, float x)
        {
            x = Math.Max(0, Math.Min(1, x));
            return b * x + a * (1 - x);
        }

        public static Color IntensityColor(float intensity)
        {
            return Lerp(Color.FromRgb(0, 0, 0), Color.FromRgb(255, 255, 255), intensity);
        }
    }
}
