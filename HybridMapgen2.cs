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
    public CharInfo[] DistinctMappedChars { get; private set; }
    public ValueSet[] Keys { get; private set; }
    public List<CharInfo> CharInfo { get; private set; }

    public IFontProvider FontProvider { get; private set; }
    public IPixelFormatProvider PixelFormatProvider { get; private set; }

    private HybridMap2()
    {
    }

    public static HybridMap2 LoadFromDisk(string dir, IFontProvider f, IPixelFormatProvider pf)
    {
      HybridMap2 ret = new HybridMap2();
      Log.WriteLine("Loading charinfo...");
      string sci = System.IO.File.ReadAllText(System.IO.Path.Combine(dir, "CharInfo.json"));
      ret.CharInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CharInfo>>(sci);

      Log.WriteLine("Loading DistinctMappedChars...");
      string sdmc = System.IO.File.ReadAllText(System.IO.Path.Combine(dir, "DistinctMappedChars.json"));
      ret.DistinctMappedChars = Newtonsoft.Json.JsonConvert.DeserializeObject<CharInfo[]>(sdmc);

      ret.FontProvider = f;
      ret.PixelFormatProvider = pf;

      return ret;
    }

    public unsafe HybridMap2(IFontProvider fontProvider,
      PartitionManager pm, IPixelFormatProvider pixelFormatProvider,
      string fullMapPath, string refMapPath, string refFontPath,
      int coreCount)
    {
      Timings timings = new Timings();

      if (coreCount < 1)
        coreCount = System.Environment.ProcessorCount - coreCount;

      this.FontProvider = fontProvider;
      this.PixelFormatProvider = pixelFormatProvider;
      this.FontProvider.Init(this.PixelFormatProvider.DiscreteNormalizedValues.Length);
      pm.Init(this.PixelFormatProvider);



      Log.WriteLine("  DiscreteNormalizedValues:");
      //bool foundSuitableMidpoint = false;
      for (int i = 0; i < PixelFormatProvider.DiscreteNormalizedValues.Length; ++i)
      {
        if (i > 14)
        {
          Log.WriteLine("    ...");
          break;
        }
        Log.WriteLine("    {0}: {1,10:0.00}", i, PixelFormatProvider.DiscreteNormalizedValues[i]);
        //if (Math.Abs(this.DiscreteNormalizedValues[i] - 0.5) < 0.0001)
        //  foundSuitableMidpoint = true;
      }



      Log.WriteLine("Number of source chars (1d): " + this.FontProvider.CharCount.ToString("N0"));
      Log.WriteLine("Chosen values per tile: " + pixelFormatProvider.DiscreteNormalizedValues.Length);
      Log.WriteLine("Dimensions: " + PixelFormatProvider.DimensionCount);
      Log.WriteLine("Partition count: " + pm.PartitionCount.ToString("N0"));
      Log.WriteLine("Resulting map will have this many entries: " + pixelFormatProvider.MapEntryCount.ToString("N0"));
      long mapdimpix = (long)Math.Sqrt(pixelFormatProvider.MapEntryCount);
      Log.WriteLine("Resulting map will be about: [" + mapdimpix.ToString("N0") + ", " + mapdimpix.ToString("N0") + "]");

      // fill in char source info (actual tile values)
      timings.EnterTask("Analyze incoming font");
      //ProgressReporter prcharinfo = new ProgressReporter(FontProvider.CharCount);
      this.CharInfo = new List<CharInfo>();

      for (int ichar = 0; ichar < FontProvider.CharCount; ++ichar)
      {
        //prcharinfo.Visit();
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

      Log.WriteLine("Number of source chars: " + this.CharInfo.Count);

      // create list of all mapkeys
      timings.EnterTask("Generating {0:N0} map key indices", pixelFormatProvider.MapEntryCount);
      this.Keys = Utils.Permutate(PixelFormatProvider.DimensionCount, pixelFormatProvider.DiscreteNormalizedValues); // returns sorted.

      // examine keys.

      timings.EndTask();

      Log.WriteLine("Key count: " + this.Keys.Length);

      foreach (var ci in this.CharInfo)
      {
        pm.AddItem(ci, false);
      }

      timings.EndTask();
      timings.EnterTask("Calculate all mappings");

      // - generate a list of mappings and their distances
      ulong theoreticalMappings = (ulong)this.CharInfo.Count * (ulong)pixelFormatProvider.MapEntryCount;
      Log.WriteLine("  Theoretical mapping count: " + theoreticalMappings.ToString("N0"));

      List<Task> comparisonBatches = new List<Task>();
      //List<MappingArray> allMappingsArray = new List<MappingArray>(coreCount);
      var Map = new Mapping[Keys.Length];// indices need to be synchronized with Keys.

      for (int icore = 0; icore < coreCount; ++icore)
      {
        //allMappingsArray.Add(new MappingArray(0));

        // create a task to process a segment of keys
        ulong keyBegin = (ulong)icore;
        keyBegin *= (ulong)Keys.Length;
        keyBegin /= (ulong)coreCount;

        ulong keyEnd = (ulong)icore + 1;
        keyEnd *= (ulong)Keys.Length;
        keyEnd /= (ulong)coreCount;

        int coreID = icore;
        bool isLastCore = (icore == coreCount - 1);

        comparisonBatches.Add(Task.Run(() =>
        {
          PopulateMap(keyBegin, keyEnd, coreID, isLastCore, pm, Map);
          //int mapEntriesToPopulate = (int)keyEnd - (int)keyBegin;
          ////MappingArray allMappings = allMappingsArray[batchID];// new MappingArray(0);
          //Log.WriteLine("    Batch processing idx {0:N0} to {1:N0}", keyBegin, keyEnd);
          //var pr = isLastCore ? new ProgressReporter((ulong)mapEntriesToPopulate) : null;
          //for (int ikey = (int)keyBegin; ikey < (int)keyEnd; ++ikey)
          //{
          //  pr?.Visit((ulong)ikey - keyBegin);
          //  var chars = pm.GetItemsInSamePartition(this.Keys[ikey], true);
          //  double p = ikey - (int)keyBegin;
          //  p /= keyEnd - keyBegin;
          //  foreach (var ci in chars)
          //  {
          //    Mapping n;
          //    n.icharInfo = ci.srcIndex;
          //    n.imapKey = ikey;
          //    double fdist = pixelFormatProvider.CalcKeyToColorDist(this.Keys[ikey], ci.actualValues);
          //    n.dist = fdist;
          //    allMappings.Add(n, p);

          //    this.Keys[ikey].MinDistFound = Math.Min(this.Keys[ikey].MinDistFound, fdist);
          //    this.Keys[ikey].Visited = true;
          //  }
          //}

          //Log.WriteLine("    Mappings generated: {0}. Now sorting them.", allMappings.Length.ToString("N0"));

          //allMappings.Sort();

          //Log.WriteLine("    Sorted batch {0}. Now enumerating and filling in map.", batchID);

          //ulong i = 0;
          //int mapEntriesPopulated = 0;
          //pr = (batchID == coreCount - 1) ? new ProgressReporter((ulong)allMappings.Length) : null;
          //foreach (var m in allMappings.GetEnumerator())
          //{
          //  pr?.Visit(i++);
          //  if (Keys[m.imapKey].Mapped)
          //  {
          //    continue;
          //  }

          //  CharInfo thisCh = this.CharInfo[m.icharInfo];

          //  Map[m.imapKey] = m;
          //  this.Keys[m.imapKey].Mapped = true;
          //  thisCh.usages++;
          //  mapEntriesPopulated++;
          //  if (mapEntriesPopulated == mapEntriesToPopulate)
          //    break;
          //}

          //double prevmem = Utils.BytesToMb(Utils.UsedMemoryBytes);
          ////allMappings = null;
          ////allMappingsArray[batchID] = null;
          //GC.Collect();
          //Log.WriteLine("Finished batch; GC mem {0:0.00} mb => {1:0.00}", prevmem, Utils.BytesToMb(Utils.UsedMemoryBytes));

        }));
      }
      Task.WaitAll(comparisonBatches.ToArray());
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

      //timings.EndTask();

      // massive dump.
#if DUMP_CHARINFO
      Log.WriteLine("ALL CHAR INFO:");
      foreach (CharInfo ci in charInfo)
      {
        Log.WriteLine("  {0}", ci);
      }
#endif
#if DUMP_MAPINFO
      Log.WriteLine("ALL MAPPING INFO:");
      foreach (var k in keys)
      {
        CharInfo ci = null;
        if (!map.TryGetValue(k.ID, out ci))
        {
          continue;
        }

        Log.WriteLine("  id:{1} key:{0} mindist:{2} mappedtoCharSrc:{3}",
          k, k.ID, k.MinDistFound, ci.srcIndex);

#if DUMP_MAPCHARINFO
        foreach (CharInfo ci2 in charInfo)
        {
          //double fdist = CalcCellDistance(k, ci2.actualValues);
          double fdist = pixelFormatProvider.CalcKeyToColorDist(k, ci2.actualValues);
          Log.WriteLine("    dist {0,6:0.00} to char {1}", fdist, ci2);
        }
#endif
      }
#endif
      Log.WriteLine("Process currently using {0:0.00} mb of memory)", Utils.BytesToMb(Utils.UsedMemoryBytes));
      //allMappingsArray = null;
      //GC.Collect();
      //Log.WriteLine("Process currently using {0:0.00} mb of memory after GC)", Utils.BytesToMb(Utils.UsedMemoryBytes));

      OutputFullMap(fullMapPath, Map);

      DistinctMappedChars = Map.DistinctBy(o => o.icharInfo).Select(o => this.CharInfo[o.icharInfo]).ToArray();
      for (int ichar = 0; ichar < DistinctMappedChars.Length; ++ichar)
      {
        CharInfo ci = DistinctMappedChars[ichar];
        Debug.Assert(ci != null);
        ci.refFontIndex = ichar;
      }

      OutputRefMapAndFont(refMapPath, refFontPath, Map);


      // save data structures
      string jsonDistinctMappedChars = Newtonsoft.Json.JsonConvert.SerializeObject(DistinctMappedChars, Newtonsoft.Json.Formatting.Indented);
      System.IO.File.WriteAllText(System.IO.Path.Combine(refMapPath, "..\\DistinctMappedChars.json"), jsonDistinctMappedChars);

      //string jsonKeys = Newtonsoft.Json.JsonConvert.SerializeObject(Keys, Newtonsoft.Json.Formatting.Indented);
      //System.IO.File.WriteAllText(System.IO.Path.Combine(refMapPath, "..\\Keys.json"), jsonKeys);

      string jsonCharInfo = Newtonsoft.Json.JsonConvert.SerializeObject(CharInfo, Newtonsoft.Json.Formatting.Indented);
      System.IO.File.WriteAllText(System.IO.Path.Combine(refMapPath, "..\\CharInfo.json"), jsonCharInfo);

      //string jsonMap = Newtonsoft.Json.JsonConvert.SerializeObject(Map, Newtonsoft.Json.Formatting.Indented);
      //System.IO.File.WriteAllText(System.IO.Path.Combine(refMapPath, "..\\Map.json"), jsonMap);

      // hm not sure what thisi s REALLY good for tbh.
      //string infopath = System.IO.Path.Combine(refMapPath, "..\\config.config");
      //StringBuilder sb = new StringBuilder();

      //sb.AppendLine("\r\n# general config:");
      //sb.AppendLine(string.Format("cores={0}", BatchCount));
      //sb.AppendLine(string.Format("fullMapPath={0}", System.IO.Path.GetFileName(fullMapPath)));
      //sb.AppendLine(string.Format("refMapPath={0}", System.IO.Path.GetFileName(refMapPath)));
      //sb.AppendLine(string.Format("refFontPath={0}", System.IO.Path.GetFileName(refFontPath)));

      //sb.AppendLine("\r\n# partition manager config:");
      //pm.WriteConfig(sb);
      //sb.AppendLine("\r\n# pixel format config:");
      //pixelFormatProvider.WriteConfig(sb);
      //sb.AppendLine("\r\n# font config:");
      //fontProvider.WriteConfig(sb);

      //System.IO.File.WriteAllText(infopath, sb.ToString());

      Log.WriteLine("Post-map stats:");
      Log.WriteLine("  Used char count: " + numCharsUsed);
      Log.WriteLine("  Number of unused char: " + (this.CharInfo.Count - numCharsUsed));
      Log.WriteLine("  Number of chars used exactly once: " + numCharsUsedOnce);
      Log.WriteLine("  Most-used char: " + mostUsedChar + " (" + mostUsedChar.usages + ") usages");
      Log.WriteLine("  Number of total char repetitions: " + numRepetitions);
    }

    private void PopulateMap(ulong keyBegin, ulong keyEnd, int coreID, bool isLastCore, PartitionManager pm, Mapping[] Map)
    {
      int mapEntriesToPopulate = (int)keyEnd - (int)keyBegin;
      //MappingArray allMappings = new MappingArray();
      Log.WriteLine("    Batch processing idx {0:N0} to {1:N0}", keyBegin, keyEnd);
      var pr = isLastCore ? new ProgressReporter((ulong)mapEntriesToPopulate) : null;
      for (int ikey = (int)keyBegin; ikey < (int)keyEnd; ++ikey)
      {
        pr?.Visit((ulong)ikey - keyBegin);
        var chars = pm.GetItemsInSamePartition(this.Keys[ikey], true);
        double p = ikey - (int)keyBegin;
        p /= keyEnd - keyBegin;

        CharInfo ciNearest = null;
        double closestDist = double.MaxValue;

        foreach (var ci in chars)
        {
          //Mapping n;
          //n.icharInfo = ci.srcIndex;
          //n.imapKey = ikey;
          double fdist = this.PixelFormatProvider.CalcKeyToColorDist(this.Keys[ikey], ci.actualValues);
          //n.dist = fdist;
          //allMappings.Add(n, p);
          if (fdist < closestDist)
          {
            this.Keys[ikey].MinDistFound = fdist;// Math.Min(this.Keys[ikey].MinDistFound, fdist);
            closestDist = fdist;
            ciNearest = ci;
          }
          //this.Keys[ikey].Visited = true;
        }

        Map[ikey].dist = closestDist;
        Map[ikey].icharInfo = ciNearest.srcIndex;
        Map[ikey].imapKey = ikey;
      }

      //Log.WriteLine("    Mappings generated: {0}. Now sorting them.", allMappings.Length.ToString("N0"));

      //allMappings.Sort();

      //Log.WriteLine("    Sorted batch {0}. Now enumerating and filling in map.", batchID);

      //ulong i = 0;
      //int mapEntriesPopulated = 0;
      //pr = (batchID == coreCount - 1) ? new ProgressReporter((ulong)allMappings.Length) : null;
      //foreach (var m in allMappings.GetEnumerator())
      //{
      //  pr?.Visit(i++);
      //  if (Keys[m.imapKey].Mapped)
      //  {
      //    continue;
      //  }

      //  CharInfo thisCh = this.CharInfo[m.icharInfo];

      //  Map[m.imapKey] = m;
      //  this.Keys[m.imapKey].Mapped = true;
      //  thisCh.usages++;
      //  mapEntriesPopulated++;
      //  if (mapEntriesPopulated == mapEntriesToPopulate)
      //    break;
      //}

      //double prevmem = Utils.BytesToMb(Utils.UsedMemoryBytes);
      ////allMappings = null;
      ////allMappingsArray[batchID] = null;
      //GC.Collect();
      //Log.WriteLine("Finished batch; GC mem {0:0.00} mb => {1:0.00}", prevmem, Utils.BytesToMb(Utils.UsedMemoryBytes));
    }

    // when a color looks wrong, let's try and trace it back. outputs mapping information for this color,
    // top char matches, and outputs an image showing the chars found.
    public void TestColor(string outputDir, ColorF rgb, params Point[] charPixPosWUT)
    {
      if (this.Keys == null)
      {
        Log.WriteLine("Keys is not yet populated. Need to generate them....");
        this.Keys = Utils.Permutate(PixelFormatProvider.DimensionCount, PixelFormatProvider.DiscreteNormalizedValues); // returns sorted.
      }
      const int charsToOutputToImage = 100;
      const int charsToOutputInConsole = 1;
      const int detailedCharOutput = 0;

      List<int> WUTcharIndex = new List<int>();
      foreach(Point p in charPixPosWUT)
      {
        WUTcharIndex.Add(this.FontProvider.GetCharIndexAtPixelPos(p));
      }

      Log.WriteLine("Displaying debug info about color");
      Log.WriteLine("  src : " + rgb);
      int mapid = this.PixelFormatProvider.DebugGetMapIndexOfColor(rgb);
      Log.WriteLine("  which lands in mapID: " + mapid);
      Log.WriteLine("   -> " + this.Keys[mapid]);

      // now display top 10 characters for that mapid.
      MappingArray marr = new MappingArray();
      Utils.ValueRangeInspector r = new Utils.ValueRangeInspector();
      foreach (CharInfo ci in this.CharInfo)
      {
        Mapping m;
        m.icharInfo = ci.srcIndex;
        m.imapKey = mapid;
        m.dist = PixelFormatProvider.CalcKeyToColorDist(this.Keys[mapid], ci.actualValues);
        r.Visit(m.dist);

        if (WUTcharIndex.Contains(ci.srcIndex))
        {
          Log.WriteLine("      You want data about char {0} well here it is:", ci);
          Log.WriteLine("        dist: {0,6:0.00} to char {1}", m.dist, ci);
          double trash = PixelFormatProvider.CalcKeyToColorDist(this.Keys[mapid], ci.actualValues, true);
        }

        marr.Add(m);
      }

      marr.Sort();

      Bitmap bmp = new Bitmap(FontProvider.CharSizeNoPadding.Width * charsToOutputToImage, FontProvider.CharSizeNoPadding.Height * 2);

      using (Graphics g = Graphics.FromImage(bmp))
      {
        g.FillRectangle(new SolidBrush(Color.FromArgb((int)rgb.R, (int)rgb.G, (int)rgb.B)), 0, 0, bmp.Width, bmp.Height);
      }

      BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, FontProvider.CharSizeNoPadding.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

      if (charsToOutputInConsole > 0)
      {
        Log.WriteLine("    listing top {0} closest characters to that map key:", charsToOutputInConsole);
      }
      int i = 0;
      foreach (var mapping in marr.GetEnumerator())
      {
        if (i < charsToOutputInConsole)
        {
          Log.WriteLine("      dist: {0,6:0.00} to char {1}", mapping.dist, this.CharInfo[mapping.icharInfo]);
          double dist = PixelFormatProvider.CalcKeyToColorDist(this.Keys[mapid], this.CharInfo[mapping.icharInfo].actualValues, i < detailedCharOutput);
        }
        if (i >= charsToOutputToImage)
          break;
        FontProvider.BlitCharacter(mapping.icharInfo, bmpData, FontProvider.CharSizeNoPadding.Width * i, 0);
        i++;
      }
      bmp.UnlockBits(bmpData);

      string path = System.IO.Path.Combine(outputDir,  string.Format("TESTVIS {0}.png", rgb));
      Log.WriteLine("    Output chars to :" + path);
      bmp.Save(path);
      bmp.Dispose();
    }

    internal void OutputFullMap(string MapFullPath, Mapping[] Map)
    {
      int numCellsX = (int)Math.Ceiling(Math.Sqrt(this.Keys.Count()));
      Size mapImageSize = Utils.Mul(FontProvider.CharSizeNoPadding, numCellsX);

      Log.WriteLine("MAP image generation...");
      Log.WriteLine("  Cells: [" + numCellsX + ", " + numCellsX + "]");
      Log.WriteLine("  Image size: [" + mapImageSize.Width + ", " + mapImageSize.Height + "]");

      if (mapImageSize.Width > 17000)
      {
        // a healthy safe amount.
        // https://stackoverflow.com/questions/29175585/what-is-the-maximum-resolution-of-c-sharp-net-bitmap
        Log.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        Log.WriteLine("!!! full map generation not possible; too big.");
        Log.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        return;
      }

      var FullMapBitmap = new Bitmap(mapImageSize.Width, mapImageSize.Height, PixelFormat.Format24bppRgb);
      BitmapData destData = FullMapBitmap.LockBits(new Rectangle(0, 0, mapImageSize.Width, mapImageSize.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

      ProgressReporter pr = new ProgressReporter(Keys.Length);

      foreach (ValueSet k in Keys)
      {
        pr.Visit();
        CharInfo ci = this.CharInfo[Map[k.ID].icharInfo];

        long cellY = k.ID / numCellsX;
        long cellX = k.ID - (numCellsX * cellY);

        FontProvider.BlitCharacter(ci.srcIndex, destData, cellX * FontProvider.CharSizeNoPadding.Width, cellY * FontProvider.CharSizeNoPadding.Height);
      }

      FullMapBitmap.UnlockBits(destData);

      FullMapBitmap.Save(MapFullPath);
      FullMapBitmap.Dispose();
    }

    // each R,G,B value of the resulting image is a mapping. the inserted value 0-1 refers to a character
    // in the font texture.
    internal unsafe void OutputRefMapAndFont(string MapRefPath, string MapRefFontPath, Mapping[] Map)
    {
      Log.WriteLine("FONT MAP image generation...");
      float fontMapCharCount = DistinctMappedChars.Length;
      Log.WriteLine("  Entries linear: " + fontMapCharCount);
      long fontImgPixels = DistinctMappedChars.Length * FontProvider.CharSizeNoPadding.Width * FontProvider.CharSizeNoPadding.Height;
      Log.WriteLine("  Total pixels: " + fontImgPixels);
      int fontImgWidthChars = (int)Math.Ceiling(Math.Sqrt(fontImgPixels) / FontProvider.CharSizeNoPadding.Width);
      int fontImgWidthPixels = fontImgWidthChars * FontProvider.CharSizeNoPadding.Width;
      int fontImgHeightChars = (int)Math.Ceiling((double)fontImgPixels / fontImgWidthPixels / FontProvider.CharSizeNoPadding.Height);
      int fontImgHeightPixels = fontImgHeightChars * FontProvider.CharSizeNoPadding.Height;
      Log.WriteLine("  Image size chars: [" + fontImgWidthChars + ", " + fontImgHeightChars + "]");
      Log.WriteLine("  Image size pixels: [" + fontImgWidthPixels + ", " + fontImgHeightPixels + "]");

      var fontBmp = new Bitmap(fontImgWidthPixels, fontImgHeightPixels, PixelFormat.Format24bppRgb);
      BitmapData destFontData = fontBmp.LockBits(new Rectangle(0, 0, fontImgWidthPixels, fontImgHeightPixels), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

      foreach(var ci in DistinctMappedChars)
      {
        long cellY = ci.refFontIndex / fontImgWidthChars;
        long cellX = ci.refFontIndex - (fontImgWidthChars * cellY);

        FontProvider.BlitCharacter(ci.srcIndex, destFontData, (cellX * FontProvider.CharSizeNoPadding.Width), (cellY * FontProvider.CharSizeNoPadding.Height));
      }

      fontBmp.UnlockBits(destFontData);

      fontBmp.Save(MapRefFontPath);
      fontBmp.Dispose();
      fontBmp = null;

      // NOW generate the small ref map. since we aim to support >65k fonts, we can't just use
      // a single R/G/B val for an index. there's just not enough precision. The most precise PNG format is 16-bit grayscale.
      // we should just aim to use RGB as 8-bit values, so each pixel is an encoded
      // 24-bit char index.

      double pixelCountD = Math.Ceiling((double)Keys.Length);

      int mapWidthPixels = (int)Math.Ceiling(Math.Sqrt(pixelCountD));
      int mapHeightPixels = (int)Math.Ceiling(pixelCountD / mapWidthPixels);

      Log.WriteLine("REF MAP image generation...");
      Log.WriteLine("  Image size: [" + mapWidthPixels + ", " + mapHeightPixels + "]");

      var refMapBmp = new Bitmap(mapWidthPixels, mapHeightPixels, PixelFormat.Format24bppRgb);
      BitmapData destData = refMapBmp.LockBits(new Rectangle(0, 0, mapWidthPixels, mapHeightPixels), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

      for (int i = 0; i < Keys.Length; ++ i)
      {
        CharInfo ci = this.CharInfo[Map[i].icharInfo];
        int y = i / mapWidthPixels;
        int x = i- (y * mapWidthPixels);
        Color c = RefFontIndexToColor(ci.refFontIndex);
        destData.SetPixel(x, y, c);
      }

      refMapBmp.UnlockBits(destData);

      refMapBmp.Save(MapRefPath);
      refMapBmp.Dispose();
    }

    public Color RefFontIndexToColor(int fontIndex)
    {
      Debug.Assert(fontIndex >= 0);
      //Debug.Assert(fontIndex < DistinctMappedChars.Length);
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

    //public unsafe void ProcessImage(string srcImagePath, string destImagePath)
    //{
    //  Log.WriteLine("  tranfsorm image + " + srcImagePath);
    //  var testImg = Image.FromFile(srcImagePath);
    //  Bitmap testBmp = new Bitmap(testImg);
    //  Bitmap destImg = new Bitmap(testBmp.Width, testBmp.Height, PixelFormat.Format32bppArgb);

    //  int mapCellsX = FullMapBitmap.Width / FontProvider.CharSizeNoPadding.Width;

    //  using (var g = Graphics.FromImage(destImg))
    //  {
    //    ColorF srcColor = ColorFUtils.Init;
    //    ColorF yuv = ColorFUtils.Init;
    //    for (int srcCellY = 0; srcCellY < testImg.Height / FontProvider.CharSizeNoPadding.Height; ++srcCellY)
    //    {
    //      for (int srcCellX = 0; srcCellX < testImg.Width / FontProvider.CharSizeNoPadding.Width; ++srcCellX)
    //      {
    //        // sample in the cell to determine the "key" "ID".
    //        ColorF charC = ColorFUtils.Init;
    //        int ID = PixelFormatProvider.GetMapIndexOfRegion(testBmp,
    //          srcCellX * FontProvider.CharSizeNoPadding.Width,
    //          srcCellY * FontProvider.CharSizeNoPadding.Height,
    //          FontProvider.CharSizeNoPadding
    //          );

    //        long mapCellY = ID / mapCellsX;
    //        long mapCellX = ID - (mapCellY * mapCellsX);

    //        // blit from map img.
    //        Rectangle srcRect = new Rectangle(
    //          (int)mapCellX * FontProvider.CharSizeNoPadding.Width,
    //          (int)mapCellY * FontProvider.CharSizeNoPadding.Height,
    //          FontProvider.CharSizeNoPadding.Width, FontProvider.CharSizeNoPadding.Height);
    //        g.DrawImage(FullMapBitmap,
    //          srcCellX * FontProvider.CharSizeNoPadding.Width,
    //          srcCellY * FontProvider.CharSizeNoPadding.Height,
    //          srcRect, GraphicsUnit.Pixel);
    //      }
    //    }
    //  }

    //  destImg.Save(destImagePath);
    //}

    // returns map from cell => REFFONT char id
    //public unsafe IDictionary<Point, int> ProcessImageUsingRef(string MapRefPath, string MapRefFontPath, string srcImagePath, string destImagePath)
    //{
    //  using (var testImg = new Bitmap(srcImagePath))
    //  using (var refMapImage = new Bitmap(MapRefPath))
    //  using (var refFontImage = new Bitmap(MapRefFontPath))
    //    return ProcessImageUsingRef(refMapImage, refFontImage, testImg, destImagePath);
    //}

    // returns map from cell => charid
    //public unsafe IDictionary<Point, int> ProcessImageUsingRef(string MapRefPath, string MapRefFontPath, string srcImagePath, Bitmap testBmp, string destImagePath)
    //{
    //  Log.WriteLine("  tranfsorm image using REF: " + srcImagePath);
    //  var refMapImage = new Bitmap(MapRefPath);
    //  //Bitmap refMapBitmap = new Bitmap(refMapImage);
    //  var refFontImage = Bitmap.FromFile(MapRefFontPath);
    //  var testBmp = new Bitmap(testImg);
    //  return ProcessImageUsingRef(refMapImage, refFontImage, srcImagePath, destImagePath);
    //}

    // returns map from cell => charid
    public unsafe IDictionary<Point, int> ProcessImageUsingRef(Bitmap refMapImage, Bitmap refFontImage, Bitmap testBmp, string destImagePath)
    {
      using (Bitmap destImg = new Bitmap(testBmp.Width, testBmp.Height, PixelFormat.Format32bppArgb))
      {
        int fontCellsX = refFontImage.Width / FontProvider.CharSizeNoPadding.Width;

        int rows = testBmp.Height / FontProvider.CharSizeNoPadding.Height;
        int columns = testBmp.Width / FontProvider.CharSizeNoPadding.Width;
        Dictionary<Point, int> rv = new Dictionary<Point, int>(rows * columns);

        using (var g = Graphics.FromImage(destImg))
        {
          ColorF srcColor = ColorFUtils.Init;
          ColorF yuv = ColorFUtils.Init;
          for (int srcCellY = 0; srcCellY < rows; ++srcCellY)
          {
            for (int srcCellX = 0; srcCellX < columns; ++srcCellX)
            {
              // sample in the cell to determine the "key" "ID".
              ColorF charC = ColorFUtils.Init;
              int ID = PixelFormatProvider.GetMapIndexOfRegion(testBmp,
                srcCellX * FontProvider.CharSizeNoPadding.Width,
                srcCellY * FontProvider.CharSizeNoPadding.Height,
                FontProvider.CharSizeNoPadding
                );

              int mapCellY = ID / refMapImage.Width;
              int mapCellX = ID % refMapImage.Width;

              // get ref
              Color refColor = refMapImage.GetPixel(mapCellX, mapCellY);

              // convert that to font map id
              int fontID = ColorToRefFontIndex(refColor);
              // split into fontcells
              int fontCellY = fontID / fontCellsX;
              int fontCellX = fontID % fontCellsX;

              // in order to know the character index for converting to text
              rv[new Point(srcCellX, srcCellY)] = DistinctMappedChars[fontID].srcIndex;

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

        return rv;
      }
    }
  }
}

