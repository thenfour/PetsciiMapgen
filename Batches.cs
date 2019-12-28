﻿/*
 
we want to generate the following types of outputs:
- usables for each font:
  - high quality
  - color, grayscale
  - just use LAB fivetile B
- demos of various parameters
  - partitions
  - colorspaces HSL LAB NYUV JPEG
  - pixelformats 1x1 2x2 3x3 5tileA 5tileB 5tileC
  - valuespertile 2 8 16 32 1024

 
 */
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
        "-testpalette", "ThreeBit",
        "-loadOrCreateMap"
        );

      // budget versions (1600x1600 = 2560000 map size)
      var grayscalePixelFormatsBudget5 = Or(Args("pftag:Budget5 Grayscale", "-cs", "lab", "-pf", "fivetile", "-pfargs", "19v5+0", "-partitions", "2"));
      var colorPixelFormatsBudget5 = Or(Args("pftag:Budget5 Color", "-cs", "lab", "-pf", "fivetile", "-pfargs", "8v5+2", "-partitions", "2"));

      var grayscalePixelFormatsBudget22 = Or(Args("pftag:Budget22 Grayscale", "-cs", "lab", "-pf", "square", "-pfargs", "40v2x2+0", "-partitions", "2"));
      var colorPixelFormatsBudget22 = Or(Args("pftag:Budget22 Color", "-cs", "lab", "-pf", "square", "-pfargs", "12v2x2+2", "-partitions", "2"));

      var grayscalePixelFormatsBudget11 = Or(Args("pftag:Budget11 Grayscale", "-cs", "lab", "-pf", "square", "-pfargs", "256v1x1+0", "-partitions", "2"));
      var colorPixelFormatsBudget11 = Or(Args("pftag:Budget11 Color", "-cs", "lab", "-pf", "square", "-pfargs", "136v1x1+2", "-partitions", "2"));

      // heavy: aiming for 16384x16384 = map size 268,435,456
      var grayscalePixelFormatsHeavy5 = Or(Args("pftag:Heavy5 Grayscale", "-cs", "lab", "-pf", "fivetile", "-pfargs", "48v5+0", "-partitions", "2"));
      var colorPixelFormatsHeavy5 = Or(Args("pftag:Heavy5 Color", "-pf", "fivetile", "-cs", "lab", "fivetile", "-pfargs", "16v5+2", "-partitions", "2"));

      var grayscalePixelFormatsHeavy22 = Or(Args("pftag:Heavy22 Grayscale", "-cs", "lab", "-pf", "square", "-pfargs", "128v2x2+0", "-partitions", "2"));
      var colorPixelFormatsHeavy22 = Or(Args("pftag:Heavy22 Color", "-cs", "lab", "-pf", "square", "-pfargs", "25v2x2+2", "-partitions", "2"));

      //var grayscalePixelFormatsHeavy11 = Or(Args("pftag:Heavy11 Grayscale", "-cs", "lab", "-pf", "square", "-pfargs", "256v1x1+0", "-partitions", "2"));
      var colorPixelFormatsHeavy11 = Or(Args("pftag:Heavy11 Color", "-cs", "lab", "-pf", "square", "-pfargs", "256v1x1+2", "-partitions", "2"));

      // extreme version. aim for 25,619 x 25,619 = map size 656,356,768
      // tooll doesn't like this size of bitmap. let's stick with heavy.
      //var grayscalePixelFormatsExtreme5 = Or(Args("pftag:Extreme5 Grayscale", "-cs", "lab", "-pf", "fivetile", "-pfargs", "58v5+0", "-partitions", "4"));
      //var colorPixelFormatsExtreme5 = Or(Args("pftag:Extreme5 Color", "-pf", "fivetile", "-cs", "lab", "fivetile", "-pfargs", "18v5+2", "-partitions", "2"));

      //var grayscalePixelFormatsExtreme22 = Or(Args("pftag:Extreme22 Grayscale", "-cs", "lab", "-pf", "square", "-pfargs", "160v2x2+0", "-partitions", "2"));
      //var colorPixelFormatsExtreme22 = Or(Args("pftag:Extreme22 Color", "-cs", "lab", "-pf", "square", "-pfargs", "29v2x2+2", "-partitions", "2"));

      var grayscalePixelFormats = Or(
        //grayscalePixelFormatsExtreme5,
        grayscalePixelFormatsHeavy5,
        grayscalePixelFormatsBudget5,
        //grayscalePixelFormatsExtreme22,
        grayscalePixelFormatsHeavy22,
        grayscalePixelFormatsBudget22,
        //grayscalePixelFormatsExtreme11,
        //grayscalePixelFormatsHeavy11,
        grayscalePixelFormatsBudget11
        );

      var colorPixelFormats = Or(
        //colorPixelFormatsExtreme5,
        colorPixelFormatsHeavy5,
        colorPixelFormatsBudget5,
        //colorPixelFormatsExtreme22,
        colorPixelFormatsHeavy22,
        colorPixelFormatsBudget22,
        //colorPixelFormatsExtreme11,
        colorPixelFormatsHeavy11,
        colorPixelFormatsBudget11
        );

      var fontPixelFormats = Args("-cs", "JPEG") + grayscalePixelFormats;/* Or(
        grayscalePixelFormatsHeavy,
        grayscalePixelFormatsBudget
        );*/

      // C64 ============================
      var C64Font = Args(
        "fonttag:C64",
        "-fonttype", "mono",
        "-fontImage", batchFontPath(@"c64opt160.png"),
        "-charsize", "8x8");

      var c64ColorPalettes = Args("-palette", "C64Color");

      var c64GrayscalePalettes = Or(
          Args("-palette", "BlackAndWhite"),
          Args("-palette", "C64ColorGray8A"),
          Args("-palette", "C64Grays"),
          Args("-palette", "C64ColorGray8B"),
          Args("-palette", "C64Color")
          );

      var C64Color = C64Font + c64ColorPalettes + colorPixelFormats;
      var C64Grayscale = C64Font + c64GrayscalePalettes + grayscalePixelFormats;

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
        Args("-palette", "RGBPrimariesHalftone16"),
        Args("-palette", "BlackAndWhite"),
        //Args("-palette", "Gray3"),
        //Args("-palette", "Gray4"),
        //Args("-palette", "Gray5"),
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
        Args("-palette", "Workbench314"),
        Args("-palette", "RGBPrimariesHalftone16")
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
        Args("-palette", "RGBPrimariesHalftone16"),
        Args("-palette", "ThreeBit"),
        //Args("-palette", "Gray3"),
        //Args("-palette", "Gray4"),
        //Args("-palette", "Gray5"),
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

      var marioTiles = marioTilesFont + Or(colorPixelFormats, grayscalePixelFormats);

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
        marioTiles//,
        //comicSans
        ) + common + outputDir + Args(batchAddArgs.ToArray());
      return All;
    } // AllBatches
  } // class
} // namespace


