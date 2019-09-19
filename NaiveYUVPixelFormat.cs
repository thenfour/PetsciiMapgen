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
  public class NaiveYUVPixelFormat : LCCPixelFormatProvider
  {
    protected override LCCColor RGBToHCL(ColorF c)
    {
      LCCColor ret;
      c.R /= 255;
      c.G /= 255;
      c.B /= 255;
      //ret.L = (c.R * .299) + (c.G * .587) + (c.B * .114);// don't use balanced grayscale because it ruins the ratios in the chroma components
      ret.L = (c.R + c.G + c.B) / 3;
      ret.C1 = c.G - c.B;
      ret.C2 = c.R - c.B;
      return ret;
    }
    protected override string FormatID { get { return "NaiveYUV"; } }
    protected override double NormalizeL(double x) { return Utils.Clamp(x, 0, 1); }
    protected override double NormalizeC1(double x) { return Utils.Clamp((x / 2)+ .5, 0, 1); }
    protected override double NormalizeC2(double x) { return NormalizeC1(x); }

    protected override double DenormalizeL(double x) { return x; }
    protected override double DenormalizeC1(double x) { return (x - .5) * 2; }
    protected override double DenormalizeC2(double x) { return DenormalizeC1(x); }

    double lumaMult;
    double chromaMult;

    public NaiveYUVPixelFormat(int valuesPerComponent, Size lumaTiles, bool useChroma, double lumaMult = 1.5, double chromaMult = 1) :
      base(valuesPerComponent, lumaTiles, useChroma)
    {
      this.lumaMult = lumaMult;
      this.chromaMult = chromaMult;
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
            Console.WriteLine("      :   dist between Y {0} and {1}", keyY, actualY);
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
      double actualU = actual.ColorData[GetValueC1Index()];
      double actualV = actual.ColorData[GetValueC2Index()];
      double keyU = key.ColorData[GetValueC1Index()];
      double keyV = key.ColorData[GetValueC2Index()];

      for (int i = 0; i < LumaComponentCount; ++i)
      {
        double keyY = key.ColorData[i];
        double actualY = actual.ColorData[i];

        double dY = Math.Abs(keyY - actualY) * lumaMult;
        double tileAcc = dY * dY;
        double dU = Math.Abs(actualU - keyU);// * f;
        double dV = Math.Abs(actualV - keyV);// * f;
        double chromaComponent = (dU * dU + dV * dV) * chromaMult;
        tileAcc += chromaComponent;

        acc += Math.Sqrt(tileAcc);

        if (verboseDebugInfo)
        {
          Console.WriteLine("      : Luma component {0}", i);
          Console.WriteLine("      :   dist between Y {0} and {1}", keyY, actualY);
          Console.WriteLine("      :   dY={0}; dY*dY={1}", dY, dY * dY);
          Console.WriteLine("      :   dU={0}; dU*dU={1}", dU, dU * dU);
          Console.WriteLine("      :   dV={0}; dV*dV={1}", dV, dV * dV);
          Console.WriteLine("      :   du+dv*1-lw={0}", chromaComponent);
          Console.WriteLine("      :   dy+du+dv={0}", tileAcc);
          Console.WriteLine("      :   Sqrt = {0}", Math.Sqrt(tileAcc));
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

