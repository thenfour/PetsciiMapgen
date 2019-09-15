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
    public int? ifg;// only for mono palette processing, index to palette
    public int? ibg;// only for mono palette processing
    public int index;// used when generating font texture

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
    public static unsafe byte* GetRGBPointer(this BitmapData data, long x, long y)
    {
      byte* ret = (byte*)data.Scan0;
      ret += y * data.Stride;
      ret += x * 3;
      return ret;
    }

    // assumes 24 bits per pixel RGB
    public static unsafe Color GetPixel(this BitmapData data, long x, long y)
    {
      byte* p = data.GetRGBPointer(x, y);
      return Color.FromArgb(p[0], p[1], p[2]);
    }

    // assumes 24 bits per pixel RGB
    public static unsafe void SetPixel(this BitmapData data, long x, long y, Color c)
    {
      byte* p = data.GetRGBPointer(x, y);
      p[0] = c.R;
      p[1] = c.G;
      p[2] = c.B;
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

