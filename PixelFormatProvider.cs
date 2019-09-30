
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
  // pixel format defines the tiling and packing of color data.
  // the LCC (luma chroma chroma) provider below can use different colorspaces.
  public interface IPixelFormatProvider
  {
    int DimensionCount { get; } // # of dimensions (UV + Y*size)
    float[] DiscreteNormalizedValues { get; }
    string PixelFormatString { get; }
    int MapEntryCount { get; }// pixel format will also determine how many entries are in the resulting map.
    double CalcKeyToColorDist(ValueSet key /* NORMALIZED VALUES */, ValueSet actual /* DENORMALIZED VALUES */, bool verboseDebugInfo = false);
    void PopulateCharColorData(CharInfo ci, IFontProvider font);
    //double NormalizeElement(ValueSet v, int elementToNormalize);
    ValueArray Denormalize(ValueArray va);
    int NormalizedValueSetToMapID(float[] vals);
    int DebugGetMapIndexOfColor(ColorF charRGB);
    int GetMapIndexOfRegion(Bitmap img, int x, int y, Size sz);
  }


  public class SquareLCCPixelFormat : IPixelFormatProvider
  {
    public int DimensionCount { get; private set; } // # of dimensions (UV + Y*size)
    public float[] DiscreteNormalizedValues { get; private set; }

    protected int LumaComponentCount { get; private set; }
    protected int ChromaComponentCount { get; private set; }

    protected Size LumaTiles { get; private set; }
    protected bool UseChroma { get; private set; }

    public ILCCColorSpace Colorspace { get; private set; }

    public string PixelFormatString
    {
      get
      {
        return string.Format("Square{4}{0}v{1}x{2}+{3}",
          DiscreteNormalizedValues.Length,
          LumaTiles.Width, LumaTiles.Height,
          UseChroma ? 2 : 0,
          Colorspace.FormatString);
      }
    }

    public double CalcKeyToColorDist(ValueSet key, ValueSet actual, bool verboseDebugInfo = false)
    {
      //Denormalize(ref key);
      return this.Colorspace.ColorDistance(key, actual, LumaComponentCount, ChromaComponentCount);
    }

    public static SquareLCCPixelFormat ProcessArgs(string[] args/*, out int valuesPerComponent, out Size lumaTiles, out bool useChroma, out ILCCColorSpace cs*/)
    {
      SquareLCCPixelFormat ret = new SquareLCCPixelFormat();

      int valuesPerComponent = 255;
      args.ProcessArg("-pfargs", o =>
      {
        valuesPerComponent = int.Parse(o.Split('v')[0]);
        o = o.Split('v')[1];// 2x3+2
        ret.UseChroma = int.Parse(o.Split('+')[1]) == 2;
        o = o.Split('+')[0];// 2x3
        ret.LumaTiles = new Size(int.Parse(o.Split('x')[0]), int.Parse(o.Split('x')[1]));
      });

      ret.Colorspace = Utils.ParseRequiredLCCColorSpaceArgs(args);
      ret.DiscreteNormalizedValues = Utils.GetDiscreteNormalizedValues(valuesPerComponent);
      ret.LumaComponentCount = Utils.Product(ret.LumaTiles);
      ret.ChromaComponentCount = (ret.UseChroma ? 2 : 0);
      ret.DimensionCount = ret.LumaComponentCount + ret.ChromaComponentCount;

      return ret;
    }

    public int MapEntryCount
    {
      get
      {
        return (int)Utils.Pow(DiscreteNormalizedValues.LongLength, (uint)DimensionCount);
      }
    }

    private SquareLCCPixelFormat()
    {
    }

    internal int GetValueLIndex(int l)
    {
      return l;
    }

    internal int GetValueLIndex(int tx, int ty)
    {
      return GetValueLIndex((ty * LumaTiles.Width) + tx);
    }

    internal int GetValueC1Index()
    {
      return LumaComponentCount;
    }

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
          LCCColorDenorm tileLAB = Colorspace.RGBToLCC(tileRGB);
          ci.actualValues.DenormalizedValues[GetValueLIndex(tx, ty)] = (float)tileLAB.L;
          ci.actualValues.NormalizedValues[GetValueLIndex(tx, ty)] = (float)Colorspace.NormalizeL(ci.actualValues.DenormalizedValues[GetValueLIndex(tx, ty)]);
        }
      }

      if (UseChroma)
      {
        charRGB = charRGB.Div(Utils.Product(LumaTiles));
        LCCColorDenorm charLAB = Colorspace.RGBToLCC(charRGB);
        ci.actualValues.DenormalizedValues[GetValueC1Index()] = (float)charLAB.C1;
        ci.actualValues.DenormalizedValues[GetValueC2Index()] = (float)charLAB.C2;
        ci.actualValues.NormalizedValues[GetValueC1Index()] = (float)Colorspace.NormalizeC1(ci.actualValues.DenormalizedValues[GetValueC1Index()]);
        ci.actualValues.NormalizedValues[GetValueC2Index()] = (float)Colorspace.NormalizeC2(ci.actualValues.DenormalizedValues[GetValueC2Index()]);
      }
    }

    public LCCColorNorm RGBToNormalizedLCC(ColorF c)
    {
      LCCColorDenorm d = Colorspace.RGBToLCC(c);
      LCCColorNorm ret;
      ret.L = Colorspace.NormalizeL(d.L);
      ret.C1 = Colorspace.NormalizeC1(d.C1);
      ret.C2 = Colorspace.NormalizeC2(d.C2);
      return ret;
    }

    public ValueArray Denormalize(ValueArray va)
    {
      ValueArray ret = ValueArray.Init(this.DimensionCount);
      if (UseChroma)
      {
        ret[GetValueC1Index()] = (float)Colorspace.DenormalizeC1(va[GetValueC1Index()]);
        ret[GetValueC2Index()] = (float)Colorspace.DenormalizeC2(va[GetValueC2Index()]);
      }
      for (int i = 0; i < LumaComponentCount; ++i)
      {
        ret[i] = (float)Colorspace.DenormalizeL(va[i]);
      }
      return ret;
    }
    //internal unsafe void Denormalize(ref ValueSet v)
    //{
    //  // changes normalized 0-1 values to YUV-ranged values. depends on value format and stuff.
    //  if (UseChroma)
    //  {
    //    v[GetValueC1Index()] = (float)Colorspace.DenormalizeC1(v[GetValueC1Index()]);
    //    v[GetValueC2Index()] = (float)Colorspace.DenormalizeC2(v[GetValueC2Index()]);
    //  }
    //  for (int i = 0; i < LumaComponentCount; ++ i)
    //  {
    //    v[i] = (float)Colorspace.DenormalizeL(v[i]);
    //  }
    //}
    //public unsafe double NormalizeElement(ValueSet v, int elementToNormalize)
    //{
    //  if (UseChroma)
    //  {
    //    if (elementToNormalize == GetValueC1Index())
    //      return Colorspace.NormalizeC1(v[elementToNormalize]);
    //    if (elementToNormalize == GetValueC2Index())
    //      return Colorspace.NormalizeC2(v[elementToNormalize]);
    //  }
    //  return Colorspace.NormalizeL(v[elementToNormalize]);
    //}

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
      var norm = RGBToNormalizedLCC(charRGB);
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
          lab = Colorspace.RGBToLCC(rgb);

          norm = RGBToNormalizedLCC(rgb);
          vals[GetValueLIndex(tx, ty)] = (float)norm.L;
          charRGB = charRGB.Add(rgb);
        }
      }

      if (UseChroma)
      {
        int numTiles = Utils.Product(LumaTiles);
        charRGB = charRGB.Div(numTiles);
        norm = RGBToNormalizedLCC(charRGB);
        vals[GetValueC1Index()] = (float)norm.C1;
        vals[GetValueC2Index()] = (float)norm.C2;
      }

      int ID = NormalizedValueSetToMapID(vals);

      return ID;
    }
  }
}

