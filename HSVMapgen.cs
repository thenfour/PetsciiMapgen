/*

similar to PetsciiMapgen,
except instead of the "value set" being NxN spacial tiles, the valueset will be 0-1 H,S,V (averaged).

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

using ValueSet = PetsciiMapgen.ValueSet;
using Utils = PetsciiMapgen.Utils;
using CharInfo = PetsciiMapgen.CharInfo;
using Mapping = PetsciiMapgen.Mapping;

namespace HSVMapgen
{
  public class PetsciiMap
  {
    public ValueSet weights = new ValueSet(-1);
    public Bitmap mapBmp;
    public Size charSize;
    public int valuesPerComponent;

    public PetsciiMap(string fontFileName, Size charSize, int valuesPerComponent)
    {
      this.weights[0] = .6;
      this.weights[1] = .2;
      this.weights[2] = .2;

      this.charSize = charSize;
      this.valuesPerComponent = valuesPerComponent;

      var srcImg = System.Drawing.Image.FromFile(fontFileName);

      var srcBmp = new Bitmap(srcImg);
      System.Drawing.Size numSrcChars = Utils.Div(srcImg.Size, charSize);

      double numDestCharacters = Math.Pow(valuesPerComponent, 3);

      Console.WriteLine("Src character size: " + Utils.ToString(charSize));
      Console.WriteLine("Src image size: " + Utils.ToString(srcBmp.Size));
      Console.WriteLine("Number of source chars: " + Utils.ToString(numSrcChars));
      Console.WriteLine("Number of source chars (1d): " + Utils.Product(numSrcChars));
      Console.WriteLine("Chosen values per tile: " + valuesPerComponent);
      Console.WriteLine("Resulting map will have this many entries: " + numDestCharacters);
      //int maxUsages = (int)Math.Ceiling(numDestCharacters / Utils.Product(numSrcChars));
      //Console.WriteLine("Characters shall be re-used maximum: " + maxUsages);

      //double pixelsPerTileAvgX = (double)charSize.Width / numTilesPerChar.Width;
      //double pixelsPerTileAvgY = (double)charSize.Height / numTilesPerChar.Height;
      //double pixelsPerTile = pixelsPerTileAvgX * pixelsPerTileAvgY;

      // fill in char source info (actual tile values)
      var charInfo = new List<CharInfo>();
      int pixelsPerChar = Utils.Product(charSize);
      Utils.ValueRangeInspector indY = new Utils.ValueRangeInspector();
      Utils.ValueRangeInspector indU = new Utils.ValueRangeInspector();
      Utils.ValueRangeInspector indV = new Utils.ValueRangeInspector();
      Utils.ValueRangeInspector avgY = new Utils.ValueRangeInspector();
      Utils.ValueRangeInspector avgU = new Utils.ValueRangeInspector();
      Utils.ValueRangeInspector avgV = new Utils.ValueRangeInspector();
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

          // process this single tile of this char.
          for (int py = 0; py < charSize.Height; ++py)
          {
            for (int px = 0; px < charSize.Width; ++px)
            {
              var c = srcBmp.GetPixel(ci.srcOrigin.X + px, ci.srcOrigin.Y + py);
              //double hue, saturation, value;
              //Utils.ColorToHSV(c, out hue, out saturation, out value);
              //hue /= 360.0;
              //hue = 0.0;
              //ci.actualValues[0] += hue;
              //ci.actualValues[1] += saturation;
              //ci.actualValues[2] += value;
              double cy, cu, cv;
              Utils.RGBtoYUV(c, out cy, out cu, out cv);
              ci.actualValues[0] += cy;
              ci.actualValues[1] += cu;
              ci.actualValues[2] += cv;

              indY.Visit(cy);
              indU.Visit(cu);
              indV.Visit(cv);
            }
          }
          // divide => average. note that because we're using averages over
          // a big area, it's less likely to reach 0.0 or 1.0. some kind of normalization might be needed.
          ci.actualValues[0] /= pixelsPerChar;
          ci.actualValues[1] /= pixelsPerChar;
          ci.actualValues[2] /= pixelsPerChar;

          avgY.Visit(ci.actualValues[0]);
          avgU.Visit(ci.actualValues[1]);
          avgV.Visit(ci.actualValues[2]);

          charInfo.Add(ci);
        }
      }

      Console.WriteLine("RANGES encountered:");
      Console.WriteLine("  individual Y: " + indY);
      Console.WriteLine("  individual U: " + indU);
      Console.WriteLine("  individual V: " + indV);
      Console.WriteLine("  avg Y       : " + avgY);
      Console.WriteLine("  avg U       : " + avgU);
      Console.WriteLine("  avg V       : " + avgV);

      // normalize all YUV seen so it will correspond nicely with permutations.
      foreach (var ci in charInfo)
      {
        ci.actualValues[0] = avgY.Normalize01(ci.actualValues[0]);
        ci.actualValues[1] = avgU.Normalize01(ci.actualValues[1]);
        ci.actualValues[2] = avgV.Normalize01(ci.actualValues[2]);
      }

      // create list of all mapkeys
      var keys = Utils.Permutate(3, Utils.GetDiscreteValues(valuesPerComponent));

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


    public void PETSCIIIZE(string srcImagePath, string destImagePath, bool shade)
    {
      var testImg = Image.FromFile(srcImagePath);
      Bitmap testBmp = new Bitmap(testImg);
      Bitmap destImg = new Bitmap(testBmp.Width, testBmp.Height, PixelFormat.Format32bppArgb);

      int mapCellsX = mapBmp.Width / charSize.Width;
      int maxID = (int)Math.Pow(valuesPerComponent, 3);

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
            Color srcColor = testBmp.GetPixel(srcCellX * charSize.Width + (charSize.Width/2), srcCellY * charSize.Height + (charSize.Height/2));
            srcColor = Utils.AdjustContrast(srcColor, 1.2);
            //srcColor = Color.Black;

            double cy, cu, cv;
            Utils.RGBtoYUV(srcColor, out cy, out cu, out cv);
            vals[2] = Utils.Clamp(cy,0,1);// reverse order from how they're put into the map
            vals[1] = Utils.Clamp(cu, 0, 1);
            vals[0] = Utils.Clamp(cv, 0, 1);

            // figure out which "ID" this value corresponds to.
            // (val - segCenter) would give us the boundary. for example between 0-1 with 2 segments, the center vals are .25 and .75.
            // subtract the center and you get .0 and .5 where you could multiply by segCount and get the proper seg of 0,1.
            // however let's not subtract the center, but rather segCenter*.5. Then after integer floor rounding, it will be the correct
            // value regardless of scale or any rounding issues.
            double halfSegCenter = 0.25 / valuesPerComponent;

            for(int i = 0; i < 3; ++ i )
            {
              double val = vals[i];
              //val = 0.0;
              val -= halfSegCenter;
              val = Utils.Clamp(val, 0, 1);
              val *= valuesPerComponent;
              ID *= valuesPerComponent;
              ID += (int)Math.Floor(val);
            }

            if (ID >= maxID)
            {
              ID = maxID - 1;
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

/*      if (shade)
      {
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
      }*/


      destImg.Save(destImagePath);
    }
  }
}

