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
  public interface IPixelFormatProvider
  {
    int DimensionCount { get; } // # of dimensions (UV + Y*size)
    float[] DiscreteNormalizedValues { get; }
    string PixelFormatString { get; }
    int MapEntryCount { get; }// pixel format will also determine how many entries are in the resulting map.
    double CalcKeyToColorDist(ValueSet key /* NORMALIZED VALUES */, ValueSet actual /* DENORMALIZED VALUES */, bool verboseDebugInfo = false);
    void PopulateCharColorData(CharInfo ci, IFontProvider font);
    double NormalizeElement(ValueSet v, int elementToNormalize);
    int NormalizedValueSetToMapID(float[] vals);
    int DebugGetMapIndexOfColor(ColorF charRGB);
    int GetMapIndexOfRegion(Bitmap img, int x, int y, Size sz);
  }



  // base class for pixel formats using colorspace represented by separate luminance + 2 chroma-ish colorants
  public abstract class LCCPixelFormatProvider : IPixelFormatProvider
  {
    // abstract stuff:
    protected abstract string FormatID { get; }
    public abstract double CalcKeyToColorDist(ValueSet key /* NORMALIZED VALUES */, ValueSet actual /* DENORMALIZED VALUES */, bool verboseDebugInfo = false);
    protected abstract LCCColor RGBToHCL(ColorF c);
    protected abstract double NormalizeL(double x);
    protected abstract double NormalizeC1(double x);
    protected abstract double NormalizeC2(double x);
    protected abstract double DenormalizeL(double x);
    protected abstract double DenormalizeC1(double x);
    protected abstract double DenormalizeC2(double x);

    public int DimensionCount { get; private set; } // # of dimensions (UV + Y*size)
    public float[] DiscreteNormalizedValues { get; private set; }

    protected int LumaComponentCount { get; private set; }
    protected int ChromaComponentCount { get; private set; }

    public string PixelFormatString
    {
      get
      {
        return string.Format("{4}{0}v{1}x{2}+{3}", DiscreteNormalizedValues.Length, LumaTiles.Width, LumaTiles.Height, UseChroma ? 2 : 0, FormatID);
      }
    }

    public static void ProcessArgs(string[] args, out int valuesPerComponent, out Size lumaTiles, out bool useChroma)
    {
      int valuesPerComponent_ = 255;
      Size lumaTiles_ = new Size(1, 1);
      bool useChroma_ = false;
      args.ProcessArg("-pfargs", o =>
      {
        // 1v2x3+2
        valuesPerComponent_ = int.Parse(o.Split('v')[0]);
        o = o.Split('v')[1];// 2x3+2
        useChroma_ = int.Parse(o.Split('+')[1]) == 2;
        o = o.Split('+')[0];// 2x3
        lumaTiles_ = new Size(int.Parse(o.Split('x')[0]), int.Parse(o.Split('x')[1]));
      });
      valuesPerComponent = valuesPerComponent_;
      lumaTiles = lumaTiles_;
      useChroma = useChroma_;
    }


    protected Size LumaTiles { get; private set; }
    protected bool UseChroma { get; private set; }

    public int MapEntryCount
    {
      get
      {
        return (int)Utils.Pow(DiscreteNormalizedValues.LongLength, (uint)DimensionCount);
      }
    }

    public LCCPixelFormatProvider(int valuesPerComponent, Size lumaComponents, bool useChroma)
    {
      this.UseChroma = useChroma;
      this.LumaTiles = lumaComponents;
      LumaComponentCount = Utils.Product(lumaComponents);
      ChromaComponentCount = (useChroma ? 2 : 0);

      DimensionCount = LumaComponentCount + ChromaComponentCount;

      this.DiscreteNormalizedValues = Utils.GetDiscreteNormalizedValues(valuesPerComponent);

      Log.WriteLine("  DiscreteNormalizedValues:");
      //bool foundSuitableMidpoint = false;
      for (int i = 0; i < this.DiscreteNormalizedValues.Length; ++ i)
      {
        Log.WriteLine("    {0}: {1,10:0.00}", i, this.DiscreteNormalizedValues[i]);
        //if (Math.Abs(this.DiscreteNormalizedValues[i] - 0.5) < 0.0001)
        //  foundSuitableMidpoint = true;
      }

      //if (!foundSuitableMidpoint)
      //{
      //  Log.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!");
      //  Log.WriteLine("!!!  no 0.5 point was found in discrete values.");
      //  Log.WriteLine("!!!  it means you're likely to have very bad quality matches for black & white points.");
      //  Log.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!");
      //}

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueLIndex(int tx, int ty)
    {
      return (ty * LumaTiles.Width) + tx;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueC1Index()
    {
      return LumaComponentCount;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueC2Index()
    {
      return LumaComponentCount + 1;
    }

    public unsafe void PopulateCharColorData(CharInfo ci, IFontProvider font)
    {
      ColorF charRGB = ColorFUtils.Init;
      for (int ty = 0; ty < LumaTiles.Height; ++ty)
      {
        for (int tx = 0; tx < LumaTiles.Width; ++tx)
        {
          Size tileSize;
          Point tilePos;

          GetTileInfo(font.CharSizeNoPadding, LumaTiles, tx, ty, out tilePos, out tileSize);
          // process this single tile of this char.
          // grab all pixels for this tile and calculate Y component for each
          ColorF tileRGB = font.GetRegionColor(ci.srcIndex, tilePos, tileSize, LumaTiles, tx, ty);

          charRGB = charRGB.Add(tileRGB);
          LCCColor tileLAB = RGBToHCL(tileRGB);
          ci.actualValues.ColorData[GetValueLIndex(tx, ty)] = (float)tileLAB.L;
        }
      }

      if (UseChroma)
      {
        charRGB = charRGB.Div(Utils.Product(LumaTiles));
        LCCColor charLAB = RGBToHCL(charRGB);
        ci.actualValues.ColorData[GetValueC1Index()] = (float)charLAB.C1;
        ci.actualValues.ColorData[GetValueC2Index()] = (float)charLAB.C2;
      }
    }



    public LCCColor RGBToNormalizedHCL(ColorF c)
    {
      LCCColor ret = RGBToHCL(c);
      ret.L = NormalizeL(ret.L);
      ret.C1 = NormalizeC1(ret.C1);
      ret.C2 = NormalizeC2(ret.C2);
      return ret;
    }

    internal unsafe void Denormalize(ref ValueSet v)
    {
      // changes normalized 0-1 values to YUV-ranged values. depends on value format and stuff.
      if (UseChroma)
      {
        v.ColorData[GetValueC1Index()] = (float)DenormalizeC1(v.ColorData[GetValueC1Index()]);
        v.ColorData[GetValueC2Index()] = (float)DenormalizeC2(v.ColorData[GetValueC2Index()]);
      }
      for (int i = 0; i < LumaComponentCount; ++ i)
      {
        v.ColorData[i] = (float)DenormalizeL(v.ColorData[i]);
      }
    }
    public unsafe double NormalizeElement(ValueSet v, int elementToNormalize)
    {
      if (UseChroma)
      {
        if (elementToNormalize == GetValueC1Index())
          return NormalizeC1(v.ColorData[elementToNormalize]);
        if (elementToNormalize == GetValueC2Index())
          return NormalizeC2(v.ColorData[elementToNormalize]);
      }
      return NormalizeL(v.ColorData[elementToNormalize]);
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

    public int DebugGetMapIndexOfColor(ColorF charRGB)
    {
      var norm = RGBToNormalizedHCL(charRGB);
      Log.WriteLine("  norm: " + norm);
      float[] vals = new float[DimensionCount];
      for (int i = 0; i < LumaComponentCount; ++ i)
      {
        vals[i] = (float)norm.L;
      }
      if (UseChroma)
      {
        vals[GetValueC1Index()] = (float)norm.C1;
        vals[GetValueC2Index()] = (float)norm.C2;
      }
      Log.WriteLine("  norm valset: " + Utils.ToString(vals, vals.Length));

      int ID = NormalizedValueSetToMapID(vals);
      return ID;
    }

    public int GetMapIndexOfRegion(Bitmap img, int x, int y, Size sz)
    {
      ColorF rgb = ColorFUtils.Init;
      LCCColor lab = LCCColor.Init;
      LCCColor norm = LCCColor.Init;
      float[] vals = new float[DimensionCount];
      ColorF charRGB = ColorFUtils.Init;
      for (int ty = LumaTiles.Height - 1; ty >= 0; --ty)
      {
        for (int tx = LumaTiles.Width - 1; tx >= 0; --tx)
        {
          Point tilePos = GetTileOrigin(sz, LumaTiles, tx, ty);
          // YES this just gets 1 pixel per char-sized area.
          rgb = ColorFUtils.From(img.GetPixel(x + tilePos.X, y + tilePos.Y));
          lab = this.RGBToHCL(rgb);

          norm = RGBToNormalizedHCL(rgb);
          vals[GetValueLIndex(tx, ty)] = (float)norm.L;
          charRGB = charRGB.Add(rgb);
        }
      }

      if (UseChroma)
      {
        int numTiles = Utils.Product(LumaTiles);
        charRGB = charRGB.Div(numTiles);
        norm = RGBToNormalizedHCL(charRGB);
        vals[GetValueC1Index()] = (float)norm.C1;
        vals[GetValueC2Index()] = (float)norm.C2;
      }

      int ID = NormalizedValueSetToMapID(vals);

#if DUMP_IMAGEPROC_PIXELS
            Log.WriteLine(" Pixel: rgb:{0} lab:[{1}] norm:[{2}] vals:[{3}] => MapID {4}",
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

