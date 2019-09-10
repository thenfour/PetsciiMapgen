/*
 * 
 * 
 * the original method attempt was 3 stages:
 * 1. insert characters into their best match in the map
 *    many chars are not inserted because their best match is already occupied.
 * 2. fill map with their best match of unused characters
 * 3. fill map with best match of whatever character
 * 
 * this was no good because the map is so much bigger than the charset. so characters
 * with great matches ended up being used only once, then the rest of the map sorta
 * gets filled up with a few chars. lousy distribution.
 * 
 * second method attempt was JUST step 3 above, except with "maximum # of usages"
 * per char. this means once a char has been used more than it should for even
 * distribution, then it's no longer considered.
 * 
 * this can also have a problem because it will insert that character at the beginning
 * of the map (whatever that even means), and it may actually be better suited
 * elsewhere.
 * 
 * i want to use some method that can balance a few factors:
 * 1) best match for the map cell
 * 2) best match for the character
 * 3) how versatile this map cell is
 *    in other words, if a map cell is only matched well by 1 character, use it.
 *    but if it fits well with many characters, then allow selecting lower priority.
 * 4) how versatile this character is
 *    same but reverse. if a character fits only 1 map entry well, then stick it
 *    there. if it fits many, then let lesser-versatile chars fit these entries.
 
 My feeling is that 3 and 4 can be somehow sorta naturally accounted for by
 some kind of sorting.

 TOTAL mappings are about 4 million for the 2x2x16 case which seems most practical.
 so it's plausible to just boil all this down to 1 heuristic and sort.

 "match", which is now "distance", should be normalized from 0-1.
 so we can generate:
 - for each character, "versatility" which is the sum of distances to map keys.
 - for each map key, "versatility" which is the sum of distances to character.
 - make those 2 both percentiles
 - a single 4-million entry list of KEY => CHARACTER
   {
    distance %ile
    versatility // a single versatility value. the sum of %ile.
   }

 we want to select the most specific and best matches first.
 have to find a way to boil dist/vers into a single sortable.
 most obvious way is to just do a weighted sum.

 OK versatility does sorta help keep things better distributed but i don't think
 it's thought through well enough. it's good enough however.

 max usage seems to not be really useful either.

 you really just need to have the right charset from the start.
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
  public class CharInfo
  {
    public System.Drawing.Size size;
    public System.Drawing.Point srcOrigin;
    public System.Drawing.Point srcIndex;
    public ValueSet actualValues = new ValueSet(-1);// N-dimension values
    public int usages = 0;

    public int versatility; // sum of distances to all map keys. lower values = more versatile

    public override string ToString()
    {
      return srcIndex.ToString();
    }
  }

  public class Mapping
  {
    public ValueSet mapKey; // a set of tile values
    public CharInfo charInfo;
    public int dist;
  }

  public class PetsciiMap
  {
    public Bitmap mapBmp;
    public Size charSize;
    public Size numTilesPerChar;
    public int valuesPerTile;

    public PetsciiMap(string fontFileName, Size charSize, Size numTilesPerChar, int valuesPerTile)
    {
      this.charSize = charSize;
      this.numTilesPerChar = numTilesPerChar;
      this.valuesPerTile = valuesPerTile;

      var srcImg = System.Drawing.Image.FromFile(fontFileName);

      var srcBmp = new Bitmap(srcImg);
      System.Drawing.Size numSrcChars = Utils.Div(srcImg.Size, charSize);

      double idealValuesPerTile = Math.Pow(Utils.Product(numSrcChars), 1.0 / Utils.Product(numTilesPerChar));
      idealValuesPerTile = Math.Ceiling(idealValuesPerTile);
      // that's the ideal values per dimension to get 1 char per value. you can increase that to duplicate chars, or decrease it for fewer chars in the map.

      double numDestCharacters = Math.Pow(valuesPerTile, Utils.Product(numTilesPerChar));

      Console.WriteLine("Tiles per char: " + Utils.ToString(numTilesPerChar));
      Console.WriteLine("Src character size: " + Utils.ToString(charSize));
      Console.WriteLine("Src image size: " + Utils.ToString(srcBmp.Size));
      Console.WriteLine("Number of source chars: " + Utils.ToString(numSrcChars));
      Console.WriteLine("Number of source chars (1d): " + Utils.Product(numSrcChars));
      Console.WriteLine("Ideal values per tile: " + idealValuesPerTile.ToString("0"));
      Console.WriteLine("Chosen values per tile: " + valuesPerTile);
      Console.WriteLine("Resulting map will have this many entries: " + numDestCharacters);
      int maxUsages = (int)Math.Ceiling(numDestCharacters / Utils.Product(numSrcChars));
      Console.WriteLine("Characters shall be re-used maximum: " + maxUsages);

      double pixelsPerTileAvgX = (double)charSize.Width / numTilesPerChar.Width;
      double pixelsPerTileAvgY = (double)charSize.Height / numTilesPerChar.Height;
      double pixelsPerTile = pixelsPerTileAvgX * pixelsPerTileAvgY;

      // fill in char source info (actual tile values)
      var charInfo = new List<CharInfo>();
      for (int y = 0; y < numSrcChars.Height; ++y)
      {
        for (int x = 0; x < numSrcChars.Width; ++x)
        {
          // gather up all the info we can abotu this char.
          var ci = new CharInfo
          {
            srcOrigin = new Point(x * charSize.Width, y * charSize.Height),
            size = charSize,
            srcIndex = new Point(x, y)
          };

          int tileIndex = 0;
          for (int sy = 0; sy < numTilesPerChar.Height; ++sy)
          {
            for (int sx = 0; sx < numTilesPerChar.Width; ++sx)
            {
              // process a tile

              Size tileSize;
              Point tilePos;
              Utils.GetTileInfo(ci.size, numTilesPerChar, sx, sy, out tilePos, out tileSize);
              // process this single tile of this char.
              double acc = 0.0;
              int count = 0;
              for (int py = 0; py < tileSize.Height; ++py)
              {
                for (int px = 0; px < tileSize.Width; ++px)
                {
                  var c = srcBmp.GetPixel(ci.srcOrigin.X + tilePos.X + px, ci.srcOrigin.Y + tilePos.Y + py);
                  acc += Utils.ToGrayscale(c);
                  count++;
                }
              }

              ci.actualValues[tileIndex] = acc / count;// normalized to pixel

              tileIndex++;
            }
          }

          charInfo.Add(ci);
        }
      }

      // create list of all mapkeys
      var keys = Utils.Permutate(Utils.Product(numTilesPerChar), Utils.GetDiscreteValues(valuesPerTile));

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
          mapping.dist = (int)(mapKey.DistFrom(ci.actualValues) * 100.0);
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


    public void PETSCIIIZE(string srcImagePath, string destImagePath)
    {
      var testImg = Image.FromFile(srcImagePath);
      Bitmap testBmp = new Bitmap(testImg);
      Bitmap destImg = new Bitmap(testBmp.Width, testBmp.Height, PixelFormat.Format32bppArgb);

      int mapCellsX = mapBmp.Width / charSize.Width;

      using (var g = Graphics.FromImage(destImg))
      {
        // roughly simulate the shader algo
        for (int srcCellY = 0; srcCellY < testImg.Height / charSize.Height; ++srcCellY)
        {
          for (int srcCellX = 0; srcCellX < testImg.Width / charSize.Width; ++srcCellX)
          {
            // sample in the cell to determine the "key" "ID".
            int ID = 0;
            for (int ty = numTilesPerChar.Height - 1; ty >= 0; --ty)
            {
              for (int tx = numTilesPerChar.Width - 1; tx >= 0; --tx)
              {
                Point tilePos = Utils.GetTileOrigin(charSize, numTilesPerChar, tx, ty);
                Color srcColor = testBmp.GetPixel((srcCellX * charSize.Width) + tilePos.X, (srcCellY * charSize.Height) + tilePos.Y);
                double val = Utils.ToGrayscale(srcColor);
                
                // edge detection & contrast should be applied to make the effect fit nicer
                //val = Utils.AdjustContrast(val, 1.2);

                // figure out which "ID" this value corresponds to.
                // (val - segCenter) would give us the boundary. for example between 0-1 with 2 segments, the center vals are .25 and .75.
                // subtract the center and you get .0 and .5 where you could multiply by segCount and get the proper seg of 0,1.
                // however let's not subtract the center, but rather segCenter*.5. Then after integer floor rounding, it will be the correct
                // value regardless of scale or any rounding issues.
                double halfSegCenter = 0.25 / valuesPerTile;
                val -= halfSegCenter;
                val *= valuesPerTile;
                int thisTileID = (int)val;
                ID *= valuesPerTile;
                ID += thisTileID;
              }
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

      Func<double, double> TransformColorant = (double c) =>
      {
        return (double)Math.Floor(c / .25f) * .25f;
      };

      Utils.TransformPixels(testBmp, c =>
      {
        c.r = TransformColorant(c.r);
        c.g = TransformColorant(c.g);
        c.b = TransformColorant(c.b);
        return c;
      });
      Utils.Pixellate(testBmp, charSize);
      Utils.Multiply(destImg, testBmp);

      destImg.Save(destImagePath);
    }
  }
}

