﻿using System;
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
      ColorMine.ColorSpaces.Lab realKeyColor = new ColorMine.ColorSpaces.Lab();

      realKeyColor.L = 0;
      realKeyColor.A = -127;
      realKeyColor.B = -200;
      var rgb = realKeyColor.ToRgb();
      var lab2 = rgb.To<ColorMine.ColorSpaces.Lab>();
      rgb = realKeyColor.ToRgb();

      var rgb2 = new ColorMine.ColorSpaces.Rgb();
      rgb2.R = 255;
      rgb2.G = 255;
      rgb2.B = 255;
      var lab3 = rgb2.To<ColorMine.ColorSpaces.Lab>();

      Timings t = new Timings();
      t.EnterTask("--- MAIN PROCESSING");

      //var map = new HybridMap2("..\\..\\img\\fonts\\pantoneswatches8x8.png", new Size(8, 8),
      //new Size(1, 1), 12, 12, 2.0f, true, null, true, true);

      var map = new HybridMap2("..\\..\\img\\fonts\\emojidark16.png", new Size(16, 16),
        new Size(2, 2), 8, 4, 1.5f, true, null, true, true, 0, new Bayer8DitherProvider(1));


      //var map = new HybridMap2("..\\..\\img\\fonts\\mariotiles4.png", new Size(16, 16),
      //  new Size(2, 2), 16, 2, 1.5f, false, null, true, true, 1);

      //var map = new HybridMap2("..\\..\\img\\fonts\\sonic.png", new Size(24, 32),
      //  new Size(2, 2), 16, 2, 1.5f, false, null, true, true);
      //var map = new HybridMap2("..\\..\\img\\fonts\\mario.png", new Size(24, 24),
      //  new Size(2, 2), 16, 2, 1.5f, false, null, true, true, 1);

      //var map = new HybridMap2("..\\..\\img\\fonts\\test2.png", new Size(1, 1),
      //  new Size(1, 1), 3, 3, 1.0f, true, null, true, true);

      //var map = new HybridMap2("..\\..\\img\\fonts\\c64opt160.png", new Size(8, 8),
      //new Size(2, 2), 8, 4, 1.4f, true, Palettes.C64, true, true);

      //var map = new HybridMap2("..\\..\\img\\fonts\\VGAboxonly45.png", new Size(8, 16),
      //  new Size(2, 2), 8, 4, 1.5f, true, Palettes.RGBPrimariesHalftone16, true, true);

      //var map = new HybridMap2("..\\..\\img\\fonts\\topaz96.gif", new Size(8, 16),
      //  new Size(2, 2), 8, 4, 1.5f, true, Palettes.Workbench4, true, true);
      //var map = new HybridMap2("..\\..\\img\\fonts\\VGA240.png",
      //new Size(8, 16), new Size(2, 2), 8, 4, 1.5f, true, Palettes.Gray3, true, true);
      t.EndTask();

      //var map = HybridMap2.Load("..\\..\\img\\maps\\mapHybrid-emoji12x12-2x2x6.png", new Size(12, 12), new Size(2, 2), 6);
      t.EnterTask("processing images");

      map.ProcessImage("..\\..\\img\\circle.png", "..\\..\\img\\testdest-circle.png");
      map.ProcessImage("..\\..\\img\\sega.png", "..\\..\\img\\testdest-sega.png");
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
