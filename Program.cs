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

      var c64Palette = new Color[] {
        Color.FromArgb(  0,   0,   0),// black
        Color.FromArgb( 98,  98,  98),//gray1
        Color.FromArgb(137, 137, 137),// gray2
        Color.FromArgb(173, 173, 173),//gray3
        Color.FromArgb(255, 255, 255),//white

        Color.FromArgb(159,  78,  68),// brick red
        Color.FromArgb(203, 126, 117),// light red
        Color.FromArgb(109,  84,  18),// dkbrown
        Color.FromArgb(161, 104,  60),// light brown
        
        Color.FromArgb(201, 212, 135),// yellowish
        Color.FromArgb(154, 226, 155),// bright green
        Color.FromArgb( 92, 171,  94),// darker green
        Color.FromArgb(106, 191, 198),// cyan
        
        Color.FromArgb(136, 126, 203),// light purple
        Color.FromArgb( 80,  69, 155),// dark purple
        Color.FromArgb(160,  87, 163),// violet

        //Color.FromArgb(  0,   0,   0),
        //Color.FromArgb(255, 255, 255),
        //Color.FromArgb(136,   0,   0),
        //Color.FromArgb(170, 255, 238),
        //Color.FromArgb(204,  68, 204),
        //Color.FromArgb(  0, 204,  85),
        //Color.FromArgb(  0,   0, 170),
        //Color.FromArgb(238, 238, 119),
        //Color.FromArgb(221, 136,  85),
        //Color.FromArgb(102,  68,   0),
        //Color.FromArgb(255, 119, 119),
        //Color.FromArgb( 51,  51,  51),
        //Color.FromArgb(119, 119, 119),
        //Color.FromArgb(170, 255, 102),
        //Color.FromArgb(  0, 136, 255),
        //Color.FromArgb(187, 187, 187),
      };
      var wb13palette = new Color[] {
        Color.FromArgb(  0,   1,  32),
        Color.FromArgb(248, 248, 248),
        Color.FromArgb(  0,  86, 173),
        Color.FromArgb(255,  138, 0 ),
      };
      var rgbPrimariesHalftone16 = new Color[] { // RGB primaries + halftone
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0,255),
        Color.FromArgb(0, 255,0),
        Color.FromArgb(0, 255,255),
        Color.FromArgb(255, 0,0),
        Color.FromArgb(255, 0,255),
        Color.FromArgb(255, 255,0),
        Color.FromArgb(255, 255,255),

        Color.FromArgb(128, 128, 128),
        Color.FromArgb(0, 0,128),
        Color.FromArgb(0, 128,0),
        Color.FromArgb(0, 128,128),
        Color.FromArgb(128, 0,0),
        Color.FromArgb(128, 0,128),
        Color.FromArgb(128, 128,0),
        Color.FromArgb(192, 192,192),
      };

      //var map = new HybridMap2("..\\..\\img\\fonts\\emojidark12.png",
      //  new Size(12, 12), new Size(2, 2), 4, 2, 4.0f, true, c64Palette);
      //var map = new HybridMap2("..\\..\\img\\fonts\\emojidark16.png",
      //new Size(16, 16), new Size(3, 3), 4, 2, 4.0f, false, null);

      var map = new HybridMap2("..\\..\\img\\fonts\\c64opt160.png",
        new Size(8, 8), new Size(2, 2), 5, 3, 1.0f, true, c64Palette, true, true);

      //var map = new HybridMap2("..\\..\\img\\fonts\\topaz96.gif",
      //   new Size(8, 16), new Size(2, 2), 5, 2, 3.0f, true, wb13palette);
      //var map = new HybridMap2("..\\..\\img\\fonts\\VGA240.png",
      //  new Size(8, 16), new Size(2, 2), 5, 2, 3.0f, true, rgbPrimariesHalftone16);
      t.EndTask();

      //var map = HybridMap2.Load("..\\..\\img\\maps\\mapHybrid-emoji12x12-2x2x6.png", new Size(12, 12), new Size(2, 2), 6);
      t.EnterTask("processing images");
      map.ProcessImage("..\\..\\img\\balloon600.jpg", "..\\..\\img\\testdest-balloon600.png");
      map.ProcessImage("..\\..\\img\\david.jpg", "..\\..\\img\\testdest-david.png");
      map.ProcessImage("..\\..\\img\\david192.jpg", "..\\..\\img\\testdest-david192.png");
      //map.PETSCIIIZE("..\\..\\img\\lisa1024.jpg", "..\\..\\img\\testdest-lisa1024.png");
      map.ProcessImage("..\\..\\img\\lisa512.jpg", "..\\..\\img\\testdest-lisa512.png");
      map.ProcessImage("..\\..\\img\\atomium.jpg", "..\\..\\img\\testdest-atomium.png");
      map.ProcessImage("..\\..\\img\\grad.png", "..\\..\\img\\testdest-grad.png");
      map.ProcessImage("..\\..\\img\\airplane.jpg", "..\\..\\img\\testdest-airplane.png");
      map.ProcessImage("..\\..\\img\\circle.png", "..\\..\\img\\testdest-circle.png");

      map.ProcessImage("..\\..\\img\\balloon1200.jpg", "..\\..\\img\\testdest-balloon1200.png");
      t.EndTask();

      Console.WriteLine("Press a key to continue...");
      Console.ReadKey();
    }
  }
}
