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

    static void CreateLUT(string paletteName, System.Drawing.Color[] palette, string outfile, ILCCColorSpace cs, int levels, bool useChroma, bool neutral)
    {
      Log.WriteLine("Creating LUT...");
      Log.WriteLine("  Palette: {0} ({1} colors)", paletteName, palette.Length);
      Log.WriteLine("  Colorspace: {0}", cs.ToString());
      Log.WriteLine("  Output file: {0}", outfile);
      Log.WriteLine("  {0}", useChroma ? "COLOR" : "GREY");
      int imageWidth = levels * levels;
      int imageHeight = levels;
      Log.WriteLine("  Image size: {0} x {1}", imageWidth, imageHeight);

      Directory.CreateDirectory(Path.GetDirectoryName(outfile));

      var bmp = new Bitmap(imageWidth, imageHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
      BitmapData destFontData = bmp.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

      int chromaComponents = useChroma ? 2 : 0;

      for (double ir = 0; ir < levels; ++ir)
      {
        for (double ig = 0; ig < levels; ++ig)
        {
          for (double ib = 0; ib < levels; ++ib)
          {
            // Y coord = green (top = 0, bottom = 1)
            // X coord = red (left = 0; right = 1)
            // X cell = blue (left = 0; right = 1)
            ColorF srcColor = ColorF.FromRGB(ir / levels, ig / levels, ib / levels);
            //srcColor = ColorF.FromRGB(.5,.3,.2);
            int y = levels - (int)ig - 1;
            int x = (int)ib * levels;
            x += (int)ir;

            if (neutral)
            {
              destFontData.SetPixel(x, y, srcColor);
            }
            else
            {
              // find the nearest color in the palette.
              // a value set normally consists of multiple luma & chroma components for a char (for example 5 luma + 2 chroma)
              // for this we just have the normal default. all our colorspaces are LCC (luma chroma chroma).
              ValueSet srcValueSet = cs.GetValueSetForSinglePixel(srcColor, useChroma);
              System.Drawing.Color closestColor = System.Drawing.Color.Black;
              double closestDistance = 1e6;

              foreach (var pc in palette)
              {
                ColorF paletteColor = pc.ToColorF();
                ValueSet palValueSet = cs.GetValueSetForSinglePixel(paletteColor, useChroma);
                double dist = cs.ColorDistance(srcValueSet, palValueSet, 1/*luma L*/, chromaComponents);
                if (dist < closestDistance)
                {
                  closestDistance = dist;
                  closestColor = pc;
                }
              }

              //closestColor = srcColor;
              destFontData.SetPixel(x, y, closestColor);
              //destFontData.SetPixel(x, y, srcColor);

            }
          }
        }
      }

      bmp.UnlockBits(destFontData);

      bmp.Save(outfile);
      bmp.Dispose();
      bmp = null;
    }

    static void Main(string[] args)
    {
      //GenerateFontMap(@"C:\root\git\thenfour\PetsciiMapgen\img\fonts\EmojiOneColor.otf", 32, @"c:\temp\emojione.png");
      //GenerateFontMap2(@"C:\root\git\thenfour\PetsciiMapgen\img\fonts\EmojiOneColor.otf", 32, @"c:\temp\comicsans.png");
      //GenerateFontMap(@"Arial Unicode MS", 32, @"c:\temp\aunicod1.png");
      //GenerateFontMap2(@"Arial Unicode MS", 32, @"c:\temp\aunicod2.png");
      //args = new string[] { "-batchrun", "C64", "heavy", "+2" };
      ArgSetList batchOverride = null;

      //batchOverride = Batches.Or(Batches.Args(new string[] { @"fonttag:emojidark12", @"-fonttype", @"normal", @"-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\emojidark12.png", @"-charsize", @"12x12", @"pftag:Heavy Grayscale", @"-cs", @"lab", @"-pf", @"fivetile", @"-pfargs", @"48v5+0", @"-partitions", @"4", @"-testpalette", @"ThreeBit", @"-loadOrCreateMap", @"-outdir", @"f:\maps\emojidark12 Heavy Grayscale" }));

      //batchOverride = Batches.Args(
      //  //@"-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //  //@"-testpalette", "ThreeBit",
      //  @"-outdir", @"f:\maps",
      //  @"-fonttype", @"mono",
      //  @"-palette", "C64Color",
      //  @"-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\c64opt160.png",
      //  @"-charsize", @"8x8",
      //  @"-cs", @"lab",

      //  @"-pf", @"fivetile",
      //  @"-pfargs", @"9v5+2"
      //) + Batches.Or(
      //  //Batches.Args(@"-tessellator", "a"),
      //  //Batches.Args(@"-tessellator", "b"),
      //  Batches.Args(@"-tessellator", "c")
      //  ) + Batches.Or(
      //    Batches.Args(@"-partitions", "2")
      //    //Batches.Args(@"-partitions", "7"),
      //    //Batches.Args(@"-partitions", "10")
      //    );

      //batchOverride = Batches.Or(
      //  Batches.Args(
      //    @"-fonttype", @"mono", @"-fontImage", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\c64opt160.png", @"-charsize", @"8x8",
      //  @"-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
      //  @"-testpalette", "ThreeBit",
      //  @"-palette", @"C64Color",
      //  @"-cs", @"lab",

      //  @"-pf", @"fivetile",
      //  @"-pfargs", @"14v5+0",
      //  @"-partitions", @"2",

      //  @"-loadOrCreateMap",
      //  @"-outdir", @"f:\maps\C64 Budget Color")
      //  );


      //batchOverride = Batches.Or(Batches.Args("-batchrun", "C64", "heavy", "+2" ));

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
              batchLog.WriteLine("  {0}: {1}", ibatch, argset.ToCSString());
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

        bool didluts = false;
        args.ProcessArg("-createlut", s =>
        {
          var colorSpace = Utils.ParseRequiredLCCColorSpaceArgs(args, true);
          string outfile = null;
          args.ProcessArg("-o", s2 => { outfile = s2; });
          List<System.Drawing.Color> palette = new List<System.Drawing.Color>();
          string paletteName = null;
          args.ProcessArg("-palette", s2 =>
          {
            paletteName = s2;
            palette.AddRange(Utils.GetNamedPalette(s2));
          });
          int levels = 32;
          args.ProcessArg("-levels", s2 => { levels = int.Parse(s2); });
          bool useChroma = false;
          args.ProcessArg("-lcc", s2 => { useChroma = true; });
          bool neutral = false;
          args.ProcessArg("-neutral", s2 => { neutral = true; });
          CreateLUT(paletteName, palette.ToArray(), outfile, colorSpace, levels, useChroma, neutral);
          didluts = true;
          return;
        });
        if (didluts)
          return;

        args.ProcessArg("-listpalettes", s =>
        {
          Log.WriteLine("Listing palettes:");
          foreach (var p in typeof(Palettes).GetProperties())
          {
            Log.WriteLine("  {0}", p.Name);
          }
        });

        args.ProcessArg("-viewpalette", s =>
        {
          Log.WriteLine("Listing palette entries for palette:");
          Log.WriteLine("{0}", s);
          var p = Utils.GetNamedPalette(s);
          Log.WriteLine("{0} entries", p.Length);
          for (int i = 0; i < p.Length; ++ i)
          {
            var c = p[i];
            Log.WriteLine("{0:000}: {1} {2}",
              i,
              ColorTranslator.ToHtml(c),
              ColorMapper.GetNearestName(c));
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
          testColors.AddRange(Utils.GetNamedPalette(s));
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
