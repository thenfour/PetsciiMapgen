﻿//#define DUMP_IMAGEPROC_PIXELS

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
  // handles packing color info.
  public class PixelFormatProvider
  {
    public Size lumaTiles;
    public bool useChroma;
    double lumaBias;

    public int DimensionCount { get; private set; } // # of dimensions (UV + Y*size)
    public int LumaComponentCount { get; private set; }
    public int ChromaComponentCount { get; private set; }
    public float[] DiscreteNormalizedValues { get; private set; }

    public string PixelFormatString
    {
      get
      {
        // 5xx(3x3+2)
        return string.Format("{0}xx({1}x{2}+{3})", DiscreteNormalizedValues.Length, lumaTiles.Width, lumaTiles.Height, useChroma ? 2 : 0);
      }
    }

    // pixel format will also determine how many entries are in the resulting map.
    public int MapEntryCount
    {
      get
      {
        return (int)Utils.Pow(DiscreteNormalizedValues.LongLength, (uint)DimensionCount);
      }
    }

    public PixelFormatProvider(int valuesPerComponent, Size lumaComponents, bool useChroma, double lumaBias)
    {
      this.lumaBias = lumaBias;
      this.useChroma = useChroma;
      this.lumaTiles = lumaComponents;
      LumaComponentCount = Utils.Product(lumaComponents);
      ChromaComponentCount = (useChroma ? 2 : 0);

      DimensionCount = LumaComponentCount + ChromaComponentCount;

      this.DiscreteNormalizedValues = Utils.GetDiscreteNormalizedValues(valuesPerComponent);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueYIndex(int tx, int ty)
    {
      return (ty * lumaTiles.Width) + tx;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueUIndex()
    {
      return LumaComponentCount;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueVIndex()
    {
      return LumaComponentCount + 1;
    }


    public unsafe double CalcKeyToColorDist(ValueSet key /* NORMALIZED VALUES */, ValueSet actual /* DENORMALIZED VALUES */)
    {
      double acc = 0.0f;
      double m;

      //ValueSet key = key__;
      Denormalize(ref key);

      if (!useChroma)
      {
        for (int i = 0; i < LumaComponentCount; ++i)
        {
          double keyY = key.ColorData[i];
          double actualY = actual.ColorData[i];
          m = Math.Abs(keyY - actualY);
          double tileAcc = m * m * lumaBias;
          acc += Math.Sqrt(tileAcc);
        }
        return acc;
      }
      double actualU = actual.ColorData[GetValueUIndex()];
      double actualV = actual.ColorData[GetValueVIndex()];
      double keyU = key.ColorData[GetValueUIndex()];
      double keyV = key.ColorData[GetValueVIndex()];

      for (int i = 0; i < LumaComponentCount; ++i)
      {
        double keyY = key.ColorData[i];
        double actualY = actual.ColorData[i];
        m = Math.Abs(keyY - actualY);
        double tileAcc = m * m * lumaBias;
          m = Math.Abs(actualU - keyU);// * f;
          tileAcc += m * m;
          m = Math.Abs(actualV - keyV);// * f;
          tileAcc += m * m;

        acc += Math.Sqrt(tileAcc);
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
          ColorF tileLAB = RGBToLAB(tileRGB);
          ci.actualValues.ColorData[GetValueYIndex(tx, ty)] = (float)tileLAB.R;
        }
      }

      if (useChroma)
      {
        charRGB = charRGB.Div(Utils.Product(lumaTiles));
        ColorF charLAB = RGBToLAB(charRGB);
        ci.actualValues.ColorData[GetValueUIndex()] = (float)charLAB.G;
        ci.actualValues.ColorData[GetValueVIndex()] = (float)charLAB.B;
      }
    }



    public static ColorF RGBToLAB(ColorF c)//, out float y, out float u, out float v)
    {
      var rgb = new ColorMine.ColorSpaces.Rgb { R = c.R, G = c.G, B = c.B };
      var lab = rgb.To<ColorMine.ColorSpaces.Lab>();
      // https://github.com/hvalidi/ColorMine/blob/master/ColorMine/ColorSpaces/ColorSpaces.xml
      return ColorFUtils.FromRGB(lab.L, lab.A, lab.B);
    }
    public static ColorF RGBToNormalizedLAB(ColorF c)
    {
      ColorF ret = RGBToLAB(c);
      ret.R = NormalizeY(ret.R);
      ret.G = NormalizeUV(ret.G);
      ret.B = NormalizeUV(ret.B);
      return ret;
    }
    internal static double NormalizeY(double y)
    {
      return Utils.Clamp(y / 100, 0, 1);
    }
    internal static double NormalizeUV(double uv)
    {
      return Utils.Clamp((uv / 255) + .5f, 0, 1);
    }
    internal static double DenormalizeUV(double uv)
    {
      return (uv - .5) * 255;
    }
    internal unsafe void Denormalize(ref ValueSet v)
    {
      // changes normalized 0-1 values to YUV-ranged values. depends on value format and stuff.
      int chromaelements = 0;
      //int n = v.ValuesLength;
      int n = DimensionCount;
      if (useChroma)
      {
        chromaelements = 2;
        v.ColorData[DimensionCount - 1] = (float)DenormalizeUV(v.ColorData[n - 1]);// - .5f) * 255;
        v.ColorData[DimensionCount - 2] = (float)DenormalizeUV(v.ColorData[n - 2]);// - .5f) * 255;
      }
      for (int i = 0; i < DimensionCount - chromaelements; ++i)
      {
        v.ColorData[i] *= 100;
      }
    }
    internal unsafe double NormalizeElement(ValueSet v, int elementToNormalize)
    {
      if (useChroma)
      {
        // valueCount-1 = element of V
        // valueCount-2 = element of U
        if (elementToNormalize >= DimensionCount - 2)
        {
          return NormalizeUV(v.ColorData[elementToNormalize]);
        }
      }
      return NormalizeY(v.ColorData[elementToNormalize]);
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

    public int GetMapIndexOfRegion(Bitmap img, int x, int y, Size sz)
    {
      ColorF rgb = ColorFUtils.Init;
      ColorF lab = ColorFUtils.Init;
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
          lab = RGBToLAB(rgb);

          norm = RGBToNormalizedLAB(rgb);
          vals[GetValueYIndex(tx, ty)] = (float)norm.R;
          charRGB = charRGB.Add(rgb);
        }
      }

      if (useChroma)
      {
        int numTiles = Utils.Product(lumaTiles);
        charRGB = charRGB.Div(numTiles);
        norm = RGBToNormalizedLAB(charRGB);
        vals[GetValueUIndex()] = (float)norm.G;
        vals[GetValueVIndex()] = (float)norm.B;
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

