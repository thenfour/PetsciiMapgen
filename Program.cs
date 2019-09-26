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
  class ArgSet
  {
    public string[] args;
    public ArgSet(params string[] a)
    {
      args = a;
    }
    public static ArgSet operator +(ArgSet a, ArgSet b)
    {
      ArgSet n = new ArgSet();
      n.args = a.args.Concat(b.args).ToArray();
      return n;
    }
    public override string ToString()
    {
      return string.Join(" ", args);
    }
  }

  class ArgSetList
  {
    public ArgSet[] argSets;
    public static ArgSetList operator +(ArgSetList a, ArgSetList b)
    {
      ArgSetList ret = new ArgSetList();
      List<ArgSet> x = new List<ArgSet>();
      foreach (var ao in a.argSets)
      {
        foreach (var bo in b.argSets)
        {
          ArgSet n = new ArgSet();
          List<string> args = ao.args.ToList();
          args.AddRange(bo.args);
          n.args = args.ToArray();
          x.Add(n);
        }
      }
      ret.argSets = x.ToArray();
      return ret;
    }
    public static ArgSetList operator +(ArgSetList a, ArgSet b)
    {
      // this just adds args to the end of all arglists
      ArgSetList ret = new ArgSetList();
      List<ArgSet> x = new List<ArgSet>();// a.argSets.ToList();
      foreach (var ao in a.argSets)
      {
        x.Add(ao + b);
      }
      ret.argSets = x.ToArray();
      return ret;
    }
    public static ArgSetList operator +(ArgSet a, ArgSetList b)
    {
      // this just adds args to the end of all arglists
      ArgSetList ret = new ArgSetList();
      List<ArgSet> x = new List<ArgSet>();// a.argSets.ToList();
      foreach (var bo in b.argSets)
      {
        x.Add(a + bo);
      }
      ret.argSets = x.ToArray();
      return ret;
    }
  }

  class Program
  {
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
      //  var c = Args("c64") + Or(Args("color"), Args("grayscale"));
      //  var r = Args("z80") + Or(Args("small"), Args("big"));
      //  var f = Or(c, r);

      //  var a = c + r;
      //  var b = Args("hello") + Args("world");
      //  var d = Or(Args("hello"), Args("Goodbye")) + Args("world");
      //  var e = Args("Hello") + Or(Args("world"), Args("jimbo"));
      //  var x = 0;
      //}

      var common = Args(
        "-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
        "-testpalette", "ThreeBit");

      //-pf [Square, FiveTile]    Pixel format: Square, FiveTile
      //var grayscalePixelFormats = Or(
      //  Args("-pf", "square", "-pfargs", "8192v1x1+0", "-partitions", "1x1"),
      //  Args("-pf", "square", "-pfargs", "128v2x2+0", "-partitions", "2x3"),
      //  Args("-pf", "square", "-pfargs", "8v3x3+0", "-partitions", "2x3"),
      //  Args("-pf", "fivetile", "-pfargs", "48v5+0", "-partitions", "2x3")
      //  );

      //var colorPixelFormats = Or(
      //  Args("-pf", "square", "-pfargs", "645v1x1+2", "-partitions", "1x1"),
      //  Args("-pf", "square", "-pfargs", "24v2x2+2", "-partitions", "2x3"),
      //  Args("-pf", "square", "-pfargs", "5v3x3+2", "-partitions", "1x1"),
      //  Args("-pf", "fivetile", "-pfargs", "16v5+2", "-partitions", "2x3")
      //  );

      // budget versions
      var grayscalePixelFormats = Or(
        Args("-pf", "square", "-pfargs", "256v1x1+0", "-partitions", "1x1"),
        Args("-pf", "square", "-pfargs", "16v2x2+0", "-partitions", "2x3"),
        Args("-pf", "fivetile", "-pfargs", "12v5+0", "-partitions", "2x3")
        );

      var colorPixelFormats = Or(
        Args("-pf", "square", "-pfargs", "16v1x1+2", "-partitions", "1x1"),
        Args("-pf", "square", "-pfargs", "8v2x2+2", "-partitions", "2x3"),
        Args("-pf", "fivetile", "-pfargs", "6v5+2", "-partitions", "2x3")
        );

      var allLCCColorspaces = Or(
        Args("-cs", "jpeg"),
        Args("-cs", "nyuv"),
        Args("-cs", "lab"));

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

      var C64Color = Args("-outdir", @"C:\temp\C64 color") + c64fontAndPalettes_Color + colorPixelFormats + allLCCColorspaces;
      var C64Grayscale = Args("-outdir", @"C:\temp\C64 grayscale") + c64fontAndPalettes_Grayscale + grayscalePixelFormats + allLCCColorspaces;

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

      var mz700color = Args("-outdir", @"C:\temp\MZ700 color") + mz700font + mz700ColorPalettes + colorPixelFormats + allLCCColorspaces;
      var mz700grayscale = Args("-outdir", @"C:\temp\MZ700 grayscale") + mz700font + mz700GrayPalettes + grayscalePixelFormats + allLCCColorspaces;


      // All ============================
      var All = common + Or(
        C64Color, C64Grayscale,
        mz700color, mz700grayscale);

      string batchLogPath = @"c:\temp\batchLog.txt";
      int ibatch = 0;
      foreach (var argset in All.argSets)
      {
        Main2(argset.args);
        System.IO.File.AppendAllLines(batchLogPath, new string[]{
          string.Format("Completed batch #{0} with args: {1}", ibatch, argset)
        });
        ibatch++;
      }
    }

    enum MapSource
    {
      Create,
      Load
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

          //// take 80% for safety.
          //ulong charCount = (ulong)fontProvider.CharCount;
          //ulong maxTheoreticalMappingCount = (ulong)coresToUtilize * (ulong)UInt32.MaxValue * (ulong)partitionCount * 3 / 4;
          //ulong maxKeyCount = maxTheoreticalMappingCount / charCount;
          //ulong NbasedOnMappings = (ulong)Math.Floor(Math.Pow(maxKeyCount, 1.0 / pixelFormat.DimensionCount));

          //Log.WriteLine("Based on the map size requested, N can be as much as        {0:N0}", NbasedOnMapSize);
          //Log.WriteLine("Based on the charset and mapping array, N can be as much as {0:N0}", NbasedOnMappings);
          //ulong m = Math.Min(NbasedOnMapSize, NbasedOnMappings);

          //ulong keyCount = (ulong)Math.Pow(m, pixelFormat.DimensionCount);
          //ulong sizeofMapping = (ulong)Marshal.SizeOf<Mapping>();
          //Log.WriteLine("Which will use {0:N0} of memory for mappings", keyCount * charCount / partitionCount * sizeofMapping);

          //ulong maxmem = Utils.GbToBytes(150);
          //args.ProcessArg("-maxmemgb", gb => {
          //  maxmem = Utils.GbToBytes(ulong.Parse(gb));
          //});
          //Log.WriteLine("Max memory to use: {0:N0}", maxmem);

          //// reduce keycount to conform.
          //maxKeyCount = maxmem / (charCount * sizeofMapping / partitionCount);
          //ulong NbasedOnMem = (ulong)Math.Floor(Math.Pow(maxKeyCount, 1.0 / pixelFormat.DimensionCount));
          //Log.WriteLine("Based on memory usage, N can be as much as                  {0:N0}", NbasedOnMem);

          //m = Math.Min(m, NbasedOnMem);
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
