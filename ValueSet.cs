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
using Newtonsoft.Json;

namespace PetsciiMapgen
{
  public class ValueSetJsonConverter : Newtonsoft.Json.JsonConverter<ValueSet>
  {
    public override unsafe ValueSet ReadJson(JsonReader reader, Type objectType, ValueSet existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
      ValueSet r;
      //Dictionary<string, object> d = new Dictionary<string, object>();
      Newtonsoft.Json.Linq.JObject jo = (Newtonsoft.Json.Linq.JObject)serializer.Deserialize(reader);
      var d = jo.ToObject<Dictionary<string, object>>();
      //d = (Dictionary<string, object>)serializer.Deserialize(reader);

      r.Mapped = false;
      r.MinDistFound = -1;
      r.Visited = true;

      //r.ID = 0;
      //r.ValuesLength = 0;

      r.ID = (long)d["ID"];
      r.ValuesLength = Convert.ToInt32(d["ValuesLength"]);
      for (int i = 0; i < r.ValuesLength; ++i)
      {
        r.ColorData[i] = (float)Convert.ToDouble(d["v" + i.ToString("00")]);
      }
      return r;
    }

    public override unsafe void WriteJson(JsonWriter writer, ValueSet value, JsonSerializer serializer)
    {
      Dictionary<string, object> d = new Dictionary<string, object>();
      d["ValuesLength"] = value.ValuesLength;
      d["ID"] = value.ID;
      for(int i = 0; i < value.ValuesLength; ++ i)
      {
        d["v" + i.ToString("00")] = value.ColorData[i];
      }
      serializer.Serialize(writer, d);
    }
    public override bool CanRead { get { return true; } }
    public override bool CanWrite { get { return true; } }
  }

  [Newtonsoft.Json.JsonConverter(typeof(ValueSetJsonConverter))]
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
    //[Newtonsoft.Json.JsonArray()]
    const int MaxDimensions = 20;
    public fixed float ColorData[MaxDimensions];
//#endif
    //public fixed float NormValues[20];// values 0-1

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
      if (dimensionsPerCharacter > MaxDimensions)
      {
        throw new Exception("Maximum dimensions is established as " + MaxDimensions);
      }
#if DEBUG
      // n.ColorData = new float[20];
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