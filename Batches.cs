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
  public class Batches
  {
    public static ArgSet Args(params string[] a)
    {
      return new ArgSet(a);
    }
    public static ArgSetList Or(params ArgSet[] argSets)
    {
      var ret = new ArgSetList();
      ret.argSets = argSets;
      return ret;
    }
    public static ArgSetList Or(params ArgSetList[] argsetLists)
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

    public static ArgSetList GetAllBatches(string batchBaseDir, Func<string, string> batchFontPath, List<string> batchAddArgs)
    {
      var common = Args(
        //"-processImagesInDir", @"C:\root\git\thenfour\PetsciiMapgen\img\testImages",
        //"-testpalette", "ThreeBit",
        "-loadOrCreateMap"
        );

      // heavy: aiming for 16384x16384 = map size 268435456
      var grayscalePixelFormatsHeavy = Args("pftag:Heavy Grayscale") + Or(
        Args("-pf", "square", "-pfargs", "4096v1x1+0", "-partitions", "64"),//1
        Args("-pf", "square", "-pfargs", "128v2x2+0", "-partitions", "4"),//4
        Args("-pf", "square", "-pfargs", "8v3x3+0", "-partitions", "2"),//9
        Args("-pf", "fivetile", "-pfargs", "48v5+0", "-partitions", "4")//5
        );

      var colorPixelFormatsHeavy = Args("pftag:Heavy Color") + Or(
        Args("-pf", "square", "-pfargs", "645v1x1+2", "-partitions", "15"),//3
        Args("-pf", "square", "-pfargs", "24v2x2+2", "-partitions", "2"),//6
        Args("-pf", "square", "-pfargs", "6v3x3+2", "-partitions", "1"),//11
        Args("-pf", "fivetile", "-pfargs", "16v5+2", "-partitions", "2")//7
        );

      // medium: aiming for 8192x8192 = map size 67108864
      var grayscalePixelFormatsMedium = Args("pftag:Medium Grayscale") + Or(
        Args("-pf", "square", "-pfargs", "2048v1x1+0", "-partitions", "32"),
        Args("-pf", "square", "-pfargs", "90v2x2+0", "-partitions", "2"),
        Args("-pf", "square", "-pfargs", "7v3x3+0", "-partitions", "2"),
        Args("-pf", "fivetile", "-pfargs", "36v5+0", "-partitions", "2")
        );

      var colorPixelFormatsMedium = Args("pftag:Medium Color") + Or(
        Args("-pf", "square", "-pfargs", "406v1x1+2", "-partitions", "7"),
        Args("-pf", "square", "-pfargs", "20v2x2+2", "-partitions", "2"),
        Args("-pf", "square", "-pfargs", "5v3x3+2", "-partitions", "1"),
        Args("-pf", "fivetile", "-pfargs", "14v5+2", "-partitions", "2")
        );

      // budget versions (512x512 = 262144 map size)
      var grayscalePixelFormatsBudget = Args("pftag:Budget Grayscale") + Or(
        Args("-pf", "square", "-pfargs", "1024v1x1+0", "-partitions", "32"),
        Args("-pf", "square", "-pfargs", "22v2x2+0", "-partitions", "2"),
        Args("-pf", "fivetile", "-pfargs", "12v5+0", "-partitions", "2")
        );

      var colorPixelFormatsBudget = Args("pftag:Budget Color") + Or(
        Args("-pf", "square", "-pfargs", "64v1x1+2", "-partitions", "8"),
        Args("-pf", "square", "-pfargs", "8v2x2+2", "-partitions", "2"),
        Args("-pf", "fivetile", "-pfargs", "6v5+2", "-partitions", "2")
        );

      // "Example" pixel formats to show the same N but with chroma subsampling
      var grayscaleExamplePixelFormats = Args("pftag:Example Grayscale") + Or(
        Args("-pf", "square", "-pfargs", "36v1x1+0", "-partitions", "2"),
        Args("-pf", "square", "-pfargs", "36v2x2+0", "-partitions", "2"),
        Args("-pf", "fivetile", "-pfargs", "36v5+0", "-partitions", "2")
        );

      var colorPixelExampleFormats = Args("pftag:Example Color") + Or(
        Args("-pf", "square", "-pfargs", "14v1x1+2", "-partitions", "2"),
        Args("-pf", "square", "-pfargs", "14v2x2+2", "-partitions", "2"),
        Args("-pf", "fivetile", "-pfargs", "14v5+2", "-partitions", "2")
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

      // comic sans ================================
      var comicSansFont = Args(
        "-fontnametag", "ComicSans",//just for batch arg processing
        "-fonttype", "fontfamily",
        "-charsize", "24x24",
        "-fontfamily", @"Comic Sans MS",
        //"-fontfamily", @"Arial Unicode MS",
        //"-fontfamily", @"Segoe UI Symbol",
        "-bgcolor", "#000000",
        "-fgcolor", "#ffffff",
        //"-fgpalette", "ThreeBit",
        "-scale", "1.2",
        "-strictGlyphCheck", "1",
        "-fontweight", "900",
        "-strictGlyphCheck", "0",
        //"-shift", "0x0",
        "-trytofit", "1"
      // aspecttolerance
      );

      var fontPixelFormats = Args("-cs", "JPEG") + Or(
        grayscalePixelFormatsHeavy,
        grayscalePixelFormatsMedium,
        grayscalePixelFormatsBudget,
        grayscaleExamplePixelFormats);

      var fontFamilyCharSources = Or(
        Args("-charListTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\BasicAlphanum.txt"),
        Args("-charListTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\Windows1252.txt"),
        Args("-charListTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\dingbats.txt"),
        Args("-charListTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\unicode\GeometricShapesMono.txt"),
        Args("-charListTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\unicode\Unicode Musical Symbols.txt"),
        Args("-charListTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\unicode\Unicode Tai Xuan Jing Symbols.txt"),
        Args("-charListTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\unicode\UnicodeBoxDrawing.txt"),
        Args("-charListTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\unicode\UnicodeBoxDrawing2.txt"),
        Args("-charListTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\unicode\UnicodeBraille.txt"),
        Args("-charListTextFile", @"C:\root\git\thenfour\PetsciiMapgen\img\fonts\unicode\UnicodeSjis.txt")
        );

      var fontFamilySmoothing = Or(
          Args("-fontsmoothing", "Aliased"),
          Args("-fontsmoothing", "Cleartype"),
          Args("-fontsmoothing", "Grayscale")
        );

      var comicSans = comicSansFont + fontFamilySmoothing + fontFamilyCharSources + fontPixelFormats + (new ArgSet.ArgGeneratorDelegate(s =>
      {
        List<string> ret = new List<string>();
        string[] argArray = s._args.ToArray();
        string fontSmoothing = "(unknown smoothing)";
        argArray.ProcessArg("-fontsmoothing", fontSmoothingArg =>
        {
          fontSmoothing = fontSmoothingArg;
        });
        string fontFamily = "(unknown font)";
        argArray.ProcessArg("-fontfamily", fontFamilyArg =>
        {
          fontFamily = fontFamilyArg;
        });
        argArray.ProcessArg("-fontnametag", fontFamilyArg =>
        {
          fontFamily = fontFamilyArg;
        });

        string charSet = "(unknown charset)";
        argArray.ProcessArg("-charListTextFile", charSetArg =>
        {
          charSet = System.IO.Path.GetFileNameWithoutExtension(charSetArg);
        });
        ret.Add(string.Format("fonttag:{0} {1} {2}", fontSmoothing, fontFamily, charSet));
        return ret;
      }));

      // output directory generator ============================
      var outputDir = new ArgSet.ArgGeneratorDelegate(s =>
      {
        // generate an output directory.
        // use pftag and fonttag
        var pftags = s._args.Where(a => a.StartsWith("pftag:")).Select(a => a.Split(':')[1]);
        var fonttags = s._args.Where(a => a.StartsWith("fonttag:")).Select(a => a.Split(':')[1]);
        var dirName = string.Join(" ", fonttags) + " " + string.Join(" ", pftags);
        var outDir = System.IO.Path.Combine(batchBaseDir, dirName);
        //batchLog.WriteLine("Output directory: {0}", outDir);
        return new string[] { "-outdir", outDir };
      });

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
        marioTiles,
        comicSans
        ) + common + outputDir + Args(batchAddArgs.ToArray());
      return All;
    } // AllBatches
  } // class
} // namespace


