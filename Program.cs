// NB:
// partitioning does cause issues at high partitions. it shows when you start seeing good ref images but the converted images have noisy black & white.
// choosing partition size is important. if you have an even # of valuespercomponent, then you should probably have odd partitioning.
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
      //List<Color> s = new List<Color>();

      //var b = Palettes.C64.OrderBy(c => {
      //  var rgb = new ColorMine.ColorSpaces.Rgb();
      //  rgb.R = c.R;
      //  rgb.G = c.G;
      //  rgb.B = c.B;
      //  var hsl = rgb.To<ColorMine.ColorSpaces.Hsl>();
      //  return hsl.L;
      //});
      //foreach (var c in b)
      //{
      //  var rgb = new ColorMine.ColorSpaces.Rgb();
      //  rgb.R = c.R;
      //  rgb.G = c.G;
      //  rgb.B = c.B;
      //  var hsl = rgb.To<ColorMine.ColorSpaces.Hsl>();
      //  Console.WriteLine("Color.FromArgb({0}, {1}, {2}), // luminance: {3}",
      //    c.R, c.G, c.B, hsl.L);
      //}

      //HugeArray x = new HugeArray(2);
      //Random r = new Random();
      //for(int i = 0; i < 100; ++ i)
      //{
      //  Mapping m;
      //  m.dist = r.NextDouble();
      //  m.icharInfo = 0;
      //  m.imapKey = 0;
      //  Console.WriteLine("{0}", m.dist);
      //  x.Add(m);
      //}

      //Console.WriteLine("-----");
      //x.SortAndPrune(1);
      //var it = x.BeginIteration();

      //foreach(var m in it)
      //{
      //  Console.WriteLine("{0}", m.dist);
      //}

      Timings t = new Timings();
      t.EnterTask("--- MAIN PROCESSING");

      var emoji12 = new FontProvider("..\\..\\img\\fonts\\emojidark12.png", new Size(12, 12));//, dither: new Bayer8DitherProvider(.1));
      var emoji16 = new FontProvider("..\\..\\img\\fonts\\emojidark16.png", new Size(16, 16));//, dither: new Bayer8DitherProvider(.1));
      var emoji24 = new FontProvider("..\\..\\img\\fonts\\emojidark24.png", new Size(24, 24));//, dither: new Bayer8DitherProvider(.1));
      var emoji32 = new FontProvider("..\\..\\img\\fonts\\emojidark32.png", new Size(32, 32));//, dither: new Bayer8DitherProvider(.1));
      var c64font = new MonoPaletteFontProvider("..\\..\\img\\fonts\\c64opt160.png", new Size(8, 8), Palettes.C64Grays);
      var mzFont = new MonoPaletteFontProvider("..\\..\\img\\fonts\\mz700.png", new Size(16, 16), Palettes.BlackAndWhite);
      var topaz = new MonoPaletteFontProvider("..\\..\\img\\fonts\\topaz96.gif", new Size(8, 16), Palettes.Workbench134);

      var noPartition = new PartitionManager(1, 1);
      var partition = new PartitionManager(3, 9);// i have the feeling partitioning in 3 is actually better

      // if we have a large array, we could do 3^4x4+0 mapping. but not before then.

      var map = new HybridMap2(
        emoji16,
        partition,
        new NaiveYUVPixelFormat(7, new Size(2,2), true), false, true, 1000000);

      t.EndTask();

      //var emoji12ShouldBeBlack = new Point(468, 264);
      //map.TestColor(ColorFUtils.FromRGB(0, 0, 0), emoji12ShouldBeBlack);//, new Point(468, 264), new Point(288, 0), new Point(0, 264));
      //map.TestColor(ColorFUtils.FromRGB(128, 0, 0));
      //map.TestColor(ColorFUtils.FromRGB(128, 128, 128));
      //map.TestColor(ColorFUtils.FromRGB(0, 128, 0), new Point(468, 264));
      //map.TestColor(ColorFUtils.FromRGB(0, 0, 128), new Point(372, 252));
      //map.TestColor(ColorFUtils.FromRGB(255, 255, 255));//, new Point(385, 277));

      t.EnterTask("processing images");

      ////Bitmap testBmp = new Bitmap(100, 100);
      ////using (Graphics g = Graphics.FromImage(testBmp))
      ////{
      ////  g.Clear(Color.Black);
      ////}

      ////map.ProcessImageUsingRef("..\\..\\img\\BLACK.png", testBmp, testBmp, "..\\..\\img\\testdest-BLACK.png");

      //map.ProcessImageUsingRef("..\\..\\img\\grad3.png", "..\\..\\img\\testdest-grad3.png");
      //map.ProcessImageUsingRef("..\\..\\img\\circle.png", "..\\..\\img\\testdest-circle.png");
      //map.ProcessImageUsingRef("..\\..\\img\\grad.png", "..\\..\\img\\testdest-grad.png");
      //map.ProcessImageUsingRef("..\\..\\img\\airplane.jpg", "..\\..\\img\\testdest-airplane.png");
      //map.ProcessImageUsingRef("..\\..\\img\\balloon600.jpg", "..\\..\\img\\testdest-balloon600.png");
      //map.ProcessImageUsingRef("..\\..\\img\\david.jpg", "..\\..\\img\\testdest-david.png");
      //map.ProcessImageUsingRef("..\\..\\img\\david192.jpg", "..\\..\\img\\testdest-david192.png");
      map.ProcessImageUsingRef("..\\..\\img\\lisa1024.jpg", "..\\..\\img\\testdest-lisa1024.png");
      //map.ProcessImageUsingRef("..\\..\\img\\lisa512.jpg", "..\\..\\img\\testdest-lisa512.png");
      //map.ProcessImageUsingRef("..\\..\\img\\atomium.jpg", "..\\..\\img\\testdest-atomium.png");
      //map.ProcessImageUsingRef("..\\..\\img\\grad.png", "..\\..\\img\\testdest-grad.png");
      //map.ProcessImageUsingRef("..\\..\\img\\grad2.png", "..\\..\\img\\testdest-grad2.png");
      //map.ProcessImageUsingRef("..\\..\\img\\gtorus.png", "..\\..\\img\\testdest-gtorus.png");
      //map.ProcessImageUsingRef("..\\..\\img\\balloon1200.jpg", "..\\..\\img\\testdest-balloon1200.png");

      t.EndTask();

      Console.WriteLine("Press a key to continue...");
      Console.ReadKey();
    }
  }
}
