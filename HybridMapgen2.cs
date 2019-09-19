//#define DUMP_CHARINFO
//#define DUMP_MAPINFO
//#define DUMP_MAPCHARINFO

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
    public Bitmap FullMapBitmap;
    public CharInfo[] DistinctMappedChars;
    public ValueSet[] Keys { get; private set; }
    public List<CharInfo> CharInfo { get; private set; }
    public Dictionary<long, CharInfo> Map { get; private set; }// maps key index to charinfo

    public IFontProvider FontProvider { get; private set; }
    public IPixelFormatProvider PixelFormatProvider { get; private set; }

    public string MapFullPath
    {
      get
      {
        return string.Format("..\\..\\img\\mapFull-{0}-{1}.png",
                System.IO.Path.GetFileNameWithoutExtension(FontProvider.FontFileName),
                this.PixelFormatProvider.PixelFormatString);
      }
    }

    public string MapRefPath
    {
      get
      {
        return string.Format("..\\..\\img\\mapRef-{0}-{1}.png",
                System.IO.Path.GetFileNameWithoutExtension(FontProvider.FontFileName),
                this.PixelFormatProvider.PixelFormatString);
      }
    }

    public string MapRefFontPath
    {
      get
      {
        return string.Format("..\\..\\img\\mapRefFont-{0}-{1}.png",
                System.IO.Path.GetFileNameWithoutExtension(FontProvider.FontFileName),
                this.PixelFormatProvider.PixelFormatString);
      }
    }

    private HybridMap2()
    {
    }

    public unsafe HybridMap2(IFontProvider fontProvider,
      PartitionManager pm, IPixelFormatProvider pixelFormatProvider, bool outputFullMap = true, bool outputRefMapAndFont = true)
    {
      Timings timings = new Timings();

      this.FontProvider = fontProvider;
      this.PixelFormatProvider = pixelFormatProvider;
      this.FontProvider.Init(this.PixelFormatProvider.DiscreteNormalizedValues.Length);
      pm.Init(this.PixelFormatProvider);

      Console.WriteLine("Number of source chars (1d): " + this.FontProvider.CharCount.ToString("N0"));
      Console.WriteLine("Chosen values per tile: " + pixelFormatProvider.DiscreteNormalizedValues.Length);
      Console.WriteLine("Dimensions: " + PixelFormatProvider.DimensionCount);
      Console.WriteLine("Resulting map will have this many entries: " + pixelFormatProvider.MapEntryCount.ToString("N0"));
      long mapdimpix = (long)Math.Sqrt(pixelFormatProvider.MapEntryCount);
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
      this.CharInfo = new List<CharInfo>();

      for (int ichar = 0; ichar < FontProvider.CharCount; ++ichar)
      {
        var ci = new CharInfo(PixelFormatProvider.DimensionCount)
        {
          srcIndex = ichar,
#if DEBUG
          fontImageCellPos = FontProvider.GetCharPosInChars(ichar),
          fontImagePixelPos = FontProvider.GetCharOriginInPixels(ichar),
#endif
        };
          pixelFormatProvider.PopulateCharColorData(ci, fontProvider);
        this.CharInfo.Add(ci);
      }

      Console.WriteLine("Number of source chars: " + this.CharInfo.Count);

      // create list of all mapkeys
      this.Keys = Utils.Permutate(PixelFormatProvider.DimensionCount, pixelFormatProvider.DiscreteNormalizedValues); // returns sorted.

      Console.WriteLine("  Key count: " + this.Keys.Length);

      foreach (var ci in this.CharInfo)
      {
        pm.AddItem(ci, false);
      }

      timings.EndTask();
      timings.EnterTask("Calculate all mappings");

      // - generate a list of mappings and their distances
      ulong theoreticalMappings = (ulong)this.CharInfo.Count * (ulong)pixelFormatProvider.MapEntryCount;
      Console.WriteLine("  Partition count: " + pm.PartitionCount.ToString("N0"));
      Console.WriteLine("  Theoretical mapping count: " + theoreticalMappings.ToString("N0"));

      Utils.ValueRangeInspector distanceRange = new Utils.ValueRangeInspector();
      MappingArray allMappings = new MappingArray();

      long comparisonsMade = 0;
      ProgressReporter pr = new ProgressReporter(this.Keys.Length);
      for (int ikey = 0; ikey < this.Keys.Length; ++ikey)
      {
        pr.Visit(ikey);
        var chars = pm.GetItemsInSamePartition(this.Keys[ikey], true);
        foreach (var ci in chars)
        {
          long imap = allMappings.Add();
          allMappings.Values[imap].icharInfo = ci.srcIndex;
          allMappings.Values[imap].imapKey = ikey;
          double fdist = pixelFormatProvider.CalcKeyToColorDist(this.Keys[ikey], ci.actualValues);
          allMappings.Values[imap].dist = fdist;
          distanceRange.Visit(fdist);
          this.Keys[ikey].MinDistFound = Math.Min(this.Keys[ikey].MinDistFound, fdist);
          this.Keys[ikey].Visited = true;
          comparisonsMade++;
        }
      }

      Console.WriteLine("  Mappings generated: {0}", allMappings.Length.ToString("N0"));
      Console.WriteLine("  Comparisons made: {0}", comparisonsMade.ToString("N0"));
      Console.WriteLine("  Distance range encountered: {0}", distanceRange);

      double maxMinDist = 0;
      foreach (var mapKey in this.Keys)
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
      this.Map = new Dictionary<long, CharInfo>((int)pixelFormatProvider.MapEntryCount);

      for (int imap = 0; imap < allMappings.Length; ++imap)
      {
        if (this.Keys[allMappings.Values[imap].imapKey].Mapped)
        {
          continue;
        }

        var m = allMappings.Values[imap];
        CharInfo thisCh = this.CharInfo[m.icharInfo];

        this.Map[this.Keys[allMappings.Values[imap].imapKey].ID] = thisCh;
        this.Keys[allMappings.Values[imap].imapKey].Mapped = true;
        this.CharInfo[(int)allMappings.Values[imap].icharInfo].usages++;
        if (this.Map.Count == pixelFormatProvider.MapEntryCount)
          break;
      }
      timings.EndTask();

      int numCharsUsed = 0;
      int numCharsUsedOnce = 0;
      CharInfo mostUsedChar = null;
      int numRepetitions = 0;
      foreach (var ci in this.CharInfo)
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

        Console.WriteLine("  id:{1} key:{0} mindist:{2} mappedtoCharSrc:{3}",
          k, k.ID, k.MinDistFound, ci.srcIndex);

#if DUMP_MAPCHARINFO
        foreach (CharInfo ci2 in charInfo)
        {
          //double fdist = CalcCellDistance(k, ci2.actualValues);
          double fdist = pixelFormatProvider.CalcKeyToColorDist(k, ci2.actualValues);
          Console.WriteLine("    dist {0,6:0.00} to char {1}", fdist, ci2);
        }
#endif
      }
#endif

      if (outputFullMap)
      {
        OutputFullMap(this.Keys, this.Map);
      }
      if (outputRefMapAndFont)
      {
        this.DistinctMappedChars = this.Map.DistinctBy(o => o.Value.srcIndex).Select(o => o.Value).ToArray();
        for (int ichar = 0; ichar < DistinctMappedChars.Length; ++ichar)
        {
          CharInfo ci = DistinctMappedChars[ichar];
          Debug.Assert(ci != null);
          ci.refFontIndex = ichar;
        }

        OutputRefMapAndFont(this.Keys, this.Map);
      }

      Console.WriteLine("Post-map stats:");
      Console.WriteLine("  Used char count: " + numCharsUsed);
      Console.WriteLine("  Number of unused char: " + (this.CharInfo.Count - numCharsUsed));
      Console.WriteLine("  Number of chars used exactly once: " + numCharsUsedOnce);
      Console.WriteLine("  Most-used char: " + mostUsedChar + " (" + mostUsedChar.usages + ") usages");
      Console.WriteLine("  Number of total char repetitions: " + numRepetitions);
    }

    // when a color looks wrong, let's try and trace it back. outputs mapping information for this color,
    // top char matches, and outputs an image showing the chars found.
    public void TestColor(ColorF rgb, params Point[] charPixPosWUT)
    {
      const int charsToOutputToImage = 100;
      const int charsToOutputInConsole = 20;
      const int detailedCharOutput = 3;

      List<int> WUTcharIndex = new List<int>();
      foreach(Point p in charPixPosWUT)
      {
        WUTcharIndex.Add(this.FontProvider.GetCharIndexAtPixelPos(p));
      }

      Console.WriteLine("Displaying debug info about color");
      Console.WriteLine("  src : " + rgb);
      int mapid = this.PixelFormatProvider.DebugGetMapIndexOfColor(rgb);
      Console.WriteLine("  which lands in mapID: " + mapid);
      Console.WriteLine("   -> " + this.Keys[mapid]);
//      Console.WriteLine("   -> mapped to char: " + this.Map[mapid]);

      // now display top 10 characters for that mapid.
      MappingArray m = new MappingArray();
      Utils.ValueRangeInspector r = new Utils.ValueRangeInspector();
      foreach (CharInfo ci in this.CharInfo)
      {
        long imap = m.Add();
        m.Values[imap].icharInfo = ci.srcIndex;
        m.Values[imap].imapKey = mapid;
        m.Values[imap].dist = PixelFormatProvider.CalcKeyToColorDist(this.Keys[mapid], ci.actualValues);
        r.Visit(m.Values[imap].dist);

        if (WUTcharIndex.Contains(ci.srcIndex))
        {
          Console.WriteLine("      You want data about char {0} well here it is:", ci);
          Console.WriteLine("        dist: {0,6:0.00} to char {1}", m.Values[imap].dist, ci);
          double trash = PixelFormatProvider.CalcKeyToColorDist(this.Keys[mapid], ci.actualValues, true);
        }
      }
      m.PruneWhereDistGT(r.MaxValue);//essential so the sort doesn't operate on like 30 million items
      m.SortByDist();

      Bitmap bmp = new Bitmap(FontProvider.CharSizeNoPadding.Width * charsToOutputToImage, FontProvider.CharSizeNoPadding.Height * 2);

      using (Graphics g = Graphics.FromImage(bmp))
      {
        g.FillRectangle(new SolidBrush(Color.FromArgb((int)rgb.R, (int)rgb.G, (int)rgb.B)), 0, 0, bmp.Width, bmp.Height);
      }

      BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, FontProvider.CharSizeNoPadding.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

      Console.WriteLine("    listing top {0} closest characters to that map key:", charsToOutputInConsole);
      for (int i = 0; i < charsToOutputToImage; ++ i)
      {
        var mapping = m.Values[i];
        if (i < charsToOutputInConsole)
        {
          Console.WriteLine("      dist: {0,6:0.00} to char {1}", mapping.dist, this.CharInfo[mapping.icharInfo]);
          double dist = PixelFormatProvider.CalcKeyToColorDist(this.Keys[mapid], this.CharInfo[mapping.icharInfo].actualValues, i < detailedCharOutput);
        }
        FontProvider.BlitCharacter(mapping.icharInfo, bmpData, FontProvider.CharSizeNoPadding.Width * i, 0);
      }
      bmp.UnlockBits(bmpData);

      string path = string.Format("..\\..\\img\\TESTVIS {0}.png", rgb);
      Console.WriteLine("    Output chars to :" + path);
      bmp.Save(path);
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

      this.FullMapBitmap = new Bitmap(mapImageSize.Width, mapImageSize.Height, PixelFormat.Format24bppRgb);
      BitmapData destData = FullMapBitmap.LockBits(new Rectangle(0, 0, mapImageSize.Width, mapImageSize.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

      foreach (ValueSet k in keys)
      {
        CharInfo ci = null;
        map.TryGetValue(k.ID, out ci);
        if (ci == null)
        {
          Debug.Assert(false);
          continue;
        }

        long cellY = k.ID / numCellsX;
        long cellX = k.ID - (numCellsX * cellY);

        FontProvider.BlitCharacter(ci.srcIndex, destData, cellX * FontProvider.CharSizeNoPadding.Width, cellY * FontProvider.CharSizeNoPadding.Height);
      }

      FullMapBitmap.UnlockBits(destData);

      FullMapBitmap.Save(MapFullPath);
    }

    // each R,G,B value of the resulting image is a mapping. the inserted value 0-1 refers to a character
    // in the font texture.
    internal unsafe void OutputRefMapAndFont(ValueSet[] keys, Dictionary<long, CharInfo> map)
    {
      Console.WriteLine("FONT MAP image generation...");
      float fontMapCharCount = DistinctMappedChars.Length;
      Console.WriteLine("  Entries linear: " + fontMapCharCount);
      long fontImgPixels = DistinctMappedChars.Length * FontProvider.CharSizeNoPadding.Width * FontProvider.CharSizeNoPadding.Height;
      Console.WriteLine("  Total pixels: " + fontImgPixels);
      int fontImgWidthChars = (int)Math.Ceiling(Math.Sqrt(fontImgPixels) / FontProvider.CharSizeNoPadding.Width);
      int fontImgWidthPixels = fontImgWidthChars * FontProvider.CharSizeNoPadding.Width;
      int fontImgHeightChars = (int)Math.Ceiling((double)fontImgPixels / fontImgWidthPixels / FontProvider.CharSizeNoPadding.Height);
      int fontImgHeightPixels = fontImgHeightChars * FontProvider.CharSizeNoPadding.Height;
      Console.WriteLine("  Image size chars: [" + fontImgWidthChars + ", " + fontImgHeightChars + "]");
      Console.WriteLine("  Image size pixels: [" + fontImgWidthPixels + ", " + fontImgHeightPixels + "]");

      var fontBmp = new Bitmap(fontImgWidthPixels, fontImgHeightPixels, PixelFormat.Format24bppRgb);
      BitmapData destFontData = fontBmp.LockBits(new Rectangle(0, 0, fontImgWidthPixels, fontImgHeightPixels), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

      //for (int ichar = 0; ichar < distinctMappedChars.Length; ++ ichar)
      foreach(var ci in DistinctMappedChars)
      {
        long cellY = ci.refFontIndex / fontImgWidthChars;
        long cellX = ci.refFontIndex - (fontImgWidthChars * cellY);

        FontProvider.BlitCharacter(ci.srcIndex, destFontData, (cellX * FontProvider.CharSizeNoPadding.Width), (cellY * FontProvider.CharSizeNoPadding.Height));
      }

      fontBmp.UnlockBits(destFontData);

      fontBmp.Save(MapRefFontPath);

      // NOW generate the small ref map. since we aim to support >65k fonts, we can't just use
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
          Debug.Assert(false);
        }
        int y = i / mapWidthPixels;
        int x = i- (y * mapWidthPixels);
        //byte* p = destData.GetRGBPointer(x, y);
        Color c = RefFontIndexToColor(ci.refFontIndex);
        //p[0] = 
        destData.SetPixel(x, y, c);
      }

      refMapBmp.UnlockBits(destData);

      refMapBmp.Save(MapRefPath);
    }

    public Color RefFontIndexToColor(int fontIndex)
    {
      Debug.Assert(fontIndex >= 0);
      Debug.Assert(fontIndex < DistinctMappedChars.Length);
      int v = (int)fontIndex;// * 0x10;
      byte r = (byte)(v & 0xff);
      v >>= 8;
      byte g = (byte)(v & 0xff);
      v >>= 8;
      byte b = (byte)(v & 0xff);
      return Color.FromArgb(r,g,b);
    }

    public int ColorToRefFontIndex(Color c)
    {
      // convert that to font map id
      double fontID = ((int)c.R) | ((int)c.G << 8) | ((int)c.B << 16);
      return ((int)fontID);// / 0x10;
    }

    public unsafe void ProcessImage(string srcImagePath, string destImagePath)
    {
      Console.WriteLine("  tranfsorm image + " + srcImagePath);
      var testImg = Image.FromFile(srcImagePath);
      Bitmap testBmp = new Bitmap(testImg);
      Bitmap destImg = new Bitmap(testBmp.Width, testBmp.Height, PixelFormat.Format32bppArgb);

      int mapCellsX = FullMapBitmap.Width / FontProvider.CharSizeNoPadding.Width;

      using (var g = Graphics.FromImage(destImg))
      {
        ColorF srcColor = ColorFUtils.Init;
        ColorF yuv = ColorFUtils.Init;
        for (int srcCellY = 0; srcCellY < testImg.Height / FontProvider.CharSizeNoPadding.Height; ++srcCellY)
        {
          for (int srcCellX = 0; srcCellX < testImg.Width / FontProvider.CharSizeNoPadding.Width; ++srcCellX)
          {
            // sample in the cell to determine the "key" "ID".
            ColorF charC = ColorFUtils.Init;
            int ID = PixelFormatProvider.GetMapIndexOfRegion(testBmp,
              srcCellX * FontProvider.CharSizeNoPadding.Width,
              srcCellY * FontProvider.CharSizeNoPadding.Height,
              FontProvider.CharSizeNoPadding
              );

            long mapCellY = ID / mapCellsX;
            long mapCellX = ID - (mapCellY * mapCellsX);

            // blit from map img.
            Rectangle srcRect = new Rectangle(
              (int)mapCellX * FontProvider.CharSizeNoPadding.Width,
              (int)mapCellY * FontProvider.CharSizeNoPadding.Height,
              FontProvider.CharSizeNoPadding.Width, FontProvider.CharSizeNoPadding.Height);
            g.DrawImage(FullMapBitmap,
              srcCellX * FontProvider.CharSizeNoPadding.Width,
              srcCellY * FontProvider.CharSizeNoPadding.Height,
              srcRect, GraphicsUnit.Pixel);
          }
        }
      }

      destImg.Save(destImagePath);
    }

    public unsafe void ProcessImageUsingRef(string srcImagePath, string destImagePath)
    {
      var testImg = Image.FromFile(srcImagePath);
      Bitmap testBmp = new Bitmap(testImg);
      ProcessImageUsingRef(srcImagePath, testImg, testBmp, destImagePath);
    }

    public unsafe void ProcessImageUsingRef(string srcImagePath, Image testImg, Bitmap testBmp, string destImagePath)
    {
      Console.WriteLine("  tranfsorm image using REF: " + srcImagePath);
      var refMapImage = Image.FromFile(MapRefPath);
      Bitmap refMapBitmap = new Bitmap(refMapImage);
      var refFontImage = Image.FromFile(MapRefFontPath);
      Bitmap destImg = new Bitmap(testBmp.Width, testBmp.Height, PixelFormat.Format32bppArgb);

      int fontCellsX = refFontImage.Width / FontProvider.CharSizeNoPadding.Width;

      using (var g = Graphics.FromImage(destImg))
      {
        ColorF srcColor = ColorFUtils.Init;
        ColorF yuv = ColorFUtils.Init;
        for (int srcCellY = 0; srcCellY < testImg.Height / FontProvider.CharSizeNoPadding.Height; ++srcCellY)
        {
          for (int srcCellX = 0; srcCellX < testImg.Width / FontProvider.CharSizeNoPadding.Width; ++srcCellX)
          {
            // sample in the cell to determine the "key" "ID".
            ColorF charC = ColorFUtils.Init;
            int ID = PixelFormatProvider.GetMapIndexOfRegion(testBmp,
              srcCellX * FontProvider.CharSizeNoPadding.Width,
              srcCellY * FontProvider.CharSizeNoPadding.Height,
              FontProvider.CharSizeNoPadding
              );

            int mapCellY = ID / refMapBitmap.Width;
            int mapCellX = ID % refMapBitmap.Width;// - (mapCellY * refMapBitmap.Width);

            // get ref
            Color refColor = refMapBitmap.GetPixel(mapCellX, mapCellY);

            // convert that to font map id
            int fontID = ColorToRefFontIndex(refColor);
            // split into fontcells
            int fontCellY = fontID / fontCellsX;
            int fontCellX = fontID % fontCellsX;// - (fontCellY * fontCellsX);

            // blit from map img.
            Rectangle srcRect = new Rectangle(
              fontCellX * FontProvider.CharSizeNoPadding.Width,
              fontCellY * FontProvider.CharSizeNoPadding.Height,
              FontProvider.CharSizeNoPadding.Width, FontProvider.CharSizeNoPadding.Height);
            g.DrawImage(refFontImage,
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

