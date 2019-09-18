/*
 
ok so here's a problem i'd love to solve. mapping works fine, but because we're using
a very inaccurate intermediate, there's a great chance of things not getting mapped well.
it means that better-fit characters may not actually be used over less-fit.
example.

MAPKEY:
  id:21 key:[33.,-42.,-42.] mindist:40.3271057619193 mappedtoCharSrc:{X=2,Y=0},fg:,bg:
CHARS in question:
  chosen:    dist 40.33 to char ID:2 src:{X=2,Y=0} [50.,-9.,-26.], keysvisited:64, usages:5
  SHOULD be: dist 57.21 to char ID:5 src:{X=5,Y=0} [34.,-1.,-3.], keysvisited:64, usages:3
IMAGE:
 Pixel: PetsciiMapgen.ColorF with vals [0.36,0.50,0.50] mapped to ID 21


indeed the values look normal and distances look good, though the wrong char is 
selected. In fact char 5 is pixel-for-pixel the same as the image. so how could it
be mapped incorrectly?
because it lies right on a boundary. again we want to avoid this boundary.

here's how the same identical pixel can get mapped incorrectly:

    0.00     0.33     0.66      1.0
     |--------|--------|--------|
           ^      ^
     font char 0.5
     which rounds in this case down to 0.33.
     the font also has something that rounds even closer to .33
     the image has something at .5.
     it will choose the other one, not the one we care about.

     this is a problem of our partitioning really. by using values that hover
     around a point that's especially problematic for partitioning, we will encounter
     tons of bad selections. we should center UV values around a partition boundary
     instead of 0.5.

     we also still need to handle mappings with nonsense LAB colors.
 */

//#define DUMP_CHARINFO
//#define DUMP_MAPINFO
//#define DUMP_MAPCHARINFO
//#define DUMP_IMAGEPROC_PIXELS

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

namespace PetsciiMapgen
{
  public class HybridMap2
  {
    public FontProvider FontProvider { get; private set; }

    public Bitmap mapBmp;
    //public Size charSizeWithPadding;
    //public Size charSize;
    public Size lumaTiles;
    public int valuesPerComponent;
    public int componentsPerCell; // # of dimensions (UV + Y*size)
    public int numYcomponents;
    public bool useChroma;
    double lumaBias;
    long numDestCharacters;
    float[] discreteValues;

    //int fontLeftTopPadding;

    private HybridMap2()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueYIndex(int tx, int ty)
    {
      return (ty * lumaTiles.Width) + tx;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueUIndex()
    {
      return numYcomponents;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueVIndex()
    {
      return numYcomponents + 1;
    }


    //// takes a "real" A or B colorant and shifts it over.
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal double UVShiftHack(double uv)
    //{
    //  double val = discreteValues[discreteValues.Length / 2];// a center point to aim for.
    //  uv = ColorUtils.NormalizeUV(uv); // 0-1
    //  if (uv <= .5)
    //  {
    //    return ColorUtils.DenormalizeUV(uv * val / .5);
    //  }
    //  uv = 1.0 - uv; // now .5,1 => .5-0
    //  uv *= (1.0-val) / .5;
    //  uv = 1.0 - uv;
    //  return ColorUtils.DenormalizeUV(uv);
    //}

    // key is NOT guaranteed to actually be valid CIELAB colors. they are sorta
    // estimates or something.
    // actual IS guaranteed. so in order to actually take a distance, we have to
    // convert key to real colors.
    public unsafe double CalcCellDistance(ValueSet key, ValueSet actual)
    {
      double acc = 0.0f;
      double m;

      if (!useChroma)
      {
        for (int i = 0; i < numYcomponents; ++i)
        {
          double keyY = key.YUVvalues[i];
          double actualY = actual.YUVvalues[i];
          m = Math.Abs(keyY - actualY);
          double tileAcc = m * m * lumaBias;
          acc += Math.Sqrt(tileAcc);
        }
        return acc;
      }
      double actualU = actual.YUVvalues[GetValueUIndex()];
      double actualV = actual.YUVvalues[GetValueVIndex()];
      double keyU = key.YUVvalues[GetValueUIndex()];
      double keyV = key.YUVvalues[GetValueVIndex()];

      //actualU = UVShiftHack(actualU);
      //actualV = UVShiftHack(actualV);
      //keyU = UVShiftHack(keyU);
      //keyV = UVShiftHack(keyV);

      for (int i = 0; i < numYcomponents; ++ i)
      {
        double keyY = key.YUVvalues[i];
        double actualY = actual.YUVvalues[i];
        m = Math.Abs(keyY - actualY);
        double tileAcc = m * m * lumaBias;

        // this is a hack, to make sure that key values which are nonsensical get corrected.
        // the problem is around black & white, where chroma is just meaningless and shouldn't be considered.
        // key values have nonsense because i just generate them from permutations rather than
        // real CIELAB colors.
        //actualU *= actualY / 100;
        //actualV *= actualY / 100;
        //keyU *= keyY / 100;
        //keyV *= keyV / 100;
        ////double f = Math.Abs(keyU - keyV); // 255 range, 0 
        //double f = Math.Max(keyY, 100 - keyY);// 0-50 how far from black or white. 0 = black.
        //if (f != 0)
        //{
        //  int a = 0;
        //}
        //f /= 100;
        //f = 1 - f;

        //if (keyY < 2 || keyY > 98)// black / white processing where UV are meaningless.
        //{
        //  // is it really an issue when the map contains nonsense values for LAB?
        //  // the image will never see those values anyway right?
        //  m = Math.Abs(actualU);
        //  tileAcc += m * m;
        //  m = Math.Abs(actualV);
        //  tileAcc += m * m;
        //}
        //else
        {
          m = Math.Abs(actualU - keyU);// * f;
          tileAcc += m * m;
          m = Math.Abs(actualV - keyV);// * f;
          tileAcc += m * m;
        }

        acc += Math.Sqrt(tileAcc);
      }
      return acc;
    }

    //internal ColorF SelectColor(ColorF c, int? ifg, int? ibg)
    //{
    //  if (!ifg.HasValue)
    //    return c;
    //  return c.R > 0.5f ? monoPalette[ifg.Value] : monoPalette[ibg.Value];
    //}

    public unsafe HybridMap2(FontProvider fontProvider,// string fontFileName, Size charSize,
      Size lumaTiles, int valuesPerComponent,
      PartitionManager pm,//int partitionSegments, int partitionDepth,
      double lumaBias, bool useChroma,
      //ColorF[] monoPalette = null,
      bool outputFullMap = true, bool outputRefMapAndFont = true//,
      //IDitherProvider ditherProvider = null
      )
    {
      Timings timings = new Timings();

      this.FontProvider = fontProvider;

      //this.ditherProvider = ditherProvider;
      //if (this.ditherProvider != null)
      //  this.ditherProvider.DiscreteTargetValues = valuesPerComponent;
      this.FontProvider.Init(valuesPerComponent);
      //this.fontLeftTopPadding = fontLeftTopPadding;
      //this.monoPalette = monoPalette;
      this.lumaBias = lumaBias;
      this.lumaTiles = lumaTiles;
      //this.charSize = charSize;
      this.useChroma = useChroma;
      this.valuesPerComponent = valuesPerComponent;
      this.numYcomponents = Utils.Product(lumaTiles);
      this.componentsPerCell = numYcomponents + (useChroma ? 2 : 0); // number of dimensions

      //var srcImg = System.Drawing.Image.FromFile(fontFileName);
      //var srcBmp = new Bitmap(srcImg);
      //this.charSizeWithPadding = new Size(charSize.Width + fontLeftTopPadding, charSize.Height + fontLeftTopPadding);
      //Size numSrcChars = Utils.Div(srcImg.Size, charSizeWithPadding);

      numDestCharacters = Utils.Pow(valuesPerComponent, (uint)componentsPerCell);

      //Console.WriteLine("Src character size: " + Utils.ToString(charSize));
      //Console.WriteLine("Src character size with padding: " + Utils.ToString(charSizeWithPadding));
      //Console.WriteLine("Src image size: " + Utils.ToString(srcBmp.Size));
      //Console.WriteLine("Number of source chars: " + Utils.ToString(numSrcChars));
      Console.WriteLine("Number of source chars (1d): " + this.FontProvider.CharCount);
      Console.WriteLine("Chosen values per tile: " + valuesPerComponent);
      Console.WriteLine("Dimensions: " + componentsPerCell);
      Console.WriteLine("Resulting map will have this many entries: " + numDestCharacters.ToString("N0"));
      long mapdimpix = (long)Math.Sqrt(numDestCharacters);
      Console.WriteLine("Resulting map will be about: [" + mapdimpix.ToString("N0") + ", " + mapdimpix.ToString("N0") + "]");

      if (mapdimpix > 17000)
      {
        // a healthy safe amount.
        // https://stackoverflow.com/questions/29175585/what-is-the-maximum-resolution-of-c-sharp-net-bitmap
        Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        Console.WriteLine("!!! full map generation will not be possible; too big.");
        Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        Console.WriteLine("press a key to continue anyway.");
        Console.ReadKey();
      }


      // fill in char source info (actual tile values)
      timings.EnterTask("Analyze incoming font");
      var charInfo = new List<CharInfo>();

      for (int ichar = 0; ichar < FontProvider.Length; ++ ichar)
      //for (int y = 0; y < numSrcChars.Height; ++y)
      {
        //for (int x = 0; x < numSrcChars.Width; ++x)
        //{
        //if (monoPalette == null)
        //{
          var ci = new CharInfo(componentsPerCell)
          {
            srcIndex = ichar
          };
          ProcessCharacter(ci);

          ci.masterIdx = charInfo.Count;
          charInfo.Add(ci);
        //}
        //else
        //{
        //  for(int ifg = 0; ifg < monoPalette.Length; ++ ifg)
        //  {
        //    ColorF fg = monoPalette[ifg];
        //    for (int ibg = 0; ibg < monoPalette.Length; ++ibg)
        //    {
        //      if (ifg != ibg)
        //      {
        //        ColorF bg = monoPalette[ibg];
        //        var ci = new CharInfo(componentsPerCell)
        //        {
        //          srcIndex = ichar,
        //          ifg = ifg,
        //          ibg = ibg
        //        };

        //        ProcessCharacter(ci, ifg, ibg);

        //        ci.masterIdx = charInfo.Count;
        //        charInfo.Add(ci);
        //      }
        //    }
        //  }
        //}
       // }
      }

      Console.WriteLine("Number of source chars after palettization: " + charInfo.Count);


      this.discreteValues = Utils.GetDiscreteNormalizedValues(valuesPerComponent);

      // create list of all mapkeys
      var keys = Utils.Permutate(componentsPerCell, useChroma, discreteValues); // returns sorted.
      Console.WriteLine("  Key count: " + keys.Length);

      //PartitionManager pm = new PartitionManager(partitionSegments, partitionDepth);
      foreach (var ci in charInfo)
      {
        pm.AddItem(ci, useChroma);
      }


      timings.EndTask();
      timings.EnterTask("Calculate all mappings");

      // - generate a list of mappings and their distances
      ulong theoreticalMappings = (ulong)charInfo.Count * (ulong)numDestCharacters;
      Console.WriteLine("  Partition count: " + pm.PartitionCount.ToString("N0"));
      Console.WriteLine("  Theoretical mapping count: " + theoreticalMappings.ToString("N0"));

      Utils.ValueRangeInspector distanceRange = new Utils.ValueRangeInspector();
      MappingArray allMappings = new MappingArray();

      long comparisonsMade = 0;
      ProgressReporter pr = new ProgressReporter(keys.Length);
      for (int ikey = 0; ikey < keys.Length; ++ikey)
      {
        pr.Visit(ikey);
        var chars = pm.GetItemsInSamePartition(keys[ikey], useChroma);
        foreach (var ci in chars)
        {
          long imap = allMappings.Add();
          allMappings.Values[imap].icharInfo = ci.masterIdx;
          allMappings.Values[imap].imapKey = ikey;
          double fdist = CalcCellDistance(keys[ikey], ci.actualValues);
          allMappings.Values[imap].dist = fdist;
          distanceRange.Visit(fdist);
          keys[ikey].MinDistFound = Math.Min(keys[ikey].MinDistFound, fdist);
          keys[ikey].Visited = true;
          ci.mapKeysVisited++;
          comparisonsMade++;
        }
      }

      Console.WriteLine("  Mappings generated: {0}", allMappings.Length.ToString("N0"));
      Console.WriteLine("  Comparisons made: {0}", comparisonsMade.ToString("N0"));
      Console.WriteLine("  Distance range encountered: {0}", distanceRange);

      double maxMinDist = 0;
      foreach (var mapKey in keys)
      {
        if (mapKey.Visited)
          maxMinDist = Math.Max(maxMinDist, mapKey.MinDistFound);
      }
      Console.WriteLine("Max minimum distance found: {0}", maxMinDist);

      timings.EndTask();

      timings.EnterTask("Pruning out mappings");
      long itemsRemoved = allMappings.PruneWhereDistGT(maxMinDist);

      Console.WriteLine("   {0} items removed", itemsRemoved);
      Console.WriteLine("   {0} mappings left to choose from", allMappings.Length.ToString("N0"));

      timings.EndTask();
      timings.EnterTask("Sorting mappings");

      allMappings.SortByDist();

      timings.EndTask();
      timings.EnterTask("Select mappings for map");

      // now walk through and fill in mappings from top to bottom.
      // maps key index to charinfo
      Dictionary<long, CharInfo> map = new Dictionary<long, CharInfo>((int)numDestCharacters);

      //foreach (var mapping in allMappings.Values)
      for (int imap = 0; imap < allMappings.Length; ++imap)
      {
        if (keys[allMappings.Values[imap].imapKey].Mapped)
        {
          continue;
        }

        var m = allMappings.Values[imap];
        CharInfo thisCh = charInfo[m.icharInfo];

        // this is an idea that doesn't work because of the way things are sorted.
        //if (imap != allMappings.Length - 1)
        //{
        //  var next = allMappings.Values[imap + 1];
        //  if (next.dist <= (m.dist + 1.1) && next.imapKey == m.imapKey)
        //  {
        //    CharInfo nci = charInfo[next.icharInfo];
        //    if (nci.usages > thisCh.usages)
        //    {
        //      // the next character is just as fit as this one, but has fewer usages. use it instead.
        //      continue;
        //    }
        //  }
        //}

        map[keys[allMappings.Values[imap].imapKey].ID] = thisCh;// charInfo[(int)allMappings.Values[imap].icharInfo];
        keys[allMappings.Values[imap].imapKey].Mapped = true;
        charInfo[(int)allMappings.Values[imap].icharInfo].usages++;
        if (map.Count == numDestCharacters)
          break;
      }
      timings.EndTask();

      int numCharsUsed = 0;
      int numCharsUsedOnce = 0;
      CharInfo mostUsedChar = null;
      int numRepetitions = 0;
      foreach (var ci in charInfo)
      {
        if (mostUsedChar == null || mostUsedChar.usages < ci.usages)
          mostUsedChar = ci;
        if (ci.usages > 0)
          numCharsUsed++;
        if (ci.usages == 1)
          numCharsUsedOnce++;
        if (ci.usages > 1)
          numRepetitions += ci.usages - 1;
      }

      timings.EndTask();

      Console.WriteLine("Post-map stats:");
      Console.WriteLine("  Used char count: " + numCharsUsed);
      Console.WriteLine("  Number of unused char: " + (charInfo.Count - numCharsUsed));
      Console.WriteLine("  Number of chars used exactly once: " + numCharsUsedOnce);
      Console.WriteLine("  Most-used char: " + mostUsedChar + " (" + mostUsedChar.usages + ") usages");
      Console.WriteLine("  Number of total char repetitions: " + numRepetitions);


      // massive dump.
#if DUMP_CHARINFO
      Console.WriteLine("ALL CHAR INFO:");
      foreach (CharInfo ci in charInfo)
      {
        Console.WriteLine("  {0}", ci);
      }
#endif
#if DUMP_MAPINFO
      Console.WriteLine("ALL MAPPING INFO:");
      foreach (var k in keys)
      {
        CharInfo ci = null;
        if (!map.TryGetValue(k.ID, out ci))
        {
          continue;
        }

        Console.WriteLine("  id:{1} key:{0} mindist:{2} mappedtoCharSrc:{3},fg:{4},bg:{5}",
          ValueSet.ToString(k), k.ID, k.MinDistFound, ci.srcIndex, ci.ifg, ci.ibg);

#if DUMP_MAPCHARINFO
        foreach (CharInfo ci2 in charInfo)
        {
          double fdist = CalcCellDistance(k, ci2.actualValues);
          Console.WriteLine("    dist {0} to char {1}", fdist.ToString("0.00"), ci2);
        }
#endif
      }
#endif

      if (outputFullMap)
      {
        OutputFullMap(keys, map);
      }
      if (outputRefMapAndFont)
      {
        OutputRefMapAndFont(keys, map);
      }
    }

    internal void OutputFullMap(ValueSet[] keys, Dictionary<long, CharInfo> map)
    {
      int numCellsX = (int)Math.Ceiling(Math.Sqrt(keys.Count()));
      Size mapImageSize = Utils.Mul(FontProvider.CharSizeNoPadding, numCellsX);// new Size(numCellsX * charSize.Width, numCellsX * charSize.Height);

      Console.WriteLine("MAP image generation...");
      Console.WriteLine("  Cells: [" + numCellsX + ", " + numCellsX + "]");
      Console.WriteLine("  Image size: [" + mapImageSize.Width + ", " + mapImageSize.Height + "]");

      if (mapImageSize.Width > 17000)
      {
        // a healthy safe amount.
        // https://stackoverflow.com/questions/29175585/what-is-the-maximum-resolution-of-c-sharp-net-bitmap
        Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        Console.WriteLine("!!! full map generation not possible; too big.");
        Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        return;
      }

      this.mapBmp = new Bitmap(mapImageSize.Width, mapImageSize.Height, PixelFormat.Format24bppRgb);
      //BitmapData srcData = srcBmp.LockBits(new Rectangle(0, 0, srcBmp.Width, srcBmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
      BitmapData destData = mapBmp.LockBits(new Rectangle(0, 0, mapImageSize.Width, mapImageSize.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

      foreach (ValueSet k in keys)
      {
        CharInfo ci = null;
        map.TryGetValue(k.ID, out ci);
        if (ci == null)
        {
          continue;
        }

        long cellY = k.ID / numCellsX;
        long cellX = k.ID - (numCellsX * cellY);

        FontProvider.BlitCharacter(ci.srcIndex, destData, cellX * FontProvider.CharSizeNoPadding.Width, cellY * FontProvider.CharSizeNoPadding.Height);

        //for (int y = 0; y < charSize.Height; ++y)
        //{
        //  for (int x = 0; x < charSize.Width; ++x)
        //  {
        //    ColorF c = srcData.GetPixel((ci.srcIndex.X * charSizeWithPadding.Width + fontLeftTopPadding) + x, (ci.srcIndex.Y * charSizeWithPadding.Height + fontLeftTopPadding) + y);
        //    c = SelectColor(c, ci.ifg, ci.ibg);
        //    destData.SetPixel((cellX * charSize.Width) + x, (cellY * charSize.Height) + y, c);
        //  }
        //}
      }

      mapBmp.UnlockBits(destData);
      //srcBmp.UnlockBits(srcData);

      mapBmp.Save(string.Format("..\\..\\img\\mapFull-{0}-pow({1},{2}x{3}+{4}).png",
        System.IO.Path.GetFileNameWithoutExtension(FontProvider.FontFileName),
        valuesPerComponent, lumaTiles.Width, lumaTiles.Height, useChroma ? "2" : "0"));
    }

    //internal long HashCharInfo(CharInfo ci)
    //{
    //  if (ci == null) return 0;
    //  long ret = (ci.srcIndex.Y * charSize.Height) + ci.srcIndex.X; // linear char index.
    //  if (ci.ifg.HasValue)
    //  {
    //    ret *= monoPalette.Length;
    //    ret += ci.ifg.Value;// bake in other values.
    //    ret *= monoPalette.Length;
    //    ret += ci.ibg.Value;
    //  }
    //  return ret;
    //}

    // each R,G,B value of the resulting image is a mapping. the inserted value 0-1 refers to a character
    // in the font texture.
    internal unsafe void OutputRefMapAndFont(ValueSet[] keys, Dictionary<long, CharInfo> map)
    {
      // first generate the font because it will determine all the IDs.
      // generate a dictionary of chars used.
      var distinctChars = map.DistinctBy(o => o.Value.srcIndex).ToArray();

      Console.WriteLine("FONT MAP image generation...");
      float fontMapCharCount = distinctChars.Length;
      Console.WriteLine("  Entries linear: " + fontMapCharCount);
      long fontImgPixels = distinctChars.Length * FontProvider.CharSizeNoPadding.Width * FontProvider.CharSizeNoPadding.Height;
      Console.WriteLine("  Total pixels: " + fontImgPixels);
      int fontImgWidthChars = (int)Math.Ceiling(Math.Sqrt(fontImgPixels) / FontProvider.CharSizeNoPadding.Width);
      int fontImgWidthPixels = fontImgWidthChars * FontProvider.CharSizeNoPadding.Width;
      int fontImgHeightChars = (int)Math.Ceiling((double)fontImgPixels / fontImgWidthPixels / FontProvider.CharSizeNoPadding.Height);
      int fontImgHeightPixels = fontImgHeightChars * FontProvider.CharSizeNoPadding.Height;
      Console.WriteLine("  Image size chars: [" + fontImgWidthChars + ", " + fontImgHeightChars + "]");
      Console.WriteLine("  Image size pixels: [" + fontImgWidthPixels + ", " + fontImgHeightPixels + "]");

      var fontBmp = new Bitmap(fontImgWidthPixels, fontImgHeightPixels, PixelFormat.Format24bppRgb);
      //BitmapData srcFontData = srcFontBmp.LockBits(new Rectangle(0, 0, srcFontBmp.Width, srcFontBmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
      BitmapData destFontData = fontBmp.LockBits(new Rectangle(0, 0, fontImgWidthPixels, fontImgHeightPixels), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

      for (int ichar = 0; ichar < distinctChars.Length; ++ ichar)
      {
        CharInfo ci = distinctChars[ichar].Value;
        if (ci == null)
          continue;

        long cellY = ichar / fontImgWidthChars;
        long cellX = ichar - (fontImgWidthChars * cellY);

        FontProvider.BlitCharacter(ci.srcIndex, destFontData, (cellX * FontProvider.CharSizeNoPadding.Width), (cellY * FontProvider.CharSizeNoPadding.Height));

        //for (int y = 0; y < charSize.Height; ++y)
        //{
        //  for (int x = 0; x < charSize.Width; ++x)
        //  {
        //    ColorF c = srcFontData.GetPixel(
        //      (ci.srcIndex.X * charSizeWithPadding.Width) + x + fontLeftTopPadding,
        //      (ci.srcIndex.Y * charSizeWithPadding.Height) + y + fontLeftTopPadding);
        //    //Color c = srcFontBmp.GetPixel((ci.srcIndex.X * charSize.Width) + x, (ci.srcIndex.Y * charSize.Height) + y);
        //    c = SelectColor(c, ci.ifg, ci.ibg);
        //    destFontData.SetPixel((cellX * charSize.Width) + x, (cellY * charSize.Height) + y, c);
        //  }
        //}
      }

      fontBmp.UnlockBits(destFontData);
      //srcFontBmp.UnlockBits(srcFontData);

      fontBmp.Save(string.Format("..\\..\\img\\mapRefFont-{0}-pow({1},{2}x{3}+{4}).png",
        System.IO.Path.GetFileNameWithoutExtension(FontProvider.FontFileName),
        valuesPerComponent, lumaTiles.Width, lumaTiles.Height, useChroma ? "2" : "0"));


      // NOW generate the ref map. since we aim to support >65k fonts, we can't just use
      // a single R/G/B val for an index. there's just not enough precision. The most precise PNG format is 16-bit grayscale.
      // we should just aim to use RGB as 8-bit values, so each pixel is an encoded
      // 24-bit char index.

      double pixelCountD = Math.Ceiling((double)keys.Length);

      int mapWidthPixels = (int)Math.Ceiling(Math.Sqrt(pixelCountD));
      int mapHeightPixels = (int)Math.Ceiling(pixelCountD / mapWidthPixels);

      Console.WriteLine("REF MAP image generation...");
      Console.WriteLine("  Image size: [" + mapWidthPixels + ", " + mapHeightPixels + "]");

      var refMapBmp = new Bitmap(mapWidthPixels, mapHeightPixels, PixelFormat.Format24bppRgb);
      BitmapData destData = refMapBmp.LockBits(new Rectangle(0, 0, mapWidthPixels, mapHeightPixels), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

      for (int i = 0; i < keys.Length; ++ i)
      {
        CharInfo ci = null;
        if (!map.TryGetValue(i, out ci))
        {
          continue;
        }
        int y = i / mapWidthPixels;
        int x = i- (y * mapWidthPixels);
        byte* p = destData.GetRGBPointer(x, y);
        int v = i;
        p[0] = (byte)(v & 0xff);
        v >>= 8;
        p[1] = (byte)(v & 0xff);
        v >>= 8;
        p[2] = (byte)(v & 0xff);
      }

      refMapBmp.UnlockBits(destData);

      refMapBmp.Save(string.Format("..\\..\\img\\mapRef-{0}-pow({1},{2}x{3}+{4}).png",
        System.IO.Path.GetFileNameWithoutExtension(FontProvider.FontFileName),
        valuesPerComponent, lumaTiles.Width, lumaTiles.Height, useChroma ? "2" : "0"));
    }

    // fills in the actual component values for this character.
    private unsafe void ProcessCharacter(CharInfo ci)//, int? ifg, int? ibg)
    {
      ColorF charRGB = ColorFUtils.Init;
      for (int sy = 0; sy < lumaTiles.Height; ++sy)
      {
        for (int sx = 0; sx < lumaTiles.Width; ++sx)
        {
          // eventually it's the pixel format provider that will do the tiling iteration.

          // process a tile
          Size tileSize;
          Point tilePos;

          Utils.GetTileInfo(FontProvider.CharSizeNoPadding, lumaTiles, sx, sy, out tilePos, out tileSize);
          // process this single tile of this char.
          // grab all pixels for this tile and calculate Y component for each
          //int tilePixelCount = 0;
          ColorF tileRGB = this.FontProvider.GetRegionColor(ci.srcIndex, tilePos, tileSize, lumaTiles, sx, sy);

          //for (int py = 0; py < tileSize.Height; ++py)
          //{
          //  for (int px = 0; px < tileSize.Width; ++px)
          //  {
          //    var c = ColorFUtils.From(srcBmp.GetPixel(
          //      charSize.Width * ci.srcIndex.X + tilePos.X + px + fontLeftTopPadding * ci.srcIndex.X + fontLeftTopPadding,
          //      charSize.Height * ci.srcIndex.Y + tilePos.Y + py + fontLeftTopPadding * ci.srcIndex.Y + fontLeftTopPadding));

          //    // dithering.
          //    if (ditherProvider != null)
          //      c = ditherProvider.TransformColor(ci.srcIndex.X * lumaTiles.Width + sx, ci.srcIndex.Y + lumaTiles.Height, c);

          //    // monochrome palette processingc
          //    c = SelectColor(c, ifg, ibg);
          //    tileC = tileC.Add(c);
          //    charC = charC.Add(c);
          //    tilePixelCount++;
          //  }
          //}

          //tileC = tileC.Div(tilePixelCount);
          charRGB = charRGB.Add(tileRGB);
          ColorF tileYUV = ColorUtils.ToMapping(tileRGB);//, out float tileY, out float tileU, out float tileV);
          ci.actualValues.YUVvalues[GetValueYIndex(sx,sy)] = (float)tileYUV.R;
        }
      }

      if (useChroma)
      {
        //int pixelsPerChar = Utils.Product(charSize);
        //charC = charC.Div(pixelsPerChar);
        charRGB = charRGB.Div(Utils.Product(lumaTiles));
        ColorF charYUV = ColorUtils.ToMapping(charRGB);//, out float charY, out float charU, out float charV);
        ci.actualValues.YUVvalues[GetValueUIndex()] = (float)charYUV.G;// charU;
        ci.actualValues.YUVvalues[GetValueVIndex()] = (float)charYUV.B;// charV;
      }
    }

    public unsafe void ProcessImage(string srcImagePath, string destImagePath)
    {
      Console.WriteLine("  tranfsorm image + " + srcImagePath);
      var testImg = Image.FromFile(srcImagePath);
      Bitmap testBmp = new Bitmap(testImg);
      Bitmap destImg = new Bitmap(testBmp.Width, testBmp.Height, PixelFormat.Format32bppArgb);

      int mapCellsX = mapBmp.Width / FontProvider.CharSizeNoPadding.Width;

      using (var g = Graphics.FromImage(destImg))
      {
        ColorF srcColor = ColorFUtils.Init;
        float[] vals = new float[componentsPerCell];
        ColorF yuv = ColorFUtils.Init;
        // roughly simulate the shader algo
        for (int srcCellY = 0; srcCellY < testImg.Height / FontProvider.CharSizeNoPadding.Height; ++srcCellY)
        {
          for (int srcCellX = 0; srcCellX < testImg.Width / FontProvider.CharSizeNoPadding.Width; ++srcCellX)
          {
            // sample in the cell to determine the "key" "ID".
            ColorF charC = ColorFUtils.Init;

            for (int ty = lumaTiles.Height - 1; ty >= 0; --ty)
            {
              for (int tx = lumaTiles.Width - 1; tx >= 0; --tx)
              {
                Point tilePos = Utils.GetTileOrigin(FontProvider.CharSizeNoPadding, lumaTiles, tx, ty);
                srcColor = ColorFUtils.From(testBmp.GetPixel(
                  ((srcCellX) * FontProvider.CharSizeNoPadding.Width) + tilePos.X,
                  ((srcCellY) * FontProvider.CharSizeNoPadding.Height) + tilePos.Y));

                ColorF srcYUV = ColorUtils.ToMappingNormalized(srcColor);//, out float cy, out float cu, out float cv);
                vals[GetValueYIndex(tx, ty)] = (float)srcYUV.R;
                charC = charC.Add(srcColor);
              }
            }
            if (useChroma)
            {
              int numTiles = Utils.Product(lumaTiles);
              charC = charC.Div(numTiles);
              yuv = ColorUtils.ToMappingNormalized(charC);//, out float cy, out float cu, out float cv);
              vals[GetValueUIndex()] = (float)yuv.G;// cu;// Utils.Clamp(cu, 0, 1);
              vals[GetValueVIndex()] = (float)yuv.B;// cv;// Utils.Clamp(cv, 0, 1);
            }

            // figure out which "ID" this value corresponds to.
            // (val - segCenter) would give us the boundary. for example between 0-1 with 2 segments, the center vals are .25 and .75.
            // subtract the center and you get .0 and .5 where you could multiply by segCount and get the proper seg of 0,1.
            // however let's not subtract the center, but rather segCenter*.5. Then after integer floor rounding, it will be the correct
            // value regardless of scale or any rounding issues.
            float halfSegCenter = 0.25f / valuesPerComponent;

            long ID = 0;
            for (int i = this.componentsPerCell - 1; i >= 0; --i)
            {
              float val = vals[i];
              val -= halfSegCenter;
              val = Utils.Clamp(val, 0, 1);
              val *= valuesPerComponent;
              ID *= valuesPerComponent;
              ID += (int)Math.Floor(val);
            }

            if (ID >= numDestCharacters)
            {
              ID = numDestCharacters - 1;
            }

            // ID is now calculated.
            long mapCellY = ID / mapCellsX;
            long mapCellX = ID - (mapCellY * mapCellsX);

#if DUMP_IMAGEPROC_PIXELS
            Console.WriteLine(" Pixel: {0} with vals [{1}] (YUV:{3}) mapped to ID {2}",
              srcColor,
              string.Join(",", vals.Select(v => v.ToString("0.00"))),
              ID,
              ColorFUtils.ToString(yuv)
              );
#endif

            // blit from map img.
            Rectangle srcRect = new Rectangle(
              (int)mapCellX * FontProvider.CharSizeNoPadding.Width,
              (int)mapCellY * FontProvider.CharSizeNoPadding.Height,
              FontProvider.CharSizeNoPadding.Width, FontProvider.CharSizeNoPadding.Height);
            g.DrawImage(mapBmp,
              srcCellX * FontProvider.CharSizeNoPadding.Width,
              srcCellY * FontProvider.CharSizeNoPadding.Height,
              srcRect, GraphicsUnit.Pixel);
          }
        }
      }

      destImg.Save(destImagePath);
    }
  }
}

