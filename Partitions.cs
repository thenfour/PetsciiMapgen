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
  public class Partition
  {
    public int Dimension { get; private set; } // which element of ValueSet is used to choose which this.Children to place it in.
    List<Partition> children;
    List<CharInfo> items = new List<CharInfo>();
    public IPixelFormatProvider PixelFormatProvider { get; private set; }

    public Partition(int partsPerLevel, int depth, int dimension, IPixelFormatProvider pf)
    {
      this.PixelFormatProvider = pf;
      this.Dimension = dimension;
      if (depth > 1)
      {
        children = new List<Partition>(partsPerLevel);
        for (int i = 0; i < partsPerLevel; ++ i)
        {
          children.Add(new Partition(partsPerLevel, depth - 1, dimension + 1, pf));
        }
      }
    }
    public int PartitionCountIncludingSelf
    {
      get
      {
        int ret = 1; // self
        if (children == null)
          return ret;
        foreach (Partition p in children)
        {
          ret += p.PartitionCountIncludingSelf;
        }
        return ret;
      }
    }

    public unsafe int GetChildIndex(ValueSet v, bool isNormalized)
    {
      //double f = ColorUtils.NormalizeElement(v, useChroma, this.Dimension);
      //double f = pixelFormatProvider.NormalizeElement(v, this.Dimension);
      double f = isNormalized ? v.ColorData[this.Dimension] : this.PixelFormatProvider.NormalizeElement(v, this.Dimension);
      Debug.Assert(f >= 0);
      Debug.Assert(f <= 1);
      //double f = normalizedValue.ColorData[this.Dimension];
      int n = (int)Math.Floor(f * children.Count);// if children=3, 0=0, .3=.9, .35 = 1.05, 1.0 = 3
      n = Utils.Clamp(n, 0, children.Count - 1);
      return n;
    }

    public void AddItem(CharInfo ci, bool isNormalized)
    {
      items.Add(ci); // this means all partitions contain references to all child charinfo

      if (children != null)
      {
        int n = GetChildIndex(ci.actualValues, isNormalized);
        children[n].AddItem(ci, isNormalized);
      }
    }

    // returns a list of items that's guaranteed to be populated, either of a more specific child's itemss, or if there are no
    // child items, our own items.
    // assumes that v is already in this current partition.
    public IEnumerable<CharInfo> GetItemsInSamePartition(ValueSet v, bool isNormalized)
    {
      if (children == null)
        return this.items;// possibly returns 0 items! (and parent calls need to respond to that)
      int n = GetChildIndex(v, isNormalized);
      var ret = children[n].GetItemsInSamePartition(v, isNormalized);
      if (ret.Any())
        return ret;// there are some child (aka more specific) items; use it.
      // child didn't return any items. use own.
      return this.items;
    }
  }
  public class PartitionManager
  {
    public int PartitionsPerDimension { get; private set; }
    public int Depth { get; private set; }

    private Partition Root { get; set; }
    public IPixelFormatProvider PixelFormatProvider { get; private set; }

    public int PartitionCount
    {
      get
      {
        return Root.PartitionCountIncludingSelf;
      }
    }

    public PartitionManager(int partsPerLevel, int depth)
    {
      this.PartitionsPerDimension = partsPerLevel;
      this.Depth = depth;
    }

    public void Init(IPixelFormatProvider pf)
    {
      this.PixelFormatProvider = pf;
      this.Root = new Partition(PartitionsPerDimension, Depth, 0, this.PixelFormatProvider);
    }

    public void AddItem(CharInfo ci, bool isNormalized)
    {
      this.Root.AddItem(ci, isNormalized);
    }

    public IEnumerable<CharInfo> GetItemsInSamePartition(ValueSet v, bool isNormalized)
    {
      return this.Root.GetItemsInSamePartition(v, isNormalized);
    }

    public override string ToString()
    {
      return string.Format("p{0}x{1}", this.PartitionsPerDimension, this.Depth);
    }
  }
}
