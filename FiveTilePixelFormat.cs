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
  public interface IFiveTileTessellator
  {
    string DisplayName { get; }
    int GetLumaTileIndexOfPixelPosInCell(int x, int y, Size cellSize);
  }

  ///////////////////////////////////////////////////////////////////////////////////////////////////////
  //   ........########################
  //   ..........######################
  //   ..........######################
  //   ............##    ##############
  //   ............        ##########33
  //   ..........            ####333333
  //   ........                33333333
  //   ......                    333333
  //   ......                    333333
  //   ........                33333333
  //   ......////            3333333333
  //   ..//////////        333333333333
  //   //////////////    //333333333333
  //   //////////////////////3333333333
  //   //////////////////////3333333333
  //   ////////////////////////33333333
  public class FiveTileTesselatorA : IFiveTileTessellator
  {
    public string DisplayName { get { return "A"; } }
    protected float Rotation { get; private set; } = 0.5f; // 0-1, default 0.5
    // this defines the tiling
    public virtual int GetLumaTileIndexOfPixelPosInCell(int x, int y, Size cellSize)
    {
      vec2 posInCell01 = vec2.Init(x, y)
        .add(.5f)// center in the pixel
        .dividedBy(cellSize);

      vec2 skewAmt = posInCell01.minus(.5f).multipliedBy(this.Rotation);
      skewAmt.y = -skewAmt.y;

      vec2 tile = posInCell01.add(skewAmt.yx());// which tile are we in (x=0,1, y=0,1)
      ivec2 itile = tile.step(.5f);
      int tileIdx = itile.x + itile.y * 2;

      posInCell01 = posInCell01.minus(.5f).abs();
      float m = posInCell01.x + posInCell01.y;
      if (m < 1.0 / 3.0)
        tileIdx = 4;// arbitrary number that looks good perceptually.
      return tileIdx;
    }
  }

  ///////////////////////////////////////////////////////////////////////////////////////////////////////
  ///this has a more regular shape for the center, no jaggies. but still has a lot of angled edges.
  //   ..........######################
  //   ..........######################
  //   ............####################
  //   ............####################
  //   ............    ################
  //   ............        ########3333
  //   ..........              33333333
  //   ..........              33333333
  //   ........              3333333333
  //   ........              3333333333
  //   ....////////        333333333333
  //   ////////////////    333333333333
  //   ////////////////////333333333333
  //   ////////////////////333333333333
  //   //////////////////////3333333333
  //   //////////////////////3333333333

  public class FiveTileTesselatorB : IFiveTileTessellator
  {
    public string DisplayName { get { return "B"; } }
    protected float Rotation { get; private set; } = 0.4f; // 0-1, default 0.5
    // this defines the tiling
    public virtual int GetLumaTileIndexOfPixelPosInCell(int x, int y, Size cellSize)
    {
      vec2 posInCell01 = vec2.Init(x, y)
        .add(.5f)// center in the pixel
        .dividedBy(cellSize);

      vec2 skewAmt = posInCell01.minus(.5f).multipliedBy(this.Rotation);
      skewAmt.y = -skewAmt.y;

      vec2 tile = posInCell01.add(skewAmt.yx());// which tile are we in (x=0,1, y=0,1)
      ivec2 itile = tile.step(.5f);
      int tileIdx = itile.x + itile.y * 2;

      //vec2 c = posInCell01 + skewAmt.yx - .5;// which tile are we in (x=0,1, y=0,1)
      vec2 c = posInCell01.add(skewAmt.yx()).add(-.5f);// which tile are we in (x=0,1, y=0,1)
      //tileIdx = step(abs(c.x) + abs(c.y), .3) == 1. ? 4. : tileIdx;
      if ((Math.Abs(c.x) + Math.Abs(c.y)) < .333)
      {
        tileIdx = 4;
      }


      //posInCell01 = posInCell01.minus(.5f).abs();
      //float m = posInCell01.x + posInCell01.y;
      //if (m < 1.0 / 3.0)
      //  tileIdx = 4;// arbitrary number that looks good perceptually.
      return tileIdx;
    }
  }

  ///////////////////////////////////////////////////////////////////////////////////////////////////////
  // this one tries to improve by having more on-axis edges, and the center is just skewed.
  // https://www.shadertoy.com/view/3dK3zm
  // :           B<------|-A-->
  // :   ................|.....###########  ^
  // :   ................|.....###########  |
  // :   ................|.....###########  |
  // :   .......0........|.....##### 1 ###  - b
  // :   ............... |      ##########
  // :   .........       |      ##########
  // : A /////////       |       #########
  // : | //////////      |       #########
  // :   ---------------------------------
  // :   ///////////     |        ######## |
  // :   ///////////     |        ######## A
  // :   ///////////     |        33333333
  // :   //// 2 /////    |  33333333333333
  // : B ////////////3333|3333333333333333
  // : ^ ////////////3333|3333333333333333
  // : | ////////////3333|3333333333333333
  // : | ////////////3333|3333333333333333
  // :               a---|       B<-------
  public class FiveTileTesselatorC : IFiveTileTessellator
  {
    public string DisplayName { get { return "C"; } }
    Size CellSize;
    int[] TileIdx = null;

    // in theory these are between 0-.5
    // but they must average to <=.25
    float A;
    float B;

    public FiveTileTesselatorC(float a = .125f, float b = .2f)
    {
      A = a;
      B = b;
    }

    public int GetIdxForCell(int x, int y)
    {
      vec2 C = vec2.Init(x, y);
      vec2 cellOrigin = (C.dividedBy(CellSize)).floor().multipliedBy(CellSize);
      vec2 celluv = (C.minus(cellOrigin)).dividedBy(CellSize);

      // mirroring
      int[] idxMap = new int[4] { 0, 2, 4, 0 };
      if (celluv.x >= .5)
      {
        if (celluv.y >= .5)
        {
          celluv = vec2.Init(1f - celluv.x, 1f - celluv.y); //  bottom right quad
          idxMap = new int[4] { 3, 1, 4, 3 };
        }
        else
        {
          celluv = vec2.Init(celluv.y, 1f - celluv.x);// top right
          idxMap = new int[4] { 1, 0, 4, 1 };
        }
      }
      else if (celluv.y >= .5)
      {
        celluv = vec2.Init(1f - celluv.y, celluv.x);// bottom left
        idxMap = new int[4] { 2, 3, 4, 2 };
      }

      // calculate relevant points
      vec2 ptA = vec2.Init(B, .5f - A); // pt in this quad
      vec2 ptB = vec2.Init(.5f + A, B); // pt in right quad
      vec2 ptC = vec2.Init(.5f - A, 1.0f - B); // pt in lower (um, upper) quad

      float slopeAC = (ptA.x - ptC.x) / (ptA.y - ptC.y);
      float slopeAB = (ptA.x - ptB.x) / (ptA.y - ptB.y);

      float yAB = (ptA.x - celluv.x) * slopeAB;
      float yAC = (ptA.x - celluv.x) * slopeAC;

      int tileIdx = idxMap[0];
      if (celluv.y - ptA.y > yAB && celluv.y > ptA.y)
      {
        tileIdx = idxMap[1];
      }
      else if (celluv.y - ptA.y > yAC)
      {
        tileIdx = idxMap[2];
      }
      else if (celluv.x > ptA.x)
      {
        tileIdx = idxMap[3];
      }
      return tileIdx;
    }


    private void Init(Size cellSize)
    {
      this.CellSize = cellSize;
      this.TileIdx = new int[Utils.Product(this.CellSize)];
      // calc all pixels in upper-left quadrant, mirror to others. well
      // i'm really just going to copy this from a shader.
      // https://www.shadertoy.com/view/3dK3zm
      for (int y = 0; y < cellSize.Height; ++y)
      {
        for (int x = 0; x < cellSize.Width; ++x)
        {
          this.TileIdx[x + y * CellSize.Width] = GetIdxForCell(x, y);
        }
      }
    }

    public virtual int GetLumaTileIndexOfPixelPosInCell(int x, int y, Size cellSize)
    {
      if (TileIdx == null || !CellSize.Equals(cellSize))
      {
        Init(cellSize);
      }
      return TileIdx[x + y * cellSize.Width];
    }
  }


  ///////////////////////////////////////////////////////////////////////////////////////////////////////
  // base class for pixel formats using colorspace represented by separate luminance + 2 chroma-ish colorants
  public class FiveTilePixelFormat : IPixelFormatProvider
  {
    public double CalcKeyToColorDist(ValueSet key, ValueSet actual, bool verboseDebugInfo = false)
    {
      //Denormalize(ref key);
      return this.Colorspace.ColorDistance(key, actual, LumaComponentCount, ChromaComponentCount);
    }
    public int DimensionCount { get; private set; } // # of dimensions (UV + Y*size)
    public float[] DiscreteNormalizedValues { get; private set; }

    protected int LumaComponentCount { get { return 5; } }
    protected int ChromaComponentCount { get; private set; }

    protected bool UseChroma { get; private set; }

    protected ILCCColorSpace Colorspace { get; private set; }
    protected IFiveTileTessellator Tessellator { get; private set; } = null;

    public virtual string PixelFormatString
    {
      get
      {
        return string.Format("FiveTile{4}{3}{0}v{1}+{2}",
          DiscreteNormalizedValues.Length,
          LumaComponentCount,
          UseChroma ? 2 : 0,
          Colorspace.FormatString,
          Tessellator.DisplayName);
      }
    }

    public static FiveTilePixelFormat ProcessArgs(string[] args, IFontProvider font)
    {
      FiveTilePixelFormat ret = new FiveTilePixelFormat();
      //ret.Rotation = 0.5f;
      int valuesPerComponent = 255;
      args.ProcessArg("-pfargs", s =>
      {
        valuesPerComponent = int.Parse(s.Split('v')[0]);
        ret.UseChroma = int.Parse(s.Split('+')[1]) > 1;
      });
      args.ProcessArg("-tessellator", s =>
      {
        switch (s.ToLowerInvariant())
        {
          case "a":
            ret.Tessellator = new FiveTileTesselatorA();
            break;
          case "b":
            ret.Tessellator = new FiveTileTesselatorB();
            break;
          case "c":
            ret.Tessellator = new FiveTileTesselatorC();
            break;
        }
      });

      if (ret.Tessellator == null)
      {
        ret.Tessellator = new FiveTileTesselatorC();
      }

      ret.Colorspace = Utils.ParseRequiredLCCColorSpaceArgs(args);
      ret.ChromaComponentCount = (ret.UseChroma ? 2 : 0);
      ret.DimensionCount = ret.LumaComponentCount + ret.ChromaComponentCount;

      ret.DiscreteNormalizedValues = Utils.GetDiscreteNormalizedValues(valuesPerComponent);

      Utils.DumpTessellation(ret.Tessellator, font.CharSizeNoPadding);
      return ret;
    }

    public int MapEntryCount
    {
      get
      {
        return (int)Utils.Pow(DiscreteNormalizedValues.LongLength, (uint)DimensionCount);
      }
    }

    private FiveTilePixelFormat()
    {
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

    public int NormalizedValueSetToMapID(float[] vals)
    {
      return Utils.NormalizedValueSetToMapID(vals, DimensionCount, DiscreteNormalizedValues, MapEntryCount);
    }

    public int DebugGetMapIndexOfColor(ColorF charRGB)
    {
      var norm = RGBToNormalizedLCC(charRGB);
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

    public unsafe void PopulateCharColorData(CharInfo ci, IFontProvider font)
    {
      ColorF charRGB = ColorF.Init;
      ColorF[] lumaRGB = new ColorF[LumaComponentCount];
      int[] pixelCounts = new int[LumaComponentCount];
      for (int i = 0; i < LumaComponentCount; ++i)
      {
        lumaRGB[i] = ColorF.Init;
        pixelCounts[i] = 0;
      }

      for (int py = 0; py < font.CharSizeNoPadding.Height; ++py)
      {
        for (int px = 0; px < font.CharSizeNoPadding.Width; ++px)
        {
          ColorF pc = font.GetPixel(ci.srcIndex, px, py);
          charRGB = charRGB.Add(pc);
          int lumaIdx = Tessellator.GetLumaTileIndexOfPixelPosInCell(px, py, font.CharSizeNoPadding);
          lumaRGB[lumaIdx] = lumaRGB[lumaIdx].Add(pc);
          pixelCounts[lumaIdx]++;
        }
      }

      for (int i = 0; i < LumaComponentCount; ++i)
      {
        var pc = pixelCounts[i];
        var lc = lumaRGB[i];
        if (pixelCounts[i] < 1)
        {
          throw new Exception("!!!!!! Your fonts are just too small; i can't sample them properly.");
        }
        lc = lc.Div(pc);
        LCCColorDenorm lccc = Colorspace.RGBToLCC(lc);
        ci.actualValues.DenormalizedValues[i] = (float)lccc.L;
        ci.actualValues.NormalizedValues[i] = (float)Colorspace.NormalizeL(ci.actualValues.DenormalizedValues[i]);
      }

      if (UseChroma)
      {
        charRGB = charRGB.Div(Utils.Product(font.CharSizeNoPadding));
        LCCColorDenorm charLAB = Colorspace.RGBToLCC(charRGB);
        ci.actualValues.DenormalizedValues[GetValueC1Index()] = (float)charLAB.C1;
        ci.actualValues.DenormalizedValues[GetValueC2Index()] = (float)charLAB.C2;
        ci.actualValues.NormalizedValues[GetValueC1Index()] = (float)Colorspace.NormalizeC1(ci.actualValues.DenormalizedValues[GetValueC1Index()]);
        ci.actualValues.NormalizedValues[GetValueC2Index()] = (float)Colorspace.NormalizeC2(ci.actualValues.DenormalizedValues[GetValueC2Index()]);
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
      ColorF charRGB = ColorF.Init;
      ColorF[] lumaRGB = new ColorF[LumaComponentCount];
      int[] pixelCounts = new int[LumaComponentCount];
      for (int i = 0; i < LumaComponentCount; ++i)
      {
        lumaRGB[i] = ColorF.Init;
        pixelCounts[i] = 0;
      }

      float[] vals = new float[DimensionCount];

      for (int py = 0; py < sz.Height; ++py)
      {
        for (int px = 0; px < sz.Width; ++px)
        {
          ColorF pc = ColorF.From(img.GetPixel(x + px, y + py));
          charRGB = charRGB.Add(pc);
          int lumaIdx = Tessellator.GetLumaTileIndexOfPixelPosInCell(px, py, sz);
          lumaRGB[lumaIdx] = lumaRGB[lumaIdx].Add(pc);
          pixelCounts[lumaIdx]++;
        }
      }

      for (int i = 0; i < LumaComponentCount; ++i)
      {
        ColorF lc = lumaRGB[i].Div(pixelCounts[i]);
        LCCColorNorm lccc = RGBToNormalizedLCC(lc);
        vals[i] = (float)lccc.L;
      }

      if (UseChroma)
      {
        charRGB = charRGB.Div(Utils.Product(sz));
        LCCColorNorm charLAB = RGBToNormalizedLCC(charRGB);
        vals[GetValueC1Index()] = (float)charLAB.C1;
        vals[GetValueC2Index()] = (float)charLAB.C2;
      }

      int ID = NormalizedValueSetToMapID(vals);
      return ID;

    }
  }


}


