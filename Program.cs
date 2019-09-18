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

namespace PetsciiMapgen
{
  class Program
  {
    static void Main(string[] args)
    {
      Timings t = new Timings();
      t.EnterTask("--- MAIN PROCESSING");

      var emoji12 = new FontProvider("..\\..\\img\\fonts\\emojidark12.png", new Size(12, 12));
      var c64font = new FontProvider("..\\..\\img\\fonts\\c64opt160.png", new Size(8, 8));
      var flatPartition = new PartitionManager(1, 1);
      var partition = new PartitionManager(2, 9);

      var map = new HybridMap2(
        new FontProvider("..\\..\\img\\fonts\\emojidark12.png", new Size(12, 12)),
        flatPartition,
        new PixelFormatProvider(5, new Size(2, 2), true, .5));

      //var map = new HybridMap2(
      //  emoji12,
      //  partition26,
      //  new PixelFormatProvider(3, new Size(2, 2), false, 2.0f));

      t.EndTask();

      //var map = HybridMap2.Load("..\\..\\img\\maps\\mapHybrid-emoji12x12-2x2x6.png", new Size(12, 12), new Size(2, 2), 6);
      t.EnterTask("processing images");

      map.ProcessImageUsingRef("..\\..\\img\\fonts\\test6.png", "..\\..\\img\\testdest-test6.png");
      map.ProcessImageUsingRef("..\\..\\img\\grad3.png", "..\\..\\img\\testdest-grad3.png");
      map.ProcessImageUsingRef("..\\..\\img\\circle.png", "..\\..\\img\\testdest-circle.png");
      map.ProcessImageUsingRef("..\\..\\img\\grad.png", "..\\..\\img\\testdest-grad.png");
      map.ProcessImageUsingRef("..\\..\\img\\airplane.jpg", "..\\..\\img\\testdest-airplane.png");
      map.ProcessImageUsingRef("..\\..\\img\\balloon600.jpg", "..\\..\\img\\testdest-balloon600.png");
      map.ProcessImageUsingRef("..\\..\\img\\david.jpg", "..\\..\\img\\testdest-david.png");
      map.ProcessImageUsingRef("..\\..\\img\\david192.jpg", "..\\..\\img\\testdest-david192.png");
      map.ProcessImageUsingRef("..\\..\\img\\lisa1024.jpg", "..\\..\\img\\testdest-lisa1024.png");
      map.ProcessImageUsingRef("..\\..\\img\\lisa512.jpg", "..\\..\\img\\testdest-lisa512.png");
      map.ProcessImageUsingRef("..\\..\\img\\atomium.jpg", "..\\..\\img\\testdest-atomium.png");
      map.ProcessImageUsingRef("..\\..\\img\\grad.png", "..\\..\\img\\testdest-grad.png");
      map.ProcessImageUsingRef("..\\..\\img\\grad2.png", "..\\..\\img\\testdest-grad2.png");
      map.ProcessImageUsingRef("..\\..\\img\\gtorus.png", "..\\..\\img\\testdest-gtorus.png");
      map.ProcessImageUsingRef("..\\..\\img\\balloon1200.jpg", "..\\..\\img\\testdest-balloon1200.png");

      t.EndTask();

      Console.WriteLine("Press a key to continue...");
      Console.ReadKey();
    }
  }
}
