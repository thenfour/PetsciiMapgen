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
    float[] childIndexBoundaries; // specifies where the partitioning occurs. We want to be strategic so that we interact nicely with our valuespercomponent quantization.

    public Partition(int partsPerLevel, int depth, int dimension, float[] childIndexBoundaries)
    {
      this.Dimension = dimension;
      this.childIndexBoundaries = childIndexBoundaries;

      if (depth > 1) // if there's depth left, create children.
      {
        children = new List<Partition>(partsPerLevel);
        for (int i = 0; i < partsPerLevel; ++ i)
        {
          children.Add(new Partition(partsPerLevel, depth - 1, dimension + 1, childIndexBoundaries));
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
     // f = .6f;
      //double f = isNormalized ? v[this.Dimension] : this.PixelFormatProvider.NormalizeElement(v, this.Dimension);
      Debug.Assert(f >= 0);
      Debug.Assert(f <= 1);
      for (int ib = 0; ib < childIndexBoundaries.Length; ++ ib)
      {
        if (f < childIndexBoundaries[ib])
          return ib;
      }
      return childIndexBoundaries.Length;
      //int n = (int)Math.Floor(f * children.Count);// if children=3, 0=0, .3=.9, .35 = 1.05, 1.0 = 3
      //n = Utils.Clamp(n, 0, children.Count - 1);
      //return n;
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
    float[] childIndexBoundaries;

    private Partition Root { get; set; }

    public int PartitionCount
    {
      get
      {
        return Root.PartitionCountIncludingSelf;
      }
    }

    public PartitionManager(int partsPerLevel, int depth, float[] discreteValues)
    {
      this.Depth = depth;

      // Calculate partition boundaries. We don't want to just split evenly,
      // because it can easily cause worst-case scenarios. Better to find boundaries
      // that are already safe points between discrete values.
      //
      // for example, if discreteValues are 
      // 0-----------.5--------------1
      // and partitions=2, then we get partition boundary
      // | p1        .5|         p2  |
      // so when searching for values around .5, it will very easily exclude
      // items that would work. in each dimension, the best-case points here would be
      // |-----.25-----------.75-----|
      // because a value of .25 is already on the boundary of a discrete value
      // quantization.
      //
      // so first gather a list of good points.
      float[] midpoints = new float[discreteValues.Length - 1];
      for (int iv = 0; iv < discreteValues.Length - 1; ++ iv)
      {
        midpoints[iv] = (discreteValues[iv + 1] + discreteValues[iv]) / 2;
      }
      //midpoints = discreteValues; // this would demonstrate the huge amount of noise you get when worst-case boundaries.

      // select midpoints that are closest to theoretical even distribution
      int[] partitionBoundIdx = new int[partsPerLevel - 1];
      for (int ib = 0; ib < partsPerLevel - 1; ++ ib)
      {
        float evenDist = (1.0f / partsPerLevel) * (ib + 1.0f);
        // and find the midpoint closest to this.
        int closestMidpointIdx = 0;
        float distToClosest = float.MaxValue;
        for( int imp = 0; imp < midpoints.Length; ++ imp)
        {
          float thisDist = Math.Abs(midpoints[imp] - evenDist);
          if (thisDist < distToClosest)
          {
            closestMidpointIdx = imp;
            distToClosest = thisDist;
          }
        }
        partitionBoundIdx[ib] = closestMidpointIdx;
      }

      this.childIndexBoundaries = partitionBoundIdx.Distinct().Select(o => midpoints[o]).ToArray();
      this.PartitionsPerDimension = childIndexBoundaries.Length + 1;
      if (this.childIndexBoundaries.Length != partsPerLevel - 1)
      {
        Log.WriteLine("!!!!!!!!!! We adjusted your partition settings. Probably you wanted to partition too much.");
      }
      Log.WriteLine("Partition boundaries:");
      foreach (var b in this.childIndexBoundaries)
      {
        Log.WriteLine("  {0:0.00}", b);
      }
      this.Root = new Partition(PartitionsPerDimension, Depth, 0, childIndexBoundaries);

      Log.WriteLine("Partitions per dim requested:{0}, actual={0}", partsPerLevel, PartitionsPerDimension);
      Log.WriteLine("Partition depth: {0}", this.Depth);
      Log.WriteLine("Partition count: " + PartitionCount.ToString("N0"));
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
