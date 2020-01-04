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
  public struct vec2
  {
    public float x;
    public float y;
    public static vec2 Init(float xy)
    {
      vec2 ret;
      ret.x = ret.y = xy;
      return ret;
    }
    public static vec2 Init(float x, float y)
    {
      vec2 ret;
      ret.x = x;
      ret.y = y;
      return ret;
    }
    public static vec2 Init(int x, int y)
    {
      vec2 ret;
      ret.x = x;
      ret.y = y;
      return ret;
    }

    public override string ToString()
    {
      return string.Format("vec2({0:0.00},{1:0.00})", x, y);
    }

    public vec2 floor()
    {
      return vec2.Init((float)Math.Floor(this.x), (float)Math.Floor(this.y));
    }

    public vec2 multipliedBy(Size cellSize)
    {
      return vec2.Init(this.x * cellSize.Width, this.y * cellSize.Height);
    }

    public vec2 minus(vec2 cellOrigin)
    {
      return vec2.Init(this.x - cellOrigin.x, this.y - cellOrigin.y);
    }
  }

  public static class vec2Utils
  {
    public static vec2 dividedBy(this vec2 x, Size sz)
    {
      vec2 ret;
      ret.x = x.x / sz.Width;
      ret.y = x.y / sz.Height;
      return ret;
    }
    public static vec2 minus(this vec2 x, float v)
    {
      vec2 ret;
      ret.x = x.x - v;
      ret.y = x.y - v;
      return ret;
    }
    public static vec2 multipliedBy(this vec2 x, float v)
    {
      vec2 ret;
      ret.x = x.x * v;
      ret.y = x.y * v;
      return ret;
    }
    public static vec2 add(this vec2 x, vec2 v)
    {
      vec2 ret;
      ret.x = x.x + v.x;
      ret.y = x.y + v.y;
      return ret;
    }
    public static vec2 add(this vec2 x, float v)
    {
      vec2 ret;
      ret.x = x.x + v;
      ret.y = x.y + v;
      return ret;
    }
    public static vec2 yx(this vec2 x)
    {
      vec2 ret;
      ret.x = x.y;
      ret.y = x.x;
      return ret;
    }
    public static ivec2 step(this vec2 o, float v)
    {
      ivec2 ret;
      ret.x = o.x > v ? 1 : 0;
      ret.y = o.y > v ? 1 : 0;
      return ret;
    }
    public static vec2 abs(this vec2 o)
    {
      vec2 ret;
      ret.x = Math.Abs(o.x);
      ret.y = Math.Abs(o.y);
      return ret;
    }
  }

  public struct ivec2
  {
    public int x;
    public int y;
    public override string ToString()
    {
      return string.Format("vec2({0},{1})", x, y);
    }
  }

  public struct ColorF
  {
    public double R;
    public double G;
    public double B;

    public override string ToString()
    {
      return string.Format("[{0},{1},{2}]", R.ToString("0.00"), G.ToString("0.00"), B.ToString("0.00"));
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

  public interface IColor
  {
    double colorant1 { get; }
    double colorant2 { get; }
    double colorant3 { get; }
  }

  public struct LCCColorDenorm : IColor
  {
    public double L;
    public double C1;
    public double C2;
    public double colorant1 { get { return L; } }
    public double colorant2 { get { return C1; } }
    public double colorant3 { get { return C2; } }

    public static LCCColorDenorm Init
    {
      get
      {
        LCCColorDenorm ret;
        ret.L = 0;
        ret.C1 = 0;
        ret.C2 = 0;
        return ret;
      }
    }

    public override string ToString()
    {
      return string.Format("[{0},{1},{2}]", L.ToString("0.00"), C1.ToString("0.00"), C2.ToString("0.00"));
    }
  }

  public struct LCCColorNorm : IColor
  {
    public double L;
    public double C1;
    public double C2;
    public double colorant1 { get { return L; } }
    public double colorant2 { get { return C1; } }
    public double colorant3 { get { return C2; } }

    public static LCCColorNorm Init
    {
      get
      {
        LCCColorNorm ret;
        ret.L = 0;
        ret.C1 = 0;
        ret.C2 = 0;
        return ret;
      }
    }

    public override string ToString()
    {
      return string.Format("[{0},{1},{2}]", L.ToString("0.00"), C1.ToString("0.00"), C2.ToString("0.00"));
    }
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
  }


  public class CharInfo
  {
    public int srcIndex;// index from the font provider.
    public ValueSet actualValues;// N-dimension values. For chardata, this is denormalized values. for map, it's normalized.
    public int usages = 0;
    public int refFontIndex;// index in the ref font texture
                            //public UInt32 mapKeysVisited = 0;
                            //public int? ifg;// only for mono palette processing, index to palette
                            //public int? ibg;// only for mono palette processing
                            //public int masterIdx;// index into the charInfo list

#if DEBUG
    //public Point fontImagePixelPos;
    //public Point fontImageCellPos;
#endif

    public CharInfo(int dimensionsPerCharacter)
    {
      actualValues = ValueSet.New(dimensionsPerCharacter, 9999);
    }

    public override string ToString()
    {
#if DEBUG
      return string.Format("ID:{0} src:{1}, usages:{2}",
        srcIndex, actualValues,// masterIdx,
        usages/*, fontImagePixelPos, fontImageCellPos*/);
#else
      return string.Format("ID:{0} src:{1}, usages:{2}",
        srcIndex, actualValues,// masterIdx,
        usages);
#endif
      //return srcIndex.ToString();
    }
  }

  class StayOn : IDisposable
  {
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

    [FlagsAttribute]
    public enum EXECUTION_STATE : uint
    {
      ES_AWAYMODE_REQUIRED = 0x00000040,
      ES_CONTINUOUS = 0x80000000,
      ES_DISPLAY_REQUIRED = 0x00000002,
      ES_SYSTEM_REQUIRED = 0x00000001
      // Legacy flag, should not be used.
      // ES_USER_PRESENT = 0x00000004
    }

    public StayOn()
    {
      SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_AWAYMODE_REQUIRED);
    }

    public void Dispose()
    {
      SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
    }
  }
  public class ArgSet
  {
    public delegate IEnumerable<string> ArgGeneratorDelegate(ArgSet x);

    public IEnumerable<string> _args = Enumerable.Empty<string>();
    public IEnumerable<ArgGeneratorDelegate> _argGenerators = Enumerable.Empty<ArgGeneratorDelegate>();

    public ArgSet(params string[] a)
    {
      _args = a;
    }
    public ArgSet(IEnumerable<string> a)
    {
      _args = a;
    }
    public static ArgSet operator +(ArgSet a, ArgSet b)
    {
      ArgSet n = new ArgSet();
      n._args = a._args.Concat(b._args);
      n._argGenerators = a._argGenerators.Concat(b._argGenerators);
      return n;
    }
    public static ArgSet operator +(ArgSet a, ArgGeneratorDelegate b)
    {
      ArgSet n = new ArgSet();
      n._args = a._args;
      n._argGenerators = a._argGenerators.Append(b);
      return n;
    }

    public IEnumerable<string> Args
    {
      get
      {
        var generatorRes = _argGenerators.Select(ag => ag(this));
        var tr = _args;
        foreach (var g in generatorRes)
        {
          tr = tr.Concat(g);
        }
        return tr;
      }
    }

    public string ToCSString()
    {
      return "new string[] {" + string.Join(", ", Args.Select(s => string.Format("@\"{0}\"", s))) + "};";
    }

    public override string ToString()
    {
      return string.Join(" ", Args);
    }
  }

  public class ArgSetList
  {
    public IEnumerable<ArgSet> argSets;

    public ArgSetList()
    {
      argSets = (new List<ArgSet>());
    }

    public IEnumerable<ArgSet> Filter(params string[] tokens)
    {
      foreach (var x in this.argSets)
      {
        string argstring = x.ToString();
        bool satisfied = true;
        foreach (string tok in tokens)
        {
          if (argstring.IndexOf(tok, StringComparison.InvariantCultureIgnoreCase) == -1)
          {
            satisfied = false;
            break;
          }
        }
        if (satisfied)
        {
          yield return x;
        }
      }
    }

    public static ArgSetList operator +(ArgSetList a, ArgSetList b)
    {
      ArgSetList ret = new ArgSetList();
      List<ArgSet> x = new List<ArgSet>(a.argSets.Count() * b.argSets.Count());
      foreach (var ao in a.argSets)
      {
        foreach (var bo in b.argSets)
        {
          ArgSet n = new ArgSet();
          List<string> args = ao._args.ToList();
          args.AddRange(bo._args);
          n._args = args.ToArray();
          x.Add(n);
        }
      }
      ret.argSets = x;
      return ret;
    }
    public static ArgSetList operator +(ArgSetList a, ArgSet b)
    {
      // this just adds args to the end of all arglists
      ArgSetList ret = new ArgSetList();
      List<ArgSet> x = new List<ArgSet>();// a.argSets.ToList();
      foreach (var ao in a.argSets)
      {
        x.Add(ao + b);
      }
      ret.argSets = x;
      return ret;
    }
    public static ArgSetList operator +(ArgSet a, ArgSetList b)
    {
      // this just adds args to the end of all arglists
      ArgSetList ret = new ArgSetList();
      List<ArgSet> x = new List<ArgSet>();// a.argSets.ToList();
      foreach (var bo in b.argSets)
      {
        x.Add(a + bo);
      }
      ret.argSets = x;
      return ret;
    }

    public static ArgSetList operator +(ArgSetList a, ArgSet.ArgGeneratorDelegate argGenerator)
    {
      // this just adds args to the end of all arglists
      ArgSetList ret = new ArgSetList();
      List<ArgSet> x = new List<ArgSet>();
      foreach (var ao in a.argSets)
      {
        x.Add(ao + argGenerator);
      }
      ret.argSets = x;
      return ret;
    }
  }

  public class ProgressReporter
  {
    ulong total;
    ulong current = 0;
    Stopwatch swseg;
    Stopwatch swtotal;
    public ProgressReporter(int total) : this((ulong)total) { }
    public ProgressReporter(ulong total)
    {
      this.total = total;
      this.swtotal = new Stopwatch();
      swtotal.Start();
      this.swseg = new Stopwatch();
      swseg.Start();
    }
    public void Visit()
    {
      Visit(current);
      current++;
    }
    public void Visit(ulong item)
    {
      if (swseg.ElapsedMilliseconds < 5000)
        return;
      swseg.Restart();
      double p = (double)item / total;
      double elapsedSec = (double)swtotal.ElapsedMilliseconds / 1000.0;
      double totalEst = elapsedSec / p;
      double estRemaining = totalEst - elapsedSec;
      Log.WriteLine("  Progress: {0}% (est remaining: {1:N0} sec ({2:N0} hours))", (p * 100).ToString("0.00"), estRemaining.ToString("0.00"), (estRemaining / 3600).ToString("0.00"));
    }
  }

  //public class Timings
  //{
  //  public struct Task
  //  {
  //    public Stopwatch sw;
  //    public string name;
  //  }
  //  Stack<Task> tasks = new Stack<Task>();
  //  public void EnterTask(string s, params object[] o)
  //  {
  //    EnterTask(string.Format(s, o));
  //  }
  //  public void EnterTask(string s)
  //  {
  //    Log.WriteLine("==> Enter task {0}", s);
  //    Log.IncreaseIndent();
  //    Task n;
  //    if (!tasks.Any())
  //    {
  //      n = new Task {
  //        name = "root",
  //        sw = new Stopwatch()
  //      };
  //      n.sw.Start();
  //      tasks.Push(n);
  //    }
  //    n = new Task
  //    {
  //      name = s,
  //      sw = new Stopwatch()
  //    };
  //    n.sw.Start();
  //    tasks.Push(n);
  //  }
  //  public void EndTask()
  //  {
  //    Debug.Assert(this.tasks.Count > 0);
  //    Task n = this.tasks.Pop();
  //    TimeSpan ts = n.sw.Elapsed;
  //    Log.DecreaseIndent();
  //    Log.WriteLine("<== {1} (end {0})", n.name, ts);
  //  }
  //}

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
      return ColorF.From(Color.FromArgb(p[2], p[1], p[0]));
    }

    // assumes 24 bits per pixel RGB
    public static unsafe void SetPixel(this BitmapData data, long x, long y, ColorF c)
    {
      byte* p = data.GetRGBPointer(x, y);
      p[2] = (byte)(c.R * 255);
      p[1] = (byte)(c.G * 255);
      p[0] = (byte)(c.B * 255);
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
      ulong numDigits = (ulong)discreteNormalizedValuesPerTile.Length;
      ulong theoreticalBase = numDigits;
      ulong totalPermutations = (ulong)Pow((long)numDigits, (uint)numDimensions);
      if (totalPermutations > int.MaxValue)
      {
        Log.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!");
        Log.WriteLine("!!!!!  Total map keys is just too big. Must be less than 2 billion.");
        Log.WriteLine("You requested a map with {0:N0} mappings", totalPermutations);
        Log.WriteLine("Which would result in a map ref image {0:N0} x {0:N0}", (int)Math.Sqrt(totalPermutations));
        Log.WriteLine("And take {0:N0} MB on disk", totalPermutations / 1024 / 1024 * 3);
        throw new Exception("Map is too big to process");
      }
      float[] normalizedValues = new float[numDimensions];

      Log.WriteLine("Allocating {0:N0} valuesets. Valueset size = {1}, so {2:N0} bytes of memory", totalPermutations, sizeof(ValueSet),
        (ulong)totalPermutations * (ulong)sizeof(ValueSet));


      ValueSet[] ret = new ValueSet[totalPermutations];
      for (ulong i = 0; i < totalPermutations; ++i)
      {
        // just like digits in a number, use % and divide to shave off "digits" one by one.
        ulong a = i;// the value that originates from i and we shift/mod to enumerate digits
        //ValueSet n = NewValueSet(numTiles, i);
        for (int d = 0; d < numDimensions; ++d)
        {
          ulong thisIndex = a % theoreticalBase;
          a /= theoreticalBase;
          normalizedValues[d] = discreteNormalizedValuesPerTile[(int)thisIndex];
          //ret[i].Values[d] = discreteValuesPerTile[(int)thisIndex];
        }
        ValueSet.Init(ref ret[i], numDimensions, (long)i, normalizedValues);
        //ret.Add(n);
        //for (int xxx = 0; xxx < ret[i].ValuesLength; ++xxx)
        //{
        //  Debug.Assert(ret[14].ColorData[xxx] == 0 || ret[14].ColorData[xxx] == 0.5 || ret[14].ColorData[xxx] == 1.0);
        //}
      }

      //foreach (var r in ret)
      //{
      //  for (int i = 0; i < r.ValuesLength; ++ i)
      //  {
      //    Debug.Assert(r.ColorData[i] == 0 || r.ColorData[i] == 0.5 || r.ColorData[i] == 1.0);
      //  }
      //}

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
      for (int i = 0; i < discreteValues; ++i)
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

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    public static void ProcessArg(this string[] args, string[] keyAliases, Action<string> tr)
    {
      for (int i = 0; i < args.Length; i++)
      {
        if (keyAliases.Any(o => o.Equals(args[i], StringComparison.InvariantCultureIgnoreCase)))
        {
          if (i < args.Length - 1)
            tr(args[i + 1]);
          else
            tr(null);
        }
      }
    }

    public static void ProcessArg(this string[] args, string key, Action<string> tr)
    {
      args.ProcessArg(new string[] { key }, tr);
    }

    public static void ProcessArg2(this string[] args, string[] keyAliases, Action<string, IEnumerable<string>> tr)
    {
      for (int i = 0; i < args.Length; i++)
      {
        if (keyAliases.Any(o => o.Equals(args[i], StringComparison.InvariantCultureIgnoreCase)))
        {
          if (i < args.Length - 1)
            tr(args[i], args.Skip(i + 1));
          else
            tr(args[i], null);
        }
      }
    }

    public static void ProcessArg2(this string[] args, string key, Action<string, IEnumerable<string>> tr)
    {
      args.ProcessArg2(new string[] { key }, tr);
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //internal static float Mix(float v0, float v1, float t)
    //{
    //  return (1 - t) * v0 + t * v1;
    //}

    public static bool ToBool(string s)
    {
      if (string.IsNullOrEmpty(s))
        return false;
      if (s == "1")
        return true;
      if (s.ToLowerInvariant() == "true")
        return true;
      if (s.ToLowerInvariant() == "yes")
        return true;
      return false;
    }

    //// 1v2x3+2
    //public static void ParsePFArgs(string o, out int valuesPerComponent_, out bool useChroma_, out Size lumaTiles_)
    //{
    //  valuesPerComponent_ = int.Parse(o.Split('v')[0]);
    //  o = o.Split('v')[1];// 2x3+2
    //  useChroma_ = int.Parse(o.Split('+')[1]) == 2;
    //  o = o.Split('+')[0];// 2x3
    //  lumaTiles_ = new Size(int.Parse(o.Split('x')[0]), int.Parse(o.Split('x')[1]));
    //}

    public static ulong GbToBytes(ulong gb)
    {
      return MbToBytes(gb) * 1024;
    }
    public static ulong MbToBytes(ulong gb)
    {
      return KbToBytes(gb) * 1024;
    }
    public static ulong KbToBytes(ulong gb)
    {
      return gb * 1024;
    }
    public static double BytesToKb(ulong b)
    {
      return b / 1024;
    }
    public static double BytesToMb(ulong b)
    {
      return BytesToKb(b) / 1024;
    }
    public static double BytesToGb(ulong b)
    {
      return BytesToMb(b) / 1024;
    }
    public static ulong UsedMemoryBytes
    {
      get
      {
        long memory = 0;
        using (Process proc = Process.GetCurrentProcess())
        {
          // The proc.PrivateMemorySize64 will returns the private memory usage in byte.
          // Would like to Convert it to Megabyte? divide it by 1e+6
          memory = proc.PrivateMemorySize64;
        }
        return (ulong)memory;
      }
    }

    public static int NormalizedValueSetToMapID(float[] vals, int dimensionCount, float[] DiscreteNormalizedValues, int mapSize)
    {
      // copy/paste from others.
      float halfSegCenter = 0.25f / DiscreteNormalizedValues.Length;

      int ID = 0;
      for (int i = dimensionCount - 1; i >= 0; --i)
      {
        float val = vals[i];
        val -= halfSegCenter;
        val = Utils.Clamp(val, 0, 1);
        val *= DiscreteNormalizedValues.Length;
        ID *= DiscreteNormalizedValues.Length;
        ID += (int)Math.Floor(val);
      }

      if (ID >= mapSize)
      {
        ID = mapSize - 1;
      }
      return ID;
    }

    public static double EuclidianColorDist(ValueSet key, ValueSet actual, int lumaElements, int chromaElements)
    {
      //double lumaMul = .8;
      //double chromaMul = .2;
      Debug.Assert(key.DenormalizedValues.Length == actual.DenormalizedValues.Length);
      Debug.Assert(key.DenormalizedValues.Length == (lumaElements + chromaElements));
      double accLuma = 0;
      for (int i = 0; i < lumaElements; ++i)
      {
        double d = Math.Abs(key.DenormalizedValues[i] - actual.DenormalizedValues[i]);
        accLuma += d * d;
      }
      accLuma /= lumaElements;
      //accLuma *= lumaMul;

      double accChroma = 0;
      for (int i = 0; i < chromaElements; ++i)
      {
        double d = Math.Abs(key.DenormalizedValues[lumaElements + i] - actual.DenormalizedValues[lumaElements + i]);
        accChroma += d * d;
      }
      //accChroma *= chromaMul;
      return accChroma + accLuma;
    }

    public static ValueSet GetValueSetForSinglePixel(this ILCCColorSpace cs, ColorF color, bool useChroma)
    {
      // a value set normally consists of multiple luma & chroma components for a char (for example 5 luma + 2 chroma)
      // for this we just have the normal default 3-component. all our colorspaces are LCC (luma chroma chroma).
      LCCColorDenorm denorm = cs.RGBToLCC(color);
      ValueSet src = new ValueSet();
      float[] normArray = new float[]
      {
        (float)cs.NormalizeL(denorm.L),
        (float)cs.NormalizeC1(denorm.C1),
        (float)cs.NormalizeC2(denorm.C2),
      };
      ValueSet.Init(ref src, useChroma ? 3 : 1, 0, normArray);
      src.DenormalizedValues[0] = (float)denorm.L;
      src.DenormalizedValues[1] = (float)denorm.C1;
      src.DenormalizedValues[2] = (float)denorm.C2;
      return src;
    }

    public static ColorF ToColorF(this System.Drawing.Color col)
    {
      ColorF ret = ColorF.FromRGB(
        (double)col.R / 255,
        (double)col.G / 255,
        (double)col.B / 255
        );
      return ret;
    }

    public static ILCCColorSpace ParseRequiredLCCColorSpaceArgs(string[] args, bool allowDefault = false)
    {
      ILCCColorSpace ret = null;
      args.ProcessArg("-cs", o =>
      {
        switch (o.ToLowerInvariant())
        {
          case "jpeg":
            ret = new JPEGColorspace();
            break;
          case "nyuv":
            ret = new NaiveYUVColorspace();
            break;
          case "lab":
            ret = new LABColorspace();
            break;
          case "hsl":
            ret = new HSLColorspace();
            break;
          default:
            throw new Exception(string.Format("Unknown LCC colorspace: {0}", o));
        }
      });
      if (ret == null)
      {
        if (allowDefault)
        {
          ret = new LABColorspace();
        }
        else
        {
          throw new Exception("Colorspace not specified");
        }
      }
      return ret;
    }

    public static Color[] GetNamedPalette(string s)
    {
      var ti = typeof(Palettes).GetProperty(s).GetValue(null);
      return (Color[])ti;
    }

    public static void DumpTessellation(IFiveTileTessellator tessellator, Size charSize)
    {
      // OUTput a visual of the tiling
      Log.WriteLine("Luma tiling breakdown for charsize {0}:", charSize);
      //Log.WriteLine(" Rotation: {0}", ret.Rotation);
      int[] pixelCounts = new int[5];
      for (int py = 0; py < charSize.Height; ++py)
      {
        string l = "  ";
        for (int px = 0; px < charSize.Width; ++px)
        {
          int lumaIdx = tessellator.GetLumaTileIndexOfPixelPosInCell(px, py, charSize);
          pixelCounts[lumaIdx]++;
          switch (lumaIdx)
          {
            case 0:
              l += "..";
              break;
            case 1:
              l += "##";
              break;
            case 2:
              l += "//";
              break;
            case 3:
              l += "33";
              break;
            case 4:
              l += "  ";
              break;
          }
          //l += lumaIdx.ToString();
        }
        Log.WriteLine(l);
      }
      for (int i = 0; i < 5; ++i)
      {
        Log.WriteLine("Tile {0}: {1} pixels", i, pixelCounts[i]);
      }
    }
  }
}

