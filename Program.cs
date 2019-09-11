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
      // values per tile is actually very important. 2x2x8 is acceptable, but 2x2x12 is much better. 2x2x16 is slightly better but things get out of hand.
      // values per tile contributes to the overall varied amount of characters in the resulting image.
      // 
      // the tile size contributes very much to resulting detail, but the map grows so big it's hard to use anything past 2x2.
      // 2x2 is already very good though.

      // everything i've checked, 2x2x16 is the best possible combo.

      //PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\c64_uppercase_norm.png", new Size(8, 8), new Size(2, 2), 16);
      PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\c64_all_onlyblock.gif", new Size(8, 8), new Size(2, 2), 16);
      //PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\c64_all.gif", new Size(8, 8), new Size(1, 1), 256);
      //PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\test-16blocks8x8.png", new Size(8, 8), new Size(2, 2), 2);
      //PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\topaznew.gif", new Size(8, 16), new Size(2, 2), 16);
      //PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\topaznew-rvs.gif", new Size(8, 16), new Size(2, 2), 16);
      //PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\Vga-rom-font.png", new Size(8, 16), new Size(2, 2), 16);
      //PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\Vga-rom-font-RVS.png", new Size(8, 16), new Size(2, 2), 16);
      //PetsciiMap map = new PetsciiMap("..\\..\\img\\fonts\\VGA-Boxonly.png", new Size(8, 16), new Size(2, 2), 16);

      map.PETSCIIIZE("..\\..\\img\\airplane.jpg", "..\\..\\img\\testdest-airplane.png", false);
      map.PETSCIIIZE("..\\..\\img\\circle.png", "..\\..\\img\\testdest-circle.png", false);
      map.PETSCIIIZE("..\\..\\img\\david.jpg", "..\\..\\img\\testdest-david.png", false);
      map.PETSCIIIZE("..\\..\\img\\david192.jpg", "..\\..\\img\\testdest-david192.png", false);
      map.PETSCIIIZE("..\\..\\img\\monalisa1024.jpg", "..\\..\\img\\testdest-monalisa1024.png", false);
      map.PETSCIIIZE("..\\..\\img\\monalisa512.jpg", "..\\..\\img\\testdest-monalisa512.png", false);
      map.PETSCIIIZE("..\\..\\img\\atomium.jpg", "..\\..\\img\\testdest-atomium.png", false);
      map.PETSCIIIZE("..\\..\\img\\balloon1200.jpg", "..\\..\\img\\testdest-balloon1200.png", false);
      map.PETSCIIIZE("..\\..\\img\\balloon600.jpg", "..\\..\\img\\testdest-balloon600.png", false);
    }
  }
}
