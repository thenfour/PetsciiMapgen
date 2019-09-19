/*
 * so a problem with HSL is .. well we already know it's not meant for comparing.
 * but the values are such that it's even worse from a practical standpoint than LAB.
 * in LAB, it's easy for things to hover around 0,.5,.5 for grayish. but HSL,
 * hue becomes 0-360 easily. hm well is it really any different once it's normalized?
 * 
 * the difference is that LAB saturation scales with black/whiteness. so colors
 * close to black will be black.
 * 
 * and overall the results are not better than LAB. so i would really stick with LAB.
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
  public class HSLPixelFormat : LCCPixelFormatProvider
  {
    protected override LCCColor RGBToHCL(ColorF c)
    {
      var rgb = new ColorMine.ColorSpaces.Rgb { R = c.R, G = c.G, B = c.B };
      var lab = rgb.To<ColorMine.ColorSpaces.Hsl>();
      // https://github.com/hvalidi/ColorMine/blob/master/ColorMine/ColorSpaces/ColorSpaces.xml
      LCCColor ret;
      ret.L = lab.L;
      ret.C1 = lab.H;
      ret.C2 = lab.S;
      return ret;
    }
    protected override string FormatID { get { return "HSL"; } }
    protected override double NormalizeL(double x) { return Utils.Clamp(x / 100, 0, 1); }
    protected override double NormalizeC1(double x) { return Utils.Clamp(x / 360, 0, 1); }// H
    protected override double NormalizeC2(double x) { return NormalizeL(x); }// S

    protected override double DenormalizeL(double x) { return x * 100; }
    protected override double DenormalizeC1(double x) { return x * 360; }
    protected override double DenormalizeC2(double x) { return DenormalizeL(x); }

    public HSLPixelFormat(int valuesPerComponent, Size lumaTiles, bool useChroma) :
      base(valuesPerComponent, lumaTiles, useChroma)
    {
    }

    public override unsafe double CalcKeyToColorDist(ValueSet key /* NORMALIZED VALUES */, ValueSet actual /* DENORMALIZED VALUES */, bool verboseDebugInfo = false)
    {
      double acc = 0.0f;
      double m;
      if (verboseDebugInfo)
      {
        Console.WriteLine("      : Calculating distance between");
        Console.WriteLine("      : denormalized actual values: " + actual);
        Console.WriteLine("      : normalized key: " + key);
      }

      Denormalize(ref key);
      if (verboseDebugInfo)
      {
        Console.WriteLine("      : denormalized key: " + key);
      }

      if (!UseChroma)
      {
        for (int i = 0; i < LumaComponentCount; ++i)
        {
          double keyY = key.ColorData[i];
          double actualY = actual.ColorData[i];
          m = Math.Abs(keyY - actualY);

          double tileAcc = m * m;
          acc += Math.Sqrt(tileAcc);

          if (verboseDebugInfo)
          {
            Console.WriteLine("      : Luma component {0}", i);
            Console.WriteLine("      :   dist between L {0} and {1}", keyY, actualY);
            Console.WriteLine("      :   m={0}; m*m={1}", m, m * m);
            Console.WriteLine("      :   acc = " + acc);
          }
        }
        if (verboseDebugInfo)
        {
          Console.WriteLine("      : retdist={0}", acc);
        }
        return acc;
      }

      double actualH = actual.ColorData[GetValueC1Index()];
      double actualS = actual.ColorData[GetValueC2Index()];
      double keyH = key.ColorData[GetValueC1Index()];
      double keyS = key.ColorData[GetValueC2Index()];

      for (int i = 0; i < LumaComponentCount; ++i)
      {
        double keyL = key.ColorData[i];
        double actualL = actual.ColorData[i];

        // we need to ignore hue when approaching black (L) or white (L) or gray (S)
        double Sfact = 1;// Math.Min(actualL, 100 - actualL) / 100; // ignore saturation around black / white.
        if (actualL < 2 || actualL > 98)
          Sfact = 0;
        //double SfactKey = Math.Min(keyL, 100 - keyL) / 200; // ignore saturation around black / white
        //double Sfact = Math.Min(SfactKey, SfactActual);

        //double hueFactActual = Math.Min(actualS / 100, SfactActual);
        //double hueFactKey = Math.Min(keyS / 100, SfactKey);
        //double hueFactor = Math.Min(hueFactKey, hueFactActual); // and ignore hue when saturation is low or we're around black/white
        //hueFactor = .5;
        //Sfact = .5;

        double hueFactor = Sfact;
        if (actualS < 2)
          hueFactor = 0;

        double lfact = 2;

        double dL = Math.Abs(keyL - actualL) * lfact;
        double tileAcc = dL * dL;

        // hue is circular. take the min of several permutations
        double dh1 = Math.Abs(actualH - keyH);
        double dh2 = Math.Abs(actualH - keyH + 1);
        double dh3 = Math.Abs(actualH - keyH - 1);
        double dH = Math.Min(Math.Min(dh1, dh2), dh3) * hueFactor;

        //tileAcc += m * m;
        double dS = Math.Abs(actualS - keyS) * Sfact;// * f;
        double chromaComponent = (dH * dH + dS * dS);
        tileAcc += chromaComponent;

        acc += Math.Sqrt(tileAcc);

        if (verboseDebugInfo)
        {
          Console.WriteLine("      : Luma component {0}", i);
          Console.WriteLine("      :   sfact: {0} huefact: {1}", Sfact, hueFactor);
          Console.WriteLine("      :   dist between Y {0} and {1}", keyL, actualL);
          Console.WriteLine("      :   dH={0}; dH*dH={1}", dH, dH * dH);
          Console.WriteLine("      :   dS={0}; dS*dS={1}", dS, dS * dS);
          Console.WriteLine("      :   dL={0}; dL*dL={1}", dL, dL * dL);
          Console.WriteLine("      :   acc = " + acc);
        }
      }
      if (verboseDebugInfo)
      {
        Console.WriteLine("      : retdist={0}", acc);
      }
      return acc;
    }
  }

}

