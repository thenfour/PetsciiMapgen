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

    public int GetChildIndex(ValueSet v, bool useChroma)
    {
      float f = ColorUtils.NormalizeElement(v, useChroma, this.Dimension);
      int n = (int)Math.Floor(f * children.Count);// if children=3, 0=0, .3=.9, .35 = 1.05, 1.0 = 3
      n = Utils.Clamp(n, 0, children.Count - 1);
      return n;
    }

    public void AddItem(CharInfo ci, bool useChroma)
    {
      items.Add(ci); // this means all partitions contain references to all child charinfo

      if (children != null)
      {
        int n = GetChildIndex(ci.actualValues, useChroma);
        children[n].AddItem(ci, useChroma);
      }
    }

    // returns a list of items that's guaranteed to be populated, either of a more specific child's itemss, or if there are no
    // child items, our own items.
    // assumes that v is already in this current partition.
    public IEnumerable<CharInfo> GetItemsInSamePartition(ValueSet v, bool useChroma)
    {
      if (children == null)
        return this.items;// possibly returns 0 items! (and parent calls need to respond to that)
      int n = GetChildIndex(v, useChroma);
      var ret = children[n].GetItemsInSamePartition(v, useChroma);
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
      this.Root = new Partition(partsPerLevel, depth, 0);
    }
    public void AddItem(CharInfo ci, bool useChroma)
    {
      this.Root.AddItem(ci, useChroma);
    }

    public IEnumerable<CharInfo> GetItemsInSamePartition(ValueSet v, bool useChroma)
    {
      return this.Root.GetItemsInSamePartition(v, useChroma);
    }
  }
}

//namespace PetsciiMapgen
//{
//  // a partition is really just a big list of map keys.
//  public struct Partition
//  {
//    public uint[] keyIdxs;
//    public uint Length;
//    public bool initialized;

//    static public void Init(ref Partition p, long prealloc)
//    {
//      p.initialized = true;
//      p.Length = 0;
//      p.keyIdxs = new uint[prealloc];
//    }

//    internal static long Add(ref Partition partition, uint i)
//    {
//      if (partition.keyIdxs.Length <= partition.Length)
//      {
//        uint[] t = new uint[partition.Length + Constants.AllocGranularityPartitions];
//        //Console.WriteLine("!!! Dynamic allocation");
//        Array.Copy(partition.keyIdxs, t, partition.Length);
//        partition.keyIdxs = t;
//      }

//      partition.keyIdxs[partition.Length] = i;
//      partition.Length++;
//      return partition.Length - 1;
//    }
//  }

//  public class PartitionManager
//  {
//    public float PartitionSize1D { get; private set; }
//    public int PartitionCount1D { get; private set; }
//    public float SpaceBegin { get; private set; }// for any given dimension, our relevant partitioning space begins here
//    public float SpaceEnd { get; private set; }
//    public float SpaceSize {  get { return SpaceEnd - SpaceBegin; } }
//    public float[] DistinctValues;
//    //public ValueSet Weights;
//    public int Dimensions { get; private set; }

//    // assumes DistinctValues is sorted ascending, and values are equidistant.
//    public unsafe PartitionManager(int valuesPerPartition, int dimensions, float[] DistinctValues)
//    {
//      this.Dimensions = dimensions;

//      // space should begin in a position where, when partfactor=1, partitions are centered over values.
//      float valueSize = DistinctValues[1] - DistinctValues[0];// 1.0f / DistinctValues.ValuesLength;
//      float valueSpaceBegin = DistinctValues[0] - valueSize * 0.5f;
//      float valueSpaceEnd = DistinctValues[DistinctValues.Length - 1] + (valueSize * 0.5f);
//      float valueSpaceSize = valueSpaceEnd - valueSpaceBegin;
//      this.PartitionCount1D = (int)Math.Ceiling((double)DistinctValues.Length / valuesPerPartition); //(int)Math.Round(Utils.Mix(1, DistinctValues.ValuesLength, partitionFactor));
//      int valueSizeShiftLeft = (valuesPerPartition - (DistinctValues.Length % valuesPerPartition)) / 2;
//      this.SpaceBegin = valueSpaceBegin - (valueSizeShiftLeft * valueSize);// DistinctValues.Values[0] - (valueSize * (0.5f + valueSizeShiftLeft));
//      this.PartitionSize1D = valuesPerPartition * valueSize;
//      //this.PartitionSize1D = valueSpaceSize / PartitionCount1D;// valuesPerPartition * valueSize;// (SpaceEnd - SpaceBegin) / PartitionCount1D;

//      // if you have a really odd partition count, center it over values for better distribution.
//      // for example 4 distinct values, but 3 values per partition.
//      // this:   .  0.0   .   0.25   .   0.5   .  1.0
//      //          \_p0____.__________.________/ \_p1____.__________.________/
//      // can be improved like:
//      // this:              .  0.0   .   0.25   .   0.5   .  1.0
//      //          \_p0____.__________.________/ \_p1____.__________.________/
//      this.SpaceEnd = SpaceBegin + PartitionCount1D * PartitionSize1D;

//      this.DistinctValues = DistinctValues;

//      Console.WriteLine("  Partition mgr // PartitionSize1D: " + PartitionSize1D);
//      Console.WriteLine("  Partition mgr // PartitionCount1D: " + PartitionCount1D);
//      Console.WriteLine("  Partition mgr // SpaceBegin: " + SpaceBegin);
//      Console.WriteLine("  Partition mgr // SpaceEnd: " + SpaceEnd);
//      Console.WriteLine("  Partition mgr // SpaceSize: " + SpaceSize);
//      Console.WriteLine("  Partition mgr // PartitionMaxElementSize: " + PartitionMaxElementSize.ToString("N0"));
//      Console.WriteLine("  Partition mgr // PartitionCountND: " + PartitionCountND);
//      Console.WriteLine("  Partition mgr // Distinct values: [" + string.Join(",", DistinctValues) + "]");
//    }

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    internal long GetPartitionID1D(float val)
//    {
//      float n = (val - SpaceBegin) / SpaceSize; // this is the 0-1 position within partitioned space.
//      n *= PartitionCount1D;// now it's a partition id.
//      long ret = (long)Math.Floor(n);
//      long maxPartition = PartitionCount1D - 1;
//      if (ret >= maxPartition)
//        ret = maxPartition;
//      return ret;
//    }

//    // maximum number of distinct map keys which fall into this partition. NOT precise, it's a maximum used for allocating.
//    internal unsafe long PartitionMaxElementSize
//    {
//      get
//      {
//        // how many distinct values are within the range
//        float valuesPerPartition = (float)DistinctValues.Length / PartitionCount1D;
//        return Utils.Pow((long)Math.Ceiling(valuesPerPartition), (uint)this.Dimensions);
//      }
//    }

//    internal long PartitionCountND
//    {
//      get
//      {
//        // each dimension can have values 0-1.
//        // but we want to support "staggered" partitions in order to reduce hard partition edges.
//        // staggered partition is just shifted half a partition away in each dimension.
//        // so in 1 dimension, and 3 partitions:
//        //
//        // non-staggered:
//        //               -0.166    0.0-----0.166----0.33-----0.5-----0.66----.833-----1.0
//        // non-staggered:           |---partition0---|---partition1---|---partition2---|
//        // staggered:      |---partition0---|---partition1---|---partition2---|---partition3---|
//        // so:
//        // 1. the total count is different between staggered & non-staggered
//        // 2. total partition space includes area outside of the valid range.
//        long ret = Utils.Pow(PartitionCount1D, (uint)this.Dimensions);
//        return ret + 1;
//      }
//    }

//    internal unsafe long GetPartitionIndex(ValueSet v)
//    {
//      long ret = 0;
//      for (int i = 0; i < v.ValuesLength; ++i)
//      {
//        long id1d = GetPartitionID1D(v.Values[i]);
//        ret *= PartitionCount1D;
//        ret += id1d;
//      }
//      return ret;
//    }

//  }
//}

