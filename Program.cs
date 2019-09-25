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

    enum MapSource
    {
      Create,
      Load
    }
    static void Main(string[] args)
    {
#if !DEBUG
      try
      {
#else
      {
#endif
        Log.WriteLine("----------------------------------------");

        //args = new string[] { "-?" };

        //args = new string[]
        //{
        //  //"-calcn", "50000000",
        //  //"-partitions", "4x11",
        //  "-outdir", "C:\\temp",
        //  "-pf", "yuv5",
        //  "-pfargs", "12v5+0",
        //  "-partitions", "2x8",

        //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
        //  //"-processImage", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages\black.png",
        //  //"-processImage", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages\white.png",
        //  //"-processImage", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages\portrait.jpg",
        //  //
        //  //"-testpalette", "RGBPrimariesHalftone16",

        //  "-fonttype", "mono",
        //  "-fontimage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\mz700.png",
        //  "-charsize", "8x8",
        //  "-palette", "BlackAndWhite",
        //};

        //// create mz700 blackandwhite
        //args = new string[] {
        //  "-outdir", "C:\\temp",
        //  "-partitions", "4x11",
        //  "-createmap",

        //  "-pf", "yuv",
        //  "-pfargs", "8v3x3+0",

        //  "-fonttype", "mono",
        //  "-fontimage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\mz700.png",
        //  "-charsize", "8x8",
        //  "-palette", "BlackAndWhite",
        //};

        //// LOAD mz700 blackandwhite
        //args = new string[] {
        //  "-loadmap", @"C:\temp\mz700-BlackAndWhite_YUV8v3x3+0_p4x11",
        //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
        //  "-testpalette", "RGBPrimariesHalftone16",
        //};

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

        //// C64 grayscale
        //args = new string[] {
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
        //  "-testpalette", "C64Color"
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

        //// C64 grayscale (using full c64 palette)
        //// 7v3x3 takes like 1 hour to process but works well!
        //args = new string[] {
        //  "-outdir", "C:\\temp",
        //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
        //  "-partitions", "3x6",

        //  "-pf", "yuv",
        //  "-pfargs", "13v2x2+0",

        //    "-fonttype", "mono",
        //  "-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\c64opt160.png",
        //  "-charsize", "8x8",
        //    "-palette", "C64Color",
        //  };

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
        List<string> processImages = new List<string>();
        int coresToUtilize = System.Environment.ProcessorCount;
        List<System.Drawing.Color> testColors = new List<System.Drawing.Color>();


        args.ProcessArg(new string[] { "-help", "-?", "-h" }, s =>
        {
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

        args.ProcessArg("-argsfile", s =>
        {
          Log.WriteLine("Reading args from file: {0}" + s);
          var lines = System.IO.File.ReadAllLines(s)
            .Select(l => l.Split('#')[0]) // remove comments
            .Where(l => !string.IsNullOrWhiteSpace(l)); // remove empty lines
          args = lines.Concat(args).ToArray();
        });

        args.ProcessArg("-outdir", o =>
        {
          outputDir = o;
        });

        MapSource mapSource = MapSource.Create;

        args.ProcessArg("-loadmap", s => 
        {
          mapSource = MapSource.Load;
          // if you're loading, then we want to process the args from that directory.
          outputDir = System.IO.Path.GetDirectoryName(s);
          string argspath = System.IO.Path.Combine(s, "args.txt");
          var lines = System.IO.File.ReadAllLines(argspath)
            .Select(l => l.Split('#')[0]) // remove comments
            .Where(l => !string.IsNullOrWhiteSpace(l)); // remove empty lines
          args = lines.Concat(args).ToArray();
        });

        args.ProcessArg("-testpalette", s => {
          var palette = (System.Drawing.Color[])typeof(Palettes).GetProperty(s).GetValue(null);
          testColors.AddRange(palette);
        });
        args.ProcessArg("-testcolor", s => {
          testColors.Add(System.Drawing.ColorTranslator.FromHtml(s));
        });

        args.ProcessArg("-partitions", s =>
        {
          partitionManager = new PartitionManager(int.Parse(s.Split('x')[0]), int.Parse(s.Split('x')[1]));
        });

        args.ProcessArg("-processImagesInDir", o =>
        {
          var files = System.IO.Directory.EnumerateFiles(o, "*", System.IO.SearchOption.TopDirectoryOnly);
          foreach (var file in files)
          {
            processImages.Add(file);
          }
        });

        args.ProcessArg("-processImage", o =>
        {
          processImages.Add(o);
        });

        args.ProcessArg("-cores", o =>
        {
          int a = int.Parse(o);
          if (a < 1)
            a = System.Environment.ProcessorCount - a;
          coresToUtilize = a;
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

        args.ProcessArg("-pf", s =>
        {
          switch (s.ToLowerInvariant())
          {
            case "hsl":
              pixelFormat = HSLPixelFormat.ProcessArgs(args);
              break;
            case "lab":
              pixelFormat = LABPixelFormat.ProcessArgs(args);
              break;
            default:
            case "yuv":
              pixelFormat = NaiveYUVPixelFormat.ProcessArgs(args);
              break;
            case "yuv5":
              pixelFormat = NaiveYUV5PixelFormat.ProcessArgs(args, fontProvider);
              break;
          }
        });

        if (pixelFormat == null)
        {
          pixelFormat = NaiveYUVPixelFormat.ProcessArgs(args);
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

        args.ProcessArg("-calcn", s => {
          //Size luma = new Size(1, 1);
          //bool useChroma = false;
          //ulong partB = 1, partD = 1;

          ulong maxMapKeys = ulong.Parse(s);

          //args.ProcessArg("-pfargs", o => {
          //  Utils.ParsePFArgs(o, out int valuesPerComponent, out useChroma, out luma);
          //});

          //args.ProcessArg("-partitions", o => {
          //  partB = ulong.Parse(o.Split('x')[0]);
          //  partD = ulong.Parse(o.Split('x')[1]);
          //});

          partitionManager.Init(pixelFormat);

          //ulong partitionCount = (ulong)Utils.Pow((long)partB, (uint)partD);
          ulong partitionCount = (ulong)partitionManager.PartitionCount;
          Log.WriteLine("Partition count: {0:N0}", partitionCount);

          // so the thing about partition count. You can't just divide by partition count,
          // because in deeper levels most partitions are simply unused / empty.
          // a decent conservative approximation is to take the first N levels
          partitionCount = (ulong)Math.Pow(partitionManager.PartitionsPerDimension, 2.5);// n = 2.5
          Log.WriteLine("Adjusted partition count: {0:N0}", partitionCount);
          Log.WriteLine("Charset count: {0:N0}", fontProvider.CharCount);
          Log.WriteLine("Cores to utilize: {0:N0}", coresToUtilize);
          //int dimensions = Utils.Product(luma) + (useChroma ? 2 : 0);
          Log.WriteLine("Luma + chroma components: {0:N0}", pixelFormat.DimensionCount);

          // figure out valuespercomponent in order to not overflow our huge mapping array.
          // maximum number of mapkeys is int.maxvalue
          // maximum number of mappings is (int.MaxValue * cores)
          // theoretical mappings is charcount * mapsize
          // mapsize = N^dimensions
          // actual mappings will divide that by partition count more-or-less

          ulong NbasedOnMapSize = (ulong)Math.Floor(Math.Pow(maxMapKeys, 1.0 / pixelFormat.DimensionCount));

          // mappings overflow when there are so many chars in the font that
          // it can't be held in memory.

          // take 80% for safety.
          ulong charCount = (ulong)fontProvider.CharCount;
          ulong maxTheoreticalMappingCount = (ulong)coresToUtilize * (ulong)UInt32.MaxValue * (ulong)partitionCount * 3 / 4;
          ulong maxKeyCount = maxTheoreticalMappingCount / charCount;
          ulong NbasedOnMappings = (ulong)Math.Floor(Math.Pow(maxKeyCount, 1.0 / pixelFormat.DimensionCount));

          Log.WriteLine("Based on the map size requested, N can be as much as        {0:N0}", NbasedOnMapSize);
          Log.WriteLine("Based on the charset and mapping array, N can be as much as {0:N0}", NbasedOnMappings);
          ulong m = Math.Min(NbasedOnMapSize, NbasedOnMappings);

          ulong keyCount = (ulong)Math.Pow(m, pixelFormat.DimensionCount);
          ulong sizeofMapping = (ulong)Marshal.SizeOf<Mapping>();
          Log.WriteLine("Which will use {0:N0} of memory for mappings", keyCount * charCount / partitionCount * sizeofMapping);

          ulong maxmem = Utils.GbToBytes(150);
          args.ProcessArg("-maxmemgb", gb => {
            maxmem = Utils.GbToBytes(ulong.Parse(gb));
          });
          Log.WriteLine("Max memory to use: {0:N0}", maxmem);

          // reduce keycount to conform.
          maxKeyCount = maxmem / (charCount * sizeofMapping / partitionCount);
          ulong NbasedOnMem = (ulong)Math.Floor(Math.Pow(maxKeyCount, 1.0 / pixelFormat.DimensionCount));
          Log.WriteLine("Based on memory usage, N can be as much as                  {0:N0}", NbasedOnMem);

          m = Math.Min(m, NbasedOnMem);
          Log.WriteLine("======================");
          Log.WriteLine("== THEREFORE, use N={0:N0}", m);
          Log.WriteLine("======================");

          System.Environment.Exit(0);
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

        string configTag = string.Format("{0}_{1}_{2}", fontProvider.DisplayName, pixelFormat.PixelFormatString, partitionManager);
        outputDir = System.IO.Path.Combine(outputDir, configTag);
        Log.WriteLine("Creating directory: {0}", outputDir);
        System.IO.Directory.CreateDirectory(outputDir);

        string logPath = System.IO.Path.Combine(outputDir, "log.txt");

        Log.SetLogFile(logPath);

        string infopath = System.IO.Path.Combine(outputDir, "args.txt");
        using (var infoFile = new StreamWriter(infopath))
        {
          foreach (var arg in args)
          {
            infoFile.WriteLine(arg);
          }
        }




        Timings t = new Timings();
        t.EnterTask("--- MAP GENERATION");

        string mapFullPath = System.IO.Path.Combine(outputDir, string.Format("mapfull_{0}.png", configTag));
        string mapRefPath = System.IO.Path.Combine(outputDir, string.Format("mapref_{0}.png", configTag));
        string mapFontPath = System.IO.Path.Combine(outputDir, string.Format("mapfont_{0}.png", configTag));

        HybridMap2 map = null;

        switch(mapSource)
        {
          case MapSource.Create:
            map = new HybridMap2(fontProvider, partitionManager, pixelFormat,
              mapFullPath,
              mapRefPath,
              mapFontPath,
              coresToUtilize);
            break;
          case MapSource.Load:
            map = HybridMap2.LoadFromDisk(outputDir, fontProvider, pixelFormat);
            break;
        }

        if (fontFamilyProvider != null)
        {
          string fontImgPath = System.IO.Path.Combine(outputDir, "font.png");
          fontFamilyProvider.SaveFontImage(fontImgPath);
        }

        t.EndTask();

        t.EnterTask("processing images");

        foreach (var c in testColors)
        {
          map.TestColor(outputDir, ColorFUtils.From(c));
        }


        using (var refMapImage = new Bitmap(mapRefPath))
        using (var refFontImage = new Bitmap(mapFontPath))
        {
          foreach (var file in processImages)
          {
            Log.WriteLine("Processing {0}", file);
            string destFile = string.Format("test-{0}.png", System.IO.Path.GetFileNameWithoutExtension(file));
            string destfullp = System.IO.Path.Combine(outputDir, destFile);
            using (var testImg = new Bitmap(file))
            {
              var rv = map.ProcessImageUsingRef(refMapImage, refFontImage, testImg, destfullp);
              if (fontFamilyProvider != null)
              {
                string str = fontFamilyProvider.ConvertToText(rv);
                string txtpath = System.IO.Path.Combine(outputDir, string.Format("test-{0}.txt", System.IO.Path.GetFileNameWithoutExtension(file)));
                System.IO.File.WriteAllText(txtpath, str);
              }
            }
          }
        }
        t.EndTask();

#if !DEBUG
      }
      catch (Exception e)
      {
        Log.WriteLine("Exception occurred:\r\n{0}", e);
      }
#else
      }
#endif
    }
  }
}
