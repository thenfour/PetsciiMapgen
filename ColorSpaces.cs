
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
      //ret.L = (c.R * .299) + (c.G * .587) + (c.B * .114);
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




  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  public class LABColorspace : ILCCColorSpace
  {
    public LABColorspace()
    {
      var lcc = RGBToLCC(ColorF.FromRGB(255, 255, 255));
      ValueSet lhs;
      ValueSet rhs;
      lhs.ValuesLength = 3;
      lhs.Mapped = false;
      lhs.MinDistFound = 0;
      lhs.Visited = false;
      lhs.ID = 0;
      lhs[0] = (float)lcc.L;
      lhs[1] = (float)lcc.C1;
      lhs[2] = (float)lcc.C2;

      var lcc2 = RGBToLCC(ColorF.FromRGB(0, 0, 0));
      rhs.ValuesLength = 3;
      rhs.Mapped = false;
      rhs.MinDistFound = 0;
      rhs.Visited = false;
      rhs.ID = 0;
      rhs[0] = (float)lcc2.L;
      rhs[1] = (float)lcc2.C1;
      rhs[2] = (float)lcc2.C2;

      double d = ColorDistance(lhs, rhs, 1, 2);
    }
    public string FormatString { get { return "LAB"; } }
    public LCCColorDenorm RGBToLCC(ColorF c)
    {
      var rgb = new ColorMine.ColorSpaces.Rgb { R = c.R, G = c.G, B = c.B };
      var lab = rgb.To<ColorMine.ColorSpaces.Lab>();
      // https://github.com/hvalidi/ColorMine/blob/master/ColorMine/ColorSpaces/ColorSpaces.xml
      LCCColorDenorm ret;
      ret.L = lab.L;// 0-100
      ret.C1 = lab.A;// -127 to 127
      ret.C2 = lab.B;// -127 to 127
      return ret;
    }

    public double NormalizeL(double x) { return Utils.Clamp(x / 100, 0, 1); }
    public double NormalizeC1(double x)
    {
      // center point around 0.5
      double ret = x / 255;// -.5 to .5
      if (ret < 0)
        ret += 1;
      return Utils.Clamp(ret, 0, 1);
    }
    public double NormalizeC2(double x) { return NormalizeC1(x); }

    public double DenormalizeL(double x) { return x * 100; }
    public double DenormalizeC1(double x)
    {
      if (x > .5)
        x -= 1;
      return x * 255;
      //return (x - .5) * 255;
    }
    public double DenormalizeC2(double x) { return DenormalizeC1(x); }

    public double ColorDistance(ValueSet lhs, ValueSet rhs, int lumaComponents, int chromaComponents)
    {
      return Utils.EuclidianColorDist(lhs, rhs, lumaComponents, chromaComponents);
    }
  }


  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  public class HSLColorspace : ILCCColorSpace
  {
    public string FormatString { get { return "HSL"; } }
    public LCCColorDenorm RGBToLCC(ColorF c)
    {
      var rgb = new ColorMine.ColorSpaces.Rgb { R = c.R, G = c.G, B = c.B };
      var lab = rgb.To<ColorMine.ColorSpaces.Hsl>();
      // https://github.com/hvalidi/ColorMine/blob/master/ColorMine/ColorSpaces/ColorSpaces.xml
      LCCColorDenorm ret;
      ret.L = lab.L;
      ret.C1 = lab.H;
      ret.C2 = lab.S;
      return ret;
    }

    public double NormalizeL(double x) { return Utils.Clamp(x / 100, 0, 1); }
    public double NormalizeC1(double x) { return Utils.Clamp(x / 360, 0, 1); }// H
    public double NormalizeC2(double x) { return NormalizeL(x); }// S
    public double DenormalizeL(double x) { return x * 100; }
    public double DenormalizeC1(double x) { return x * 360; }
    public double DenormalizeC2(double x) { return DenormalizeL(x); }

    public double ColorDistance(ValueSet lhs, ValueSet rhs, int lumaComponents, int chromaComponents)
    {
      double acc = 0;
      for (int i = 0; i < lumaComponents; ++i)
      {
        double d = Math.Abs(lhs[i] - rhs[i]);
        acc += d * d;
      }
      acc /= lumaComponents;
      if (chromaComponents == 2)
      {
        // C1 (HUE)
        double h1 = lhs[lumaComponents];
        double h2 = rhs[lumaComponents];
        double dh1 = Math.Abs(h1 - h2);
        double dh2 = Math.Abs((360 + h1) - h2);
        double dh3 = Math.Abs(h1 - (360 + h2));
        double dh = Math.Min(Math.Min(dh1, dh2), dh3);
        acc += dh * dh;

        // C2 (Saturation)
        double ds = Math.Abs(lhs[lumaComponents + 1] - rhs[lumaComponents + 1]);
        acc += ds * ds;
      }
      return acc;
    }
  }

}

