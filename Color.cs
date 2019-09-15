using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PetsciiMapgen
{
  public static class ColorUtils
  {
    public static void ToMapping(Color c, out float y, out float u, out float v)
    {
      var rgb = new ColorMine.ColorSpaces.Rgb { R = c.R, G = c.G, B = c.B };
      var lab = rgb.To<ColorMine.ColorSpaces.Lab>();
      // https://github.com/hvalidi/ColorMine/blob/master/ColorMine/ColorSpaces/ColorSpaces.xml
      y = (float)lab.L / 100;
      u = (float)lab.A / 255;
      v = (float)lab.B / 255;
      u += .5f;
      v += .5f;
      y = Utils.Clamp(y, 0, 1);
      u = Utils.Clamp(u, 0, 1);
      v = Utils.Clamp(v, 0, 1);
    }
    //public static float ColorDist(float y1, float u1, float v1, float y2, float u2, float v2)
    //{
    //  //var laba = new ColorMine.ColorSpaces.Lab { L = y1, A = u1, B = v1 };
    //  //var labb = new ColorMine.ColorSpaces.Lab { L = y2, A = u2, B = v2 };
    //  //var deltaE = laba.Compare(labb, new ColorMine.ColorSpaces.Comparisons.CieDe2000Comparison());
    //  //return (float)deltaE;

    //  // euclidian distance
    //  float acc = 0;
    //  float m = Math.Abs(y1 - y2);
    //  acc = m * m * 10; // prioritize luma
    //  m = Math.Abs(u1 - u2);
    //  acc += m * m;
    //  m = Math.Abs(v1 - v2);
    //  acc += m * m;
    //  return acc;
    //}
  }
}

