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
#if !DEBUG
      try
      {
#endif
        Log.WriteLine("----------------------------------------");

        args = new string[] { "-?" };

      //// DOS color!
      //args = new string[] {
      //  "-listpalettes",
      //  "-outdir", "C:\\temp",

      //  "-fonttype", "mono",
      //  "-fontimage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\VGA240.png",
      //  "-charsize", "8x16",
      //  "-palette", "RGBPrimariesHalftone16",

      //  "-pf", "yuv",
      //  "-pfargs", "4v2x2+2",
      //  "-partitions", "2x5",
      //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //};


      //// DOS colored grayscale
      //args = new string[] {
      //  "-listpalettes",
      //  "-outdir", "C:\\temp",

      //  "-fonttype", "mono",
      //  "-fontimage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\VGA240.png",
      //  "-charsize", "8x16",
      //  "-palette", "RGBPrimariesHalftone16",

      //  "-pf", "yuv",
      //  "-pfargs", "9v2x2+0",
      //  //"-pfargs", "11v2x2+0",
      //  "-partitions", "3x11",
      //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //};



      //// DOS gray
      //args = new string[] {
      //  "-listpalettes",
      //  "-outdir", "C:\\temp",

      //  "-fonttype", "mono",
      //  "-fontimage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\VGA240.png",
      //  "-charsize", "8x16",
      //  "-palette", "Gray3",
      //  //"-palette", "Workbench314",

      //  "-pf", "yuv",
      //  "-pfargs", "9v3x2+0",
      //  //"-pfargs", "11v2x2+0",
      //  "-partitions", "3x11",
      //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //};

      //// topaz 3.1
      //args = new string[] {
      //  "-outdir", "C:\\temp",
      //  "-partitions", "3x11",
      //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",

      //  "-pf", "yuv",
      //  "-pfargs", "10v3x2+0",

      //  "-fonttype", "mono",
      //  "-fontimage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\topaz96.gif",
      //  "-charsize", "8x16",
      //  //"-palette", "Workbench134",
      //  "-palette", "Workbench314",
      //};

      // emoji grayscale
      //args = new string[] {
      //  "-listpalettes",
      //  "-outdir", "C:\\temp",

      //  "-fonttype", "normal",
      //  "-fontimage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\emojidark12.png",
      //  "-charsize", "12x12",

      //  "-pf", "yuv",
      //  "-pfargs", "9v3x2+0",
      //  //"-pfargs", "11v2x2+0",
      //  "-partitions", "3x11",
      //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //};

      //// mz700 blackandwhite
      //args = new string[] {
      //  "-outdir", "C:\\temp",
      //  "-partitions", "3x11",
      //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",

      //  "-pf", "yuv",
      //  "-pfargs", "12v3x2+0",

      //  "-fonttype", "mono",
      //  "-fontimage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\mz700.png",
      //  "-charsize", "8x8",
      //  "-palette", "BlackAndWhite",
      //};

      //  // C64 grayscale
      //  args = new string[] {
      //  "-listpalettes",
      //  "-outdir", "C:\\temp",

      //  "-fonttype", "mono",
      //  "-fontimage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\c64opt160.png",
      //  "-charsize", "8x8",
      //  "-palette", "C64Gray8",

      //  "-pf", "yuv",
      //  //"-pfargs", "5v3x3+0",
      //  "-pfargs", "11v3x2+0",
      //  "-partitions", "3x11",
      //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //};

      //// EMOJI ONE
      //args = new string[] {
      //  "-outdir", "C:\\temp",
      //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //  "-partitions", "3x6",

      //  "-pf", "yuv",
      //  "-pfargs", "5v2x2+2",

      //  "-fonttype", "fontfamily",
      //  "-fontfile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\EmojiOneColor.otf",
      //  "-fontname", "EmojiOne",
      //  "-trytofit", "true",
      //  "-charsize", "16x16",
      //  "-scale", "1.2",
      //  "-shift", "0x-1",
      //  "-UnicodeGlyphTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\emoji-test.txt",
      //  "-aspecttolerance", "1.0",
      //  "-bgcolor", "#000000",
      //  "-fgcolor", "#ffffff",
      //};

      //// this is pretty well tuned for Noto
      //args = new string[] {
      //  "-listpalettes",
      //  "-outdir", "C:\\temp",

      //  "-fonttype", "fontfamily",
      //  "-fontfile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\NotoColorEmoji.ttf",
      //  //"-fontfamily", "Segoe UI emoji",
      //  "-fontname", "Noto",
      //  // for 24x24, 1.2x, shift 0x-3
      //  // for 12x12, 1.2x, shift 0x-2
      //  "-trytofit", "true",
      //  "-charsize", "16x16",
      //  "-scale", "1.2",
      //  "-shift", "0x-1",
      //  "-UnicodeGlyphTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\emoji-test.txt",
      //  "-aspecttolerance", "1.0",
      //  "-bgcolor", "#000000",
      //  "-fgcolor", "#ffffff",

      //  "-pf", "yuv",
      //  "-pfargs", "5v2x2+2",
      //  "-partitions", "3x6",
      //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //};

      //// Segoe tuned!
      //args = new string[] {
      //    "-outdir", "C:\\temp",

      //    "-fonttype", "fontfamily",
      //    "-fontfamily", "Segoe UI emoji",
      //    "-fontname", "Segoe",
      //    "-charsize", "16x16",
      //    "-scale", "1.3",
      //    "-shift", "-1x-1",
      //    "-trytofit", "true",
      //    "-UnicodeGlyphTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\emoji-test.txt",
      //    "-aspecttolerance", "0.08",
      //    "-bgcolor", "#000000",// this should be 0, because otherwise the "blackest" character is like a dark-skin facepalm glyph with a white background.
      //    "-fgcolor", "#ffffff",

      //    "-pf", "yuv",
      //    "-pfargs", "6v2x2+2",
      //    "-partitions", "3x6",
      //    "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //  };



      //args = new string[] {
      //    "-outdir", "C:\\temp",

      //    "-fonttype", "fontfamily",
      //    "-fontfamily", "Arial Unicode MS",
      //    "-fontname", "Arial",
      //    "-charsize", "16x16",
      //    "-scale", "1.6",
      //    "-shift", "0x0",
      //    "-trytofit", "true",
      //    "-CharListTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\alphanum.txt",
      //    "-bgcolor", "#000000",
      //    "-fgcolor", "#ffffff",

      //    "-pf", "yuv",
      //    "-pfargs", "4v2x2+0",
      //    "-partitions", "2x6",
      //    "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //  };

      // C64 grayscale (using full c64 palette)
      // 7v3x3 takes like 1 hour to process but works well!
      args = new string[] {
        "-outdir", "C:\\temp",
        "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
        "-partitions", "3x6",

        "-pf", "yuv",
        "-pfargs", "7v2x2+0",

          "-fonttype", "mono",
        "-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\c64opt160.png",
        "-charsize", "8x8",
          "-palette", "C64Color",
        };

      //args = new string[] {
      //  "-outdir", "C:\\temp",
      //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //  "-partitions", "3x5",

      //  "-pf", "yuv",
      //  "-pfargs", "9v2x2+2",

      //  "-fonttype", "colorkey",
      //  "-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\mariotiles4.png",
      //  "-charsize", "16x16",
      //  "-topleftpadding", "1",
      //  "-colorkey", "#04c1aa",
      //  "-palette", "MarioBg",
      //};

      PartitionManager partitionManager = new PartitionManager(1, 1);
        IPixelFormatProvider pixelFormat = null;
        IFontProvider fontProvider = null;
        string outputDir = null;
        string processImagesInDir = null;
        int coresToUtilize = System.Environment.ProcessorCount;

        args.ProcessArg(new string[] { "-help", "-?", "-h" }, s => {

          var assem = System.Reflection.Assembly.GetExecutingAssembly();// typeof(PartitionManager).Assembly;
          var ns = assem.EntryPoint.DeclaringType.Namespace;
          using (Stream stream = assem.GetManifestResourceStream(ns + ".cmdhelp.txt"))
          {
            using (var reader = new StreamReader(stream))
            {
              while (!reader.EndOfStream)
              {
                string ln = reader.ReadLine();
                Log.WriteLine(ln);
              }
            }
          }
        });

        args.ProcessArg("-listpalettes", s =>
        {
          Log.WriteLine("Listing palettes:");
          foreach (var p in typeof(Palettes).GetProperties())
          {
            Log.WriteLine("  {0}", p.Name);
          }
        });

        args.ProcessArg("-partitions", s =>
        {
          partitionManager = new PartitionManager(int.Parse(s.Split('x')[0]), int.Parse(s.Split('x')[1]));
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

        map.TestColor(outputDir, ColorFUtils.FromRGB(0, 0, 0));
        map.TestColor(outputDir, ColorFUtils.FromRGB(128, 0, 0));
        map.TestColor(outputDir, ColorFUtils.FromRGB(128, 128, 128));
        map.TestColor(outputDir, ColorFUtils.FromRGB(0, 128, 0));
        map.TestColor(outputDir, ColorFUtils.FromRGB(0, 0, 128));
        map.TestColor(outputDir, ColorFUtils.FromRGB(255, 255, 255));

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

#if !DEBUG
      }
      catch (Exception e)
      {
        Log.WriteLine("Exception occurred:\r\n{0}", e);
      }
#endif
    }
  }
}
