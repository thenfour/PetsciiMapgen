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
    enum BatchCommand
    {
      List,
      Run,
      None
    }

    static ArgSet Args(params string[] a)
    {
      return new ArgSet(a);
    }
    static ArgSetList Or(params ArgSet[] argSets)
    {
      var ret = new ArgSetList();
      ret.argSets = argSets;
      return ret;
    }
    static ArgSetList Or(params ArgSetList[] argsetLists)
    {
      var ret = new ArgSetList();
      List<ArgSet> l = new List<ArgSet>();
      foreach (var x in argsetLists)
      {
        l.AddRange(x.argSets);
      }
      ret.argSets = l.ToArray();
      return ret;
    }

    static void Main(string[] args)
    {
      string rootDir = @"f:\maps";
      string batchLogPath = rootDir + @"\batchLog.txt";

      //args = new string[] { "-batchlist" };

      using (var stayon = new StayOn())
      {
        string[] batchKeywords = new string[] { };
        BatchCommand batchCommand = BatchCommand.None;
        Log.SetLogFile(batchLogPath);

        args.ProcessArg2(new string[] { "-batchrun", "-batchlist" }, (thisArg, remainingArgs) => 
        {
          if (remainingArgs != null)
          {
            batchKeywords = remainingArgs.ToArray();
          }
          switch (thisArg.ToLowerInvariant())
          {
            case "-batchrun":
              batchCommand = BatchCommand.Run;
              break;
            default:
            case "-batchlist":
              batchCommand = BatchCommand.List;
              break;
          }
        });

        if (batchCommand == BatchCommand.None)
        {
          Main2(args);
          return;
        }
        
        var common = Args(
          "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
          "-testpalette", "ThreeBit");

        // heavy: aiming for 16384x16384 = map size 268435456
        var grayscalePixelFormatsHeavy = Args("pftag:heavy,pftag:grayscale") + Or(
          Args("-pf", "square", "-pfargs", "4096v1x1+0", "-partitions", "1x1"),//1
          Args("-pf", "square", "-pfargs", "128v2x2+0", "-partitions", "2x3"),//4
          Args("-pf", "square", "-pfargs", "8v3x3+0", "-partitions", "2x3"),//9
          Args("-pf", "fivetile", "-pfargs", "48v5+0", "-partitions", "2x3")//5
          );

        var colorPixelFormatsHeavy = Args("pftag:heavy,pftag:color") + Or(
          Args("-pf", "square", "-pfargs", "645v1x1+2", "-partitions", "1x1"),//3
          Args("-pf", "square", "-pfargs", "24v2x2+2", "-partitions", "2x3"),//6
          Args("-pf", "square", "-pfargs", "6v3x3+2", "-partitions", "1x1"),//11
          Args("-pf", "fivetile", "-pfargs", "16v5+2", "-partitions", "2x3")//7
          );

        // medium: aiming for 8192x8192 = map size 67108864
        var grayscalePixelFormatsMedium = Args("pftag:medium,pftag:grayscale") + Or(
          Args("-pf", "square", "-pfargs", "2048v1x1+0", "-partitions", "1x1"),
          Args("-pf", "square", "-pfargs", "90v2x2+0", "-partitions", "2x3"),
          Args("-pf", "square", "-pfargs", "7v3x3+0", "-partitions", "2x3"),
          Args("-pf", "fivetile", "-pfargs", "36v5+0", "-partitions", "2x3")
          );

        var colorPixelFormatsMedium = Args("pftag:medium,pftag:color") + Or(
          Args("-pf", "square", "-pfargs", "406v1x1+2", "-partitions", "1x1"),
          Args("-pf", "square", "-pfargs", "20v2x2+2", "-partitions", "2x3"),
          Args("-pf", "square", "-pfargs", "5v3x3+2", "-partitions", "1x1"),
          Args("-pf", "fivetile", "-pfargs", "13v5+2", "-partitions", "2x3")
          );

        // budget versions (512x512 = 262144 map size)
        var grayscalePixelFormatsBudget = Args("pftag:budget,pftag:grayscale") + Or(
          Args("-pf", "square", "-pfargs", "1024v1x1+0", "-partitions", "1x1"),
          Args("-pf", "square", "-pfargs", "22v2x2+0", "-partitions", "2x3"),
          Args("-pf", "fivetile", "-pfargs", "12v5+0", "-partitions", "2x3")
          );

        var colorPixelFormatsBudget = Args("pftag:budget,pftag:color") + Or(
          Args("-pf", "square", "-pfargs", "64v1x1+2", "-partitions", "1x1"),
          Args("-pf", "square", "-pfargs", "8v2x2+2", "-partitions", "2x3"),
          Args("-pf", "fivetile", "-pfargs", "6v5+2", "-partitions", "2x3")
          );

        // "Example" pixel formats to show the same N but with chroma subsampling
        var grayscaleExamplePixelFormats = Args("pftag:example,pftag:grayscale") + Or(
          Args("-pf", "square", "-pfargs", "12v1x1+0", "-partitions", "2x3"),
          Args("-pf", "square", "-pfargs", "12v2x2+0", "-partitions", "2x3"),
          Args("-pf", "fivetile", "-pfargs", "12v5+0", "-partitions", "2x3")
          );

        var colorPixelExampleFormats = Args("pftag:example,pftag:color") + Or(
          Args("-pf", "square", "-pfargs", "6v1x1+2", "-partitions", "2x3"),
          Args("-pf", "square", "-pfargs", "6v2x2+2", "-partitions", "2x3"),
          Args("-pf", "fivetile", "-pfargs", "6v5+2", "-partitions", "2x3")
          );

        var allLCCColorspaces = Or(
          Args("-cs", "jpeg"),
          Args("-cs", "nyuv"),
          Args("-cs", "lab"));

        var grayscalePixelFormats = allLCCColorspaces + Or(
          grayscalePixelFormatsHeavy,
          grayscalePixelFormatsMedium,
          grayscalePixelFormatsBudget,
          grayscaleExamplePixelFormats);

        var colorPixelFormats = allLCCColorspaces + Or(
          colorPixelFormatsHeavy,
          colorPixelFormatsMedium,
          colorPixelFormatsBudget,
          colorPixelExampleFormats);

        // C64 ============================
        var C64Font = Args(
          "-fonttype", "mono",
          "-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\c64opt160.png",
          "-charsize", "8x8");

        var c64fontAndPalettes_Color = C64Font + Args("-palette", "C64Color");

        var c64fontAndPalettes_Grayscale = C64Font + Or(
            Args("-palette", "BlackAndWhite"),
            Args("-palette", "C64ColorGray8A"),
            Args("-palette", "C64Grays"),
            Args("-palette", "C64ColorGray8B"),
            Args("-palette", "C64Color")
            );

        var C64Color = Args("-outdir", rootDir + @"\C64 color") + c64fontAndPalettes_Color + colorPixelFormats;
        var C64Grayscale = Args("-outdir", rootDir + @"\C64 grayscale") + c64fontAndPalettes_Grayscale + grayscalePixelFormats;

        // mz700 ============================
        var mz700font = Args(
          "-fonttype", "mono",
          "-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\mz700.png",
          "-charsize", "8x8");

        var mz700ColorPalettes = Or(
          Args("-palette", "RGBPrimariesHalftone16"),
          Args("-palette", "ThreeBit")
          );
        var mz700GrayPalettes = Or(
          Args("-palette", "BlackAndWhite"),
          Args("-palette", "Gray3"),
          Args("-palette", "Gray4"),
          Args("-palette", "Gray5"),
          Args("-palette", "Gray8")
          );

        var mz700color = Args("-outdir", rootDir + @"\MZ700 color") + mz700font + mz700ColorPalettes + colorPixelFormats;
        var mz700grayscale = Args("-outdir", rootDir + @"\MZ700 grayscale") + mz700font + mz700GrayPalettes + grayscalePixelFormats;


        // topaz ============================
        var topazFont = Args(
          "-fonttype", "mono",
          "-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\topaz96.gif",
          "-charsize", "8x16");

        var topazPalettes = Or(
          Args("-palette", "Workbench134"),
          Args("-palette", "Workbench314")
          );

        var topazGrayscale = Args("-outdir", rootDir + @"\Topaz grayscale") + topazFont + topazPalettes + grayscalePixelFormats;

        // DOS ============================
        var dosFont = Args(
    "-fonttype", "mono",
    "-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\VGA240.png",
    "-charsize", "8x16");

        var dosColorPalettes = Or(
          Args("-palette", "RGBPrimariesHalftone16"),
          Args("-palette", "ThreeBit")
          );
        var dosGrayPalettes = Or(
          Args("-palette", "BlackAndWhite"),
          Args("-palette", "Gray3"),
          Args("-palette", "Gray4"),
          Args("-palette", "Gray5"),
          Args("-palette", "Gray8")
          );

        var dosColor = Args("-outdir", rootDir + @"\Dos color") + dosFont + dosColorPalettes + colorPixelFormats;
        var dosGrayscale = Args("-outdir", rootDir + @"\Dos grayscale") + dosFont + dosGrayPalettes + grayscalePixelFormats;

        // VGAboxonly45.png ============================
        var dosBoxFont = Args(
  "-fonttype", "mono",
  "-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\VGAboxonly45.png",
  "-charsize", "8x16");

        var dosBoxColor = Args("-outdir", rootDir + @"\Dos box color") + dosBoxFont + dosColorPalettes + colorPixelFormats;
        var dosBoxGrayscale = Args("-outdir", rootDir + @"\Dos box grayscale") + dosBoxFont + dosGrayPalettes + grayscalePixelFormats;

        // emoji ============================
        Func<string, int, ArgSetList> emoji = delegate (string pngimagenamewoext, int dimsq)
        {
          var font = Args(
    "-fonttype", "normal",
    "-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\" + pngimagenamewoext + ".png",
    "-charsize", string.Format("{0}x{0}", dimsq));

          var col = Args("-outdir", rootDir + @"\" + pngimagenamewoext + " Color") + font + colorPixelFormats;
          var gray = Args("-outdir", rootDir + @"\" + pngimagenamewoext + " Grayscale") + font + grayscalePixelFormats;
          return Or(col, gray);
        };

        // mario tiles ============================
        var marioTilesFont = Args(
          "-fonttype", "colorkey",
          "-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\mariotiles4.png",
          "-colorkey", "#04c1aa",
          "-palette", "MarioBg",
          "-lefttoppadding", "1",
          "-charsize", "16x16");

        var marioTiles = Args("-outdir", rootDir + @"\mariotiles Color") + marioTilesFont + colorPixelFormats;
        marioTiles += Args("-outdir", rootDir + @"\mariotiles Grayscale") + marioTilesFont + grayscalePixelFormats;

        // All ============================
        var All = Or(
          C64Color,
          C64Grayscale,
          topazGrayscale,
          mz700color,
          mz700grayscale,
          dosColor,
          dosGrayscale,
          dosBoxColor,
          dosBoxGrayscale,
          emoji("emojidark12", 12),
          emoji("emojidark16", 16),
          emoji("emojidark24", 24),
          emoji("emojidark32", 32),
          emoji("emojidark64", 64),
          emoji("emojiappleblack12", 12),
          emoji("emojiappleblack16", 16),
          emoji("emojiappleblack24", 24),
          emoji("emojiappleblack32", 32),
          emoji("emojiappleblack64", 64),
          marioTiles
          ) + common;

        var filtered = All.Filter(batchKeywords).ToArray();

        switch (batchCommand)
        {
          case BatchCommand.None:
            Debug.Assert(false);// handled above.
            break;
          case BatchCommand.List:
            int ibatch = 0;
            foreach (var argset in filtered)
            {
              Log.WriteLine("  {0}: {1}", ibatch, argset);
              ibatch++;
            }
            Log.WriteLine("Batch contains {0} runs", filtered.Length);
            break;
          case BatchCommand.Run:
            ibatch = 0;
            Timings t = new Timings();
            foreach (var argset in filtered)
            {
              Log.SetLogFile(batchLogPath);
              t.EnterTask("Running batch #{0}", ibatch);
              Log.WriteLine("Args: {0}", argset);
              Main2(argset.args);
              Log.SetLogFile(batchLogPath);
              t.EndTask();
              ibatch++;
            }
            break;
        }
      }
    }

    static void Main2(string[] args)
    {
#if !DEBUG
      try
      {
#else
      {
#endif

        Log.WriteLine("----------------------------------------");

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

        args.ProcessArg("-testpalette", s =>
        {
          var palette = (System.Drawing.Color[])typeof(Palettes).GetProperty(s).GetValue(null);
          testColors.AddRange(palette);
        });
        args.ProcessArg("-testcolor", s =>
        {
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
            case "square":
              pixelFormat = SquareLCCPixelFormat.ProcessArgs(args);// HSLPixelFormat.ProcessArgs(args);
              break;
            default:
            case "fivetile":
              pixelFormat = FiveTilePixelFormat.ProcessArgs(args, fontProvider);
              break;
          }
        });

        if (pixelFormat == null)
        {
          Log.WriteLine("Pixel format not specified.");
          return;
          //pixelFormat = NaiveYUVPixelFormat.ProcessArgs(args);
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

        args.ProcessArg("-calcn", s =>
        {
          ulong maxMapKeys = ulong.Parse(s);

          partitionManager.Init(pixelFormat);

          ulong partitionCount = (ulong)partitionManager.PartitionCount;
          Log.WriteLine("Partition count: {0:N0}", partitionCount);

          // so the thing about partition count. You can't just divide by partition count,
          // because in deeper levels most partitions are simply unused / empty.
          // a decent conservative approximation is to take the first N levels
          partitionCount = (ulong)Math.Pow(partitionManager.PartitionsPerDimension, 2.5);// n = 2.5
          Log.WriteLine("Adjusted partition count: {0:N0}", partitionCount);
          Log.WriteLine("Charset count: {0:N0}", fontProvider.CharCount);
          Log.WriteLine("Cores to utilize: {0:N0}", coresToUtilize);
          Log.WriteLine("Luma + chroma components: {0:N0}", pixelFormat.DimensionCount);

          ulong NbasedOnMapSize = (ulong)Math.Floor(Math.Pow(maxMapKeys, 1.0 / pixelFormat.DimensionCount));

          Log.WriteLine("======================");
          Log.WriteLine("== THEREFORE, use N={0:N0}", NbasedOnMapSize);
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
          System.IO.Directory.CreateDirectory(outputDir);
          //Log.WriteLine("Output directory doesn't exist.");
          //return;
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

        switch (mapSource)
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
          map.TestColor(outputDir, ColorF.From(c));
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
