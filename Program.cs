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

      //var map = new HybridMap2(
      //  new FontProvider("..\\..\\img\\fonts\\test7.png", new Size(12, 12)),
      //  flatPartition,
      //  new PixelFormatProvider(3, new Size(1, 1), true, 1));

      var map = new HybridMap2(
        emoji12,
        flatPartition,
        new PixelFormatProvider(5, new Size(2, 2), true, 2.0f));

      t.EndTask();

      //var map = HybridMap2.Load("..\\..\\img\\maps\\mapHybrid-emoji12x12-2x2x6.png", new Size(12, 12), new Size(2, 2), 6);
      t.EnterTask("processing images");

      //map.ProcessImage("..\\..\\img\\fonts\\test6.png", "..\\..\\img\\testdest-test6.png");
      map.ProcessImage("..\\..\\img\\grad3.png", "..\\..\\img\\testdest-grad3.png");
      map.ProcessImage("..\\..\\img\\circle.png", "..\\..\\img\\testdest-circle.png");
      map.ProcessImage("..\\..\\img\\grad.png", "..\\..\\img\\testdest-grad.png");
      map.ProcessImage("..\\..\\img\\airplane.jpg", "..\\..\\img\\testdest-airplane.png");
      map.ProcessImage("..\\..\\img\\balloon600.jpg", "..\\..\\img\\testdest-balloon600.png");
      map.ProcessImage("..\\..\\img\\david.jpg", "..\\..\\img\\testdest-david.png");
      map.ProcessImage("..\\..\\img\\david192.jpg", "..\\..\\img\\testdest-david192.png");
      map.ProcessImage("..\\..\\img\\lisa1024.jpg", "..\\..\\img\\testdest-lisa1024.png");
      map.ProcessImage("..\\..\\img\\lisa512.jpg", "..\\..\\img\\testdest-lisa512.png");
      map.ProcessImage("..\\..\\img\\atomium.jpg", "..\\..\\img\\testdest-atomium.png");
      map.ProcessImage("..\\..\\img\\grad.png", "..\\..\\img\\testdest-grad.png");
      map.ProcessImage("..\\..\\img\\grad2.png", "..\\..\\img\\testdest-grad2.png");
      map.ProcessImage("..\\..\\img\\gtorus.png", "..\\..\\img\\testdest-gtorus.png");
      map.ProcessImage("..\\..\\img\\balloon1200.jpg", "..\\..\\img\\testdest-balloon1200.png");

      t.EndTask();

      Console.WriteLine("Press a key to continue...");
      Console.ReadKey();
    }
  }
}
