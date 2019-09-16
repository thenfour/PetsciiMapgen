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

    public static float RestoreY(float y)
    {
      return y * 100f;
    }
    public static float RestoreU(float u)
    {
      return (u - .5f) * 255f;
    }
    public static float RestoreV(float v)
    {
      return (v - .5f) * 255f;
    }
  }
}

