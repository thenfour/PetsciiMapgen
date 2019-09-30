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
using System.Windows.Media.Imaging;

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

    static void Main(string[] args)
    {
      //GenerateFontMap(@"C:\root\git\thenfour\PetsciiMapgen\img\fonts\EmojiOneColor.otf", 32, @"c:\temp\emojione.png");
      //GenerateFontMap2(@"C:\root\git\thenfour\PetsciiMapgen\img\fonts\EmojiOneColor.otf", 32, @"c:\temp\comicsans.png");
      //GenerateFontMap(@"Arial Unicode MS", 32, @"c:\temp\aunicod1.png");
      //GenerateFontMap2(@"Arial Unicode MS", 32, @"c:\temp\aunicod2.png");
      //args = new string[] { "-batchrun", "C64", "LAB", "budget", "C64color ", "2x2+2" };
      ArgSetList batchOverride = null; ;

      //batchOverride = Batches.Args(
      //  @"-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //  @"-testpalette", "ThreeBit",
      //  @"-outdir", @"f:\maps",
      //  @"-fonttype", @"normal",
      //  @"-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\emojidark12.png",
      //  @"-charsize", @"12x12",
      //  @"-cs", @"lab",

      //  @"-pf", @"fivetile",
      //  @"-pfargs", @"16v5+0"
      //) + Batches.Or(
      //  //Batches.Args(@"-tessellator", "a"),
      //  Batches.Args(@"-tessellator", "b")
      //  ) + Batches.Or(
      //  Batches.Args(@"-partitions", "13")
      //  //Batches.Args(@"-partitions", "7"),
      //  //Batches.Args(@"-partitions", "10")
      //    );

      using (var stayon = new StayOn())
      {
        if (batchOverride != null)
        {
          int ibatch = 0;
          Log.WriteLine("Batch contains {0} runs", batchOverride.argSets.Count());
          foreach (var argset in batchOverride.argSets)
          {
            Log.EnterTask("Running batch #{0}", ibatch);
            Log.WriteLine("Args: {0}", argset.ToCSString());
            Main2(argset.Args.ToArray());
            Log.EndTask();
            ibatch++;
          }
          return;
        }

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
        
        var All = Batches.GetAllBatches(batchBaseDir, batchFontPath, batchAddArgs);

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
              batchLog.WriteLine("Args: {0}", argset.ToCSString());
              Main2(argset.Args.ToArray());
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

        //PartitionManager partitionManager = null;// = new PartitionManager(1, 1, discreteValues);
        int partitionsPerDimension = 1;
        int? partitionDepth = null;
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
          if (s.Contains('x'))
          {
            partitionsPerDimension = int.Parse(s.Split('x')[0]);
            partitionDepth = int.Parse(s.Split('x')[1]);
          } else
          {
            partitionsPerDimension = int.Parse(s);
          }
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

        args.ProcessArg("-calcn", s =>
        {
          ulong maxMapKeys = ulong.Parse(s);

          //partitionManager.Init();

          //ulong partitionCount = (ulong)partitionManager.PartitionCount;
          //Log.WriteLine("Partition count: {0:N0}", partitionCount);

          // so the thing about partition count. You can't just divide by partition count,
          // because in deeper levels most partitions are simply unused / empty.
          // a decent conservative approximation is to take the first N levels
          //partitionCount = (ulong)Math.Pow(partitionManager.PartitionsPerDimension, 2.5);// n = 2.5
          //Log.WriteLine("Adjusted partition count: {0:N0}", partitionCount);
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

        string partitionConfigTag = string.Format("p{0}x{1}", partitionsPerDimension, partitionDepth.HasValue ? partitionDepth.ToString() : "N");

        string configTag = string.Format("{0}_{1}_{2}", fontProvider.DisplayName, pixelFormat.PixelFormatString, partitionConfigTag);
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
            map = new HybridMap2(fontProvider, pixelFormat,
              mapFullPath,
              mapRefPath,
              mapFontPath,
              coresToUtilize, partitionsPerDimension, partitionDepth);
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
