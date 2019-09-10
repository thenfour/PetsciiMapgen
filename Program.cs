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
      //PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\c64_uppercase_norm.png", new Size(8, 8), new Size(2, 2), 16);
      PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\c64_all.gif", new Size(8, 8), new Size(2, 2), 16);
      //PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\topaznew.gif", new Size(8, 16), new Size(2, 2), 16);
      //PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\Vga-rom-font.png", new Size(8, 16), new Size(2, 2), 16);
      //PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\Vga-rom-font-RVS.png", new Size(8, 16), new Size(2, 2), 16);

      //map.PETSCIIIZE("..\\..\\img\\airplane.jpg", "..\\..\\img\\testdest-airplane.png");
      //map.PETSCIIIZE("..\\..\\img\\david.jpg", "..\\..\\img\\testdest-david.png");
      //map.PETSCIIIZE("..\\..\\img\\david192.jpg", "..\\..\\img\\testdest-david192.png");
      //map.PETSCIIIZE("..\\..\\img\\monalisa1024.jpg", "..\\..\\img\\testdest-monalisa1024.png");
      map.PETSCIIIZE("..\\..\\img\\monalisa512.jpg", "..\\..\\img\\testdest-monalisa512.png");
      //map.PETSCIIIZE("..\\..\\img\\atomium.jpg", "..\\..\\img\\testdest-atomium.png");
      //map.PETSCIIIZE("..\\..\\img\\balloon1200.jpg", "..\\..\\img\\testdest-balloon1200.png");
      //map.PETSCIIIZE("..\\..\\img\\balloon600.jpg", "..\\..\\img\\testdest-balloon600.png");
    }
  }
}
