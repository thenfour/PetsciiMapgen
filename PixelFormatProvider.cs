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
    void WriteConfig(StringBuilder sb);
  }



  // base class for pixel formats using colorspace represented by separate luminance + 2 chroma-ish colorants
  public abstract class LCCPixelFormatProvider : IPixelFormatProvider
  {
    // abstract stuff:
    protected abstract string FormatID { get; }
    public abstract double CalcKeyToColorDist(ValueSet key /* NORMALIZED VALUES */, ValueSet actual /* DENORMALIZED VALUES */, bool verboseDebugInfo = false);
    protected abstract LCCColorDenorm RGBToHCL(ColorF c);
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

    protected Size LumaTiles { get; private set; }
    protected bool UseChroma { get; private set; }

    public virtual void WriteConfig(StringBuilder sb)
    {
      sb.AppendLine("# LCC pixel format provider config");
      sb.AppendLine(string.Format("valuesPerComponent={0}", DiscreteNormalizedValues.Length));
      sb.AppendLine(string.Format("lumaRows={0}", LumaTiles.Height));
      sb.AppendLine(string.Format("lumaColumns={0}", LumaTiles.Width));
      sb.AppendLine(string.Format("chromaComponents={0}", ChromaComponentCount));
    }

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
        Utils.ParsePFArgs(o, out valuesPerComponent_, out useChroma_, out lumaTiles_);
      });
      valuesPerComponent = valuesPerComponent_;
      lumaTiles = lumaTiles_;
      useChroma = useChroma_;
    }

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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueLIndex(int l)
    {
      return l;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueLIndex(int tx, int ty)
    {
      return GetValueLIndex((ty * LumaTiles.Width) + tx);
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
      ColorF charRGB = ColorF.Init;
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
          LCCColorDenorm tileLAB = RGBToHCL(tileRGB);
          ci.actualValues[GetValueLIndex(tx, ty)] = (float)tileLAB.L;
        }
      }

      if (UseChroma)
      {
        charRGB = charRGB.Div(Utils.Product(LumaTiles));
        LCCColorDenorm charLAB = RGBToHCL(charRGB);
        ci.actualValues[GetValueC1Index()] = (float)charLAB.C1;
        ci.actualValues[GetValueC2Index()] = (float)charLAB.C2;
      }
    }



    public LCCColorNorm RGBToNormalizedHCL(ColorF c)
    {
      LCCColorDenorm d = RGBToHCL(c);
      LCCColorNorm ret;
      ret.L = NormalizeL(d.L);
      ret.C1 = NormalizeC1(d.C1);
      ret.C2 = NormalizeC2(d.C2);
      return ret;
    }

    internal unsafe void Denormalize(ref ValueSet v)
    {
      // changes normalized 0-1 values to YUV-ranged values. depends on value format and stuff.
      if (UseChroma)
      {
        v[GetValueC1Index()] = (float)DenormalizeC1(v[GetValueC1Index()]);
        v[GetValueC2Index()] = (float)DenormalizeC2(v[GetValueC2Index()]);
      }
      for (int i = 0; i < LumaComponentCount; ++ i)
      {
        v[i] = (float)DenormalizeL(v[i]);
      }
    }
    public unsafe double NormalizeElement(ValueSet v, int elementToNormalize)
    {
      if (UseChroma)
      {
        if (elementToNormalize == GetValueC1Index())
          return NormalizeC1(v[elementToNormalize]);
        if (elementToNormalize == GetValueC2Index())
          return NormalizeC2(v[elementToNormalize]);
      }
      return NormalizeL(v[elementToNormalize]);
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
      ColorF rgb = ColorF.Init;
      LCCColorDenorm lab = LCCColorDenorm.Init;
      LCCColorNorm norm = LCCColorNorm.Init;
      float[] vals = new float[DimensionCount];
      ColorF charRGB = ColorF.Init;
      for (int ty = LumaTiles.Height - 1; ty >= 0; --ty)
      {
        for (int tx = LumaTiles.Width - 1; tx >= 0; --tx)
        {
          Point tilePos = GetTileOrigin(sz, LumaTiles, tx, ty);
          // YES this just gets 1 pixel per char-sized area.
          rgb = ColorF.From(img.GetPixel(x + tilePos.X, y + tilePos.Y));
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

