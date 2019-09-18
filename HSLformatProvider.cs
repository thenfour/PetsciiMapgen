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
  public class HSLPixelFormat : IPixelFormatProvider
  {
    public int DimensionCount { get; private set; } // # of dimensions (UV + Y*size)
    public int LumaComponentCount { get; private set; }
    public int ChromaComponentCount { get; private set; } // actually hue + saturation, not chroma
    public float[] DiscreteNormalizedValues { get; private set; }

    public string PixelFormatString
    {
      get
      {
        // 5xx(3x3+2)
        return string.Format("HSL{0}xx({1}x{2}+{3})", DiscreteNormalizedValues.Length, lumaTiles.Width, lumaTiles.Height, useChroma ? 2 : 0);
      }
    }

    private Size lumaTiles;
    private bool useChroma;

    // pixel format will also determine how many entries are in the resulting map.
    public int MapEntryCount
    {
      get
      {
        return (int)Utils.Pow(DiscreteNormalizedValues.LongLength, (uint)DimensionCount);
      }
    }

    public HSLPixelFormat(int valuesPerComponent, Size lumaComponents, bool useChroma)
    {
      this.useChroma = useChroma;
      this.lumaTiles = lumaComponents;
      LumaComponentCount = Utils.Product(lumaComponents);
      ChromaComponentCount = (useChroma ? 2 : 0);

      DimensionCount = LumaComponentCount + ChromaComponentCount;

      this.DiscreteNormalizedValues = Utils.GetDiscreteNormalizedValues(valuesPerComponent);

      Console.WriteLine("  DiscreteNormalizedValues:");
      for (int i = 0; i < this.DiscreteNormalizedValues.Length; ++ i)
      {
        Console.WriteLine("    {0}: {1,10:0.00}", i, this.DiscreteNormalizedValues[i]);
      }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueLIndex(int tx, int ty)
    {
      return (ty * lumaTiles.Width) + tx;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueHIndex()
    {
      return LumaComponentCount;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueSIndex()
    {
      return LumaComponentCount + 1;
    }

    public unsafe double CalcKeyToColorDist(ValueSet key /* NORMALIZED VALUES */, ValueSet actual /* DENORMALIZED VALUES */
#if DEBUG
      , bool verboseDebugInfo = false)
    {
#else
      )
    {
      const bool verboseDebugInfo = false;
#endif
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

      if (!useChroma)
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

      double actualH = actual.ColorData[GetValueHIndex()];
      double actualS = actual.ColorData[GetValueSIndex()];
      double keyH = key.ColorData[GetValueHIndex()];
      double keyS = key.ColorData[GetValueSIndex()];

      for (int i = 0; i < LumaComponentCount; ++i)
      {
        double keyL = key.ColorData[i];
        double actualL = actual.ColorData[i];

        // we need to ignore hue when approaching black (L) or white (L) or gray (S)
        double Sfact = 1;// Math.Min(actualL, 100 - actualL) / 100; // ignore saturation around black / white.
        if (actualL < 5 || actualL > 95)
          Sfact = 0;
        //double SfactKey = Math.Min(keyL, 100 - keyL) / 200; // ignore saturation around black / white
        //double Sfact = Math.Min(SfactKey, SfactActual);

        //double hueFactActual = Math.Min(actualS / 100, SfactActual);
        //double hueFactKey = Math.Min(keyS / 100, SfactKey);
        //double hueFactor = Math.Min(hueFactKey, hueFactActual); // and ignore hue when saturation is low or we're around black/white
        //hueFactor = .5;
        //Sfact = .5;

        double hueFactor = Sfact;
        if (actualS < 5)
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

    public unsafe void PopulateCharColorData(CharInfo ci, FontProvider font)
    {
      ColorF charRGB = ColorFUtils.Init;
      for (int ty = 0; ty < lumaTiles.Height; ++ty)
      {
        for (int tx = 0; tx < lumaTiles.Width; ++tx)
        {
          Size tileSize;
          Point tilePos;

          GetTileInfo(font.CharSizeNoPadding, lumaTiles, tx, ty, out tilePos, out tileSize);
          // process this single tile of this char.
          // grab all pixels for this tile and calculate Y component for each
          ColorF tileRGB = font.GetRegionColor(ci.srcIndex, tilePos, tileSize, lumaTiles, tx, ty);

          charRGB = charRGB.Add(tileRGB);
          ColorF tileHSL = RGBToHSL(tileRGB);
          ci.actualValues.ColorData[GetValueLIndex(tx, ty)] = (float)tileHSL.B;
        }
      }

      if (useChroma)
      {
        charRGB = charRGB.Div(Utils.Product(lumaTiles));
        ColorF charLAB = RGBToHSL(charRGB);
        ci.actualValues.ColorData[GetValueHIndex()] = (float)charLAB.R;
        ci.actualValues.ColorData[GetValueSIndex()] = (float)charLAB.G;
      }
    }



    public static ColorF RGBToHSL(ColorF c)//, out float y, out float u, out float v)
    {
      var rgb = new ColorMine.ColorSpaces.Rgb{ R = c.R, G = c.G, B = c.B };
      var lab = rgb.To<ColorMine.ColorSpaces.Hsl>();
      // https://github.com/hvalidi/ColorMine/blob/master/ColorMine/ColorSpaces/ColorSpaces.xml
      return ColorFUtils.FromRGB(lab.H, lab.S, lab.L);
    }
    public static ColorF RGBToNormalizedHSL(ColorF c)
    {
      ColorF ret = RGBToHSL(c);
      ret.R = NormalizeH(ret.R);
      ret.G = NormalizeSL(ret.G);
      ret.B = NormalizeSL(ret.B);
      return ret;
    }
    internal static double NormalizeH(double y)
    {
      return Utils.Clamp(y / 360, 0, 1);
    }
    internal static double NormalizeSL(double v)
    {
      return Utils.Clamp(v / 100, 0, 1);
    }
    internal static double DenormalizeSL(double uv)
    {
      return uv * 100;
    }
    internal static double DenormalizeH(double uv)
    {
      return uv * 360;
    }
    internal unsafe void Denormalize(ref ValueSet v)
    {
      // changes normalized 0-1 values to YUV-ranged values. depends on value format and stuff.
      int chromaelements = 0;
      int n = DimensionCount;
      if (useChroma)
      {
        chromaelements = 2;
        v.ColorData[DimensionCount - 1] = (float)DenormalizeSL(v.ColorData[n - 1]);// hue
        v.ColorData[DimensionCount - 2] = (float)DenormalizeH(v.ColorData[n - 2]);// sat
      }
      for (int i = 0; i < DimensionCount - chromaelements; ++i)
      {
        v.ColorData[i] = (float)DenormalizeSL(v.ColorData[i]);//*= 100;
      }
    }
    public unsafe double NormalizeElement(ValueSet v, int elementToNormalize)
    {
      if (useChroma)
      {
        if (elementToNormalize == DimensionCount - 2)
        {
          return NormalizeH(v.ColorData[elementToNormalize]);
        }
      }
      return NormalizeSL(v.ColorData[elementToNormalize]);
    }


    public static Point GetTileOrigin(Size charSize, Size numTilesPerChar, int tx, int ty)
    {
      int x = (int)Math.Round(((double)tx / numTilesPerChar.Width) * charSize.Width);
      int y = (int)Math.Round(((double)ty / numTilesPerChar.Height) * charSize.Height);
      return new Point(x, y);
    }

    // takes tile index and returns the position and size of the tile. unified function for this to avoid
    // rounding issues.
    public static void GetTileInfo(Size charSize, Size numTilesPerChar, int tx, int ty, out Point origin, out Size sz)
    {
      Point begin = GetTileOrigin(charSize, numTilesPerChar, tx, ty);
      Point end = GetTileOrigin(charSize, numTilesPerChar, tx + 1, ty + 1);
      origin = begin;
      sz = Utils.Sub(end, begin);
    }

    public int NormalizedValueSetToMapID(float[] vals)
    {
      // figure out which "ID" this value corresponds to.
      // (val - segCenter) would give us the boundary. for example between 0-1 with 2 segments, the center vals are .25 and .75.
      // subtract the center and you get .0 and .5 where you could multiply by segCount and get the proper seg of 0,1.
      // however let's not subtract the center, but rather segCenter*.5. Then after integer floor rounding, it will be the correct
      // value regardless of scale or any rounding issues.
      float halfSegCenter = 0.25f / DiscreteNormalizedValues.Length;

      int ID = 0;
      for (int i = this.DimensionCount - 1; i >= 0; --i)
      {
        float val = vals[i];
        val -= halfSegCenter;
        val = Utils.Clamp(val, 0, 1);
        val *= DiscreteNormalizedValues.Length;
        ID *= DiscreteNormalizedValues.Length;
        ID += (int)Math.Floor(val);
      }

      if (ID >= MapEntryCount)
      {
        ID = MapEntryCount - 1;
      }
      return ID;
    }

    // poooosibly not needed but so far helpful i think for diagnostics.
    public int DebugGetMapIndexOfColor(ColorF charRGB)
    {
      var norm = RGBToNormalizedHSL(charRGB);
      Console.WriteLine("  norm: " + norm);
      float[] vals = new float[DimensionCount];
      for (int i = 0; i < LumaComponentCount; ++ i)
      {
        vals[i] = (float)norm.B;
      }
      vals[GetValueHIndex()] = (float)norm.R;
      vals[GetValueSIndex()] = (float)norm.G;
      Console.WriteLine("  norm valset: " + Utils.ToString(vals, vals.Length));

      int ID = NormalizedValueSetToMapID(vals);
      return ID;
    }

    public int GetMapIndexOfRegion(Bitmap img, int x, int y, Size sz)
    {
      ColorF rgb = ColorFUtils.Init;
      ColorF hsl = ColorFUtils.Init;
      ColorF norm = ColorFUtils.Init;
      float[] vals = new float[DimensionCount];
      ColorF charRGB = ColorFUtils.Init;
      for (int ty = lumaTiles.Height - 1; ty >= 0; --ty)
      {
        for (int tx = lumaTiles.Width - 1; tx >= 0; --tx)
        {
          Point tilePos = GetTileOrigin(sz, lumaTiles, tx, ty);
          // YES this just gets 1 pixel per char-sized area.
          rgb = ColorFUtils.From(img.GetPixel(x + tilePos.X, y + tilePos.Y));
          hsl = RGBToHSL(rgb);

          norm = RGBToNormalizedHSL(rgb);
          vals[GetValueLIndex(tx, ty)] = (float)norm.B;
          charRGB = charRGB.Add(rgb);
        }
      }

      if (useChroma)
      {
        int numTiles = Utils.Product(lumaTiles);
        charRGB = charRGB.Div(numTiles);
        norm = RGBToNormalizedHSL(charRGB);
        vals[GetValueHIndex()] = (float)norm.R;
        vals[GetValueSIndex()] = (float)norm.G;
      }

      int ID = NormalizedValueSetToMapID(vals);

#if DUMP_IMAGEPROC_PIXELS
            Console.WriteLine(" Pixel: rgb:{0} lab:[{1}] norm:[{2}] vals:[{3}] => MapID {4}",
              ColorFUtils.ToString(rgb),
              ColorFUtils.ToString(lab),
              ColorFUtils.ToString(norm),
              string.Join(",", vals.Select(v => v.ToString("0.00"))),
              ID
              );
#endif

      return ID;
    }
  }
}

