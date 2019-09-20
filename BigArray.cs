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
  public struct Mapping
  {
    public int imapKey; // a set of tile values
    public int icharInfo;
    public double dist;
  }

  public class MappingArray
  {
    public Mapping[] Values = new Mapping[30000000]; // RESERVED values therefore don't use Values.Length or do set operations like Values.sort()
    public ulong Length { get; private set; } = 0;

    public MappingArray(int chunkSizeIgnored)
    {

    }

    public void Add(Mapping m)
    {
      if ((ulong)Values.LongLength <= Length)
      {
        Mapping[] t = new Mapping[Length * 2];
        Console.WriteLine("!!! Dynamic allocation");
        Array.Copy(this.Values, t, (int)this.Length);
        this.Values = t;
      }
      Length++;
      this.Values[Length - 1] = m;
    }
    internal ulong SortAndPrune(double maxMinDist)
    {
      var prunedMappings = Values.Take((int)this.Length).Where(o => o.dist <= maxMinDist).ToArray();
      ulong ret = this.Length - (ulong)prunedMappings.Length;
      this.Length = (ulong)prunedMappings.LongLength;
      this.Values = prunedMappings;

      //Debug.Assert(this.Values.LongLength == this.Length);
      Array.Sort<Mapping>(this.Values, (a, b) => a.dist.CompareTo(b.dist));

      return ret;
    }

    public IEnumerable<Mapping> GetEnumerator()
    {
      return this.Values.AsEnumerable().Take((int)this.Length);
      //return (IEnumerable<Mapping>)this.Values.GetEnumerator();
    }
  }



  //// http://dzmitryhuba.blogspot.com/2010/02/container-ordering-support.html
  //// Unbounded priority queue based on binary min heap
  //public class PriorityQueue<T>
  //{
  //  private const int c_initialCapacity = 4;
  //  private readonly IComparer<T> m_comparer;
  //  private T[] m_items;
  //  private int m_count;

  //  private PriorityQueue()
  //    : this(Comparer<T>.Default)
  //  {
  //  }

  //  private PriorityQueue(IComparer<T> comparer)
  //    : this(comparer, c_initialCapacity)
  //  {
  //  }

  //  private PriorityQueue(IComparer<T> comparer, int capacity)
  //  {
  //    //Contract.Requires<ArgumentOutOfRangeException>(capacity >= 0);
  //    //Contract.Requires<ArgumentNullException>(comparer != null);
  //    m_comparer = comparer;
  //    m_items = new T[capacity];
  //  }

  //  private PriorityQueue(T[] source)
  //    : this(source, Comparer<T>.Default)
  //  {
  //  }

  //  public PriorityQueue(T[] source, IComparer<T> comparer)
  //  {
  //    //Contract.Requires<ArgumentNullException>(source != null);
  //    //Contract.Requires<ArgumentNullException>(comparer != null);

  //    m_comparer = comparer;
  //    // In most cases queue that is created out of sequence
  //    // of items will be emptied step by step rather than
  //    // new items added and thus initially the queue is
  //    // not expanded but rather left full
  //    m_items = new T[8];
  //    Array.Copy(source, m_items, source.Length);//.ToArray();
  //    m_count = source.Length;
  //    // Restore heap order
  //    FixWhole();
  //  }

  //  public int Capacity
  //  {
  //    get { return m_items.Length; }
  //  }

  //  public int Count
  //  {
  //    get { return m_count; }
  //  }

  //  public void Enqueue(T e)
  //  {
  //    m_items[m_count++] = e;
  //    // Restore heap if it was broken
  //    FixUp(m_count - 1);
  //    // Once items count reaches half of the queue capacity
  //    // it is doubled
  //    if (m_count >= m_items.Length / 2)
  //    {
  //      Expand(m_items.Length * 2);
  //    }
  //  }

  //  public T Dequeue()
  //  {
  //    //Contract.Requires<InvalidOperationException>(m_count > 0);

  //    var e = m_items[0];
  //    m_items[0] = m_items[--m_count];
  //    // Restore heap if it was broken
  //    FixDown(0);
  //    // Once items count reaches one eighth  of the queue
  //    // capacity it is reduced to half so that items
  //    // still occupy one fourth (if it is reduced when
  //    // count reaches one fourth after reduce items will
  //    // occupy half of queue capacity and next enqueued item
  //    // will require queue expand)
  //    if (m_count <= m_items.Length / 8)
  //    {
  //      //Expand(m_items.Length / 2);
  //      // NO!~
  //    }

  //    return e;
  //  }

  //  public T Peek()
  //  {
  //    //Contract.Requires<InvalidOperationException>(m_count > 0);

  //    return m_items[0];
  //  }

  //  private void FixWhole()
  //  {
  //    // Using bottom-up heap construction method enforce
  //    // heap property
  //    for (int k = m_items.Length / 2 - 1; k >= 0; k--)
  //    {
  //      FixDown(k);
  //    }
  //  }

  //  private void FixUp(int i)
  //  {
  //    // Make sure that starting with i-th node up to the root
  //    // the tree satisfies the heap property: if B is a child
  //    // node of A, then key(A) ≤ key(B)
  //    for (int c = i, p = Parent(c); c > 0; c = p, p = Parent(p))
  //    {
  //      if (Compare(m_items[p], m_items[c]) < 0)
  //      {
  //        break;
  //      }
  //      Swap(m_items, c, p);
  //    }
  //  }

  //  private void FixDown(int i)
  //  {
  //    // Make sure that starting with i-th node down to the leaf
  //    // the tree satisfies the heap property: if B is a child
  //    // node of A, then key(A) ≤ key(B)
  //    for (int p = i, c = FirstChild(p); c < m_count; p = c, c = FirstChild(c))
  //    {
  //      if (c + 1 < m_count && Compare(m_items[c + 1], m_items[c]) < 0)
  //      {
  //        c++;
  //      }
  //      if (Compare(m_items[p], m_items[c]) < 0)
  //      {
  //        break;
  //      }
  //      Swap(m_items, p, c);
  //    }
  //  }

  //  private static int Parent(int i)
  //  {
  //    return (i - 1) / 2;
  //  }

  //  private static int FirstChild(int i)
  //  {
  //    return i * 2 + 1;
  //  }

  //  private int Compare(T a, T b)
  //  {
  //    return m_comparer.Compare(a, b);
  //  }

  //  private void Expand(int capacity)
  //  {
  //    Array.Resize(ref m_items, capacity);
  //  }

  //  private static void Swap(T[] arr, int i, int j)
  //  {
  //    var t = arr[i];
  //    arr[i] = arr[j];
  //    arr[j] = t;
  //  }
  //}





  // From http://visualstudiomagazine.com/articles/2012/11/01/priority-queues-with-c.aspx
  public class PriorityQueue<T> where T : IComparable<T>
  {
    private List<T> data;
    //private T[] data;

    public PriorityQueue(T[] init)
    {
      this.data = new List<T>(init);
    }

    public void Enqueue(T item)
    {
      data.Add(item);
      int ci = data.Count - 1; // child index; start at end
      while (ci > 0)
      {
        int pi = (ci - 1) / 2; // parent index
        if (data[ci].CompareTo(data[pi]) >= 0)
          break; // child item is larger than (or equal) parent so we're done
        T tmp = data[ci];
        data[ci] = data[pi];
        data[pi] = tmp;
        ci = pi;
      }
    }

    public T Dequeue()
    {
      // assumes pq is not empty; up to calling code
      int li = data.Count - 1; // last index (before removal)
      T frontItem = data[0];   // fetch the front
      data[0] = data[li];
      data.RemoveAt(li);

      --li; // last index (after removal)
      int pi = 0; // parent index. start at front of pq
      while (true)
      {
        int ci = pi * 2 + 1; // left child index of parent
        if (ci > li)
          break;  // no children so done
        int rc = ci + 1;     // right child
        if (rc <= li && data[rc].CompareTo(data[ci]) < 0) // if there is a rc (ci + 1), and it is smaller than left child, use the rc instead
          ci = rc;
        if (data[pi].CompareTo(data[ci]) <= 0)
          break; // parent is smaller than (or equal to) smallest child so done
        T tmp = data[pi];
        data[pi] = data[ci];
        data[ci] = tmp; // swap parent and child
        pi = ci;
      }
      return frontItem;
    }

    public T Peek()
    {
      T frontItem = data[0];
      return frontItem;
    }

    public int Count()
    {
      return data.Count;
    }

    public override string ToString()
    {
      string s = "";
      for (int i = 0; i < data.Count; ++i)
        s += data[i].ToString() + " ";
      s += "count = " + data.Count;
      return s;
    }

    public bool IsConsistent()
    {
      // is the heap property true for all data?
      if (data.Count == 0)
        return true;
      int li = data.Count - 1; // last index
      for (int pi = 0; pi < data.Count; ++pi)
      { // each parent index
        int lci = 2 * pi + 1; // left child index
        int rci = 2 * pi + 2; // right child index

        if (lci <= li && data[pi].CompareTo(data[lci]) > 0)
          return false; // if lc exists and it's greater than parent then bad.
        if (rci <= li && data[pi].CompareTo(data[rci]) > 0)
          return false; // check the right child too.
      }
      return true; // passed all checks
    }
    // IsConsistent
  }


  // roll our own sparse array because we don't need many facilities. just adding,
  // sorting, a cleaning-out of sorts, and in-order enumeration.
  // the sort algo will likely need to be 
  public class HugeArray
  {
    private readonly int ChunkElementCount;
    // just hold a huge amount of data. how much?
    // assuming 16 cores, and a huge enough data that we really care, then let's say
    // 2 billion items / 16, which is like 125 million. le'ts go for that.
    public class SubArray : IEnumerable<Mapping>
    {
      public Mapping[] Data;
      public int Length = 0;
      public SubArray(int elementCount)
      {
        Data = new Mapping[elementCount];
      }

      public IEnumerator<Mapping> GetEnumerator()
      {
        return new MappingEnumerator(this);//.Take(this.Length).GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
        return Data.Take(this.Length).GetEnumerator();
      }
    }

    public List<SubArray> data = new List<SubArray>(100);

    public HugeArray(int ChunkElementCount = 125000000)
    {
      this.ChunkElementCount = ChunkElementCount;
      data.Add(new SubArray(ChunkElementCount));
    }

    public void Add(Mapping m)
    {
      SubArray c = data.Last();
      if (c.Length == ChunkElementCount)
      {
        // FULL. add a new.
        data.Add(new SubArray(ChunkElementCount));
        c = data.Last();
      }
      c.Data[c.Length] = m;
      c.Length++;
    }

    public ulong SortAndPrune(double maxDist)
    {
      ulong prevLength = this.Length;
      List<Task> tasks = new List<Task>();
      Console.WriteLine("    Executing {0} tasks", this.data.Count);
      // for each sub-array, sort and skip items with dist > x
      foreach (var c in this.data)
      {
        int n = tasks.Count;
        tasks.Add(Task.Run(() =>
        {
          //Console.WriteLine("    thread {0} starting", n);
          SortAndPrune(c, maxDist);
          //Console.WriteLine("    thread {0} ending", n);
        }));
      }
      Task.WaitAll(tasks.ToArray());
      //Console.WriteLine("    finish");
      ulong pruned = prevLength - this.Length;
      return pruned;
    }

    private void SortAndPrune(SubArray c, double maxDist)
    {
      for (int i = 0; i < c.Length; ++i)
      {
        // if the element is bad, swap with last good element and chop off array len
        if (c.Data[i].dist > maxDist)
        {
          // find last good element (till i!)
          int lastGoodIdx = c.Length - 1;
          while (true)
          {
            if (c.Data[lastGoodIdx].dist <= maxDist)
            {
              c.Data[i] = c.Data[lastGoodIdx];// use it
              c.Length = lastGoodIdx;// and truncate
              break;
            }
            else if (lastGoodIdx == i)
            {
              // never found any usable. guess we're done.
              c.Length = i;
              break;
            }
            lastGoodIdx--;
          }
        }
      }
      // then array.sort.
      Array.Sort(c.Data, 0, c.Length, new MappingComparer());
    }

    public ulong Length
    {
      get
      {
        ulong ret = 0;
        foreach (var a in this.data)
        {
          ret += (ulong)a.Length;
        }
        return ret;
      }
    }

    // https://blogs.msdn.microsoft.com/dhuba/2010/03/04/k-way-merge/
    public IEnumerable<Mapping> GetEnumerator()
    {
      // Each sequence is expected to be ordered according to 
      // the same comparison logic as elementComparer provides
      var coll = data.Where(o => o.Length > 0);
      if (coll.Count() == 1)
      {
        var c = coll.First();
        for (int i = 0; i < c.Length; ++ i)
        {
          yield return c.Data[i];
        }
        yield break;
      }

      var enumerators = coll.Select(e => new MappingEnumerator(e, 0));
      // Disposing sequence of lazily acquired resources as 
      // a single resource
      //using (var disposableEnumerators = enumerators.AsDisposable())
      //{
      // The code below holds the following loop invariant:
      // - Priority queue contains enumerators that positioned at 
      // sequence element
      // - The queue at the top has enumerator that positioned at 
      // the smallest element of the remaining elements of all 
      // sequences

      // Ensures that only non empty sequences participate  in merge
      //var nonEmpty = enumerators.Where(e => e.MoveNext());
      // Current value of enumerator is its priority 
      var comparer = new MappingEnumeratorComparer();
      //var comparer = new MappingComparer();
      // Use priority queue to get enumerator with smallest 
      // priority (current value)
      //var queue = new PriorityQueue<IEnumerator<Mapping>>(enumerators.ToArray(), comparer);
      var queue = new PriorityQueue<MappingEnumerator>(enumerators.ToArray());

      // The queue is empty when all sequences are empty
      while (queue.Count() > 0)
      {
        // Dequeue enumerator that positioned at element that 
        // is next in the merged sequence
        var min = queue.Dequeue();
        yield return min.Current;
        // Advance enumerator to next value
        if (min.MoveNext())
        {
          // If it has value that can be merged into resulting
          // sequence put it into the queue
          queue.Enqueue(min);
        }
      }
      //}
    }

    public class MappingComparer : IComparer<Mapping>
    {
      public int Compare(Mapping x, Mapping y)
      {
        return x.dist.CompareTo(y.dist);
      }
    }

    public class MappingEnumerator : IEnumerator<Mapping>, IComparable<MappingEnumerator>
    {
      private SubArray arr;
      private int i;

      public MappingEnumerator(SubArray a, int startIndex = -1)
      {
        this.arr = a;
        this.i = startIndex;
      }

      public Mapping Current
      {
        get
        {
          return arr.Data[i];
        }
      }

      object IEnumerator.Current
      {
        get
        {
          return arr.Data[i];
        }
      }

      public int CompareTo(MappingEnumerator other)
      {
        return Current.dist.CompareTo(other.Current.dist);
      }

      public void Dispose()
      {
      }

      public bool MoveNext()
      {
        i++;
        return i < arr.Length;
      }

      public void Reset()
      {
        i = 0;
      }
    }

    // Provides comparison functionality for enumerators
    private class MappingEnumeratorComparer : Comparer<IEnumerator<Mapping>>
    {
      public override int Compare(IEnumerator<Mapping> x, IEnumerator<Mapping> y)
      {
        return x.Current.dist.CompareTo(y.Current.dist);
        //return m_comparer.Compare(x.Current, y.Current);
      }
    }


  }

}

