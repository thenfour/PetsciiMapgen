/*
 
the idea here is to do a "naive YUV" format, which doesn't care so much about HUMAN
perception as much as it cares about the perspective of our horrible grainy color mapping.

Here, Luminance is 0-1, grayscale
C1 is just r-b (scale: -1 to 1)
C2 is just r-g (scale: -1 to 1)

the point is to that near blackness / whiteness, differences in R-B and R-G are
small, so this format retains that behavior.

BUT, after trying this, it works, but with lots of caveats and basically it needs
to be tweaked, curves and stuff to ensure balance.
...which is what LAB is for. too bad LAB doesn't work great in our context.

*/

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
  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  public interface ILCCColorSpace
  {
    string FormatString { get; }
    LCCColorDenorm RGBToLCC(ColorF c); // convert 0-255 RGB to denormalized LCC.
    double NormalizeL(double x);
    double DenormalizeL(double x);
    double NormalizeC1(double x);
    double DenormalizeC1(double x);
    double NormalizeC2(double x);
    double DenormalizeC2(double x);
    double ColorDistance(ValueSet lhs, ValueSet rhs, int lumaComponents, int chromaComponents);
  }

  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  public class JPEGColorspace : ILCCColorSpace
  {
    public string FormatString { get { return "JPEG"; } }
    public LCCColorDenorm RGBToLCC(ColorF rgb)
    {
      double Y = (0.2989 * rgb.R + 0.5866 * rgb.G + 0.1145 * rgb.B);
      double Cb = (-0.1687 * rgb.R - 0.3313 * rgb.G + 0.5000 * rgb.B);
      double Cr = (0.5000 * rgb.R - 0.4184 * rgb.G - 0.0816 * rgb.B);

      LCCColorDenorm ret;
      ret.L = Y / 255;
      ret.C1 = Cb / 255; // -.5, .5 center 0
      ret.C2 = Cr / 255; // -.5, .5 center 0
      return ret;
    }

    public double NormalizeC1(double x)
    {
      if (x < 0)
        x += 1;
      return Utils.Clamp(x, 0, 1);
    }
    public double DenormalizeC1(double x)
    {
      if (x > .5)
        x -= 1;
      return x;
    }
    public double NormalizeC2(double x) { return NormalizeC1(x); }
    public double DenormalizeC2(double x) { return DenormalizeC1(x); }
    public double NormalizeL(double x) { return Utils.Clamp(x, 0, 1); }
    public double DenormalizeL(double x) { return x; }

    public double ColorDistance(ValueSet lhs, ValueSet rhs, int lumaComponents, int chromaComponents)
    {
      return Utils.EuclidianColorDist(lhs, rhs, lumaComponents, chromaComponents);
    }
  }

  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  public class NaiveYUVColorspace : ILCCColorSpace
  {
    public string FormatString { get { return "NYUV"; } }
    public LCCColorDenorm RGBToLCC(ColorF c)
    {
      LCCColorDenorm ret;
      c.R /= 255;
      c.G /= 255;
      c.B /= 255;
      //ret.L = (c.R * .299) + (c.G * .587) + (c.B * .114);// don't use balanced grayscale because it ruins the ratios in the chroma components
      ret.L = (c.R + c.G + c.B) / 3;// 0-1
      ret.C1 = c.G - c.B;
      ret.C2 = c.R - c.B;
      return ret;
    }

    public double NormalizeC1(double x)
    {
      // normalized UV values should map .5 => 0, so our mapping granularity plays better. we'll always have an exactly 0
      // point, but we won't always have .5 in our discrete values.
      x /= 2;// -.5 to .5
      if (x < 0)
        x += 1;
      // 0 to .5, or .5 to 1
      return Utils.Clamp(x, 0, 1);
    }
    public double DenormalizeC1(double x)
    {
      if (x > .5)
        x -= 1;
      return x * 2;
    }
    public double NormalizeC2(double x) { return NormalizeC1(x); }
    public double DenormalizeC2(double x) { return DenormalizeC1(x); }
    public double NormalizeL(double x) { return Utils.Clamp(x, 0, 1); }
    public double DenormalizeL(double x) { return x; }

    public double ColorDistance(ValueSet lhs, ValueSet rhs, int lumaComponents, int chromaComponents)
    {
      return Utils.EuclidianColorDist(lhs, rhs, lumaComponents, chromaComponents);
    }
  }
}

