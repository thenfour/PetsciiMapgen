/*

combination of the other 2 map generators.
considers YUV components, spread over sub-char tiles.

similar to video encoding, we give more detail to the Y component.
So, Y is expressed through the spacial mapping.
And UV are always given 1 component each (whole-char)

This means more dimensions than before, which means bigger maps.
You'll never go more than 2x2 for Y. and UV are 1
so realistically this is ALWAYS 6 dimensions.

Each dimension can theoretically have N discrete values, however for simplicity and performance I just want 
to make them all have the same buckets.

the original 2x2x16 map had 64k entries and felt reasonable. To achieve that
with 6 dimensions, it means about 6 values per dimension. and heck that's not
bad at all really.

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
  public class HybridMap
  {
    public ValueSet weights = new ValueSet(-1); // weights for calculating distance.
    public Bitmap mapBmp;
    public Size charSize;
    public Size tilesPerCell;
    public int valuesPerComponent;
    public int componentsPerCell; // # of dimensions (UV + Y*size)

    private HybridMap()
    {
      //
    }

    public HybridMap(string fontFileName, Size charSize, Size tilesPerCell, int valuesPerComponent, double Yweight, double UVweight)
    {
      // we want Y to be weighted at .6 and UV to be .4.
      this.weights[0] = UVweight / 2;
      this.weights[1] = UVweight / 2;
      int numYcomponents = Utils.Product(tilesPerCell);
      for (int i = 0; i < numYcomponents; ++ i)
      {
        this.weights[i + 2] = Yweight / numYcomponents;
      }

      this.tilesPerCell = tilesPerCell;
      this.charSize = charSize;
      this.valuesPerComponent = valuesPerComponent;
      this.componentsPerCell = numYcomponents + 2;

      var srcImg = System.Drawing.Image.FromFile(fontFileName);
      var srcBmp = new Bitmap(srcImg);
      Size numSrcChars = Utils.Div(srcImg.Size, charSize);

      double numDestCharacters = Math.Pow(valuesPerComponent, componentsPerCell);

      Console.WriteLine("Src character size: " + Utils.ToString(charSize));
      Console.WriteLine("Src image size: " + Utils.ToString(srcBmp.Size));
      Console.WriteLine("Number of source chars: " + Utils.ToString(numSrcChars));
      Console.WriteLine("Number of source chars (1d): " + Utils.Product(numSrcChars));
      Console.WriteLine("Chosen values per tile: " + valuesPerComponent);
      Console.WriteLine("Resulting map will have this many entries: " + numDestCharacters);

      // fill in char source info (actual tile values)
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
          var ci = new CharInfo
          {
            srcOrigin = new Point(x * charSize.Width, y * charSize.Height),
            size = charSize,
            srcIndex = new Point(x, y)
          };

          ProcessCharacter(srcBmp, ci);

          for (int i = 0; i < componentsPerCell; ++i)
          {
            ranges[i].Visit(ci.actualValues[i]);
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
          ci.actualValues[i] = ranges[i].Normalize01(ci.actualValues[i]);
        }
      }

      // create list of all mapkeys
      Console.WriteLine("Generating permutations...");
      var keys = Utils.Permutate(componentsPerCell, Utils.GetDiscreteValues(valuesPerComponent));

      // - generate a list of all mappings and their distances
      // - accumulate versatility for chars
      Console.WriteLine("Generating list of mappings (BIG = " + (Utils.Product(numSrcChars) * (int)numDestCharacters) + ")");
      var allMappings = new List<Mapping>(Utils.Product(numSrcChars) * (int)numDestCharacters);
      foreach (var mapKey in keys)
      {
        foreach (var ci in charInfo)
        {
          Mapping mapping = new Mapping
          {
            charInfo = ci,
            mapKey = mapKey
          };
          mapping.dist = (int)(mapKey.DistFrom(ci.actualValues, this.weights) * 1000.0);
          ci.versatility += mapping.dist;
          allMappings.Add(mapping);
        }
      }

      Console.WriteLine("sort");
      allMappings.Sort((x, y) =>
      {
        var d = x.dist.CompareTo(y.dist);
        if (d == 0)
        {
          d = x.charInfo.versatility.CompareTo(y.charInfo.versatility);
        }
        return d;
      });

      // now walk through and fill in mappings from top to bottom.
      Dictionary<ValueSet, CharInfo> map = new Dictionary<ValueSet, CharInfo>();

      Console.WriteLine("insert into map");
      foreach (var mapping in allMappings)
      {
        if (map.ContainsKey(mapping.mapKey))
          continue; // already mapped.
        map[mapping.mapKey] = mapping.charInfo;
        mapping.charInfo.usages++;
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

      Console.WriteLine("Post-map stats:");
      Console.WriteLine("  Used char count: " + numCharsUsed);
      Console.WriteLine("  Number of unused char: " + (charInfo.Count - numCharsUsed));
      Console.WriteLine("  Number of chars used exactly once: " + numCharsUsedOnce);
      Console.WriteLine("  Most-used char: " + mostUsedChar + " (" + mostUsedChar.usages + ") usages");
      Console.WriteLine("  Number of total char repetitions: " + numRepetitions);

      // now generate the map image from the map struct. It won't be human-readable; it's going to simply
      // be a 2D wrapped row of the map keys.

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
          CharInfo ci = map[k];
          int cellY = k.ID / numCellsX;
          int cellX = k.ID - (numCellsX * cellY);
          Rectangle srcRect = new Rectangle(ci.srcOrigin.X, ci.srcOrigin.Y, charSize.Width, charSize.Height);
          g.DrawImage(srcBmp, cellX * charSize.Width, cellY * charSize.Height, srcRect, GraphicsUnit.Pixel);
        }
      }

      mapBmp.Save("..\\..\\img\\map.png");
    }

    internal static HybridMap Load(string path, Size charSize, Size tilesPerCell, int valuesPerComponent)
    {
      HybridMap ret = new HybridMap();
      ret.mapBmp = new Bitmap(Bitmap.FromFile(path));
      ret.charSize = charSize;
      ret.tilesPerCell = tilesPerCell;
      ret.valuesPerComponent = valuesPerComponent;
      ret.weights = null;
      ret.componentsPerCell = 2 + Utils.Product(tilesPerCell);
      return ret;
    }

  // fills in the actual component values for this character.
  private void ProcessCharacter(Bitmap srcBmp, CharInfo ci)
    {
      int componentIndex = 0;
      double charU = 0, charV = 0;
      for (int sy = 0; sy < tilesPerCell.Height; ++sy)
      {
        for (int sx = 0; sx < tilesPerCell.Width; ++sx)
        {
          // process a tile
          Size tileSize;
          Point tilePos;
          Utils.GetTileInfo(ci.size, tilesPerCell, sx, sy, out tilePos, out tileSize);
          // process this single tile of this char.
          // grab all pixels for this tile and calculate Y component for each
          int tilePixelCount = 0;
          double tileY = 0;
          for (int py = 0; py < tileSize.Height; ++py)
          {
            for (int px = 0; px < tileSize.Width; ++px)
            {
              var c = srcBmp.GetPixel(ci.srcOrigin.X + tilePos.X + px, ci.srcOrigin.Y + tilePos.Y + py);
              double pixY, pixU, pixV;
              Utils.RGBtoYUV(c, out pixY, out pixU, out pixV);
              tileY += pixY;
              charU += pixU;
              charV += pixV;
              tilePixelCount++;
            }
          }

          ci.actualValues[componentIndex] = tileY / tilePixelCount;// normalized to pixel
          componentIndex++;
        }
      }

      int pixelsPerChar = Utils.Product(ci.size);
      ci.actualValues[componentIndex] = charU / pixelsPerChar;
      componentIndex++;
      ci.actualValues[componentIndex] = charV / pixelsPerChar;
      componentIndex++;
    }

    public void PETSCIIIZE(string srcImagePath, string destImagePath, bool shade)
    {
      var testImg = Image.FromFile(srcImagePath);
      Bitmap testBmp = new Bitmap(testImg);
      Bitmap destImg = new Bitmap(testBmp.Width, testBmp.Height, PixelFormat.Format32bppArgb);

      int mapCellsX = mapBmp.Width / charSize.Width;
      int numDestCharacters = (int)Math.Pow(valuesPerComponent, componentsPerCell);

      using (var g = Graphics.FromImage(destImg))
      {
        ValueSet vals = new ValueSet(-1);
        // roughly simulate the shader algo
        for (int srcCellY = 0; srcCellY < testImg.Height / charSize.Height; ++srcCellY)
        {
          for (int srcCellX = 0; srcCellX < testImg.Width / charSize.Width; ++srcCellX)
          {
            // sample in the cell to determine the "key" "ID".
            int ID = 0;
            double charU = 0, charV = 0;// accumulate Cr and Cb

            for (int ty = tilesPerCell.Height - 1; ty >= 0; --ty)
            {
              for (int tx = tilesPerCell.Width - 1; tx >= 0; --tx)
              {
                Point tilePos = Utils.GetTileOrigin(charSize, tilesPerCell, tx, ty);
                Color srcColor = testBmp.GetPixel(((srcCellX) * charSize.Width) + tilePos.X, ((srcCellY) * charSize.Height) + tilePos.Y);

                srcColor = Utils.AdjustContrast(srcColor, 1.2);

                double cy, cu, cv;
                Utils.RGBtoYUV(srcColor, out cy, out cu, out cv);

                int tileIndex = tx + (ty * tilesPerCell.Width);
                vals[this.componentsPerCell - 1 - tileIndex] = Utils.Clamp(cy, 0, 1);
                charU += cu;
                charV += cv;
              }
            }
            int numTiles = Utils.Product(tilesPerCell);
            vals[1] = Utils.Clamp(charU / numTiles, 0, 1);
            vals[0] = Utils.Clamp(charV / numTiles, 0, 1);

            // figure out which "ID" this value corresponds to.
            // (val - segCenter) would give us the boundary. for example between 0-1 with 2 segments, the center vals are .25 and .75.
            // subtract the center and you get .0 and .5 where you could multiply by segCount and get the proper seg of 0,1.
            // however let's not subtract the center, but rather segCenter*.5. Then after integer floor rounding, it will be the correct
            // value regardless of scale or any rounding issues.
            double halfSegCenter = 0.25 / valuesPerComponent;

            for(int i = 0; i < this.componentsPerCell; ++ i )
            {
              double val = vals[i];
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

