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
      t.EnterTask("---");

      ValueSet br = new ValueSet(3, 0);
      br[0] = 4;
      br[1] = 5;
      br[2] = 6;
      var t2 = Utils.Permutate(3, br);
      Utils.AssertSortedByDimension(t2, 2);

      var map = new HybridMap2("..\\..\\img\\fonts\\emojidark12.png", new Size(12, 12), new Size(2, 2), 5, 0.65f, 0.35f, true);

      //var map = HybridMap2.Load("..\\..\\img\\maps\\mapHybrid-emoji12x12-2x2x6.png", new Size(12, 12), new Size(2, 2), 6);
      t.EnterTask("processing images");
      map.PETSCIIIZE("..\\..\\img\\airplane.jpg", "..\\..\\img\\testdest-airplane.png", false);
      map.PETSCIIIZE("..\\..\\img\\circle.png", "..\\..\\img\\testdest-circle.png", false);
      map.PETSCIIIZE("..\\..\\img\\david.jpg", "..\\..\\img\\testdest-david.png", false);
      map.PETSCIIIZE("..\\..\\img\\david192.jpg", "..\\..\\img\\testdest-david192.png", false);
      map.PETSCIIIZE("..\\..\\img\\monalisa1024.jpg", "..\\..\\img\\testdest-monalisa1024.png", false);
      map.PETSCIIIZE("..\\..\\img\\monalisa512.jpg", "..\\..\\img\\testdest-monalisa512.png", false);
      map.PETSCIIIZE("..\\..\\img\\atomium.jpg", "..\\..\\img\\testdest-atomium.png", false);
      map.PETSCIIIZE("..\\..\\img\\balloon1200.jpg", "..\\..\\img\\testdest-balloon1200.png", false);
      map.PETSCIIIZE("..\\..\\img\\balloon600.jpg", "..\\..\\img\\testdest-balloon600.png", false);
      t.EndTask();

      t.EndTask();
      Console.WriteLine("Press a key to continue...");
      Console.ReadKey();
    }
  }
}
