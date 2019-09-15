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
      var rgb = new ColorMine.ColorSpaces.Rgb { R = 255, G = 255, B =255 };
      var lab = rgb.To<ColorMine.ColorSpaces.Lab>();

      Timings t = new Timings();
      t.EnterTask("--- MAIN PROCESSING");

      //var map = new HybridMap2("..\\..\\img\\fonts\\emojidark16.png", new Size(16, 16),
      //  new Size(2, 2), 8, 4, 3.0f, true, null, true, true);

      //var map = new HybridMap2("..\\..\\img\\fonts\\test2.png", new Size(1, 1),
      //  new Size(1, 1), 3, 3, 1.0f, true, null, true, true);

      var map = new HybridMap2("..\\..\\img\\fonts\\c64opt160.png", new Size(8, 8),
        new Size(2, 2), 5, 2, 3.0f, true, Palettes.C64, true, true);
      //var map = new HybridMap2("..\\..\\img\\fonts\\topaz96.gif",
      //   new Size(8, 16), new Size(2, 2), 5, 2, 3.0f, true, wb13palette);
      //var map = new HybridMap2("..\\..\\img\\fonts\\VGA240.png",
      //  new Size(8, 16), new Size(2, 2), 5, 2, 3.0f, true, rgbPrimariesHalftone16);
      t.EndTask();

      //var map = HybridMap2.Load("..\\..\\img\\maps\\mapHybrid-emoji12x12-2x2x6.png", new Size(12, 12), new Size(2, 2), 6);
      t.EnterTask("processing images");

      map.ProcessImage("..\\..\\img\\grad.png", "..\\..\\img\\testdest-grad.png");

      map.ProcessImage("..\\..\\img\\airplane.jpg", "..\\..\\img\\testdest-airplane.png");
      map.ProcessImage("..\\..\\img\\balloon600.jpg", "..\\..\\img\\testdest-balloon600.png");
      map.ProcessImage("..\\..\\img\\david.jpg", "..\\..\\img\\testdest-david.png");
      map.ProcessImage("..\\..\\img\\david192.jpg", "..\\..\\img\\testdest-david192.png");
      //map.PETSCIIIZE("..\\..\\img\\lisa1024.jpg", "..\\..\\img\\testdest-lisa1024.png");
      map.ProcessImage("..\\..\\img\\lisa512.jpg", "..\\..\\img\\testdest-lisa512.png");
      map.ProcessImage("..\\..\\img\\atomium.jpg", "..\\..\\img\\testdest-atomium.png");
      map.ProcessImage("..\\..\\img\\grad.png", "..\\..\\img\\testdest-grad.png");
      map.ProcessImage("..\\..\\img\\circle.png", "..\\..\\img\\testdest-circle.png");

      map.ProcessImage("..\\..\\img\\balloon1200.jpg", "..\\..\\img\\testdest-balloon1200.png");
      t.EndTask();

      Console.WriteLine("Press a key to continue...");
      Console.ReadKey();
    }
  }
}
