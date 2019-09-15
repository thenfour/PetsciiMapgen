/*

ADDS some stuff to the hybrid map
- optimize processing
- monochrome bitmap with palette handling
- map generation should produce a reference, not replicate every single glyph

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

namespace PetsciiMapgen
{
  public class HybridMap2
  {
    public ValueSet weights; // weights for calculating distance.
    public Bitmap mapBmp;
    public Size charSize;
    public Size tilesPerCell;
    public int valuesPerComponent;
    public int componentsPerCell; // # of dimensions (UV + Y*size)
    public int numYcomponents;
    public bool useChroma;
    //int UdimIdx = -1;
    //int VdimIdx = -1;
    float Yweight;
    float Uweight;
    float Vweight;
    long numDestCharacters;// = (int)Utils.Pow(valuesPerComponent, (uint)componentsPerCell);

    private HybridMap2()
    {
    }

    // value indices are YYYY U V
    internal unsafe void EstablishWeights()
    {
      weights = ValueSet.New(componentsPerCell, 9997);
      for (int i = 0; i < numYcomponents; ++i)
      {
        this.weights.Values[i] = Yweight / numYcomponents;
      }
      if (useChroma)
      {
        this.weights.Values[GetValueUIndex()] = Uweight;
        this.weights.Values[GetValueVIndex()] = Vweight;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetValueYIndex(int tx, int ty)
    {
      return (ty * tilesPerCell.Width) + tx;
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetHueIndex() { return GetValueVIndex(); }

    public unsafe HybridMap2(string fontFileName, Size charSize, Size tilesPerCell, int valuesPerComponent, int valuesPerPartition, float Yweight, float Uweight, float Vweight, bool useChroma)
    {
      Timings timings = new Timings();

      this.Yweight = Yweight;
      this.Uweight = Uweight;
      this.Vweight = Vweight;
      this.tilesPerCell = tilesPerCell;
      this.charSize = charSize;
      this.useChroma = useChroma;
      this.valuesPerComponent = valuesPerComponent;
      this.numYcomponents = Utils.Product(tilesPerCell);
      this.componentsPerCell = numYcomponents + (useChroma ? 2 : 0); // number of dimensions

      EstablishWeights();

      var srcImg = System.Drawing.Image.FromFile(fontFileName);
      var srcBmp = new Bitmap(srcImg);
      Size numSrcChars = Utils.Div(srcImg.Size, charSize);

      numDestCharacters = Utils.Pow(valuesPerComponent, (uint)componentsPerCell);

      Console.WriteLine("Src character size: " + Utils.ToString(charSize));
      Console.WriteLine("Src image size: " + Utils.ToString(srcBmp.Size));
      Console.WriteLine("Number of source chars: " + Utils.ToString(numSrcChars));
      Console.WriteLine("Number of source chars (1d): " + Utils.Product(numSrcChars));
      Console.WriteLine("Chosen values per tile: " + valuesPerComponent);
      Console.WriteLine("Dimensions: " + componentsPerCell);
      Console.WriteLine("Resulting map will have this many entries: " + numDestCharacters);

      //Console.WriteLine("\r\nhit a key to continue");
      //Console.ReadKey();

      // fill in char source info (actual tile values)
      timings.EnterTask("Analyze incoming font");
      var charInfo = new List<CharInfo>();
      int pixelsPerChar = Utils.Product(charSize);

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
          // gather up all the info we can about this char.
          var ci = new CharInfo(componentsPerCell)
          {
            srcIndex = new Point(x, y)
          };

          ProcessCharacter(srcBmp, ci);

          for (int i = 0; i < componentsPerCell; ++i)
          {
            ranges[i].Visit(ci.actualValues.Values[i]);
          }

          charInfo.Add(ci);
        }
      }

      Console.WriteLine("RANGES encountered:");
      for (int i = 0; i < componentsPerCell; ++i)
      {
        Console.WriteLine("  [" + i + "]: " + ranges[i]);
      }

      // normalize all YUV seen so it will correspond nicely with permutations.
      Console.WriteLine("Normalizing values...");
      float[] discreteValues = Utils.GetDiscreteValues(valuesPerComponent);
      PartitionManager pm = new PartitionManager(valuesPerPartition, componentsPerCell, discreteValues, weights);

      foreach (var ci in charInfo)
      {
        for (int i = 0; i < componentsPerCell; ++i)
        {
          ci.actualValues.Values[i] = ranges[i].Normalize01(ci.actualValues.Values[i]);
        }
        ci.partition = pm.GetPartitionIndex(ci.actualValues);
      }
      timings.EndTask();

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
      //var maxPartitionElementSize = pm.PartitionMaxElementSize;  //Utils.GetPartitionMaxElementSize(componentsPerCell, valuesPerComponent);
      Console.WriteLine("  Elements per partition: " + pm.PartitionMaxElementSize);
      for (int i = 0; i < partitions.Length; ++i)
      {
        Partition.Init(ref partitions[i], pm.PartitionMaxElementSize);
      }


      // TESTING
      ValueSet vs1 = ValueSet.New(componentsPerCell, 0);
      ValueSet vs2 = ValueSet.New(componentsPerCell, 0);
      for (int i = 0; i < componentsPerCell; ++ i)
      {
        vs1.Values[i] = 1.0f;
        vs2.Values[i] = 1.0f;
      }
      var tpi = pm.GetPartitionIndex(vs1);
      //Utils.DistFrom();
      
      // assign map keys to partitions.
      for (uint i = 0; i <  keys.Length; ++ i)
      {
        long partitionIndex = pm.GetPartitionIndex(keys[i]); // Utils.GetPartitionIndex(keys[i], false);
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
      // - accumulate versatility for chars
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
          float fdist = ValueSet.DistFrom(keys[ikey], ci.actualValues, this.weights, GetHueIndex());
          UInt32 dist = (UInt32)(fdist * Constants.DistanceRange);
          allMappings.Values[imap].dist = dist;
          distanceRange.Visit(dist);
          keys[ikey].MinDistFound = Math.Min(keys[ikey].MinDistFound, dist);
          keys[ikey].Visited = true;
          ci.versatility += dist;
          ci.mapKeysVisited++;
        }
      }

      Console.WriteLine("  Remaining elements: {0}", allMappings.Length);

      // some versatility adjustment and normalization
      uint maxCharVersatility = 0;
      foreach (CharInfo ci in charInfo)
      {
        ci.versatility = ci.versatility * Constants.CharVersatilityRange / ci.mapKeysVisited;
        if (ci.versatility > maxCharVersatility)
          maxCharVersatility = ci.versatility;
      }
      maxCharVersatility += 1;// so normalized values never quite reach 1

      Console.WriteLine("Distance range encountered: {0}", distanceRange);

      uint maxMinDist = 0;
      foreach (var mapKey in keys)
      {
        if (mapKey.Visited)
          maxMinDist = Math.Max(maxMinDist, mapKey.MinDistFound);
      }
      Console.WriteLine("Max minimum distance found: {0}", maxMinDist);

      timings.EndTask();

      timings.EnterTask("Pruning out mappings");
      long itemsRemoved = allMappings.PruneWhereDistGT(maxMinDist);

      Console.WriteLine("  calculating sort metric");
      for (int i = 0; i < allMappings.Length; ++i)
      {
        uint dist = allMappings.Values[i].dist;
        float fv = ((float)charInfo[(int)allMappings.Values[i].icharInfo].versatility) / maxCharVersatility;
        fv = 1.0f - fv;
        int vers = (int)(fv * Constants.DistanceRange);

        allMappings.Values[i].dist = (uint)(dist * Constants.DistanceRange + vers);// / 256;
      }

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
      timings.EnterTask(string.Format("Fill in any missing mappings (est: {0} / {1}%)", missingMappings, missingMappings / numDestCharacters));

      int missingKeys = 0;

      foreach (ValueSet k in keys)
      {
        CharInfo ci = null;
        if (!map.TryGetValue(k.ID, out ci))
        {
          // fill in this map key! just find the closest char.
          float minDist = distanceRange.MaxValue;
          foreach (var ci2 in charInfo)
          {
            float fdist = ValueSet.DistFrom(k, ci2.actualValues, this.weights, GetHueIndex());
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

      int numCellsX = (int)Math.Ceiling(Math.Sqrt(keys.Count()));
      Size mapImageSize = new Size(numCellsX * charSize.Width, numCellsX * charSize.Height);

      Console.WriteLine("MAP image generation...");
      Console.WriteLine("  Cells: [" + numCellsX + ", " + numCellsX + "]");
      Console.WriteLine("  Image size: [" + mapImageSize.Width + ", " + mapImageSize.Height + "]");

      this.mapBmp = new Bitmap(mapImageSize.Width, mapImageSize.Height, PixelFormat.Format24bppRgb);
      using (Graphics g = Graphics.FromImage(mapBmp))
      {
        foreach (ValueSet k in keys)
        {
          CharInfo ci = map[k.ID];

          long cellY = k.ID / numCellsX;
          long cellX = k.ID - (numCellsX * cellY);
          Rectangle srcRect = new Rectangle(ci.srcIndex.X * charSize.Width, ci.srcIndex.Y * charSize.Height, charSize.Width, charSize.Height);
          g.DrawImage(srcBmp, (int)(cellX * charSize.Width), (int)(cellY * charSize.Height), srcRect, GraphicsUnit.Pixel);
        }
      }

      Console.WriteLine("  Map keys in place: {0}", numDestCharacters - missingKeys);
      Console.WriteLine("MISSING MAP KEYS: {0} ({1}%)", missingKeys, (float)missingKeys / keys.Length * 100);

      mapBmp.Save(string.Format("..\\..\\img\\map-{0}-pow({1},{2}x{3}+{4}).png",
        System.IO.Path.GetFileNameWithoutExtension(fontFileName),
        valuesPerComponent, tilesPerCell.Width, tilesPerCell.Height, useChroma ? "2" : "0"));
    }

    internal static HybridMap2 Load(string path, Size charSize, Size tilesPerCell, int valuesPerComponent)
    {
      HybridMap2 ret = new HybridMap2();
      ret.mapBmp = new Bitmap(Bitmap.FromFile(path));
      ret.charSize = charSize;
      ret.tilesPerCell = tilesPerCell;
      ret.valuesPerComponent = valuesPerComponent;
      ret.componentsPerCell = 2 + Utils.Product(tilesPerCell);
      return ret;
    }

    // fills in the actual component values for this character.
    private unsafe void ProcessCharacter(Bitmap srcBmp, CharInfo ci)
    {
      //int componentIndex = 0;
      float charU = 0, charV = 0;
      for (int sy = 0; sy < tilesPerCell.Height; ++sy)
      {
        for (int sx = 0; sx < tilesPerCell.Width; ++sx)
        {
          // process a tile
          Size tileSize;
          Point tilePos;
          Utils.GetTileInfo(charSize, tilesPerCell, sx, sy, out tilePos, out tileSize);
          // process this single tile of this char.
          // grab all pixels for this tile and calculate Y component for each
          int tilePixelCount = 0;
          float tileY = 0;
          for (int py = 0; py < tileSize.Height; ++py)
          {
            for (int px = 0; px < tileSize.Width; ++px)
            {
              var c = srcBmp.GetPixel(charSize.Width * ci.srcIndex.X + tilePos.X + px, charSize.Height * ci.srcIndex.Y + tilePos.Y + py);
              float pixY, pixU, pixV;
              Utils.RGBtoYUV_Mapping(c, out pixY, out pixU, out pixV);
              //Utils.RGBtoHSL(c, out pixH, out pixS, out pixL);
              tileY += pixY;
              charU += pixU;
              charV += pixV;
              tilePixelCount++;
            }
          }

          ci.actualValues.Values[GetValueYIndex(sx,sy)] = tileY / tilePixelCount;// normalized to pixel
          //componentIndex++;
        }
      }

      int pixelsPerChar = Utils.Product(charSize);
      if (useChroma)
      {
        ci.actualValues.Values[GetValueUIndex()] = charU / pixelsPerChar;
        ci.actualValues.Values[GetValueVIndex()] = charV / pixelsPerChar;
      }
    }

    public unsafe void PETSCIIIZE(string srcImagePath, string destImagePath)
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
            float charU = 0, charV = 0;// accumulate Cr and Cb

            for (int ty = tilesPerCell.Height - 1; ty >= 0; --ty)
            {
              for (int tx = tilesPerCell.Width - 1; tx >= 0; --tx)
              {
                Point tilePos = Utils.GetTileOrigin(charSize, tilesPerCell, tx, ty);
                Color srcColor = testBmp.GetPixel(((srcCellX) * charSize.Width) + tilePos.X, ((srcCellY) * charSize.Height) + tilePos.Y);

                //srcColor = Utils.AdjustContrast(srcColor, 1.2);

                float cy, cu, cv;
                Utils.RGBtoYUV(srcColor, out cy, out cu, out cv);

                //int tileIndex = tx + (ty * tilesPerCell.Width);
                //vals.Values[this.componentsPerCell - 1 - tileIndex] = Utils.Clamp(cy, 0, 1);
                vals.Values[GetValueYIndex(tx, ty)] = Utils.Clamp(cy, 0, 1);
                charU += cu;
                charV += cv;
              }
            }
            int numTiles = Utils.Product(tilesPerCell);
            if (useChroma)
            {
              vals.Values[GetValueUIndex()] = Utils.Clamp(charU / numTiles, 0, 1);
              vals.Values[GetValueVIndex()] = Utils.Clamp(charV / numTiles, 0, 1);
            }

            // figure out which "ID" this value corresponds to.
            // (val - segCenter) would give us the boundary. for example between 0-1 with 2 segments, the center vals are .25 and .75.
            // subtract the center and you get .0 and .5 where you could multiply by segCount and get the proper seg of 0,1.
            // however let's not subtract the center, but rather segCenter*.5. Then after integer floor rounding, it will be the correct
            // value regardless of scale or any rounding issues.
            float halfSegCenter = 0.25f / valuesPerComponent;

            //for(int i = 0; i < this.componentsPerCell; ++ i )
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

