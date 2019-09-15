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

      var map = new HybridMap2("..\\..\\img\\fonts\\emojidark12.png",
        new Size(12, 12),
        new Size(2, 2), 6, 2,
        4.0f, true);
      t.EndTask();

      //var map = HybridMap2.Load("..\\..\\img\\maps\\mapHybrid-emoji12x12-2x2x6.png", new Size(12, 12), new Size(2, 2), 6);
      t.EnterTask("processing images");
      map.PETSCIIIZE("..\\..\\img\\grad.png", "..\\..\\img\\testdest-grad.png");
      map.PETSCIIIZE("..\\..\\img\\airplane.jpg", "..\\..\\img\\testdest-airplane.png");
      map.PETSCIIIZE("..\\..\\img\\circle.png", "..\\..\\img\\testdest-circle.png");
      map.PETSCIIIZE("..\\..\\img\\david.jpg", "..\\..\\img\\testdest-david.png");
      //map.PETSCIIIZE("..\\..\\img\\david192.jpg", "..\\..\\img\\testdest-david192.png");
      //map.PETSCIIIZE("..\\..\\img\\lisa1024.jpg", "..\\..\\img\\testdest-lisa1024.png");
      map.PETSCIIIZE("..\\..\\img\\lisa512.jpg", "..\\..\\img\\testdest-lisa512.png");
      map.PETSCIIIZE("..\\..\\img\\atomium.jpg", "..\\..\\img\\testdest-atomium.png");
      map.PETSCIIIZE("..\\..\\img\\balloon1200.jpg", "..\\..\\img\\testdest-balloon1200.png");
      //map.PETSCIIIZE("..\\..\\img\\balloon600.jpg", "..\\..\\img\\testdest-balloon600.png");
      t.EndTask();

      Console.WriteLine("Press a key to continue...");
      Console.ReadKey();
    }
  }
}
