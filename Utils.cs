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
  public struct ColorF
  {
    public double R;
    public double G;
    public double B;

    public override string ToString()
    {
      return string.Format("[{0},{1},{2}]", R.ToString("0.00"), G.ToString("0.00"), B.ToString("0.00"));
    }

    //public static ColorF Init { R: 0 };
  }
  public static class ColorFUtils
  {
    public static ColorF Add(this ColorF c, ColorF other)
    {
      c.R += other.R;
      c.G += other.G;
      c.B += other.B;
      return c;
    }
    public static ColorF Add(this ColorF c, double other)
    {
      c.R += other;
      c.G += other;
      c.B += other;
      return c;
    }
    public static ColorF Div(this ColorF c, int other)
    {
      c.R /= other;
      c.G /= other;
      c.B /= other;
      return c;
    }
    public static bool IsBlackOrWhite(this ColorF c)
    {
      if (c.R > 2 && c.R < 253)
        return false;
      if (c.G > 2 && c.G < 253)
        return false;
      if (c.B > 2 && c.B < 253)
        return false;
      return true;
    }
    public static ColorF Clamp(this ColorF c)
    {
      c.R = Utils.Clamp(c.R, 0, 255);
      c.G = Utils.Clamp(c.G, 0, 255);
      c.B = Utils.Clamp(c.B, 0, 255);
      return c;
    }
    public static ColorF From(Color c)
    {
      ColorF ret;
      ret.R = c.R;
      ret.G = c.G;
      ret.B = c.B;
      return ret;
    }
    public static ColorF FromRGB(double r, double g, double b)
    {
      ColorF ret;
      ret.R = r;
      ret.G = g;
      ret.B = b;
      return ret;
    }
    public static ColorF Init
    {
      get
      {
        ColorF ret;
        ret.R = 0;
        ret.G = 0;
        ret.B = 0;
        return ret;
      }
    }
  }
  public class Constants
  {
    // values are 0-1 so distances are on avg even less. (sqrt(2)/2).
    // actual pixel values are 0-255 so i can just multiply by 255/(sqrt(2)/2) to make "real" sameness be equal here.
    // funny that's actually 360. no relation to angles/radians.
    //public static ulong DistanceRange { get { return 1000; } }

    public const long AllocGranularity = 30000000;
    //public const long AllocGranularityPartitions = 1000;
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
    internal long PruneWhereDistGT(double maxMinDist)
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
    public int srcIndex;// index from the font provider.
    public ValueSet actualValues;// N-dimension values
    public int usages = 0;
    public int refFontIndex;// index in the ref font texture
                            //public UInt32 mapKeysVisited = 0;
                            //public int? ifg;// only for mono palette processing, index to palette
                            //public int? ibg;// only for mono palette processing
                            //public int masterIdx;// index into the charInfo list

#if DEBUG
    public Point fontImagePixelPos;
    public Point fontImageCellPos;
#endif

    public CharInfo(int dimensionsPerCharacter)
    {
      actualValues = ValueSet.New(dimensionsPerCharacter, 9999);
    }

    public override string ToString()
    {
#if DEBUG
      return string.Format("ID:{0} src:{1}, usages:{2}, pixelpos:{3}, cellpos:{4}",
        srcIndex, actualValues,// masterIdx,
        usages, fontImagePixelPos, fontImageCellPos);
#else
      return string.Format("ID:{0} src:{1}, usages:{2}",
        srcIndex, actualValues,// masterIdx,
        usages);
#endif
      //return srcIndex.ToString();
    }
  }

  public struct Mapping
  {
    public int imapKey; // a set of tile values
    public int icharInfo;
    public double dist;
  }

  public class ProgressReporter
  {
    long total;
    Stopwatch swseg;
    Stopwatch swtotal;
    public ProgressReporter(long total)
    {
      this.total = total;
      this.swtotal = new Stopwatch();
      swtotal.Start();
      this.swseg = new Stopwatch();
      swseg.Start();
    }
    public void Visit(long item)
    {
      if (swseg.ElapsedMilliseconds < 5000)
        return;
      swseg.Restart();
      double p = (double)item / total;
      double elapsedSec = (double)swtotal.ElapsedMilliseconds / 1000.0;
      double totalEst = elapsedSec / p;
      //double estRemaining = (double)swtotal.ElapsedMilliseconds / (1.0-p);
      double estRemaining = totalEst - elapsedSec;
      Console.WriteLine("  Progress: {0}% (est remaining: {1} sec)", (p*100).ToString("0.00"), estRemaining.ToString("0.00"));
    }
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

  public static class Utils
  {
    public static unsafe byte* GetRGBPointer(this BitmapData data, long x, long y)
    {
      byte* ret = (byte*)data.Scan0;
      ret += y * data.Stride;
      ret += x * 3;
      return ret;
    }

    // assumes 24 bits per pixel RGB
    public static unsafe ColorF GetPixel(this BitmapData data, long x, long y)
    {
      byte* p = data.GetRGBPointer(x, y);
      return ColorFUtils.From(Color.FromArgb(p[2], p[1], p[0]));
    }

    // assumes 24 bits per pixel RGB
    public static unsafe void SetPixel(this BitmapData data, long x, long y, ColorF c)
    {
      byte* p = data.GetRGBPointer(x, y);
      p[2] = (byte)c.R;
      p[1] = (byte)c.G;
      p[0] = (byte)c.B;
    }
    public static unsafe void SetPixel(this BitmapData data, long x, long y, Color c)
    {
      byte* p = data.GetRGBPointer(x, y);
      p[2] = c.R;
      p[1] = c.G;
      p[0] = c.B;
    }

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
      public double MinValue { get; private set; } = default(double);
      public double MaxValue { get; private set; } = default(double);

      bool encountered = false;

      public void Visit(double v)
      {
        if (!encountered)
        {
          MinValue = MaxValue = v;
          encountered = true;
          return;
        }
        MinValue = Utils.Min<double>(v, MinValue);
        MaxValue = Utils.Max<double>(v, MaxValue);
      }

      // normalize a value based on min/max values seen, returning 0-1.
      public double Normalize01(double v)
      {
        if (MinValue == MaxValue)
          return 1;
        return (v - MinValue) / (MaxValue - MinValue);
      }

      public override string ToString()
      {
        return string.Format("[{0}, {1}]", MinValue, MaxValue);
      }
    }

    public static float Clamp(float v, float m, float x)
    {
      if (v < m) return m;
      if (v > x) return x;
      return v;
    }
    public static double Clamp(double v, double m, double x)
    {
      if (v < m) return m;
      if (v > x) return x;
      return v;
    }
    public static int Clamp(int v, int m, int x)
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

    public static string ToString(this float[] vals, int count)
    {
      List<string> items = new List<string>();
      for (int i = 0; i < count; ++i)
      {
        items.Add(string.Format("{0,6:0.00}", vals[i]));
      }
      return string.Format("[{0}]", string.Join(",", items));
    }

    public static System.Drawing.Size Div(System.Drawing.Size a, System.Drawing.Size b)
    {
      return new System.Drawing.Size(a.Width / b.Width, a.Height / b.Height);
    }
    public static int Product(System.Drawing.Size a)
    {
      return a.Height * a.Width;
    }
    public static Point Mul(Point a, Size b)
    {
      return new Point(a.X * b.Width, a.Y * b.Height);
    }
    public static Size Mul(Size a, int b)
    {
      return new Size(a.Width * b, a.Height * b);
    }
    public static Point Add(Point a, int o)
    {
      return new Point(a.X + o, a.Y + o);
    }
    public static Point Add(Point a, Point o)
    {
      return new Point(a.X + o.X, a.Y + o.Y);
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
    public unsafe static ValueSet[] Permutate(int numDimensions, float[] discreteNormalizedValuesPerTile)
    {
      // we will just do this as if each value is a digit in a number. that's the analogy that drives this.
      // actually this symbolizes the # of digits in the result, PLUS the number of possible values per digit.
      long numDigits = discreteNormalizedValuesPerTile.Length;
      long theoreticalBase = numDigits;
      long totalPermutations = Pow(numDigits, (uint)numDimensions);
      float[] normalizedValues = new float[numDimensions];

      ValueSet[] ret = new ValueSet[totalPermutations];
      for (long i = 0; i < totalPermutations; ++i)
      {
        // just like digits in a number, use % and divide to shave off "digits" one by one.
        long a = i;// the value that originates from i and we shift/mod to enumerate digits
        //ValueSet n = NewValueSet(numTiles, i);
        for (int d = 0; d < numDimensions; ++d)
        {
          long thisIndex = a % theoreticalBase;
          a /= theoreticalBase;
          normalizedValues[d] = discreteNormalizedValuesPerTile[(int)thisIndex];
          //ret[i].Values[d] = discreteValuesPerTile[(int)thisIndex];
        }
        ValueSet.Init(ref ret[i], numDimensions, i, normalizedValues);
        //ret.Add(n);
      }
      return ret;
    }

    public unsafe static float[] GetDiscreteNormalizedValues(int discreteValues)
    {
      // returning [0, 1] for 2 discrete values. [0,.5,1] for 3, etc.
      // this is preferred because you want BLACK to match to BLACK.
      // if we return the centers of partitions (.25, .75), then black and white
      // will get mapped very loosely.
      float segSpan = 1.0f / (discreteValues - 1);
      float[] ret = new float[discreteValues];
      //int i = 0;
      //for (float v = 0; v <= 1.0f; v += segSpan)
      for (int i = 0; i < discreteValues; ++ i)
      {
        ret[i] = i * 100000 / (discreteValues - 1); // using fixed precision here.
        ret[i] /= 100000;
      }
      return ret;

      // return centers of partitions.
      //float[] ret = new float[discreteValues];
      //float segSpan = 1.0f / discreteValues;
      //int i = 0;
      //for (float v = 0; v < 1.0f; v += segSpan)
      //{
      //  ret[i] = v + segSpan / 2;
      //  ++i;
      //}
      //return ret;
    }

    //internal unsafe static void AssertSortedByDimension(ValueSet[] keys, int lastDimensionIndex)
    //{
    //  float lastVal = 0;
    //  for (int i = 0; i < keys.Length; ++i)
    //  {
    //    float lastDimensionValue = keys[i].Values[lastDimensionIndex];
    //    Debug.Assert(lastDimensionValue >= lastVal);
    //    lastVal = lastDimensionValue;
    //  }
    //}

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

