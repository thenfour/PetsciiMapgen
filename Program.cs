/*
 * 
 * which i think is not exactly trivial. in the 2x2x16 case, the total # of mappings is 64k x 64 = 4 million mappings.
 * that's sorta ridiculous, but not so ridiculous we shouldn't consider it.
 * so generate a huge list of mappings from key, character => distance
 * and then start filling in from best matches.
 * we should aim to use characters equally, so after a char has been used N times, stop considering it.
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
  // basically a wrapper around double to ensure values are handled properly WRT
  // comparison and distance etc.
  public class Value : IComparable<Value>
  {
    private double _v;
    public Value()
    {
      _v = 0;
    }
    public Value(double v)
    {
      _v = v;
    }
    public Value DistanceFrom(Value rhs)
    {
      return new Value(Math.Abs(_v - rhs._v));
    }
    public bool IsLessThan(Value rhs)
    {
      return _v < rhs._v;
    }
    public void Accumulate(Value rhs) // yes this is just add. again just to be extra certain of proper usage.
    {
      _v += rhs._v;
    }
    public void AccMax(Value rhs) // takes either current value or rhs, whichever is max
    {
      _v = Math.Max(_v, rhs._v);
    }
    public Value DividedBy(int c)
    {
      return new Value(_v / c);
    }
    public Value DividedBy(double c)
    {
      return new Value(_v / c);
    }

    public int CompareTo(Value other)
    {
      if (other == null)
        return -1;
      return _v.CompareTo(other._v);
    }

    int IComparable<Value>.CompareTo(Value other)
    {
      return CompareTo(other);
    }

    public override string ToString()
    {
      return _v.ToString("0.0000");
    }
  }

  // basically wraps List<Value>.
  // simplifies code that wants to do set operations.
  public class ValueSet : IComparable<ValueSet>, IEqualityComparer<ValueSet>
  {
    public ValueSet(int id)
    {
      _id = id;
    }

    List<Value> values = new List<Value>();
    int _id;

    public int Length { get { return values.Count; } }
    public int ID {  get { return _id; } }

    public Value this[int i]
    {
      get {
        return values[i];
      }
      set {
        // ensure we can hold this value
        if (i >= values.Count)
        {
          values.AddRange(Enumerable.Repeat<Value>(null, 1 + (i - values.Count)));
        }
        values[i] = value;
      }
    }

    // compares two sets of values and comes up with a "distance" measuring the lack of similarity between them.
    // here we just return the sum of distances and normalize so it's "per pixel avg".
    // i think there's probably a smarter way to do this.
    public Value DistFrom(ValueSet b)
    {
      Debug.Assert(this.Length == b.Length);
      Value acc = new Value();
      for (int i = 0; i < this.Length; ++i)
      {
        acc.Accumulate(this[i].DistanceFrom(b[i])); // also possible: use accMax
      }
      return acc;
      //return acc.DividedBy(numPixels);
    }

    int IComparable<ValueSet>.CompareTo(ValueSet other)
    {
      if (other == null)
        return -1;
      int d = other.values.Count.CompareTo(this.values.Count);
      if (d != 0)
        return d;
      for (int i = 0; i < values.Count; ++i)
      {
        d = other.values[i].CompareTo(values[i]);
        if (d != 0)
          return d;
      }
      return 0;
    }

    bool IEqualityComparer<ValueSet>.Equals(ValueSet x, ValueSet y)
    {
      return x.Equals(y);
    }

    int IEqualityComparer<ValueSet>.GetHashCode(ValueSet obj)
    {
      return obj.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (!(obj is ValueSet))
        return false;
      return (this as IComparable<ValueSet>).CompareTo(obj as ValueSet) == 0;
    }
    public override int GetHashCode()
    {
      return ToString().GetHashCode();
    }
    public override string ToString()
    {
      List<string> tokens = new List<string>();
      foreach (var v in values)
      {
        tokens.Add(v.ToString());
      }
      return "[" + string.Join(",", tokens) + "]";
    }
  }

  public static class Utils
  {
    public static string ToString(Size s)
    {
      return string.Format("[{0},{1}]", s.Width, s.Height);
    }
    public static string ToString(Point s)
    {
      return string.Format("[{0},{1}]", s.X, s.Y);
    }
    public static double ToGrayscale(Color c)
    {
      return ((0.3 * c.R) + (0.59 * c.G) + (0.11 * c.B)) / 256;
    }
    public static System.Drawing.Size Div(System.Drawing.Size a, System.Drawing.Size b)
    {
      return new System.Drawing.Size(a.Width / b.Width, a.Height / b.Height);
    }
    public static int Product(System.Drawing.Size a)
    {
      return a.Height * a.Width;
    }
    public static Size Sub(Point end, Point begin)
    {
      return new Size(end.X - begin.X, end.Y - begin.Y);
    }

    // returns all possible combinations of tile values.
    // this sets the ID for the resulting ValueSets which is an ordered number,
    // required for the un-mapping algo to know where things are.
    public static IEnumerable<ValueSet> Permutate(int numTiles, ValueSet discreteValuesPerTile)
    {
      // we will just do this as if each value is a digit in a number. that's the analogy that drives this.
      // actually this symbolizes the # of digits in the result, PLUS the number of possible values per digit.
      int numDigits = discreteValuesPerTile.Length;
      int theoreticalBase = numDigits;
      double dtp = Math.Pow(numDigits, numTiles);
      if (dtp > 1000000)
        throw new Exception("nope; too many permutations.");
      int totalPermutations = (int)dtp;
      List<ValueSet> ret = new List<ValueSet>();
      for (int i = 0; i < totalPermutations; ++ i)
      {
        // just like digits in a number, use % and divide to shave off "digits" one by one.
        int a = i;// the value that originates from i and we shift/mod to enumerate digits
        ValueSet n = new ValueSet(i);
        for (int d = 0; d < numTiles; ++ d)
        {
          int thisIndex = a % theoreticalBase;
          a /= theoreticalBase;
          n[d] = discreteValuesPerTile[thisIndex];
        }
        ret.Add(n);
      }
      return ret;
    }


    public static Point GetTileOrigin(Size charSize, Size numTilesPerChar, int tx, int ty)
    {
      int x = (int)Math.Round(((double)tx / numTilesPerChar.Width) * charSize.Width);
      int y = (int)Math.Round(((double)ty / numTilesPerChar.Height) * charSize.Height);
      return new Point(x, y);
    }

    // takes tile index and returns the position and size of the tile. unified function for this to avoid
    // rounding issues.
    public static void GetTileInfo(Size charSize, Size numTilesPerChar, int tx, int ty, out Point origin, out Size sz)
    {
      Point begin = GetTileOrigin(charSize, numTilesPerChar, tx, ty);
      Point end = GetTileOrigin(charSize, numTilesPerChar, tx + 1, ty + 1);
      origin = begin;
      sz = Utils.Sub(end, begin);
    }
    public static ValueSet GetDiscreteValues(int discreteValues)
    {
      // returns a list of possible values given the number of segments between 0-1.
      // we want values to CENTER in the segment. so for example 2 discrete values between 0-1
      // means we return .25 and .75.
      //double segCenter = 0.5 / discreteValues;
      //double segSpan = 1.0 / discreteValues;
      //var ret = new ValueSet(-1);
      //int i = 0;
      //for (double v = segCenter; v < 1.0; v += segSpan)
      //{
      //  ret[i] = new Value(v);
      //  ++i;
      //}
      //return ret;

      // another approach... just go 0-1 in discreteValue steps. return 0 and .5 for 2 discrete values.
      //double segSpan = 1.0 / discreteValues;
      //var ret = new ValueSet(-1);
      //int i = 0;
      //for (double v = 0; v < 1.0; v += segSpan)
      //{
      //  ret[i] = new Value(v);
      //  ++i;
      //}
      //return ret;

      // and another, returning [0, 1] for 2 discrete values. [0,.5,1] for 3, etc.
      double segSpan = 1.0 / (discreteValues - 1);
      var ret = new ValueSet(-1);
      int i = 0;
      for (double v = 0; v <= 1.0001; v += segSpan)
      {
        ret[i] = new Value(v);
        ++i;
      }
      return ret;
    }

    public static Value FindClosestValue(ValueSet possibleValues, Value v)
    {
      int indexOfNearest = -1;
      Value distanceToNearest = new Value();
      for(int i = 0; i < possibleValues.Length; ++ i)
      {
        Value d = possibleValues[i].DistanceFrom(v);
        if (indexOfNearest == -1 || d.IsLessThan(distanceToNearest))
        {
          distanceToNearest = d;
          indexOfNearest = i;
        }
      }
      Debug.Assert(indexOfNearest != -1);
      return possibleValues[indexOfNearest];
    }

    public static ValueSet FindClosestDestValueSet(int discreteValues, ValueSet src)
    {
      ValueSet possibleValues = GetDiscreteValues(discreteValues);
      ValueSet ret = new ValueSet(-1);
      for (int i = 0; i < src.Length; ++ i)
      {
        ret[i] = FindClosestValue(possibleValues, src[i]);
      }
      return ret;
    }
  }

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

    public PetsciiMap(string fontFileName, Size charSize, Size numTilesPerChar, int valuesPerTile, double _distThresh)
    {
      this.charSize = charSize;
      this.numTilesPerChar = numTilesPerChar;
      this.valuesPerTile = valuesPerTile;

      //System.Drawing.Size numTilesPerChar = new System.Drawing.Size(1, 1);
      //int valuesPerTile = 256;
      Value distThreshold = new Value(_distThresh);// .2);
      //System.Drawing.Size charSize = new System.Drawing.Size(8, 8);
      //var srcImg = System.Drawing.Image.FromFile("..\\..\\img\\testfont.png");
      //var srcImg = System.Drawing.Image.FromFile("..\\..\\img\\c64_uppercase_norm.png"); // characters.901225-01.gif
      //var srcImg = System.Drawing.Image.FromFile("..\\..\\img\\characters.901225-01.gif");
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

              ci.actualValues[tileIndex] = acc.DividedBy(count);

              tileIndex++;
            }
          }

          ci.closestDestPos = Utils.FindClosestDestValueSet(valuesPerTile, ci.actualValues);
          ci.totalDistance = ci.actualValues.DistFrom(ci.closestDestPos/*, pixelsPerTile*/);

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

      //// FIRST PASS:
      //// start inserting the best matches into the map until we hit threshold of badness.
      //// also try not to overlap items.
      //int inserted = 0;
      //int conflictedChars = 0;
      //foreach (var ci in charInfo)
      //{
      //  break;
      //  if (distThreshold.IsLessThan(ci.totalDistance))
      //    break;
      //  if (map[ci.closestDestPos] != null)
      //  {
      //    Console.WriteLine(string.Format(" => Char @ {0} wants to overwrite pos {1}", ci, ci.closestDestPos));
      //    conflictedChars++;
      //    continue;
      //  }
      //  map[ci.closestDestPos] = ci;
      //  ci.usages++;
      //  inserted++;
      //}

      //Console.WriteLine("Characters inserted, 1st pass: " + inserted);
      //Console.WriteLine("Conflicts: " + conflictedChars);


      List<ValueSet> keys = map.Keys.ToList();



      //// SECOND PASS:
      //// walk through remaining empty map entries, find the best UNUSED char which is still within some threshold.
      //int inserted2 = 0;
      //foreach (var k in keys)
      //{
      //  if (map[k] != null)
      //    continue;
      //  // find the best character match for this key within a threshold
      //  Value closestD = null;
      //  CharInfo closestChar = null;
      //  foreach (var ci in charInfo)
      //  {
      //    if (ci.usages > 0)
      //      continue;
      //    var d = ci.actualValues.DistFrom(k, pixelsPerTile);
      //    if (closestD == null || d.IsLessThan(closestD))
      //    {
      //      if (d.IsLessThan(distThreshold))
      //      {
      //        closestD = d;
      //        closestChar = ci;
      //      }
      //    }
      //  }
      //  if (closestChar != null)
      //  {
      //    map[k] = closestChar;
      //    closestChar.usages++;
      //    inserted2++;
      //  }
      //}

      //Console.WriteLine("Characters inserted 2nd pass: " + inserted2);





      // THIRD PASS:
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
          // here you could consider that more-used chars have less priority, to make the output map more varied.
          var d = ci.actualValues.DistFrom(k/*, pixelsPerTile*/);
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

      Console.WriteLine("Characters inserted 3rd pass: " + inserted3);




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
  }

  class Program
  {
    static void Main(string[] args)
    {
      PetsciiMap map = new PetsciiMap("..\\..\\img\\characters.901225-01.gif", new Size(8, 8), new Size(2, 2), 16, 1.0);
      PETSCIIIZE("..\\..\\img\\david.jpg", "..\\..\\img\\testdest-david-2x2x16.png", map);
      PETSCIIIZE("..\\..\\img\\david192.jpg", "..\\..\\img\\testdest-david192-2x2x16.png", map);

      // OK now lets test this thing.
      //PETSCIIIZE("..\\..\\img\\c64_uppercase_norm.png", "..\\..\\img\\testdest.png", map);
      //PETSCIIIZE("..\\..\\img\\airplane.jpg", "..\\..\\img\\testdest-airplane.png", map);
      //PETSCIIIZE("..\\..\\img\\circle-of-fifths--1523016231.jpg", "..\\..\\img\\testdest-circle.png", map);
    }

    public static void PETSCIIIZE(string srcImagePath, string destImagePath, PetsciiMap map)
    {
      var testImg = Image.FromFile(srcImagePath);
      Bitmap testBmp = new Bitmap(testImg);
      Bitmap destImg = new Bitmap(testBmp.Width, testBmp.Height, PixelFormat.Format32bppArgb);

      int mapCellsX = map.mapBmp.Width / map.charSize.Width;

      using (var g = Graphics.FromImage(destImg))
      {
        // roughly simulate the shader algo
        for (int srcCellY = 0; srcCellY < testImg.Height / map.charSize.Height; ++srcCellY)
        {
          for (int srcCellX = 0; srcCellX < testImg.Width / map.charSize.Width; ++srcCellX)
          {
            // sample in the cell to determine the "key" "ID".
            int ID = 0;
            for (int ty = map.numTilesPerChar.Height - 1; ty >= 0; --ty)
            {
              for (int tx = map.numTilesPerChar.Width - 1; tx >= 0; --tx)
              {
                Point tilePos = Utils.GetTileOrigin(map.charSize, map.numTilesPerChar, tx, ty);
                Color srcColor = testBmp.GetPixel((srcCellX * map.charSize.Width) + tilePos.X, (srcCellY * map.charSize.Height) + tilePos.Y);
                double val = Utils.ToGrayscale(srcColor);
                // figure out which "ID" this value corresponds to.
                // (val - segCenter) would give us the boundary. for example between 0-1 with 2 segments, the center vals are .25 and .75.
                // subtract the center and you get .0 and .5 where you could multiply by segCount and get the proper seg of 0,1.
                // however let's not subtract the center, but rather segCenter*.5. Then after integer floor rounding, it will be the correct
                // value regardless of scale or any rounding issues.
                double halfSegCenter = 0.25 / map.valuesPerTile;
                val -= halfSegCenter;
                val *= map.valuesPerTile;
                int thisTileID = (int)val;
                ID *= map.valuesPerTile;
                ID += thisTileID;
              }
            }

            // ID is now calculated.
            int mapCellY = ID / mapCellsX;
            int mapCellX = ID - (mapCellY * mapCellsX);

            // blit from map img.
            Rectangle srcRect = new Rectangle(mapCellX * map.charSize.Width, mapCellY * map.charSize.Height, map.charSize.Width, map.charSize.Height);
            g.DrawImage(map.mapBmp, srcCellX * map.charSize.Width, srcCellY * map.charSize.Height, srcRect, GraphicsUnit.Pixel);
          }
        }
      }

      destImg.Save(destImagePath);
    }
  }
}
