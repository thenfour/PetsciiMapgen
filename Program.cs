// NB:
// partitioning does cause issues at high partitions. it shows when you start seeing good ref images but the converted images have noisy black & white.
// choosing partition size is important.
// you want partition boundaries to fall on discrete value boundaries as well. with 2 discrete values, [0,1], then the midpoint is .5,
// where you want the partition seam. thus, 2 partitions.
// that's a tempting approach but distances then are more like taxicab distances. you will still get plenty of error because of
// distance in other dimensions. so at least make it a divisor of discrete values.
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
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Media;

namespace PetsciiMapgen
{
  class Program
  {
    static void Main(string[] args)
    {
      Log.WriteLine("----------------------------------------");

      args = new string[] {
        "-listpalettes",
        "-outdir", "C:\\temp",

        "-fonttype", "fontfamily",
        "-fontfamily", "Segoe UI emoji",
        "-charsize", "12x12",
        "-scale", "1.09",
        "-UnicodeGlyphTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\emoji-data-v12.txt",
        "-aspecttolerance", "0.15",
        "-bgcolor", "#000000",
        "-fgcolor", "#000000",

        "-pf", "yuv",
        "-pfargs", "255v1x1+0",
        "-partitions", "1x1",
        "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      };

      //args = new string[] {
      //  //"-listpalettes",
      //  "-outdir", "C:\\temp\\xyz",
      //  "-fonttype", "Normal",
      //  "-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\c64opt160.png",
      //  "-charsize", "8x8",
      //  "-pf", "yuv",
      //  "-pfargs", "7v2x2+2",
      //  "-partitions", "2x3",
      //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //};

      //args = new string[] {
      //  "-listpalettes",
      //  "-outdir", "C:\\temp\\xyz",
      //  "-fonttype", "colorkey",
      //  "-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\mariotiles4.png",
      //  "-charsize", "16x16",
      //  "-topleftpadding", "1",
      //  "-colorkey", "#04c1aa",
      //  "-palette", "MarioBg",
      //  "-pf", "yuv",
      //  "-pfargs", "5v3x3+0",
      //  "-partitions", "3x3",
      //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //};

      PartitionManager partitionManager = new PartitionManager(1, 1);
      IPixelFormatProvider pixelFormat = null;
      IFontProvider fontProvider = null;
      string outputDir = null;
      string processImagesInDir = null;
      int coresToUtilize = System.Environment.ProcessorCount;

      args.ProcessArg("-listpalettes", s =>
      {
        Log.WriteLine("Listing palettes:");
        foreach(var p in typeof(Palettes).GetProperties())
        {
          Log.WriteLine("  {0}", p.Name);
        }
      });

      args.ProcessArg("-partitions", s =>
      {
        partitionManager = new PartitionManager(int.Parse(s.Split('x')[0]), int.Parse(s.Split('x')[1]));
      });
      args.ProcessArg("-pf", s =>
      {
        switch (s.ToLowerInvariant())
        {
          case "yuv":
            pixelFormat = NaiveYUVPixelFormat.ProcessArgs(args);
            break;
          case "hsl":
            pixelFormat = HSLPixelFormat.ProcessArgs(args);
            break;
          case "lab":
            pixelFormat = LABPixelFormat.ProcessArgs(args);
            break;
          default:
            throw new Exception("Unknown pixel format: " + s);
        }
      });

      FontFamilyFontProvider fontFamilyProvider = null;

      args.ProcessArg("-fonttype", s =>
      {
        switch (s.ToLowerInvariant())
        {
          case "mono":
            fontProvider = MonoPaletteFontProvider.ProcessArgs(args);
            break;
          case "normal":
            fontProvider = FontProvider.ProcessArgs(args);
            break;
          case "colorkey":
            fontProvider = ColorKeyFontProvider.ProcessArgs(args);
            break;
          case "fontfamily":
            fontProvider = fontFamilyProvider = FontFamilyFontProvider.ProcessArgs(args);
            break;
          default:
            throw new Exception("Unknown font type: " + s);
        }
      });

      args.ProcessArg("-outdir", o =>
      {
        outputDir = o;
      });

      args.ProcessArg("-processImagesInDir", o =>
      {
        processImagesInDir = o;
      });

      args.ProcessArg("-cores", o =>
      {
        int a = int.Parse(o);
        if (a < 1)
          a = System.Environment.ProcessorCount - a;
        coresToUtilize = a;
      });

      if (outputDir == null)
      {
        Log.WriteLine("No output directory was specified.");
        return;
      }
      outputDir = System.IO.Path.GetFullPath(outputDir);
      Log.WriteLine("Output directory: {0}", outputDir);

      if (!System.IO.Directory.Exists(outputDir))
      {
        Log.WriteLine("Output directory doesn't exist.");
        return;
      }

      // emoji12-C64_YUV-2v5x5+2
      if (fontProvider == null)
      {
        Log.WriteLine("Font information not specified.");
        return;
      }
      if (pixelFormat == null)
      {
        Log.WriteLine("Pixel format not specified.");
        return;
      }
      if (partitionManager == null)
      {
        Log.WriteLine("Space partitioning unspecified");
        return;
      }
      string configTag = string.Format("{0}_{1}_{2}", fontProvider.DisplayName, pixelFormat.PixelFormatString, partitionManager);
      outputDir = System.IO.Path.Combine(outputDir, configTag);
      Log.WriteLine("Creating directory: {0}", outputDir);
      System.IO.Directory.CreateDirectory(outputDir);

      string logPath = System.IO.Path.Combine(outputDir, "log.txt");

      Log.SetLogFile(logPath);

      Timings t = new Timings();
      t.EnterTask("--- MAP GENERATION");

      string mapFullPath = System.IO.Path.Combine(outputDir, string.Format("mapfull_{0}.png", configTag));
      string mapRefPath = System.IO.Path.Combine(outputDir, string.Format("mapref_{0}.png", configTag));
      string mapFontPath = System.IO.Path.Combine(outputDir, string.Format("mapfont_{0}.png", configTag));

      var map = new HybridMap2(fontProvider, partitionManager, pixelFormat,
        mapFullPath,
        mapRefPath,
        mapFontPath,
        coresToUtilize);

      if (fontFamilyProvider != null)
      {
        string fontImgPath = System.IO.Path.Combine(outputDir, "font.png");
        fontFamilyProvider.SaveFontImage(fontImgPath);
      }

      t.EndTask();

      ////var emoji12ShouldBeBlack = new Point(468, 264);
      //map.TestColor(outputDir, ColorFUtils.FromRGB(0, 0, 0));//, emoji12ShouldBeBlack);//, new Point(468, 264), new Point(288, 0), new Point(0, 264));
      //map.TestColor(outputDir, ColorFUtils.FromRGB(128, 0, 0));
      //map.TestColor(outputDir, ColorFUtils.FromRGB(128, 128, 128));
      //map.TestColor(outputDir, ColorFUtils.FromRGB(0, 128, 0), new Point(468, 264));
      //map.TestColor(outputDir, ColorFUtils.FromRGB(0, 0, 128), new Point(372, 252));
      //map.TestColor(outputDir, ColorFUtils.FromRGB(255, 255, 255));//, new Point(385, 277));

      if (processImagesInDir != null && System.IO.Directory.Exists(processImagesInDir))
      {
        t.EnterTask("processing images");

        var files = System.IO.Directory.EnumerateFiles(processImagesInDir, "*", System.IO.SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
          //Log.WriteLine("Processing {0}", file);
          string destFile = string.Format("test-{0}.png", System.IO.Path.GetFileNameWithoutExtension(file));
          string destfullp = System.IO.Path.Combine(outputDir, destFile);
          var rv = map.ProcessImageUsingRef(mapRefPath, mapFontPath, file, destfullp);
          if (fontFamilyProvider != null)
          {
            string str = fontFamilyProvider.ConvertToText(rv);
            string txtpath = System.IO.Path.Combine(outputDir, string.Format("test-{0}.txt", System.IO.Path.GetFileNameWithoutExtension(file)));
            System.IO.File.WriteAllText(txtpath, str);
          }
        }

        t.EndTask();
      }

    }
  }
}
