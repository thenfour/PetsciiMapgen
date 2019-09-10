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
using System.Runtime.InteropServices;

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
    public int ID { get { return _id; } }

    public Value this[int i]
    {
      get
      {
        return values[i];
      }
      set
      {
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
    public Value DistFrom(ValueSet b, int numPixels)
    {
      Debug.Assert(this.Length == b.Length);
      Value acc = new Value();
      for (int i = 0; i < this.Length; ++i)
      {
        acc.Accumulate(this[i].DistanceFrom(b[i])); // also possible: use accMax
      }
      //return acc;
      return acc.DividedBy(numPixels);
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
    public struct RGBColorF
    {
      public float r;
      public float g;
      public float b;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct RGBColor
    {
      [FieldOffset(0)] public byte r;
      [FieldOffset(1)] public byte g;
      [FieldOffset(2)] public byte b;
    }
    public static float ColorantFromByte(byte b)
    {
      return ((float)b) / 255.0f;
    }
    public static byte ByteFromColorant(float c)
    {
      int i = (int)(c * 255.0f);
      if (i < 0) i = 0;
      if (i > 255) i = 255;
      return (byte)i;
    }
    public static void TransformPixels(Bitmap srcA, Func<RGBColorF, RGBColorF> trans)
    {
      Rectangle roi = new Rectangle(0, 0, srcA.Width, srcA.Height);
      BitmapData dataA = srcA.LockBits(roi, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

      unsafe
      {
        byte* ptrA = (byte*)dataA.Scan0;

        for (int y = 0; y < dataA.Height; ++y)
        {
          for (int x = 0; x < dataA.Width * 3; x += 3)
          {
            RGBColorF c = new RGBColorF
            {
              r = ColorantFromByte(ptrA[x]),
              g = ColorantFromByte(ptrA[x+1]),
              b = ColorantFromByte(ptrA[x+2])
            };

            c = trans(c);

            ptrA[x] = ByteFromColorant(c.r);
            ptrA[x+1] = ByteFromColorant(c.g);
            ptrA[x+2] = ByteFromColorant(c.b);
          }
          ptrA += dataA.Stride;
        }
      }
      srcA.UnlockBits(dataA);
    }

    public static void Pixellate(Bitmap src, Size cellSize)
    {
      Rectangle roi = new Rectangle(0, 0, src.Width, src.Height);
      BitmapData data = src.LockBits(roi, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

      unsafe
      {
        for (int y = 0; y < data.Height; y += cellSize.Height)
        {
          for (int x = 0; x < data.Width; x += cellSize.Width)
          {
            byte* pCellScanLine = (byte*)data.Scan0;
            pCellScanLine += y * data.Stride;
            RGBColor* pCell = (RGBColor*)pCellScanLine;
            pCell += x;

            // color this tile using the upper-left pixel.
            for (int py = 0; py < cellSize.Height; ++ py)
            {
              if ((y + py) >= data.Height)
                break;
              for (int px = 0; px < cellSize.Width; ++ px)
              {
                byte* pScanLine = pCellScanLine + (data.Stride * py);
                RGBColor* pPix = (RGBColor*)pScanLine;
                pPix += px + x;
                *pPix = *pCell;
              }
            }

          }
        }
      }
      src.UnlockBits(data);
    }

    public static void Multiply(Bitmap srcA/* dest */, Bitmap srcB)
    {
      if (srcA.Width != srcB.Width)
        throw new Exception("images must be the same size");
      if (srcA.Height != srcB.Height)
        throw new Exception("images must be the same size");

      Rectangle roi = new Rectangle(0, 0, srcA.Width, srcA.Height);
      BitmapData dataA = srcA.LockBits(roi, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
      BitmapData dataB = srcB.LockBits(roi, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

      unsafe
      {
        byte* ptrA = (byte*)dataA.Scan0;
        byte* ptrB = (byte*)dataB.Scan0;

        for (int y = 0; y < dataA.Height; ++y)
        {
          for (int x = 0; x < dataA.Width * 3; ++x)
          {
            float fa = ptrA[x];
            fa /= 255;
            float fb = ptrB[x];
            fb /= 255;

            fa *= fb;
            //fa = fb;

            if (fa < 0.0) fa = 0.0f;
            if (fa > 1.0) fa = 1.0f;
            ptrA[x] = (byte)(fa * 255.0f);
          }
          ptrA += dataA.Stride;
          ptrB += dataB.Stride;
        }
      }
      srcA.UnlockBits(dataA);
      srcB.UnlockBits(dataB);
    }


    //public static void Pixellate(Image src, Size cellSize)
    //{
    //  // create a new image
    //  var bigRect = new Rectangle(0, 0, src.Width, src.Height);
    //  var smallRect = new Rectangle(0, 0, src.Width / cellSize.Width, src.Height / cellSize.Height);
    //  var small = new Bitmap(smallRect.Width, smallRect.Height);
    //  using (var g = Graphics.FromImage(small))
    //  {
    //    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
    //    g.DrawImage(src,
    //      smallRect, 0, 0, bigRect.Width, bigRect.Height, GraphicsUnit.Pixel
    //      );
    //  }
    //  using (var g = Graphics.FromImage(src))
    //  {
    //    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
    //    g.DrawImage(small,
    //      bigRect, 0, 0, smallRect.Width, smallRect.Height, GraphicsUnit.Pixel
    //      );
    //  }
    //}

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
      for (int i = 0; i < totalPermutations; ++i)
      {
        // just like digits in a number, use % and divide to shave off "digits" one by one.
        int a = i;// the value that originates from i and we shift/mod to enumerate digits
        ValueSet n = new ValueSet(i);
        for (int d = 0; d < numTiles; ++d)
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
      for (int i = 0; i < possibleValues.Length; ++i)
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
      for (int i = 0; i < src.Length; ++i)
      {
        ret[i] = FindClosestValue(possibleValues, src[i]);
      }
      return ret;
    }

    internal static double AdjustContrast(double val, double factor, double centerPoint = 0.5)
    {
      val -= centerPoint;
      val *= factor;
      val += centerPoint;
      return val;
    }
  }
}

