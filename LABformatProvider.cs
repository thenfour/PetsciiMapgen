//#define DUMP_IMAGEPROC_PIXELS

/*
 
even though LAB + euclid distance is perceptually great, there are a number of issues with
the way i use it:

1. i combine average luminance over an area with A*B* from other regions. I am not sure
  it's acceptable to do this.

2. the fact that I use such poor granularity means rounding errors are devastating. you can have
  a font char that perfectly matches the image region, but it won't be chosen, because the
  in-between map key is so far off, some other char matched that better.

3. for the charsets i'm using, it's probably better to specialize somehow. for example
  HSL may actually bet preferred with pixel-art type charsets. the problem is that
  i think for these charsets, grayscale is much preferred over wrongly-colored. basically,
  desaturated chars should match better.

  another small issue is that some map values are totally nonsense. for black and white for
  example, there's no point in the A*B* components. we have a whole 2 dimensions there
  which are worthless. the good news is that the mapping still works because the
  image will map to the proper values.
 
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
  public class LABPixelFormat : LCCPixelFormatProvider
  {
    protected override LCCColor RGBToHCL(ColorF c)
    {
      var rgb = new ColorMine.ColorSpaces.Rgb { R = c.R, G = c.G, B = c.B };
      var lab = rgb.To<ColorMine.ColorSpaces.Lab>();
      // https://github.com/hvalidi/ColorMine/blob/master/ColorMine/ColorSpaces/ColorSpaces.xml
      LCCColor ret;
      ret.L = lab.L;
      ret.C1 = lab.A;
      ret.C2 = lab.B;
      return ret;
    }
    protected override string FormatID {  get { return "LAB"; } }
    protected override double NormalizeL(double x) { return Utils.Clamp(x / 100, 0, 1); }
    protected override double NormalizeC1(double x) { return Utils.Clamp((x / 255) + .5f, 0, 1); }
    protected override double NormalizeC2(double x) { return NormalizeC1(x); }

    protected override double DenormalizeL(double x) { return x * 100; }
    protected override double DenormalizeC1(double x) { return (x - .5) * 255; }
    protected override double DenormalizeC2(double x) { return DenormalizeC1(x); }

    public LABPixelFormat(int valuesPerComponent, Size lumaTiles, bool useChroma) :
      base(valuesPerComponent, lumaTiles, useChroma)
    {
    }

    public static LABPixelFormat ProcessArgs(string[] args)
    {
      int valuesPerComponent;
      bool useChroma;
      Size lumaTiles;
      LABPixelFormat.ProcessArgs(args, out valuesPerComponent, out lumaTiles, out useChroma);
      return new LABPixelFormat(valuesPerComponent, lumaTiles, useChroma);
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
          double keyY = key[i];
          double actualY = actual[i];
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
      double actualU = actual[GetValueC1Index()];
      double actualV = actual[GetValueC2Index()];
      double keyU = key[GetValueC1Index()];
      double keyV = key[GetValueC2Index()];

      for (int i = 0; i < LumaComponentCount; ++i)
      {
        double keyY = key[i];
        double actualY = actual[i];
        double dY = Math.Abs(keyY - actualY);
        double tileAcc = dY * dY;
        double dU = Math.Abs(actualU - keyU);// * f;
        //tileAcc += m * m;
        double dV = Math.Abs(actualV - keyV);// * f;
        double chromaComponent = (dU * dU + dV * dV);
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

