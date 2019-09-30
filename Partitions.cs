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
    CharInfo[] itemsArray = null;// null when dirty.

    public Partition(int partsPerLevel, int depth, int dimension)
    {
      this.Dimension = dimension;
      if (depth > 1)
      {
        children = new List<Partition>(partsPerLevel);
        for (int i = 0; i < partsPerLevel; ++ i)
        {
          children.Add(new Partition(partsPerLevel, depth - 1, dimension + 1));
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

    public unsafe int GetChildIndex(ValueSet v)
    {
      float f = v.NormalizedValues[this.Dimension];
      //double f = isNormalized ? v[this.Dimension] : this.PixelFormatProvider.NormalizeElement(v, this.Dimension);
      Debug.Assert(f >= 0);
      Debug.Assert(f <= 1);
      int n = (int)Math.Floor(f * children.Count);// if children=3, 0=0, .3=.9, .35 = 1.05, 1.0 = 3
      n = Utils.Clamp(n, 0, children.Count - 1);
      return n;
    }

    public void AddItem(CharInfo ci)
    {
      items.Add(ci); // this means all partitions contain references to all child charinfo
      itemsArray = null;

      if (children != null)
      {
        int n = GetChildIndex(ci.actualValues);
        children[n].AddItem(ci);
      }
    }

    // returns a list of items that's guaranteed to be populated, either of a more specific child's itemss, or if there are no
    // child items, our own items.
    // assumes that v is already in this current partition.
    public CharInfo[] GetItemsInSamePartition(ValueSet v)
    {
      if (itemsArray == null)
      {
        itemsArray = items.ToArray();
      }
      if (children == null)
        return this.itemsArray;// possibly returns 0 items! (and parent calls need to respond to that)
      int n = GetChildIndex(v);
      var ret = children[n].GetItemsInSamePartition(v);
      if (ret.Any())
        return ret;// there are some child (aka more specific) items; use it.
      // child didn't return any items. use own.
      return this.itemsArray;
    }
  }
  public class PartitionManager
  {
    public int PartitionsPerDimension { get; private set; }
    public int Depth { get; private set; }

    private Partition Root { get; set; }

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

    public void Init()
    {
      this.Root = new Partition(PartitionsPerDimension, Depth, 0);
    }

    public void AddItem(CharInfo ci)
    {
      this.Root.AddItem(ci);
    }

    public CharInfo[] GetItemsInSamePartition(ValueSet v)
    {
      return this.Root.GetItemsInSamePartition(v);
    }

    public override string ToString()
    {
      return string.Format("p{0}x{1}", this.PartitionsPerDimension, this.Depth);
    }

    //internal void WriteConfig(StringBuilder sb)
    //{
    //  sb.AppendLine(string.Format("partitionsPerDimension=" + this.PartitionsPerDimension));
    //  sb.AppendLine(string.Format("partitionDepth=" + this.Depth));
    //}
  }
}
