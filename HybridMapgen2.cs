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
    public Bitmap mapBmp;
    public Size charSizeWithPadding;
    public Size charSize;
    public Size lumaTiles;
    public int valuesPerComponent;
    public int componentsPerCell; // # of dimensions (UV + Y*size)
    public int numYcomponents;
    public bool useChroma;
    float lumaBias;
    long numDestCharacters;
    Color[] monoPalette = null;
    int fontLeftTopPadding;

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

    // key is NOT guaranteed to actually be valid CIELAB colors. they are sorta
    // estimates or something.
    // actual IS guaranteed. so in order to actually take a distance, we have to
    // convert key to real colors.
    public unsafe double CalcCellDistance(ValueSet key, ValueSet actual)
    {
      double actualU = ColorUtils.RestoreU(actual.Values[GetValueUIndex()]);
      double actualV = ColorUtils.RestoreV(actual.Values[GetValueVIndex()]);
      double keyU = ColorUtils.RestoreU(key.Values[GetValueUIndex()]);
      double keyV = ColorUtils.RestoreV(key.Values[GetValueVIndex()]);
      double acc = 0.0f;
      double m;
      for (int i = 0; i < numYcomponents; ++ i)
      {
        double keyY = ColorUtils.RestoreY(key.Values[i]);

        m = Math.Abs(keyY - ColorUtils.RestoreY(actual.Values[i]));
        double tileAcc = m * m * lumaBias;

        if (keyY < 1 || keyY > 99)// black / white processing where UV are meaningless.
        {
          m = Math.Abs(actualU);
          tileAcc += m * m;
          m = Math.Abs(actualV);
          tileAcc += m * m;
        }
        else
        {
          m = Math.Abs(actualU - keyU);
          tileAcc += m * m;
          m = Math.Abs(actualV - keyV);
          tileAcc += m * m;
        }

        acc += Math.Sqrt(tileAcc);
      }
      return acc;
    }

    internal Color SelectColor(Color c, int? ifg, int? ibg)
    {
      if (!ifg.HasValue)
        return c;
      return c.R > 0.5f ? monoPalette[ifg.Value] : monoPalette[ibg.Value];
    }

    public unsafe HybridMap2(string fontFileName, Size charSize,
      Size lumaTiles, int valuesPerComponent, int valuesPerPartition, float lumaBias, bool useChroma,
      Color[] monoPalette, bool outputFullMap, bool outputRefMapAndFont, int fontLeftTopPadding = 0)
    {
      Timings timings = new Timings();

      this.fontLeftTopPadding = fontLeftTopPadding;
      this.monoPalette = monoPalette;
      this.lumaBias = lumaBias;
      this.lumaTiles = lumaTiles;
      this.charSize = charSize;
      this.useChroma = useChroma;
      this.valuesPerComponent = valuesPerComponent;
      this.numYcomponents = Utils.Product(lumaTiles);
      this.componentsPerCell = numYcomponents + (useChroma ? 2 : 0); // number of dimensions

      var srcImg = System.Drawing.Image.FromFile(fontFileName);
      var srcBmp = new Bitmap(srcImg);
      this.charSizeWithPadding = new Size(charSize.Width + fontLeftTopPadding, charSize.Height + fontLeftTopPadding);
      Size numSrcChars = Utils.Div(srcImg.Size, charSizeWithPadding);

      numDestCharacters = Utils.Pow(valuesPerComponent, (uint)componentsPerCell);

      Console.WriteLine("Src character size: " + Utils.ToString(charSize));
      Console.WriteLine("Src character size with padding: " + Utils.ToString(charSizeWithPadding));
      Console.WriteLine("Src image size: " + Utils.ToString(srcBmp.Size));
      Console.WriteLine("Number of source chars: " + Utils.ToString(numSrcChars));
      Console.WriteLine("Number of source chars (1d): " + Utils.Product(numSrcChars));
      Console.WriteLine("Chosen values per tile: " + valuesPerComponent);
      Console.WriteLine("Dimensions: " + componentsPerCell);
      Console.WriteLine("Resulting map will have this many entries: " + numDestCharacters);

      // fill in char source info (actual tile values)
      timings.EnterTask("Analyze incoming font");
      var charInfo = new List<CharInfo>();
      //int pixelsPerChar = Utils.Product(charSize);

      // used for normalization later
      List<Utils.ValueRangeInspector> ranges = new List<Utils.ValueRangeInspector>();
      for (int i = 0; i < componentsPerCell; ++ i)
      {
        ranges.Add(new Utils.ValueRangeInspector());
      }

      for (int y = 0; y < numSrcChars.Height; ++y)
      {
        for (int x = 0; x < numSrcChars.Width; ++x)
        {
          if (monoPalette == null)
          {
            var ci = new CharInfo(componentsPerCell)
            {
              srcIndex = new Point(x, y)
            };
            ProcessCharacter(srcBmp, ci, null, null);
            for (int i = 0; i < componentsPerCell; ++i)
            {
              ranges[i].Visit(ci.actualValues.Values[i]);
            }

            charInfo.Add(ci);
          }
          else
          {
            //foreach(Color fg in monoPalette)
            for(int ifg = 0; ifg < monoPalette.Length; ++ ifg)
            {
              Color fg = monoPalette[ifg];
              for (int ibg = 0; ibg < monoPalette.Length; ++ibg)
              {
                if (ifg != ibg)
                {
                  Color bg = monoPalette[ibg];
                  var ci = new CharInfo(componentsPerCell)
                  {
                    srcIndex = new Point(x, y),
                    ifg = ifg,
                    ibg = ibg
                  };

                  ProcessCharacter(srcBmp, ci, ifg, ibg);
                  for (int i = 0; i < componentsPerCell; ++i)
                  {
                    ranges[i].Visit(ci.actualValues.Values[i]);
                  }

                  charInfo.Add(ci);
                }
              }
            }
          }
        }
      }

      Console.WriteLine("RANGES encountered:");
      for (int i = 0; i < componentsPerCell; ++i)
      {
        Console.WriteLine("  [" + i + "]: " + ranges[i]);
      }

      float[] discreteValues = Utils.GetDiscreteValues(valuesPerComponent);
      PartitionManager pm = new PartitionManager(valuesPerPartition, componentsPerCell, discreteValues);

      var dp = charInfo.DistinctBy(a => a.partition);
      Console.WriteLine("  Chars placed into distinct partitions: " + dp.Count());

      // create list of all mapkeys
      timings.EnterTask("Generating permutations and partitions");
      var keys = Utils.Permutate(componentsPerCell, discreteValues); // returns sorted.

      Console.WriteLine("  Key count: " + keys.Length);

      // generate a list of partitions and map them to keys.
      timings.EnterTask("Generating permutations");
      var partitions = new Partition[pm.PartitionCountND];
      Console.WriteLine("  Partition count: " + partitions.Length);
      Console.WriteLine("  Elements per partition: " + pm.PartitionMaxElementSize);
      for (int i = 0; i < partitions.Length; ++i)
      {
        Partition.Init(ref partitions[i], pm.PartitionMaxElementSize);
      }


      // assign map keys to partitions.
      // find a definitive "black" and "white" key.
      // all others will just refer to this one.
      // the reason  for this is that black & white are troublesome wrt chromatic colorspaces.
      // at 0% and 100%, chroma is meaningless so it will screw with distance.
      for (uint i = 0; i <  keys.Length; ++ i)
      {
        long partitionIndex = pm.GetPartitionIndex(keys[i]);
        Partition.Add(ref partitions[partitionIndex], i);
      }

      Utils.ValueRangeInspector pe = new Utils.ValueRangeInspector();
      for (int i = 0; i < partitions.Length; ++i)
      {
        pe.Visit(partitions[i].Length);
      }
      Console.WriteLine("  Partition element count range: " + pe.ToString());

      timings.EndTask();
      timings.EndTask();
      timings.EnterTask("Calculate all mappings");

      // - generate a list of mappings and their distances
      ulong theoreticalMappings = (ulong)Utils.Product(numSrcChars) * (ulong)numDestCharacters;
      Console.WriteLine("  (" + theoreticalMappings + " mappings)");
      Utils.ValueRangeInspector distanceRange = new Utils.ValueRangeInspector();
      MappingArray allMappings = new MappingArray();
      for (UInt32 ici = 0; ici < charInfo.Count; ++ ici)
      {
        var ci = charInfo[(int)ici];
        Debug.Assert(partitions[ci.partition].initialized == true);
        foreach (var ikey in partitions[ci.partition].keyIdxs)
        {
          long imap = allMappings.Add();
          allMappings.Values[imap].icharInfo = ici;
          allMappings.Values[imap].imapKey = (uint)ikey;
          double fdist = CalcCellDistance(keys[ikey], ci.actualValues);
          ulong dist = (ulong)(fdist * Constants.DistanceRange);
          allMappings.Values[imap].dist = dist;
          distanceRange.Visit(fdist);
          keys[ikey].MinDistFound = Math.Min(keys[ikey].MinDistFound, dist);
          keys[ikey].Visited = true;
          //ci.versatility += dist;
          ci.mapKeysVisited++;
        }
      }

      Console.WriteLine("  Remaining elements: {0}", allMappings.Length);

      //// some versatility adjustment and normalization
      //ulong maxCharVersatility = 0;
      //foreach (CharInfo ci in charInfo)
      //{
      //  ci.versatility = ci.versatility * Constants.CharVersatilityRange / ci.mapKeysVisited;
      //  if (ci.versatility > maxCharVersatility)
      //    maxCharVersatility = ci.versatility;
      //}
      //maxCharVersatility += 1;// so normalized values never quite reach 1

      Console.WriteLine("Distance range encountered: {0}", distanceRange);

      ulong maxMinDist = 0;
      foreach (var mapKey in keys)
      {
        if (mapKey.Visited)
          maxMinDist = Math.Max(maxMinDist, mapKey.MinDistFound);
      }
      Console.WriteLine("Max minimum distance found: {0}", maxMinDist);

      timings.EndTask();

      timings.EnterTask("Pruning out mappings");
      long itemsRemoved = allMappings.PruneWhereDistGT(maxMinDist);

      //Console.WriteLine("  calculating sort metric");
      //for (int i = 0; i < allMappings.Length; ++i)
      //{
      //  ulong dist = allMappings.Values[i].dist;
      //  float fv = ((float)charInfo[(int)allMappings.Values[i].icharInfo].versatility) / maxCharVersatility;
      //  fv = 1.0f - fv;
      //  ulong vers = (ulong)(fv * Constants.DistanceRange);

      //  allMappings.Values[i].dist = (ulong)(dist * Constants.DistanceRange + vers);// / 256;
      //}

      Console.WriteLine("   {0} items removed", itemsRemoved);
      Console.WriteLine("   {0} items remaining", allMappings.Length);

      timings.EndTask();
      timings.EnterTask("Sorting mappings");

      allMappings.SortByDist();

      timings.EndTask();
      timings.EnterTask("Select mappings for map");

      // now walk through and fill in mappings from top to bottom.
      // maps key index to charinfo
      Dictionary<long, CharInfo> map = new Dictionary<long, CharInfo>((int)numDestCharacters);

      foreach (var mapping in allMappings.Values)
      {
        if (keys[mapping.imapKey].Mapped)
          continue;
        map[keys[mapping.imapKey].ID] = charInfo[(int)mapping.icharInfo];
        keys[mapping.imapKey].Mapped = true;
        charInfo[(int)mapping.icharInfo].usages++;
        if (map.Count == numDestCharacters)
          break;
      }

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

      double missingMappings = numDestCharacters - map.Count;
      timings.EnterTask(string.Format("Fill in any missing mappings (est: {0} / {1}%)", missingMappings, missingMappings * 100 / numDestCharacters));

      int missingKeys = 0;

      foreach (ValueSet k in keys)
      {
        CharInfo ci = null;
        map.TryGetValue(k.ID, out ci);
        if (ci == null)
        {
          // fill in this map key! just find the closest char.
          double minDist = distanceRange.MaxValue;
          foreach (var ci2 in charInfo)
          {
            double fdist = CalcCellDistance(k, ci2.actualValues);
            if (fdist < minDist)
            {
              minDist = fdist;
              ci = ci2;
            }
          }
          missingKeys++;
          map[k.ID] = ci;
        }
      }

      timings.EndTask();

      Console.WriteLine("Post-map stats:");
      Console.WriteLine("  Used char count: " + numCharsUsed);
      Console.WriteLine("  Number of unused char: " + (charInfo.Count - numCharsUsed));
      Console.WriteLine("  Number of chars used exactly once: " + numCharsUsedOnce);
      Console.WriteLine("  Most-used char: " + mostUsedChar + " (" + mostUsedChar.usages + ") usages");
      Console.WriteLine("  Number of total char repetitions: " + numRepetitions);


      // massive dump.
      //Console.WriteLine("ALL CHAR INFO:");
      //foreach (CharInfo ci in charInfo)
      //{
      //  Console.WriteLine("  {0}", ci);
      //}

      //Console.WriteLine("ALL MAPPING INFO:");
      //foreach (var k in keys)
      //{
      //  CharInfo ci = null;
      //  if (!map.TryGetValue(k.ID, out ci))
      //  {
      //    continue;
      //  }

      //  Console.WriteLine("  id:{1} key:{0} mindist:{2} mappedtoCharSrc:{3},fg:{4},bg:{5}",
      //    ValueSet.ToString(k), k.ID, k.MinDistFound, ci.srcIndex, ci.ifg, ci.ibg);

      //  foreach (CharInfo ci2 in charInfo)
      //  {
      //    double fdist = CalcCellDistance(k, ci2.actualValues);
      //    ulong dist = (ulong)(fdist * Constants.DistanceRange);
      //    Console.WriteLine("    dist {0} to char {1}", dist, ci2);
      //  }
      //}


      if (outputFullMap)
      {
        OutputFullMap(keys, map, srcBmp, fontFileName);
      }
      if (outputRefMapAndFont)
      {
        OutputRefMapAndFont(keys, map, srcBmp, fontFileName);
      }
    }

    internal void OutputFullMap(ValueSet[] keys, Dictionary<long, CharInfo> map, Bitmap srcBmp, string fontFileName)
    {
      int numCellsX = (int)Math.Ceiling(Math.Sqrt(keys.Count()));
      Size mapImageSize = new Size(numCellsX * charSize.Width, numCellsX * charSize.Height);

      Console.WriteLine("MAP image generation...");
      Console.WriteLine("  Cells: [" + numCellsX + ", " + numCellsX + "]");
      Console.WriteLine("  Image size: [" + mapImageSize.Width + ", " + mapImageSize.Height + "]");

      this.mapBmp = new Bitmap(mapImageSize.Width, mapImageSize.Height, PixelFormat.Format24bppRgb);
      BitmapData srcData = srcBmp.LockBits(new Rectangle(0, 0, srcBmp.Width, srcBmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
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

        for (int y = 0; y < charSize.Height; ++y)
        {
          for (int x = 0; x < charSize.Width; ++x)
          {
            Color c = srcData.GetPixel((ci.srcIndex.X * charSizeWithPadding.Width + fontLeftTopPadding) + x, (ci.srcIndex.Y * charSizeWithPadding.Height + fontLeftTopPadding) + y);
            c = SelectColor(c, ci.ifg, ci.ibg);
            destData.SetPixel((cellX * charSize.Width) + x, (cellY * charSize.Height) + y, c);
          }
        }
      }

      mapBmp.UnlockBits(destData);
      srcBmp.UnlockBits(srcData);

      mapBmp.Save(string.Format("..\\..\\img\\mapFull-{0}-pow({1},{2}x{3}+{4}).png",
        System.IO.Path.GetFileNameWithoutExtension(fontFileName),
        valuesPerComponent, lumaTiles.Width, lumaTiles.Height, useChroma ? "2" : "0"));
    }

    internal long HashCharInfo(CharInfo ci)
    {
      if (ci == null) return 0;
      long ret = (ci.srcIndex.Y * charSize.Height) + ci.srcIndex.X; // linear char index.
      if (ci.ifg.HasValue)
      {
        ret *= monoPalette.Length;
        ret += ci.ifg.Value;// bake in other values.
        ret *= monoPalette.Length;
        ret += ci.ibg.Value;
      }
      return ret;
    }

    // each R,G,B value of the resulting image is a mapping. the inserted value 0-1 refers to a character
    // in the font texture.
    internal unsafe void OutputRefMapAndFont(ValueSet[] keys, Dictionary<long, CharInfo> map, Bitmap srcFontBmp, string fontFileName)
    {
      // first generate the font because it will determine all the IDs.
      // generate a dictionary of chars used.
      var distinctChars = map.DistinctBy(o => HashCharInfo(o.Value)).ToArray();

      Console.WriteLine("FONT MAP image generation...");
      float fontMapCharCount = distinctChars.Length;
      Console.WriteLine("  Entries linear: " + fontMapCharCount);
      long fontImgPixels = distinctChars.Length * charSize.Width * charSize.Height;
      Console.WriteLine("  Total pixels: " + fontImgPixels);
      int fontImgWidthChars = (int)Math.Ceiling(Math.Sqrt(fontImgPixels) / charSize.Width);
      int fontImgWidthPixels = fontImgWidthChars * charSize.Width;
      int fontImgHeightChars = (int)Math.Ceiling((double)fontImgPixels / fontImgWidthPixels / charSize.Height);
      int fontImgHeightPixels = fontImgHeightChars * charSize.Height;
      Console.WriteLine("  Image size chars: [" + fontImgWidthChars + ", " + fontImgHeightChars + "]");
      Console.WriteLine("  Image size pixels: [" + fontImgWidthPixels + ", " + fontImgHeightPixels + "]");

      var fontBmp = new Bitmap(fontImgWidthPixels, fontImgHeightPixels, PixelFormat.Format24bppRgb);
      BitmapData srcFontData = srcFontBmp.LockBits(new Rectangle(0, 0, srcFontBmp.Width, srcFontBmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
      BitmapData destFontData = fontBmp.LockBits(new Rectangle(0, 0, fontImgWidthPixels, fontImgHeightPixels), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

      for (int ichar = 0; ichar < distinctChars.Length; ++ ichar)
      {
        CharInfo ci = distinctChars[ichar].Value;
        if (ci == null)
          continue;
        ci.index = ichar;

        long cellY = ichar / fontImgWidthChars;
        long cellX = ichar - (fontImgWidthChars * cellY);

        for (int y = 0; y < charSize.Height; ++y)
        {
          for (int x = 0; x < charSize.Width; ++x)
          {
            Color c = srcFontData.GetPixel(
              (ci.srcIndex.X * charSizeWithPadding.Width) + x + fontLeftTopPadding,
              (ci.srcIndex.Y * charSizeWithPadding.Height) + y + fontLeftTopPadding);
            //Color c = srcFontBmp.GetPixel((ci.srcIndex.X * charSize.Width) + x, (ci.srcIndex.Y * charSize.Height) + y);
            c = SelectColor(c, ci.ifg, ci.ibg);
            destFontData.SetPixel((cellX * charSize.Width) + x, (cellY * charSize.Height) + y, c);
          }
        }
      }

      fontBmp.UnlockBits(destFontData);
      srcFontBmp.UnlockBits(srcFontData);

      fontBmp.Save(string.Format("..\\..\\img\\mapRefFont-{0}-pow({1},{2}x{3}+{4}).png",
        System.IO.Path.GetFileNameWithoutExtension(fontFileName),
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
        System.IO.Path.GetFileNameWithoutExtension(fontFileName),
        valuesPerComponent, lumaTiles.Width, lumaTiles.Height, useChroma ? "2" : "0"));
    }

    //internal static HybridMap2 Load(string path, Size charSize, Size tilesPerCell, int valuesPerComponent)
    //{
    //  HybridMap2 ret = new HybridMap2();
    //  ret.mapBmp = new Bitmap(Bitmap.FromFile(path));
    //  ret.charSize = charSize;
    //  ret.lumaTiles = tilesPerCell;
    //  ret.valuesPerComponent = valuesPerComponent;
    //  ret.componentsPerCell = 2 + Utils.Product(tilesPerCell);
    //  return ret;
    //}

    // fills in the actual component values for this character.
    private unsafe void ProcessCharacter(Bitmap srcBmp, CharInfo ci, int? ifg, int? ibg)
    {
      int charR = 0, charG = 0, charB = 0;
      for (int sy = 0; sy < lumaTiles.Height; ++sy)
      {
        for (int sx = 0; sx < lumaTiles.Width; ++sx)
        {
          // process a tile
          Size tileSize;
          Point tilePos;
          Utils.GetTileInfo(charSize, lumaTiles, sx, sy, out tilePos, out tileSize);
          // process this single tile of this char.
          // grab all pixels for this tile and calculate Y component for each
          int tilePixelCount = 0;
          //float tileY = 0;
          int tileR = 0, tileG = 0, tileB = 0;
          for (int py = 0; py < tileSize.Height; ++py)
          {
            for (int px = 0; px < tileSize.Width; ++px)
            {
              var c = srcBmp.GetPixel(
                charSize.Width * ci.srcIndex.X + tilePos.X + px + fontLeftTopPadding * ci.srcIndex.X + fontLeftTopPadding,
                charSize.Height * ci.srcIndex.Y + tilePos.Y + py + fontLeftTopPadding * ci.srcIndex.Y + fontLeftTopPadding);

              // monochrome palette processingc
              c = SelectColor(c, ifg, ibg);
              tileR += c.R;
              tileG += c.G;
              tileB += c.B;
              charR += c.R;
              charG += c.G;
              charB += c.B;
              tilePixelCount++;
            }
          }

          tileR /= tilePixelCount;
          tileG /= tilePixelCount;
          tileB /= tilePixelCount;
          Color tileC = Color.FromArgb(tileR, tileG, tileB);
          ColorUtils.ToMapping(tileC, out float tileY, out float tileU, out float tileV);
          ci.actualValues.Values[GetValueYIndex(sx,sy)] = tileY;// normalized to pixel
        }
      }

      if (useChroma)
      {
        int pixelsPerChar = Utils.Product(charSize);
        charR /= pixelsPerChar;
        charG /= pixelsPerChar;
        charB /= pixelsPerChar;
        Color tileC = Color.FromArgb(charR, charG, charB);
        ColorUtils.ToMapping(tileC, out float charY, out float charU, out float charV);
        ci.actualValues.Values[GetValueUIndex()] = charU;
        ci.actualValues.Values[GetValueVIndex()] = charV;
      }
    }

    public unsafe void ProcessImage(string srcImagePath, string destImagePath)
    {
      Console.WriteLine("  tranfsorm image + " + srcImagePath);
      var testImg = Image.FromFile(srcImagePath);
      Bitmap testBmp = new Bitmap(testImg);
      Bitmap destImg = new Bitmap(testBmp.Width, testBmp.Height, PixelFormat.Format32bppArgb);

      int mapCellsX = mapBmp.Width / charSize.Width;

      using (var g = Graphics.FromImage(destImg))
      {
        ValueSet vals = ValueSet.New(componentsPerCell, 9995);
        // roughly simulate the shader algo
        for (int srcCellY = 0; srcCellY < testImg.Height / charSize.Height; ++srcCellY)
        {
          for (int srcCellX = 0; srcCellX < testImg.Width / charSize.Width; ++srcCellX)
          {
            // sample in the cell to determine the "key" "ID".
            //float charU = 0, charV = 0;// accumulate Cr and Cb
            int charR = 0, charG = 0, charB = 0;

            for (int ty = lumaTiles.Height - 1; ty >= 0; --ty)
            {
              for (int tx = lumaTiles.Width - 1; tx >= 0; --tx)
              {
                Point tilePos = Utils.GetTileOrigin(charSize, lumaTiles, tx, ty);
                Color srcColor = testBmp.GetPixel(((srcCellX) * charSize.Width) + tilePos.X, ((srcCellY) * charSize.Height) + tilePos.Y);

                ColorUtils.ToMapping(srcColor, out float cy, out float cu, out float cv);
                vals.Values[GetValueYIndex(tx, ty)] = Utils.Clamp(cy, 0, 1);
                charR += srcColor.R;
                charG += srcColor.G;
                charB += srcColor.B;
              }
            }
            if (useChroma)
            {
              int numTiles = Utils.Product(lumaTiles);
              charR /= numTiles;
              charG /= numTiles;
              charB /= numTiles;
              ColorUtils.ToMapping(Color.FromArgb(charR, charG, charB), out float cy, out float cu, out float cv);
              vals.Values[GetValueUIndex()] = Utils.Clamp(cu, 0, 1);
              vals.Values[GetValueVIndex()] = Utils.Clamp(cv, 0, 1);
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
              float val = vals.Values[i];
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

            // blit from map img.
            Rectangle srcRect = new Rectangle((int)mapCellX * charSize.Width, (int)mapCellY * charSize.Height, charSize.Width, charSize.Height);
            g.DrawImage(mapBmp, srcCellX * charSize.Width, srcCellY * charSize.Height, srcRect, GraphicsUnit.Pixel);
          }
        }
      }

      destImg.Save(destImagePath);
    }
  }
}

