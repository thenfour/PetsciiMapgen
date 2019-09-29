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
      //{
      //  HSLColorspace s = new HSLColorspace();
      //  var s1 = ValueSet.New(3, 0);
      //  var s2 = ValueSet.New(3, 0);
      //  s1[0] = 100;
      //  s1[1] = 190;
      //  s1[2] = 100;
      //  s2[0] = 100;
      //  s2[1] = 360;
      //  s2[2] = 100;
      //  var d = s.ColorDistance(s1, s2, 1, 2);
      //}

      ////args = new string[] { "-batchrun", "c64", "grayscale", "c64color ", "example", "lab", "v5" };
      //args = new string[] {
      //  "-outdir", @"f:\maps",
      //  "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testimages",
      //  "-testpalette", "ThreeBit",

      //  "-pf", "square",
      //  "-pfargs", "16v2x2+0",
      //  "-cs", "JPEG",
      //  "-partitions", "6x3",

      //  "-fonttype", "fontfamily",
      //  "-charsize", "24x24",
      //  //"-fontfamily", @"Comic Sans MS",
      //  "-fontfamily", @"Arial Unicode MS",
      //  "-fontName", "hangul24",
      //  "-charListTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\UnicodeHangul.txt",
      //  "-bgcolor", "#ffffff",
      //  "-fgcolor", "#000000",
      //  //"-fgpalette", "ThreeBit",
      //  "-scale", "1",
      //  //"-shift", "0x0",
      //  //"-trytofit", "1",
      //  // aspecttolerance
      //};


      using (var stayon = new StayOn())
      {
        string[] batchKeywords = new string[] { };
        List<string> batchAddArgs = new List<string>();
        BatchCommand batchCommand = BatchCommand.None;

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

        string batchBaseDir = @"f:\maps";
        string batchFontDir = @"C:\root\git\thenfour\PetsciiMapgen\img\fonts";
        LogCore batchLog = new LogCore();

        args.ProcessArg("-batchfontdir", s =>
        {
          batchFontDir = s;
          batchLog.WriteLine("Setting font dir: {0}", batchFontDir);
        });
        args.ProcessArg("-batchbasedir", s =>
        {
          batchBaseDir = s;
          batchLog.WriteLine("Setting base dir: {0}", batchBaseDir);
        });

        batchLog.WriteLine("Batch font dir: {0}", batchFontDir);
        batchLog.WriteLine("Batch base dir: {0}", batchBaseDir);

        string batchLogPath = System.IO.Path.Combine(batchBaseDir, @"batchLog.txt");
        batchLog.SetLogFile(batchLogPath);
        Func<string, string> batchFontPath = delegate (string s)
        {
          return System.IO.Path.Combine(batchFontDir, s);
        };

        args.ProcessArg("-batchaddarg", s =>
        {
          batchAddArgs.Add(s);
        });

        foreach (var arg in batchKeywords)
        {
          batchLog.WriteLine("Using batch keyword: {0}", arg);
        }

        foreach (var arg in batchAddArgs)
        {
          batchLog.WriteLine("Adding additional batch argument: {0}", arg);
        }

        var common = Args(
          //"-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
          "-testpalette", "ThreeBit",
          "-loadOrCreateMap"
          );

        // heavy: aiming for 16384x16384 = map size 268435456
        var grayscalePixelFormatsHeavy = Args("pftag:Heavy Grayscale") + Or(
          Args("-pf", "square", "-pfargs", "4096v1x1+0", "-partitions", "64x2"),//1
          Args("-pf", "square", "-pfargs", "128v2x2+0", "-partitions", "4x3"),//4
          Args("-pf", "square", "-pfargs", "8v3x3+0", "-partitions", "2x3"),//9
          Args("-pf", "fivetile", "-pfargs", "48v5+0", "-partitions", "4x3")//5
          );

        var colorPixelFormatsHeavy = Args("pftag:Heavy Color") + Or(
          Args("-pf", "square", "-pfargs", "645v1x1+2", "-partitions", "15x2"),//3
          Args("-pf", "square", "-pfargs", "24v2x2+2", "-partitions", "2x3"),//6
          Args("-pf", "square", "-pfargs", "6v3x3+2", "-partitions", "1x1"),//11
          Args("-pf", "fivetile", "-pfargs", "16v5+2", "-partitions", "2x3")//7
          );

        // medium: aiming for 8192x8192 = map size 67108864
        var grayscalePixelFormatsMedium = Args("pftag:Medium Grayscale") + Or(
          Args("-pf", "square", "-pfargs", "2048v1x1+0", "-partitions", "32x2"),
          Args("-pf", "square", "-pfargs", "90v2x2+0", "-partitions", "2x3"),
          Args("-pf", "square", "-pfargs", "7v3x3+0", "-partitions", "2x3"),
          Args("-pf", "fivetile", "-pfargs", "36v5+0", "-partitions", "2x3")
          );

        var colorPixelFormatsMedium = Args("pftag:Medium Color") + Or(
          Args("-pf", "square", "-pfargs", "406v1x1+2", "-partitions", "7x2"),
          Args("-pf", "square", "-pfargs", "20v2x2+2", "-partitions", "2x3"),
          Args("-pf", "square", "-pfargs", "5v3x3+2", "-partitions", "1x1"),
          Args("-pf", "fivetile", "-pfargs", "14v5+2", "-partitions", "2x3")
          );

        // budget versions (512x512 = 262144 map size)
        var grayscalePixelFormatsBudget = Args("pftag:Budget Grayscale") + Or(
          Args("-pf", "square", "-pfargs", "1024v1x1+0", "-partitions", "32x2"),
          Args("-pf", "square", "-pfargs", "22v2x2+0", "-partitions", "2x3"),
          Args("-pf", "fivetile", "-pfargs", "12v5+0", "-partitions", "2x3")
          );

        var colorPixelFormatsBudget = Args("pftag:Budget Color") + Or(
          Args("-pf", "square", "-pfargs", "64v1x1+2", "-partitions", "8x1"),
          Args("-pf", "square", "-pfargs", "8v2x2+2", "-partitions", "2x3"),
          Args("-pf", "fivetile", "-pfargs", "6v5+2", "-partitions", "2x3")
          );

        // "Example" pixel formats to show the same N but with chroma subsampling
        var grayscaleExamplePixelFormats = Args("pftag:Example Grayscale") + Or(
          Args("-pf", "square", "-pfargs", "36v1x1+0", "-partitions", "2x3"),
          Args("-pf", "square", "-pfargs", "36v2x2+0", "-partitions", "2x3"),
          Args("-pf", "fivetile", "-pfargs", "36v5+0", "-partitions", "2x3")
          );

        var colorPixelExampleFormats = Args("pftag:Example Color") + Or(
          Args("-pf", "square", "-pfargs", "14v1x1+2", "-partitions", "2x3"),
          Args("-pf", "square", "-pfargs", "14v2x2+2", "-partitions", "2x3"),
          Args("-pf", "fivetile", "-pfargs", "14v5+2", "-partitions", "2x3")
          );

        var allLCCColorspaces = Or(
          Args("-cs", "jpeg"),
          Args("-cs", "nyuv"),
          Args("-cs", "lab"),
          Args("-cs", "hsl")
          );

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
          "fonttag:C64",
          "-fonttype", "mono",
          "-fontImage", batchFontPath(@"c64opt160.png"),
          "-charsize", "8x8");

        var c64fontAndPalettes_Color = C64Font + Args("-palette", "C64Color");

        var c64fontAndPalettes_Grayscale = C64Font + Or(
            Args("-palette", "BlackAndWhite"),
            Args("-palette", "C64ColorGray8A"),
            Args("-palette", "C64Grays"),
            Args("-palette", "C64ColorGray8B"),
            Args("-palette", "C64Color")
            );

        var C64Color = c64fontAndPalettes_Color + colorPixelFormats;
        var C64Grayscale = c64fontAndPalettes_Grayscale + grayscalePixelFormats;

        // mz700 ============================
        var mz700font = Args(
          "fonttag:MZ700",
          "-fonttype", "mono",
          "-fontImage", batchFontPath(@"mz700.png"),
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

        var mz700color = mz700font + mz700ColorPalettes + colorPixelFormats;
        var mz700grayscale = mz700font + mz700GrayPalettes + grayscalePixelFormats;


        // topaz ============================
        var topazFont = Args(
          "fonttag:Topaz",
          "-fonttype", "mono",
          "-fontImage", batchFontPath(@"topaz96.gif"),
          "-charsize", "8x16");

        var topazPalettes = Or(
          Args("-palette", "Workbench134"),
          Args("-palette", "Workbench314")
          );

        var topazGrayscale = topazFont + topazPalettes + grayscalePixelFormats;

        // DOS ============================
        var dosFont = Args(
          "fonttag:VGA",
          "-fonttype", "mono",
          "-fontImage", batchFontPath(@"VGA240.png"),
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

        var dosColor = dosFont + dosColorPalettes + colorPixelFormats;
        var dosGrayscale = dosFont + dosGrayPalettes + grayscalePixelFormats;

        // VGAboxonly45.png ============================
        var dosBoxFont = Args(
          "fonttag:VGABox",
          "-fonttype", "mono",
          "-fontImage", batchFontPath(@"VGAboxonly45.png"),
          "-charsize", "8x16");

        var dosBoxColor = dosBoxFont + dosColorPalettes + colorPixelFormats;
        var dosBoxGrayscale = dosBoxFont + dosGrayPalettes + grayscalePixelFormats;

        // emoji ============================
        Func<string, int, ArgSetList> emoji = delegate (string pngimagenamewoext, int dimsq)
        {
          var font = Args(
          "fonttag:" + pngimagenamewoext,
          "-fonttype", "normal",
          "-fontImage", batchFontPath(pngimagenamewoext + ".png"),
          "-charsize", string.Format("{0}x{0}", dimsq));

          var col = font + colorPixelFormats;
          var gray = font + grayscalePixelFormats;
          return Or(col, gray);
        };

        // mario tiles ============================
        var marioTilesFont = Args(
          "fonttag:MarioTiles",
          "-fonttype", "colorkey",
          "-fontImage", batchFontPath(@"mariotiles4.png"),
          "-colorkey", "#04c1aa",
          "-palette", "MarioBg",
          "-lefttoppadding", "1",
          "-charsize", "16x16");

        var marioTiles = marioTilesFont + colorPixelFormats;
        marioTiles += marioTilesFont + grayscalePixelFormats;

        // All ============================
        // fonttag pftag
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
          ) + common + Args(batchAddArgs.ToArray());

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
              batchLog.WriteLine("  {0}: {1}", ibatch, argset);
              ibatch++;
            }
            batchLog.WriteLine("Batch contains {0} runs", filtered.Length);
            break;
          case BatchCommand.Run:
            ibatch = 0;
            batchLog.WriteLine("Batch contains {0} runs", filtered.Length);
            foreach (var argset in filtered)
            {
              batchLog.EnterTask("Running batch #{0} of keywords", ibatch, string.Join(", ", batchKeywords));
              batchLog.WriteLine("Args: {0}", argset);
              // generate an output directory.
              // use pftag and fonttag
              var pftags = argset.args.Where(a => a.StartsWith("pftag:")).Select(a => a.Split(':')[1]);
              var fonttags = argset.args.Where(a => a.StartsWith("fonttag:")).Select(a => a.Split(':')[1]);
              var dirName = string.Join(" ", fonttags) + " " + string.Join(" ", pftags);
              var outDir = System.IO.Path.Combine(batchBaseDir, dirName);
              batchLog.WriteLine("Output directory: {0}", outDir);
              var realArgs = argset + Args("-outdir", outDir);
              Main2(realArgs.args.ToArray());
              batchLog.EndTask();
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

        args.ProcessArg("-loadmap", _ =>
        {
          mapSource = MapSource.Load;
          // if you're loading, then we want to process the args from that directory.
          //outputDir = System.IO.Path.GetDirectoryName(s);
          string argspath = System.IO.Path.Combine(outputDir, "args.txt");
          var lines = System.IO.File.ReadAllLines(argspath)
            .Select(l => l.Split('#')[0]) // remove comments
            .Where(l => !string.IsNullOrWhiteSpace(l)); // remove empty lines
          args = lines.Concat(args).ToArray();
        });

        string configTag = string.Format("{0}_{1}_{2}", fontProvider.DisplayName, pixelFormat.PixelFormatString, partitionManager);
        outputDir = System.IO.Path.Combine(outputDir, configTag);
        Log.WriteLine("Ensuring directory exists: {0}", outputDir);
        System.IO.Directory.CreateDirectory(outputDir);

        string logPath = System.IO.Path.Combine(outputDir, "log.txt");
        Log.SetLogFile(logPath);

        string infopath = System.IO.Path.Combine(outputDir, "args.txt");
        string mapFullPath = System.IO.Path.Combine(outputDir, string.Format("mapfull_{0}.png", configTag));
        string mapRefPath = System.IO.Path.Combine(outputDir, string.Format("mapref_{0}.png", configTag));
        string mapFontPath = System.IO.Path.Combine(outputDir, string.Format("mapfont_{0}.png", configTag));

        args.ProcessArg("-loadOrCreateMap", _ =>
        {
          if (System.IO.File.Exists(mapRefPath) && System.IO.File.Exists(mapFontPath))
          {
            Log.WriteLine("-loadOrCreateMap: Loading existing map.");
            mapSource = MapSource.Load;
          }
          else
          {
            Log.WriteLine("-loadOrCreateMap: Looks like we have to create the map.");
            mapSource = MapSource.Create;
          }
        });

        if (mapSource == MapSource.Create)
        {
          using (var infoFile = new StreamWriter(infopath))
          {
            foreach (var arg in args)
            {
              infoFile.WriteLine(arg);
            }
          }
        }


        HybridMap2 map = null;

        switch (mapSource)
        {
          case MapSource.Create:
            Log.EnterTask("--- MAP GENERATION");
            map = new HybridMap2(fontProvider, partitionManager, pixelFormat,
              mapFullPath,
              mapRefPath,
              mapFontPath,
              coresToUtilize);
            Log.EndTask();
            break;
          case MapSource.Load:
            Log.EnterTask("--- MAP LOAD");
            map = HybridMap2.LoadFromDisk(outputDir, fontProvider, pixelFormat);
            Log.EndTask();
            break;
        }

        if (fontFamilyProvider != null)
        {
          //string fontImgPath = System.IO.Path.Combine(outputDir, "font.png");
          //fontFamilyProvider.SaveFontImage(fontImgPath);
        }


        Log.EnterTask("processing images");

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
        Log.EndTask();

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
