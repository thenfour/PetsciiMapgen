/*

ADDS some stuff to the hybrid map
- optimize processing
- optional chroma components
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
    public bool useChroma;

    private HybridMap2()
    {
    }

    public unsafe HybridMap2(string fontFileName, Size charSize, Size tilesPerCell, int valuesPerComponent, float Yweight, float UVweight, bool useChroma)
    {
      Timings timings = new Timings();

      this.tilesPerCell = tilesPerCell;
      this.charSize = charSize;
      this.valuesPerComponent = valuesPerComponent;
      int numYcomponents = Utils.Product(tilesPerCell);
      this.componentsPerCell = numYcomponents + 2; // number of dimensions

      weights = Utils.NewValueSet(componentsPerCell, 9997);
      this.weights.Values[0] = UVweight / 2;
      this.weights.Values[1] = UVweight / 2;
      for (int i = 0; i < numYcomponents; ++i)
      {
        this.weights.Values[i + 2] = Yweight / numYcomponents;
      }

      var srcImg = System.Drawing.Image.FromFile(fontFileName);
      var srcBmp = new Bitmap(srcImg);
      Size numSrcChars = Utils.Div(srcImg.Size, charSize);

      double numDestCharacters = Math.Pow(valuesPerComponent, componentsPerCell);

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
      foreach (var ci in charInfo)
      {
        for (int i = 0; i < componentsPerCell; ++i)
        {
          ci.actualValues.Values[i] = ranges[i].Normalize01(ci.actualValues.Values[i]);
        }
      }
      timings.EndTask();

      // create list of all mapkeys
      timings.EnterTask("Generating permutations");
      var keys = Utils.Permutate(componentsPerCell, Utils.GetDiscreteValues(valuesPerComponent)); // returns sorted.

      int lastDimensionIndex = componentsPerCell - 1;
      //Utils.AssertSortedByDimension(keys, lastDimensionIndex);

      timings.EndTask();
      timings.EnterTask("Calculate all mappings");

      // - generate a list of all mappings and their distances
      // - accumulate versatility for chars
      ulong theoreticalMappings = (ulong)Utils.Product(numSrcChars) * (ulong)numDestCharacters;
      Console.WriteLine("  (" + theoreticalMappings + " mappings)");
      Utils.ValueRangeInspector distanceRange = new Utils.ValueRangeInspector();
      //var allMappings = new Mapping[theoreticalMappings];
      MappingArray allMappings = new MappingArray();
      //int imap = 0;
      UInt32 shortCircuitSavedEnd = 0;
      UInt32 shortCircuitSavedBegin = 0;
      for (UInt32 ici = 0; ici < charInfo.Count; ++ ici)
      {
        var ci = charInfo[(int)ici];

        float lastDimensionCharValue = ci.actualValues.Values[lastDimensionIndex];

        // find literally any value that matches
        UInt32 iKeyOrigin = 0;

        // if our max dist is .5, then we should start at .5 in the list.right in the middle, because we're guaranteed it will match.
        // if maxdist is .3, then we want to check ranges where maxdist will never be exceeded.
        // that would be 0-.3, .3-.6. .6-.9, .9-1
        // and maybe with just a bit of overlap in there, so shrink the window a bit.
        UInt32 windowSize = (UInt32)(keys.Length * Constants.MaxDimensionDist * .9);
        windowSize = Math.Min((UInt32)keys.Length / 3, windowSize);
        for (iKeyOrigin = 0; iKeyOrigin < keys.Length; iKeyOrigin += windowSize)
        {
          float lastDimDist = Math.Abs(lastDimensionCharValue - keys[iKeyOrigin].Values[lastDimensionIndex]);
          if (lastDimDist <= Constants.MaxDimensionDist)
          {
            break;
          }
        }

        if (iKeyOrigin >= keys.Length)
          iKeyOrigin = (UInt32)keys.Length - 1;

        // walk left
        for (UInt32 ikey = iKeyOrigin; ; --ikey)
        {
          float lastDimDist = Math.Abs(lastDimensionCharValue - keys[ikey].Values[lastDimensionIndex]);
          if (lastDimDist > Constants.MaxDimensionDist)
          {
            shortCircuitSavedBegin += (uint)ikey;
            break;
          }

          long imap = allMappings.Add();
          allMappings.Values[imap].icharInfo = ici;
          allMappings.Values[imap].imapKey = ikey;
          float fdist = Utils.DistFrom(keys[ikey], ci.actualValues, this.weights);
          UInt32 dist = (UInt32)(fdist * Constants.DistanceRange);
          allMappings.Values[imap].dist = dist;
          distanceRange.Visit(dist);
          keys[ikey].MinDistFound = Math.Min(keys[ikey].MinDistFound, dist);

          ci.versatility += dist;
          ci.mapKeysVisited++;

          // uint loop exiting...
          if (ikey == 0)
            break;
        }

        // walk right
        for (UInt32 ikey = iKeyOrigin + 1; ikey < keys.Length; ++ ikey)
        {
          float lastDimDist = Math.Abs(lastDimensionCharValue - keys[ikey].Values[lastDimensionIndex]);
          if (lastDimDist > Constants.MaxDimensionDist)
          {
            shortCircuitSavedEnd += (uint)keys.Length - ikey;
            break;
          }

          long imap = allMappings.Add();
          allMappings.Values[imap].icharInfo = ici;
          allMappings.Values[imap].imapKey = ikey;
          float fdist = Utils.DistFrom(keys[ikey], ci.actualValues, this.weights);
          UInt32 dist = (UInt32)(fdist * Constants.DistanceRange);
          allMappings.Values[imap].dist = dist;
          distanceRange.Visit(dist);
          keys[ikey].MinDistFound = Math.Min(keys[ikey].MinDistFound, dist);

          // keys is sorted by its LAST dimension. let's figure out if the last dimension is within usable range,
          // and short circuit if not.
          ci.versatility += dist;
          ci.mapKeysVisited++;
        }
      }

      Console.WriteLine("  Short circuit saved us {0} mappings (END)", shortCircuitSavedBegin);
      Console.WriteLine("  Short circuit saved us {0} mappings (END)", shortCircuitSavedEnd);
      Console.WriteLine("  Remaining elements: {0}", allMappings.Length);

      // we short circuited many more map entries than we actually need. remove those.
      //allMappings.Truncate(imap);
      //var t = new Mapping[imap];
      //Array.Copy(allMappings, t, imap);
      //allMappings = t;
      //t = null;

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
        maxMinDist = Math.Max(maxMinDist, mapKey.MinDistFound);
      }
      Console.WriteLine("Max minimum distance found: {0}", maxMinDist);

      timings.EndTask();

      timings.EnterTask("Pruning out mappings");
      long itemsRemoved = allMappings.PruneWhereDistGT(maxMinDist);
      //var prunedMappings = allMappings.Where(o => o.dist <= maxMinDist).ToArray();

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

      //allMappings = null;

      timings.EndTask();
      timings.EnterTask("Sorting mappings");

      allMappings.SortByDist();
      //Array.Sort<Mapping>(prunedMappings, (a, b) => a.dist.CompareTo(b.dist));

      timings.EndTask();
      timings.EnterTask("Select mappings for map");

      // now walk through and fill in mappings from top to bottom.
      // maps key index to charinfo
      Dictionary<UInt64, CharInfo> map = new Dictionary<UInt64, CharInfo>((int)numDestCharacters);

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
      int missingKeys = 0;
      using (Graphics g = Graphics.FromImage(mapBmp))
      {
        foreach (ValueSet k in keys)
        {
          CharInfo ci = null;
          if (!map.TryGetValue(k.ID, out ci))
          {
            missingKeys++;
            continue;
          }

          ulong cellY = k.ID / (ulong)numCellsX;
          ulong cellX = k.ID - ((ulong)numCellsX * cellY);
          Rectangle srcRect = new Rectangle(ci.srcIndex.X * charSize.Width, ci.srcIndex.Y * charSize.Height, charSize.Width, charSize.Height);
          g.DrawImage(srcBmp, (int)(cellX * (ulong)charSize.Width), (int)(cellY * (ulong)charSize.Height), srcRect, GraphicsUnit.Pixel);
        }
      }

      Console.WriteLine("MISSING MAP KEYS: {0} ({1}%)", missingKeys, (float)missingKeys / keys.Length * 100);

      mapBmp.Save(string.Format("..\\..\\img\\map-{0}-pow({1},{2}x{3}+{4}).png",
        System.IO.Path.GetFileNameWithoutExtension(fontFileName),
        valuesPerComponent, tilesPerCell.Width, tilesPerCell.Height, useChroma ? "1" : "0"));
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
      int componentIndex = 0;
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
              Utils.RGBtoYUV(c, out pixY, out pixU, out pixV);
              tileY += pixY;
              charU += pixU;
              charV += pixV;
              tilePixelCount++;
            }
          }

          ci.actualValues.Values[componentIndex] = tileY / tilePixelCount;// normalized to pixel
          componentIndex++;
        }
      }

      int pixelsPerChar = Utils.Product(charSize);
      ci.actualValues.Values[componentIndex] = charU / pixelsPerChar;
      componentIndex++;
      ci.actualValues.Values[componentIndex] = charV / pixelsPerChar;
      componentIndex++;
    }

    public unsafe void PETSCIIIZE(string srcImagePath, string destImagePath, bool shade)
    {
      Console.WriteLine("  tranfsorm image + " + srcImagePath);
      var testImg = Image.FromFile(srcImagePath);
      Bitmap testBmp = new Bitmap(testImg);
      Bitmap destImg = new Bitmap(testBmp.Width, testBmp.Height, PixelFormat.Format32bppArgb);

      int mapCellsX = mapBmp.Width / charSize.Width;
      int numDestCharacters = (int)Math.Pow(valuesPerComponent, componentsPerCell);

      using (var g = Graphics.FromImage(destImg))
      {
        ValueSet vals = Utils.NewValueSet(componentsPerCell, 9995);
        // roughly simulate the shader algo
        for (int srcCellY = 0; srcCellY < testImg.Height / charSize.Height; ++srcCellY)
        {
          for (int srcCellX = 0; srcCellX < testImg.Width / charSize.Width; ++srcCellX)
          {
            // sample in the cell to determine the "key" "ID".
            int ID = 0;
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

                int tileIndex = tx + (ty * tilesPerCell.Width);
                vals.Values[this.componentsPerCell - 1 - tileIndex] = Utils.Clamp(cy, 0, 1);
                charU += cu;
                charV += cv;
              }
            }
            int numTiles = Utils.Product(tilesPerCell);
            vals.Values[1] = Utils.Clamp(charU / numTiles, 0, 1);
            vals.Values[0] = Utils.Clamp(charV / numTiles, 0, 1);

            // figure out which "ID" this value corresponds to.
            // (val - segCenter) would give us the boundary. for example between 0-1 with 2 segments, the center vals are .25 and .75.
            // subtract the center and you get .0 and .5 where you could multiply by segCount and get the proper seg of 0,1.
            // however let's not subtract the center, but rather segCenter*.5. Then after integer floor rounding, it will be the correct
            // value regardless of scale or any rounding issues.
            float halfSegCenter = 0.25f / valuesPerComponent;

            for(int i = 0; i < this.componentsPerCell; ++ i )
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
            int mapCellY = ID / mapCellsX;
            int mapCellX = ID - (mapCellY * mapCellsX);

            // blit from map img.
            Rectangle srcRect = new Rectangle(mapCellX * charSize.Width, mapCellY * charSize.Height, charSize.Width, charSize.Height);
            g.DrawImage(mapBmp, srcCellX * charSize.Width, srcCellY * charSize.Height, srcRect, GraphicsUnit.Pixel);
          }
        }
      }

      destImg.Save(destImagePath);
    }
  }
}

