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
  public class Constants
  {
    public static UInt32 CharVersatilityRange { get { return 360; } }

    // values are 0-1 so distances are on avg even less. (sqrt(2)/2).
    // actual pixel values are 0-255 so i can just multiply by 255/(sqrt(2)/2) to make "real" sameness be equal here.
    // funny that's actually 360. no relation to angles/radians.
    public static UInt32 DistanceRange { get { return 360; } }

    public static float MaxDimensionDist {  get { return .7f; } }
  }

  public class CharInfo
  {
    //public System.Drawing.Size size;
    //public System.Drawing.Point srcOrigin;
    public System.Drawing.Point srcIndex;
    public ValueSet actualValues;// N-dimension values
    public int usages = 0;
    public UInt32 versatility; // sum of distances to all map keys. lower values = more versatile
    public UInt32 mapKeysVisited = 0;

    public CharInfo(int dimensionsPerCharacter)
    {
      actualValues = Utils.NewValueSet(dimensionsPerCharacter, 9999);
    }

    public override string ToString()
    {
      return srcIndex.ToString();
    }
  }

  public struct Mapping
  {
    public UInt32 imapKey; // a set of tile values
    public UInt32 icharInfo;
    public UInt32 dist;
  }

  public class Timings
  {
    public struct Task
    {
      public Stopwatch sw;
      public string name;
    }
    Stack<Task> tasks = new Stack<Task>();
    public void EnterTask(string s)
    {
      Console.WriteLine("==> Enter task {0}", s);
      Task n;
      if (!tasks.Any())
      {
        n = new Task {
          name = "root",
          sw = new Stopwatch()
        };
        n.sw.Start();
        tasks.Push(n);
      }
      n = new Task
      {
        name = s,
        sw = new Stopwatch()
      };
      n.sw.Start();
      tasks.Push(n);
    }
    public void EndTask()
    {
      Task n = this.tasks.Pop();
      TimeSpan ts = n.sw.Elapsed;
      Console.WriteLine("<== {1} (end {0})", n.name, ts);
    }
  }

  // basically wraps List<Value>.
  // simplifies code that wants to do set operations.
  public struct ValueSet
  {
    public float[] Values;
    public UInt64 ID;

    // optimizations
    public bool Mapped;
    public uint MinDistFound;
  }

  public static class Utils
  {
    // returns squared dist
    public static float DistFrom(ValueSet a, ValueSet b, ValueSet weights)
    {
      Debug.Assert(a.Values.Length == b.Values.Length);
      Debug.Assert(a.Values.Length == weights.Values.Length);
      float acc = 0;
      for (int i = 0; i < a.Values.Length; ++i)
      {
        var m = Math.Abs(a.Values[i] - b.Values[i]);
        acc += m * m * weights.Values[i];
      }
      return acc;
    }

    public static int CompareTo(ValueSet a, ValueSet other)
    {
      int d = other.Values.Length.CompareTo(a.Values.Length);
      if (d != 0)
        return d;
      for (int i = 0; i < a.Values.Length; ++i)
      {
        d = other.Values[i].CompareTo(a.Values[i]);
        if (d != 0)
          return d;
      }
      return 0;
    }



    public class ValueRangeInspector
    {
      public float MinValue { get; private set; } = default(float);
      public float MaxValue { get; private set; } = default(float);

      bool encountered = false;

      public void Visit(float v)
      {
        if (!encountered)
        {
          MinValue = MaxValue = v;
          encountered = true;
          return;
        }
        MinValue = Utils.Min<float>(v, MinValue);
        MaxValue = Utils.Max<float>(v, MaxValue);
      }

      // normalize a value based on min/max values seen, returning 0-1.
      public float Normalize01(float v)
      {
        return (v - MinValue) / (MaxValue - MinValue);
      }

      public override string ToString()
      {
        return string.Format("[{0}, {1}]", MinValue, MaxValue);
      }
    }
    
    // https://stackoverflow.com/questions/359612/how-to-change-rgb-color-to-hsv
    public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
    {
      int max = Math.Max(color.R, Math.Max(color.G, color.B));
      int min = Math.Min(color.R, Math.Min(color.G, color.B));

      hue = color.GetHue();
      saturation = (max == 0) ? 0 : 1d - (1d * min / max);
      value = max / 255d;
    }

    public static Color ColorFromHSV(double hue, double saturation, double value)
    {
      int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
      double f = hue / 60 - Math.Floor(hue / 60);

      value = value * 255;
      int v = Convert.ToInt32(value);
      int p = Convert.ToInt32(value * (1 - saturation));
      int q = Convert.ToInt32(value * (1 - f * saturation));
      int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

      if (hi == 0)
        return Color.FromArgb(255, v, t, p);
      else if (hi == 1)
        return Color.FromArgb(255, q, v, p);
      else if (hi == 2)
        return Color.FromArgb(255, p, v, t);
      else if (hi == 3)
        return Color.FromArgb(255, p, q, v);
      else if (hi == 4)
        return Color.FromArgb(255, t, p, v);
      else
        return Color.FromArgb(255, v, p, q);
    }

    // adapted from https://www.programmingalgorithms.com/algorithm/rgb-to-ycbcr
    public static void RGBtoYCbCr(float fr, float fg, float fb, out float y, out float u, out float v)
    {
      float Y = (0.2989f * fr + 0.5866f * fg + 0.1145f * fb);
      float Cb = (-0.1687f * fr - 0.3313f * fg + 0.5000f * fb);
      float Cr = (0.5000f * fr - 0.4184f * fg - 0.0816f * fb);
      y = Y;
      u = Cb + .5f;
      v = Cr + .5f;
    }

    public static void RGBtoYUV(Color rgb, out float y, out float u, out float v)
    {
      RGBtoYCbCr(
        (float)rgb.R / 255.0f,
        (float)rgb.G / 255.0f,
        (float)rgb.B / 255.0f,
        out y,
        out u,
        out v
        );
    }

    public struct RGBColorF
    {
      public double r;
      public double g;
      public double b;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct RGBColor
    {
      [FieldOffset(0)] public byte r;
      [FieldOffset(1)] public byte g;
      [FieldOffset(2)] public byte b;
    }
    public static double ColorantFromByte(byte b)
    {
      return ((double)b) / 255.0;
    }
    public static byte ByteFromColorant(double c)
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

    public static float Clamp(float v, float m, float x)
    {
      if (v < m) return m;
      if (v > x) return x;
      return v;
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
            double fa = ptrA[x];
            fa /= 255;
            double fb = ptrB[x];
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

    public static T Max<T>(T x, T y)
    {
      return (Comparer<T>.Default.Compare(x, y) > 0) ? x : y;
    }
    public static T Min<T>(T x, T y)
    {
      return (Comparer<T>.Default.Compare(x, y) < 0) ? x : y;
    }

    // returns all possible combinations of tile values.
    // this sets the ID for the resulting ValueSets which is an ordered number,
    // required for the un-mapping algo to know where things are.
    public static ValueSet[] Permutate(int numTiles, ValueSet discreteValuesPerTile)
    {
      // we will just do this as if each value is a digit in a number. that's the analogy that drives this.
      // actually this symbolizes the # of digits in the result, PLUS the number of possible values per digit.
      UInt64 numDigits = (UInt64)discreteValuesPerTile.Values.Length;
      UInt64 theoreticalBase = numDigits;
      UInt64 totalPermutations = (UInt64)Math.Pow(numDigits, numTiles);

      ValueSet[] ret = new ValueSet[totalPermutations];
      for (UInt64 i = 0; i < totalPermutations; ++i)
      {
        // just like digits in a number, use % and divide to shave off "digits" one by one.
        UInt64 a = i;// the value that originates from i and we shift/mod to enumerate digits
        InitValueSet(ref ret[i], numTiles, i);
        //ValueSet n = NewValueSet(numTiles, i);
        for (int d = 0; d < numTiles; ++d)
        {
          UInt64 thisIndex = a % theoreticalBase;
          a /= theoreticalBase;
          ret[i].Values[d] = discreteValuesPerTile.Values[(int)thisIndex];
        }
        //ret.Add(n);
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
      // returning [0, 1] for 2 discrete values. [0,.5,1] for 3, etc.
      float segSpan = 1.0f / (discreteValues - 1);
      var ret = NewValueSet(discreteValues, 9998);
      int i = 0;
      for (float v = 0; v <= 1.0001f; v += segSpan)
      {
        ret.Values[i] = v;
        ++i;
      }
      return ret;
    }

    internal static void AssertSortedByDimension(ValueSet[] keys, int lastDimensionIndex)
    {
      float lastVal = 0;
      for (int i = 0; i < keys.Length; ++i)
      {
        float lastDimensionValue = keys[i].Values[lastDimensionIndex];
        Debug.Assert(lastDimensionValue >= lastVal);
        lastVal = lastDimensionValue;
      }
    }

    internal static ValueSet NewValueSet(int dimensionsPerCharacter, UInt64 id)
    {
      ValueSet ret = new ValueSet();
      InitValueSet(ref ret, dimensionsPerCharacter, id);
      return ret;
    }
    internal static void InitValueSet(ref ValueSet n, int dimensionsPerCharacter, UInt64 id)
    {
      n.Values = new float[dimensionsPerCharacter];
      n.ID = id;
      n.MinDistFound = UInt32.MaxValue;
    }
  }
}

