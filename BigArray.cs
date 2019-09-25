using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
  public class MappingComparer : IComparer<Mapping>
  {
    public int Compare(Mapping x, Mapping y)
    {
      return x.dist.CompareTo(y.dist);
    }
  }
  public struct Mapping
  {
    public int imapKey; // a set of tile values
    public int icharInfo;
    public double dist;
  }

  public class MappingArray
  {
    public Mapping[] Values = new Mapping[30000000]; // RESERVED values therefore don't use Values.Length or do set operations like Values.sort()
    public int Length { get; private set; } = 0;

    public MappingArray(int chunkSizeIgnored)
    {
    }

    private void SuperficialClear()
    {
      Length = 0;
    }

    private void Add__(Mapping m, int potentialNewLength)
    {
      if (Values.Length <= Length)
      {
        Log.WriteLine("!!! Dynamic allocation: {0:N0} to {1:N0} (process currently using {2:0.00})", this.Length, potentialNewLength, Utils.BytesToMb(Utils.UsedMemoryBytes));
        Mapping[] t = new Mapping[potentialNewLength];
        Array.Copy(this.Values, t, (int)this.Length);
        this.Values = t;
        //Array.Resize(ref this.Values, Length * 2);
      }
      this.Values[Length] = m;
      Length++;
    }
    public void Add(Mapping m, double percentComplete)
    {
      // potential optimization: resize the array as soon as we have a good idea how big it should be.
      int newLength = (int)((double)this.Length / percentComplete);
      // add 20% for padding.
      newLength = Math.Max(newLength + (newLength / 5), this.Length * 2);// don't bother with small jumps. always go big.
      Add__(m, newLength);
    }

    public void Add(Mapping m)
    {
      Add__(m, Length * 2);
    }

    public void Sort()
    {
      Array.Sort(this.Values, 0, Length, new MappingComparer());
    }



    public IEnumerable<Mapping> GetEnumerator()
    {
      return this.Values.AsEnumerable().Take((int)this.Length);
      //return (IEnumerable<Mapping>)this.Values.GetEnumerator();
    }
  }

}

