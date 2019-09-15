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
    public static UInt32 DistanceRange { get { return 1000; } }

    public const long AllocGranularity = 30000000;
    public const long AllocGranularityPartitions = 1000;
  }

  public class MappingArray
  {
    public Mapping[] Values = new Mapping[Constants.AllocGranularity]; // RESERVED values therefore don't use Values.Length!
    public long Length { get; private set; } = 0;
    public long Add() // returns an index
    {
      if (Values.Length <= Length)
      {
        Mapping[] t = new Mapping[Length + Constants.AllocGranularity];
        Console.WriteLine("!!! Dynamic allocation");
        Array.Copy(this.Values, t, this.Length);
        this.Values = t;
      }
      Length++;
      return Length - 1;
    }

    internal long PruneWhereDistGT(uint maxMinDist)
    {
      var prunedMappings = Values.Take((int)this.Length).Where(o => o.dist <= maxMinDist).ToArray();
      long ret = this.Length - prunedMappings.Length;
      this.Length = prunedMappings.LongLength;
      this.Values = prunedMappings;
      return ret;
    }
    public void SortByDist()
    {
      Array.Sort<Mapping>(this.Values, (a, b) => a.dist.CompareTo(b.dist));
    }
  }

  public class CharInfo
  {
    public System.Drawing.Point srcIndex;
    public ValueSet actualValues;// N-dimension values
    public int usages = 0;
    public UInt32 versatility;
    public UInt32 mapKeysVisited = 0;
    public long partition; // which spatial partition does this character fit into?

    public CharInfo(int dimensionsPerCharacter)
    {
      actualValues = ValueSet.New(dimensionsPerCharacter, 9999);
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
  public unsafe struct ValueSet
  {
    public int ValuesLength;
    public long ID;
    public bool Mapped;
    public uint MinDistFound;
    public bool Visited;

    public fixed float Values[11];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueSet New(int dimensionsPerCharacter, long id)
    {
      ValueSet ret = new ValueSet();
      Init(ref ret, dimensionsPerCharacter, id);
      return ret;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Init(ref ValueSet n, int dimensionsPerCharacter, long id)
    {
      //n.Values = new float[dimensionsPerCharacter];
      n.ValuesLength = dimensionsPerCharacter;
      n.ID = id;
      n.MinDistFound = UInt32.MaxValue;
    }

    // returns squared dist
    public unsafe static float DistFrom(ValueSet a, ValueSet b, ValueSet weights, int hueIndex)
    {
      //Debug.Assert((SdimIdx < HdimIdx) || (SdimIdx == -1 && HdimIdx == -1));
      //Debug.Assert(a.ValuesLength == b.ValuesLength);
      //Debug.Assert(a.ValuesLength == weights.ValuesLength);
      float acc = 0;
      for (int i = 0; i < a.ValuesLength; ++i)
      {
        float m = 0;
        if (hueIndex == i)
        {
          // hue is circular, 0-1 where 1 == 0. at least we know the values are in range 0-1 so we can take advantage of that.
          m = Utils.HueDifference(a.Values[i], b.Values[i]);
        }
        else
        {
          m = Math.Abs(a.Values[i] - b.Values[i]);
        }
        m = m * m * weights.Values[i];
        acc += m;
      }
      return acc;
    }

    public unsafe static int CompareTo(ValueSet a, ValueSet other)
    {
      int d = other.ValuesLength.CompareTo(a.ValuesLength);
      if (d != 0)
        return d;
      for (int i = 0; i < a.ValuesLength; ++i)
      {
        d = other.Values[i].CompareTo(a.Values[i]);
        if (d != 0)
          return d;
      }
      return 0;
    }

    public unsafe static string ToString(ValueSet o)
    {
      List<string> items = new List<string>();
      for (int i = 0; i < o.ValuesLength; ++i) {
        items.Add(o.Values[i].ToString());
      }
      return string.Format("[{0}]", string.Join(",", items));
    }

  }

  public static class Utils
  {
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>
         (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
      HashSet<TKey> knownKeys = new HashSet<TKey>();
      foreach (TSource element in source)
      {
        if (knownKeys.Add(keySelector(element)))
        {
          yield return element;
        }
      }
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
        if (MinValue == MaxValue)
          return 1.0f;
        return (v - MinValue) / (MaxValue - MinValue);
      }

      public override string ToString()
      {
        return string.Format("[{0}, {1}]", MinValue, MaxValue);
      }
    }

    // adapted from https://www.programmingalgorithms.com/algorithm/rgb-to-ycbcr
    //public static void RGBtoYCbCr(float sat, float fr, float fg, float fb, out float y, out float u, out float v)
    //{
    //  float Y = (0.2989f * fr + 0.5866f * fg + 0.1145f * fb);
    //  float Cb = (-0.1687f * fr - 0.3313f * fg + 0.5000f * fb);
    //  float Cr = (0.5000f * fr - 0.4184f * fg - 0.0816f * fb);
    //  y = Y;
    //  Cb *= sat;
    //  Cr *= sat;
    //  u = Cb + .5f;
    //  v = Cr + .5f;
    //}
    //public static void RGBtoYCbCr_Naive(float red, float green, float blue, out float y, out float u, out float v)
    //{
    //  y = (red + green + blue) / 3;
    //  u = (red - y) / 2 + .5f;
    //  v = (blue - y) / 2 + .5f;
    //  y = Utils.Clamp(y, 0, 1);
    //  u = Utils.Clamp(u, 0, 1);
    //  v = Utils.Clamp(v, 0, 1);
    //}

    public static float HueDifference(float hue1, float hue2)
    {
      return Math.Min(Math.Abs(hue1 - hue2), 1 - Math.Abs(hue1 - hue2));
    }

    public static void RGBtoYUV_Mapping(Color rgb, out float y, out float u, out float v)
    {
      RGBtoYUV(rgb, out y, out u, out v);
      ////y = (float)rgb.R / 255.0f;
      ////u = (float)rgb.G / 255.0f;
      ////v = (float)rgb.B / 255.0f;

      //y = rgb.GetBrightness();
      //u = rgb.GetSaturation();
      //v = rgb.GetHue() / 360.0f;

      //// the less saturated a color is, the closer i want hues to match.
      //v *= u;
    }



    public static void RGBtoYCbCr(float red, float green, float blue, out float y, out float u, out float v)
    {
      // http://www.fourcc.org/fccyvrgb.php#mikes_answer
      //Ey = 0.299R + 0.587G + 0.114B
      //Ecr = 0.713(R - Ey) = 0.500R - 0.419G - 0.081B
      //Ecb = 0.564(B - Ey) = -0.169R - 0.331G + 0.500B
      y = ((0.299f * red) + (0.587f * green) + (0.114f * blue));
      u = 0.713f * (red - y);// = 0.500R - 0.419G - 0.081B
      v = 0.564f * (blue - y);// = 0.500R - 0.419G - 0.081B
      u += .5f;
      v += .5f;
    }
    public static void RGBtoYUV(Color rgb, out float y, out float u, out float v)
    {
      //y = rgb.GetBrightness();
      //u = rgb.GetSaturation();
      //v = rgb.GetHue() / 360.0f;

      RGBtoYCbCr(
        (float)rgb.R / 255.0f,
        (float)rgb.G / 255.0f,
        (float)rgb.B / 255.0f,
        out y,
        out u,
        out v
        );
    }


    //public static void RGBtoHSL(float _R, float _G, float _B, out float H, out float S, out float L)
    //{
    //  float _Min = Math.Min(Math.Min(_R, _G), _B);
    //  float _Max = Math.Max(Math.Max(_R, _G), _B);
    //  float _Delta = _Max - _Min;

    //  H = 0;
    //  S = 0;
    //  L = (float)((_Max + _Min) / 2.0f);

    //  if (_Delta != 0)
    //  {
    //    if (L < 0.5f)
    //    {
    //      S = (float)(_Delta / (_Max + _Min));
    //    }
    //    else
    //    {
    //      S = (float)(_Delta / (2.0f - _Max - _Min));
    //    }

    //    if (_R == _Max)
    //    {
    //      H = (_G - _B) / _Delta;
    //    }
    //    else if (_G == _Max)
    //    {
    //      H = 2f + (_B - _R) / _Delta;
    //    }
    //    else if (_B == _Max)
    //    {
    //      H = 4f + (_R - _G) / _Delta;
    //    }
    //  }
    //}

    //// Convert an RGB value into an HLS value.
    //public static void RGBtoHSL(int r, int g, int b,
    //    out float h, out float s, out float l)
    //{
    //  // Convert RGB to a 0.0 to 1.0 range.
    //  float double_r = r / 255.0f;
    //  float double_g = g / 255.0f;
    //  float double_b = b / 255.0f;

    //  // Get the maximum and minimum RGB components.
    //  float max = double_r;
    //  if (max < double_g) max = double_g;
    //  if (max < double_b) max = double_b;

    //  float min = double_r;
    //  if (min > double_g) min = double_g;
    //  if (min > double_b) min = double_b;

    //  float diff = max - min;
    //  l = (max + min) / 2;
    //  if (Math.Abs(diff) < 0.00001)
    //  {
    //    s = 0;
    //    h = 0;  // H is really undefined.
    //  }
    //  else
    //  {
    //    if (l <= 0.5) s = diff / (max + min);
    //    else s = diff / (2 - max - min);

    //    float r_dist = (max - double_r) / diff;
    //    float g_dist = (max - double_g) / diff;
    //    float b_dist = (max - double_b) / diff;

    //    if (double_r == max) h = b_dist - g_dist;
    //    else if (double_g == max) h = 2 + r_dist - b_dist;
    //    else h = 4 + g_dist - r_dist;

    //    h = h * 60;
    //    if (h < 0) h += 360;
    //  }
    //  h /= 360;
    //}
    //public static void RGBtoHSL(Color rgb, out float h, out float s, out float l)
    //{
    //  RGBtoHSL(
    //    rgb.R,
    //    rgb.G,
    //    rgb.B,
    //    out h,
    //    out s,
    //    out l
    //    );
    //}


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
    public unsafe static ValueSet[] Permutate(int numTiles, float[] discreteValuesPerTile)
    {
      // we will just do this as if each value is a digit in a number. that's the analogy that drives this.
      // actually this symbolizes the # of digits in the result, PLUS the number of possible values per digit.
      long numDigits = discreteValuesPerTile.Length;
      long theoreticalBase = numDigits;
      long totalPermutations = Pow(numDigits, (uint)numTiles);

      ValueSet[] ret = new ValueSet[totalPermutations];
      for (long i = 0; i < totalPermutations; ++i)
      {
        // just like digits in a number, use % and divide to shave off "digits" one by one.
        long a = i;// the value that originates from i and we shift/mod to enumerate digits
        ValueSet.Init(ref ret[i], numTiles, i);
        //ValueSet n = NewValueSet(numTiles, i);
        for (int d = 0; d < numTiles; ++d)
        {
          long thisIndex = a % theoreticalBase;
          a /= theoreticalBase;
          ret[i].Values[d] = discreteValuesPerTile[(int)thisIndex];
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
    public unsafe static float[] GetDiscreteValues(int discreteValues)
    {
      // returning [0, 1] for 2 discrete values. [0,.5,1] for 3, etc.
      float segSpan = 1.0f / (discreteValues - 1);
      //var ret = ValueSet.New(discreteValues, 9998);
      float[] ret = new float[discreteValues];
      int i = 0;
      for (float v = 0; v <= 1.0001f; v += segSpan)
      {
        ret[i] = v;
        ++i;
      }
      return ret;
    }

    internal unsafe static void AssertSortedByDimension(ValueSet[] keys, int lastDimensionIndex)
    {
      float lastVal = 0;
      for (int i = 0; i < keys.Length; ++i)
      {
        float lastDimensionValue = keys[i].Values[lastDimensionIndex];
        Debug.Assert(lastDimensionValue >= lastVal);
        lastVal = lastDimensionValue;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long Pow(long x, uint pow)
    {
      long ret = 1;
      while (pow != 0)
      {
        if ((pow & 1) == 1)
          ret *= x;
        x *= x;
        pow >>= 1;
      }
      return ret;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float Mix(float v0, float v1, float t)
    {
      return (1 - t) * v0 + t * v1;
    }
  }
}

