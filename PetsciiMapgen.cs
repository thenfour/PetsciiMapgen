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
    public ValueSet closestDestPos;// = new ValueSet();
    public Value totalDistance;
    public int usages = 0;

    public override string ToString()
    {
      return srcIndex.ToString();
    }
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

      // process all chars finding ideal locations and values
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
              Value acc = new Value();
              int count = 0;
              for (int py = 0; py < tileSize.Height; ++py)
              {
                for (int px = 0; px < tileSize.Width; ++px)
                {
                  var c = srcBmp.GetPixel(ci.srcOrigin.X + tilePos.X + px, ci.srcOrigin.Y + tilePos.Y + py);
                  acc.Accumulate(new Value(Utils.ToGrayscale(c)));
                  count++;
                }
              }

              ci.actualValues[tileIndex] = acc.DividedBy(count);// normalized to pixel

              tileIndex++;
            }
          }

          ci.closestDestPos = Utils.FindClosestDestValueSet(valuesPerTile, ci.actualValues);
          ci.totalDistance = ci.actualValues.DistFrom(ci.closestDestPos, 1);

          charInfo.Add(ci);
        }
      }

      // contains mapping from destvalueset => char
      Dictionary<ValueSet, CharInfo> map = new Dictionary<ValueSet, CharInfo>();

      // fill in all map positions with nulls. the following code will aim to fill them in.
      var permutations = Utils.Permutate(Utils.Product(numTilesPerChar), Utils.GetDiscreteValues(valuesPerTile));
      foreach (var p in permutations)
      {
        map[p] = null;
      }

      // sort from best match to worst
      charInfo.Sort((CharInfo x, CharInfo y) => x.totalDistance.CompareTo(y.totalDistance));

      Console.WriteLine("Best matching distance : " + charInfo.First().totalDistance.ToString());
      Console.WriteLine("Worst matching distance: " + charInfo.Last().totalDistance.ToString());

      List<ValueSet> keys = map.Keys.ToList();

      // walk through remaining empty map entries, find best char, whether used or not.
      int inserted3 = 0;
      foreach (var k in keys)
      {
        if (map[k] != null)
          continue;
        Value closestD = null;
        CharInfo closestChar = null;
        foreach (var ci in charInfo)
        {
          if (ci.usages >= maxUsages)
            continue;
          // here you could consider that more-used chars have less priority, to make the output map more varied.
          var d = ci.actualValues.DistFrom(k, 1);
          if (closestD == null || d.IsLessThan(closestD))
          {
            closestD = d;
            closestChar = ci;
          }
        }
        Debug.Assert(closestChar != null);
        map[k] = closestChar;
        closestChar.usages++;
        inserted3++;
      }

      Console.WriteLine("Characters inserted: " + inserted3);




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

      int numCellsX = (int)Math.Ceiling(Math.Sqrt(keys.Count));
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
          //break;
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

      Func<float, float> TransformColorant = (float c) =>
      {
        return (float)Math.Floor(c / .25f) * .25f;
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

