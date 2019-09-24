/*
 similar to naive YUV format, but using a fixed 5-tiling per cell.

void mainImage( out vec4 o, vec2 C)
{
    vec2 R = iResolution.xy;
    
    vec2 cellSize = vec2(120.);
    
    vec2 cellOrigin = floor(C/cellSize)*cellSize;
    vec2 posInCell01 = (C - cellOrigin) / cellSize;
    vec2 skewAmt = (posInCell01-.5)*.5;
    skewAmt.y = -skewAmt.y;
    
    //skewAmt = vec2(0);
    
    vec2 tile = posInCell01 + skewAmt.yx;// which tile are we in (x=0,1, y=0,1)
    tile = step(tile, vec2(.5));
    float tileIdx = tile.x + tile.y * 2.;

    posInCell01 -= .5;
    float m = abs(posInCell01.x) + abs(posInCell01.y);
    if (m < .333) tileIdx = 4.;// arbitrary number that looks good perceptually.
    
    o = vec4(tileIdx / 4.);
}

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
  // base class for pixel formats using colorspace represented by separate luminance + 2 chroma-ish colorants
  public abstract class LCC5PixelFormat : IPixelFormatProvider
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

    protected int LumaComponentCount { get { return 5; } }
    protected int ChromaComponentCount { get; private set; }

    //protected Size LumaTiles { get; private set; }
    protected bool UseChroma { get; private set; }

    public virtual void WriteConfig(StringBuilder sb)
    {
      sb.AppendLine("# LCC5 pixel format provider config");
      sb.AppendLine(string.Format("valuesPerComponent={0}", DiscreteNormalizedValues.Length));
      sb.AppendLine(string.Format("chromaComponents={0}", ChromaComponentCount));
    }

    public string PixelFormatString
    {
      get
      {
        return string.Format("{3}{0}v{1}+{2}", DiscreteNormalizedValues.Length, LumaComponentCount, UseChroma ? 2 : 0, FormatID);
      }
    }

    public static void ProcessArgs(string[] args, out int valuesPerComponent, out bool useChroma)
    {
      int valuesPerComponent_ = 255;
      bool useChroma_ = false;
      args.ProcessArg("-pfargs", s =>
      {
        valuesPerComponent_ = int.Parse(s.Split('v')[0]);
        useChroma_ = int.Parse(s.Split('+')[1]) > 1;
      });
      valuesPerComponent = valuesPerComponent_;
      useChroma = useChroma_;
    }

    public int MapEntryCount
    {
      get
      {
        return (int)Utils.Pow(DiscreteNormalizedValues.LongLength, (uint)DimensionCount);
      }
    }

    public LCC5PixelFormat(int valuesPerComponent, bool useChroma)
    {
      this.UseChroma = useChroma;
      ChromaComponentCount = (useChroma ? 2 : 0);

      DimensionCount = LumaComponentCount + ChromaComponentCount;

      this.DiscreteNormalizedValues = Utils.GetDiscreteNormalizedValues(valuesPerComponent);
    }

    internal int GetValueLIndex(int l)
    {
      return l;
    }
    internal int GetValueC1Index()
    {
      return LumaComponentCount;
    }
    internal int GetValueC2Index()
    {
      return LumaComponentCount + 1;
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
      for (int i = 0; i < LumaComponentCount; ++i)
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

    public int NormalizedValueSetToMapID(float[] vals)
    {
      return Utils.NormalizedValueSetToMapID(vals, DimensionCount, DiscreteNormalizedValues, MapEntryCount);
    }

    public int DebugGetMapIndexOfColor(ColorF charRGB)
    {
      var norm = RGBToNormalizedHCL(charRGB);
      Log.WriteLine("  norm: " + norm);
      float[] vals = new float[DimensionCount];
      for (int i = 0; i < LumaComponentCount; ++i)
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


    public int GetLumaTileIndexOfPixelPosInCell(int x, int y, Size cellSize)
    {
      vec2 posInCell01 = vec2.Init(x,y).dividedBy(cellSize);
      vec2 skewAmt = posInCell01.minus(.5f).multipliedBy(.5f);
      skewAmt.y = -skewAmt.y;

      vec2 tile = posInCell01.add(skewAmt.yx());// which tile are we in (x=0,1, y=0,1)
      ivec2 itile = tile.step(.5f);
      int tileIdx = itile.x + itile.y * 2;

      posInCell01 = posInCell01.minus(.5f).abs();
      float m = posInCell01.x + posInCell01.y;
      if (m < .333)
        tileIdx = 4;// arbitrary number that looks good perceptually.
      return tileIdx;
    }


    public unsafe void PopulateCharColorData(CharInfo ci, IFontProvider font)
    {
      ColorF charRGB = ColorFUtils.Init;
      ColorF[] lumaRGB = new ColorF[LumaComponentCount];
      int[] pixelCounts = new int[LumaComponentCount];
      for (int i = 0; i < LumaComponentCount; ++ i)
      {
        lumaRGB[i] = ColorFUtils.Init;
        pixelCounts[i] = 0;
      }

      for (int py = 0; py < font.CharSizeNoPadding.Height; ++py)
      {
        for (int px = 0; px < font.CharSizeNoPadding.Width; ++ px)
        {
          ColorF pc = font.GetPixel(ci.srcIndex, px, py);
          charRGB.Add(pc);
          int lumaIdx = GetLumaTileIndexOfPixelPosInCell(px, py, font.CharSizeNoPadding);
          lumaRGB[lumaIdx].Add(pc);
          pixelCounts[lumaIdx]++;
        }
      }

      for (int i = 0; i < LumaComponentCount; ++i)
      {
        ColorF lc = lumaRGB[i].Div(pixelCounts[i]);
        LCCColor lccc = RGBToHCL(lc);
        ci.actualValues.ColorData[i] = (float)lccc.L;
      }

      if (UseChroma)
      {
        charRGB = charRGB.Div(Utils.Product(font.CharSizeNoPadding));
        LCCColor charLAB = RGBToHCL(charRGB);
        ci.actualValues.ColorData[GetValueC1Index()] = (float)charLAB.C1;
        ci.actualValues.ColorData[GetValueC2Index()] = (float)charLAB.C2;
      }
    }

    /*
     * for analyzing an image, we don't want to sample every single pixel. but we
     * also don't want to only sample once per tile. since conceptually each
     * outer tile is supposed to cover multiple important regions, sample those
     * regions and average.
     */
    public int GetMapIndexOfRegion(Bitmap img, int x, int y, Size sz)
    {
      ColorF charRGB = ColorFUtils.Init;
      ColorF[] lumaRGB = new ColorF[LumaComponentCount];
      int[] pixelCounts = new int[LumaComponentCount];
      for (int i = 0; i < LumaComponentCount; ++i)
      {
        lumaRGB[i] = ColorFUtils.Init;
        pixelCounts[i] = 0;
      }

      float[] vals = new float[DimensionCount];

      for (int py = 0; py < sz.Height; ++py)
      {
        for (int px = 0; px < sz.Width; ++px)
        {
          ColorF pc = ColorFUtils.From(img.GetPixel(x + px, y + py));
          charRGB.Add(pc);
          int lumaIdx = GetLumaTileIndexOfPixelPosInCell(px, py, sz);
          lumaRGB[lumaIdx].Add(pc);
          pixelCounts[lumaIdx]++;
        }
      }

      for (int i = 0; i < LumaComponentCount; ++i)
      {
        ColorF lc = lumaRGB[i].Div(pixelCounts[i]);
        LCCColor lccc = RGBToHCL(lc);
        vals[i] = (float)lccc.L;
      }

      if (UseChroma)
      {
        charRGB = charRGB.Div(Utils.Product(sz));
        LCCColor charLAB = RGBToHCL(charRGB);
        vals[GetValueC1Index()] = (float)charLAB.C1;
        vals[GetValueC2Index()] = (float)charLAB.C2;
      }

      int ID = NormalizedValueSetToMapID(vals);
      return ID;

    }
  }


}


