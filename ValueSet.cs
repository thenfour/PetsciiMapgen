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
  public unsafe struct ValueSet
  {
    public int ValuesLength;
    public long ID;
    public bool Mapped;
    public bool Visited;
    public double MinDistFound;

    public override string ToString()
    {
      List<string> items = new List<string>();
      for (int i = 0; i < ValuesLength; ++i)
      {
        items.Add(string.Format("{0,6:0.00}", ColorData[i]));
      }
      return string.Format("[{0}]", string.Join(",", items));
    }

//#if DEBUG
// DONT DO THIS because it will cause issues with references vs. copies to this field.
//    public float[] ColorData;
//#else
    public fixed float ColorData[11];
//#endif
    //public fixed float NormValues[11];// values 0-1

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueSet New(int dimensionsPerCharacter, long id)
    {
      ValueSet ret = new ValueSet();
      Init(ref ret, dimensionsPerCharacter, id, null);
      return ret;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Init(ref ValueSet n, int dimensionsPerCharacter, long id, float[] normalizedValues)
    {
#if DEBUG
     // n.ColorData = new float[11];
#endif
      n.ValuesLength = dimensionsPerCharacter;
      n.ID = id;
      n.MinDistFound = double.MaxValue;// UInt32.MaxValue;
      if (normalizedValues != null)
      {
        Debug.Assert(dimensionsPerCharacter == normalizedValues.Length);
        for (int  i = 0; i < normalizedValues.Length; ++ i)
        {
          n.ColorData[i] = normalizedValues[i];
        }
      //  // un-normalize these.
      //  ColorUtils.Denormalize(useChroma, n);
      }
    }

    public unsafe static int CompareTo(ValueSet a, ValueSet other)
    {
      int d = other.ValuesLength.CompareTo(a.ValuesLength);
      if (d != 0)
        return d;
      for (int i = 0; i < a.ValuesLength; ++i)
      {
        d = other.ColorData[i].CompareTo(a.ColorData[i]);
        if (d != 0)
          return d;
      }
      return 0;
    }

  }
}