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
      Newtonsoft.Json.Linq.JObject jo = (Newtonsoft.Json.Linq.JObject)serializer.Deserialize(reader);
      var d = jo.ToObject<Dictionary<string, object>>();

      r.Mapped = false;
      r.MinDistFound = -1;
      r.Visited = true;

      r.ID = (long)d["ID"];
      r.NormalizedValues = ValueArray.Deserialize(((Newtonsoft.Json.Linq.JObject)d["NormalizedValues"]).ToObject< Dictionary<string, object>>());
      r.DenormalizedValues = ValueArray.Deserialize(((Newtonsoft.Json.Linq.JObject)d["DenormalizedValues"]).ToObject<Dictionary<string, object>>());
      return r;
    }

    public override unsafe void WriteJson(JsonWriter writer, ValueSet value, JsonSerializer serializer)
    {
      Dictionary<string, object> d = new Dictionary<string, object>();
      d["ID"] = value.ID;
      d["NormalizedValues"] = value.NormalizedValues.Serialize();
      d["DenormalizedValues"] = value.DenormalizedValues.Serialize();
      serializer.Serialize(writer, d);
    }
    public override bool CanRead { get { return true; } }
    public override bool CanWrite { get { return true; } }
  }

  public unsafe struct ValueArray
  {
    const int MaxElements = 20;
    private fixed float _data[MaxElements];

    public int Length;

    public static ValueArray Init(int len)
    {
      ValueArray ret;
      ret.Length = len;
#if DEBUG
      for (int i = 0; i < len; ++i)
      {
        ret._data[i] = -420.0f;
      }
#endif
      return ret;
    }

    public Dictionary<string, object> Serialize()
    {
      Dictionary<string, object> d = new Dictionary<string, object>();
      d["Length"] = this.Length;
      for (int i = 0; i < this.Length; ++i)
      {
        d["v" + i.ToString("00")] = this[i];
      }
      return d;
    }
    public static ValueArray Deserialize(Dictionary<string, object> d)
    {
      ValueArray ret;
      ret.Length = Convert.ToInt32(d["Length"]);
      for (int i = 0; i < ret.Length; ++i)
      {
        ret[i] = (float)Convert.ToDouble(d["v" + i.ToString("00")]);
      }
      return ret;
    }

    public unsafe float this[int n]
    {
      get { return this._data[n]; }
      set { this._data[n] = value; }
    }

    public void Init(float[] vals, int len)
    {
      Debug.Assert(len <= MaxElements);
      this.Length = len;
      if (vals == null)
      {
        // leave uninitialized
#if DEBUG
        for (int i = 0; i < len; ++i)
        {
          this[i] = -420.0f;
        }
#endif
        return;
      }
      for (int i = 0; i < vals.Length; ++i)
      {
        this[i] = vals[i];
      }
    }
    public override string ToString()
    {
      List<string> items = new List<string>();
      for (int i = 0; i < Length; ++i)
      {
        items.Add(string.Format("{0,6:0.00}", _data[i]));
      }
      return string.Format("[{0}]", string.Join(",", items));
    }
  }

  [Newtonsoft.Json.JsonConverter(typeof(ValueSetJsonConverter))]
  public struct ValueSet
  {
    public long ID;
    public bool Mapped;
    public bool Visited;
    public double MinDistFound;

    public override string ToString()
    {
      return string.Format("norm:{0}, denorm:{1}", NormalizedValues.ToString(), DenormalizedValues.ToString());
    }

    public ValueArray NormalizedValues;
    public ValueArray DenormalizedValues;

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
      n.ID = id;
      n.MinDistFound = double.MaxValue;
      n.NormalizedValues.Init(normalizedValues, dimensionsPerCharacter);
      n.DenormalizedValues.Init(null, dimensionsPerCharacter);
    }
    
  }
}