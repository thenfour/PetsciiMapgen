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
  // a partition is really just a big list of map keys.
  public struct Partition
  {
    public uint[] keyIdxs;
    public uint Length;
    public bool initialized;

    static public void Init(ref Partition p, long prealloc)
    {
      p.initialized = true;
      p.Length = 0;
      p.keyIdxs = new uint[prealloc];
    }

    internal static long Add(ref Partition partition, uint i)
    {
      if (partition.keyIdxs.Length <= partition.Length)
      {
        uint[] t = new uint[partition.Length + Constants.AllocGranularityPartitions];
        //Console.WriteLine("!!! Dynamic allocation");
        Array.Copy(partition.keyIdxs, t, partition.Length);
        partition.keyIdxs = t;
      }

      partition.keyIdxs[partition.Length] = i;
      partition.Length++;
      return partition.Length - 1;
    }
  }

  public class PartitionManager
  {
    public float PartitionSize1D { get; private set; }
    public int PartitionCount1D { get; private set; }
    public float SpaceBegin { get; private set; }// for any given dimension, our relevant partitioning space begins here
    public float SpaceEnd { get; private set; }
    public float SpaceSize {  get { return SpaceEnd - SpaceBegin; } }
    public float[] DistinctValues;
    public ValueSet Weights;
    public int Dimensions { get; private set; }

    // assumes DistinctValues is sorted ascending, and values are equidistant.
    public unsafe PartitionManager(int valuesPerPartition, int dimensions, float[] DistinctValues, ValueSet dimensionWeights)
    {
      this.Dimensions = dimensions;
      this.Weights = dimensionWeights;

      // normalize weights so the max is 1.
      // this is a solution to a problem related to weights. the problem is that low-weighted dimensions
      // can cause points to end up in separate partitions when their "weighted distance" is very close.
      // in 2D, imagne you're ignoring the Y component. (1,0) and (1,10000) should be equal.
      // but with partitioning, they'll never be able to reach each other.
      // we want to use these weights to make sure items end up in the same partition. it's not difficult; we just scale so the partitions follow weights.
      Utils.ValueRangeInspector r = new Utils.ValueRangeInspector();
      for (int i = 0; i < this.Weights.ValuesLength; ++ i)
      {
        r.Visit(this.Weights.Values[i]);
      }
      // scale.
      Console.WriteLine("TODO!!! This may not be correct. Penguins.");
      for (int i = 0; i < this.Weights.ValuesLength; ++i)
      {
        this.Weights.Values[i] /= r.MaxValue;
      }

      // for UI purposes i want partitionfactor to mean:
      // 0 = no partitioning. (1 partition in total)
      // 1 = maximum partitioning (partitions = # of distinct values exactly).

      // space should begin in a position where, when partfactor=1, partitions are centered over values.
      float valueSize = DistinctValues[1] - DistinctValues[0];// 1.0f / DistinctValues.ValuesLength;
      float valueSpaceBegin = DistinctValues[0] - valueSize * 0.5f;
      float valueSpaceEnd = DistinctValues[DistinctValues.Length - 1] + (valueSize * 0.5f);
      float valueSpaceSize = valueSpaceEnd - valueSpaceBegin;
      this.PartitionCount1D = (int)Math.Ceiling((double)DistinctValues.Length / valuesPerPartition); //(int)Math.Round(Utils.Mix(1, DistinctValues.ValuesLength, partitionFactor));
      int valueSizeShiftLeft = (DistinctValues.Length % valuesPerPartition) / 2;
      this.SpaceBegin = valueSpaceBegin - (valueSizeShiftLeft * valueSize);// DistinctValues.Values[0] - (valueSize * (0.5f + valueSizeShiftLeft));
      this.PartitionSize1D = valuesPerPartition * valueSize;
      //this.PartitionSize1D = valueSpaceSize / PartitionCount1D;// valuesPerPartition * valueSize;// (SpaceEnd - SpaceBegin) / PartitionCount1D;

      // if you have a really odd partition count, center it over values for better distribution.
      // for example 4 distinct values, but 3 values per partition.
      // this:   .  0.0   .   0.25   .   0.5   .  1.0
      //          \_p0____.__________.________/ \_p1____.__________.________/
      // can be improved like:
      // this:              .  0.0   .   0.25   .   0.5   .  1.0
      //          \_p0____.__________.________/ \_p1____.__________.________/
      //this.SpaceEnd = DistinctValues.Values[DistinctValues.ValuesLength - 1] + (valueSize * 0.5f);
      this.SpaceEnd = SpaceBegin + PartitionCount1D * PartitionSize1D;

      this.DistinctValues = DistinctValues;

      Console.WriteLine("  Partition mgr // PartitionSize1D: " + PartitionSize1D);
      Console.WriteLine("  Partition mgr // PartitionCount1D: " + PartitionCount1D);
      Console.WriteLine("  Partition mgr // SpaceBegin: " + SpaceBegin);
      Console.WriteLine("  Partition mgr // SpaceEnd: " + SpaceEnd);
      Console.WriteLine("  Partition mgr // SpaceSize: " + SpaceSize);
      Console.WriteLine("  Partition mgr // PartitionMaxElementSize: " + PartitionMaxElementSize);
      Console.WriteLine("  Partition mgr // PartitionCountND: " + PartitionCountND);
      Console.WriteLine("  Partition mgr // Distinct values: [" + string.Join(",", DistinctValues) + "]");
      Console.WriteLine("  Partition mgr // Weights unscaled: " + ValueSet.ToString(dimensionWeights));
      Console.WriteLine("  Partition mgr // Weights scaled: " + ValueSet.ToString(Weights));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal long GetPartitionID1D(float val, float weight)
    {
      float n = (val - SpaceBegin) / SpaceSize; // this is the 0-1 position within partitioned space.
      n *= PartitionCount1D;// now it's a partition id.
      long ret = (long)Math.Floor(n * weight);
      long maxPartition = PartitionCount1D - 1;
      if (ret >= maxPartition)
        ret = maxPartition;
      return ret;
    }

    // maximum number of distinct map keys which fall into this partition. NOT precise, it's a maximum used for allocating.
    internal unsafe long PartitionMaxElementSize
    {
      get
      {
        // how many distinct values are within the range
        //float partitionSize = 1.0f / PartitionCount1D;
        //float valuesPerPartition = partitionSize * distinctValuesPerDimension;
        float valuesPerPartition = (float)DistinctValues.Length / PartitionCount1D;
        //double ret = 1;
        //for(int i = 0; i < this.Dimensions; ++ i)
        //{
        //  ret *= Math.Ceiling(valuesPerPartition / this.Weights.Values[i]);
        //}
        //return (long)ret;
        return Utils.Pow((long)Math.Ceiling(valuesPerPartition), (uint)this.Dimensions);
      }
    }

    internal long PartitionCountND
    {
      get
      {
        // each dimension can have values 0-1.
        // but we want to support "staggered" partitions in order to reduce hard partition edges.
        // staggered partition is just shifted half a partition away in each dimension.
        // so in 1 dimension, and 3 partitions:
        //
        // non-staggered:
        //               -0.166    0.0-----0.166----0.33-----0.5-----0.66----.833-----1.0
        // non-staggered:           |---partition0---|---partition1---|---partition2---|
        // staggered:      |---partition0---|---partition1---|---partition2---|---partition3---|
        // so:
        // 1. the total count is different between staggered & non-staggered
        // 2. total partition space includes area outside of the valid range.
        long ret = Utils.Pow(PartitionCount1D, (uint)this.Dimensions);
        return ret + 1;
      }
    }

    internal unsafe long GetPartitionIndex(ValueSet v)
    {
      long ret = 0;
      for (int i = 0; i < v.ValuesLength; ++i)
      {
        long id1d = GetPartitionID1D(v.Values[i], Weights.Values[i]);
        ret *= PartitionCount1D;
        ret += id1d;
      }
      return ret;
    }

  }
}

