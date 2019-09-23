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
    protected override string FormatID { get { return "YUV"; } }
    protected override double NormalizeL(double x) { return Utils.Clamp(x, 0, 1); }
    protected override double NormalizeC1(double x)
    {
      // normalized UV values should map .5 => 0, so our mapping granularity plays better. we'll always have an exactly 0
      // point, but we won't always have .5 in our discrete values.
      x /= 2;// -.5 to .5
      if (x < 0)
        x += 1;
      // 0 to .5, or .5 to 1
      return Utils.Clamp(x, 0, 1);
      //return Utils.Clamp((x / 2)+ .5, 0, 1);
    }
    protected override double NormalizeC2(double x) { return NormalizeC1(x); }

    protected override double DenormalizeL(double x) { return x; }
    protected override double DenormalizeC1(double x)
    {
      //return (x - .5) * 2;
      if (x > .5)
        x -= 1;
      return x * 2;
    }
    protected override double DenormalizeC2(double x) { return DenormalizeC1(x); }

    double lumaMult;
    double chromaMult;

    public override void WriteConfig(StringBuilder sb)
    {
      base.WriteConfig(sb);
      sb.AppendLine(string.Format("lumaMult={0}", this.lumaMult));
      sb.AppendLine(string.Format("chromaMult={0}", this.chromaMult));
    }

    public NaiveYUVPixelFormat(int valuesPerComponent, Size lumaTiles, bool useChroma, double lumaMult = 1.5, double chromaMult = 1) :
      base(valuesPerComponent, lumaTiles, useChroma)
    {
      this.lumaMult = lumaMult;
      this.chromaMult = chromaMult;
    }

    public static NaiveYUVPixelFormat ProcessArgs(string[] args)
    {
      int valuesPerComponent;
      bool useChroma;
      Size lumaTiles;
      LCCPixelFormatProvider.ProcessArgs(args, out valuesPerComponent, out lumaTiles, out useChroma);
      return new NaiveYUVPixelFormat(valuesPerComponent, lumaTiles, useChroma);
    }

    public override unsafe double CalcKeyToColorDist(ValueSet key /* NORMALIZED VALUES */, ValueSet actual /* DENORMALIZED VALUES */, bool verboseDebugInfo = false)
    {
      double acc = 0.0f;
      double m;
      if (verboseDebugInfo)
      {
        Log.WriteLine("      : Calculating distance between");
        Log.WriteLine("      : denormalized actual values: " + actual);
        Log.WriteLine("      : normalized key: " + key);
      }

      Denormalize(ref key);
      if (verboseDebugInfo)
      {
        Log.WriteLine("      : denormalized key: " + key);
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
            Log.WriteLine("      : Luma component {0}", i);
            Log.WriteLine("      :   dist between Y {0} and {1}", keyY, actualY);
            Log.WriteLine("      :   m={0}; m*m={1}", m, m * m);
            Log.WriteLine("      :   acc = " + acc);
          }
        }
        if (verboseDebugInfo)
        {
          Log.WriteLine("      : retdist={0}", acc);
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
          Log.WriteLine("      : Luma component {0}", i);
          Log.WriteLine("      :   dist between Y {0} and {1}", keyY, actualY);
          Log.WriteLine("      :   dY={0}; dY*dY={1}", dY, dY * dY);
          Log.WriteLine("      :   dU={0}; dU*dU={1}", dU, dU * dU);
          Log.WriteLine("      :   dV={0}; dV*dV={1}", dV, dV * dV);
          Log.WriteLine("      :   du+dv*1-lw={0}", chromaComponent);
          Log.WriteLine("      :   dy+du+dv={0}", tileAcc);
          Log.WriteLine("      :   Sqrt = {0}", Math.Sqrt(tileAcc));
          Log.WriteLine("      :   acc = " + acc);
        }
      }
      if (verboseDebugInfo)
      {
        Log.WriteLine("      : retdist={0}", acc);
      }
      return acc;
    }
  }

}

