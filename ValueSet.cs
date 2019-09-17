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
  // basically wraps List<Value>.
  // simplifies code that wants to do set operations.
  public unsafe struct ValueSet
  {
    public int ValuesLength;
    public long ID;
    public bool Mapped;
    public bool Visited;
    public double MinDistFound;

#if DEBUG
    public float[] YUVvalues;
#else
    public fixed float YUVvalues[11];
#endif
    //public fixed float NormValues[11];// values 0-1

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueSet New(int dimensionsPerCharacter, long id)
    {
      ValueSet ret = new ValueSet();
      Init(ref ret, dimensionsPerCharacter, false, id, null);
      return ret;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Init(ref ValueSet n, int dimensionsPerCharacter, bool useChroma, long id, float[] discreteNormalizedValues)
    {
#if DEBUG
      n.YUVvalues = new float[11];
#endif
      n.ValuesLength = dimensionsPerCharacter;
      n.ID = id;
      n.MinDistFound = double.MaxValue;// UInt32.MaxValue;
      if (discreteNormalizedValues != null)
      {
        Debug.Assert(dimensionsPerCharacter == discreteNormalizedValues.Length);
        for (int  i = 0; i < discreteNormalizedValues.Length; ++ i)
        {
          n.YUVvalues[i] = discreteNormalizedValues[i];
        }
        // un-normalize these.
        ColorUtils.Denormalize(useChroma, n);
      }
    }

    public unsafe static int CompareTo(ValueSet a, ValueSet other)
    {
      int d = other.ValuesLength.CompareTo(a.ValuesLength);
      if (d != 0)
        return d;
      for (int i = 0; i < a.ValuesLength; ++i)
      {
        d = other.YUVvalues[i].CompareTo(a.YUVvalues[i]);
        if (d != 0)
          return d;
      }
      return 0;
    }

    public unsafe static string ToString(ValueSet o)
    {
      List<string> items = new List<string>();
      for (int i = 0; i < o.ValuesLength; ++i)
      {
        items.Add(o.YUVvalues[i].ToString("0.000000"));
      }
      return string.Format("[{0}]", string.Join(",", items));
    }

  }
}