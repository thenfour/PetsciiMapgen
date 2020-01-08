// define some palettes to use

// the original purpose was to combine them with character sets of course.
// but now we can also output LUTs for them, so i added tons of extra more 
// stylized palettes not connected to any particular character set.

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
using System.Runtime.InteropServices;

namespace PetsciiMapgen
{
  public static class Palettes
  {
    public static Color[] BlackAndWhite
    {
      get
      {
        return new Color[] {
        Color.FromArgb(  0,   0,   0),// black
        Color.FromArgb(255, 255, 255),//white
        };
      }
    }

    public static Color[] MarioBg
    {
      get
      {// 98f5e9
        return new Color[] {
        Color.FromArgb(  0,   0,   0),// black
        //System.Drawing.ColorTranslator.FromHtml("#98f5e9"),
        //Color.FromArgb(255, 255, 255),//white
        };
      }
    }

    public static Color[] Gray3
    {
      get
      {
        return new Color[] {
        Color.FromArgb(  0,   0,   0),// black
        Color.FromArgb(128, 128, 128),//gray
        Color.FromArgb(255, 255, 255),//white
        };
      }
    }

    public static Color[] Gray4
    {
      get
      {
        return new Color[] {
        Color.FromArgb(  0,   0,   0),// black
        Color.FromArgb( 85,  85,  85),//gray
        Color.FromArgb(170, 170, 170),//gray
        Color.FromArgb(255, 255, 255),//white
        };
      }
    }

    public static Color[] Gray5
    {
      get
      {
        return new Color[] {
        Color.FromArgb(  0,   0,   0),// black
        Color.FromArgb(64, 64, 64),//gray
        Color.FromArgb(128, 128, 128),//gray
        Color.FromArgb(192, 192, 192),//gray
        Color.FromArgb(255, 255, 255),//white
        };
      }
    }

    public static Color[] Gray8
    {
      get
      {
        return new Color[] {
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(36, 36, 36), // 255/7
        Color.FromArgb(72, 72, 72),
        Color.FromArgb(109, 109, 109),
        Color.FromArgb(145, 145, 145),
        Color.FromArgb(182, 182, 182),
        Color.FromArgb(218, 218, 218),
        Color.FromArgb(255, 255, 255),
        };
      }
    }

    public static Color[] C64ColorGray8A
    {
      get
      {
        return new Color[] {
            Color.FromArgb(0, 0, 0), // luminance: 0
            //Color.FromArgb(109, 84, 18), // luminance: 24.9019607901573
            Color.FromArgb(98, 98, 98), // luminance: 38.4313732385635
            //Color.FromArgb(161, 104, 60), // luminance: 43.3333337306976
            Color.FromArgb(80, 69, 155), // luminance: 43.921571969986
            //Color.FromArgb(159, 78, 68), // luminance: 44.5098042488098
            Color.FromArgb(160, 87, 163), // luminance: 49.0196108818054
            //Color.FromArgb(92, 171, 94), // luminance: 51.5686273574829
            //Color.FromArgb(137, 137, 137), // luminance: 53.7254929542542
            Color.FromArgb(106, 191, 198), // luminance: 59.6078455448151
            //Color.FromArgb(203, 126, 117), // luminance: 62.7451002597809
            Color.FromArgb(136, 126, 203), // luminance: 64.5098030567169
            //Color.FromArgb(173, 173, 173), // luminance: 67.8431391716003
            //Color.FromArgb(201, 212, 135), // luminance: 68.0392146110535

            Color.FromArgb(154, 226, 155), // luminance: 74.5098054409027
            Color.FromArgb(255, 255, 255), // luminance: 100
          };
      }
    }

    public static Color[] C64Grays
    {
      get
      {
        return new Color[] {
            Color.FromArgb(0, 0, 0), // luminance: 0
            Color.FromArgb(98, 98, 98), // luminance: 38.4313732385635
            Color.FromArgb(137, 137, 137), // luminance: 53.7254929542542
            Color.FromArgb(173, 173, 173), // luminance: 67.8431391716003
            Color.FromArgb(255, 255, 255), // luminance: 100
          };
      }
    }


    public static Color[] C64ColorGray8B
    {
      get
      {
        return new Color[] {
// in order of luminance.
Color.FromArgb(0, 0, 0), // luminance: 0
Color.FromArgb(109, 84, 18), // luminance: 24.9019607901573
Color.FromArgb(98, 98, 98), // luminance: 38.4313732385635
//Color.FromArgb(161, 104, 60), // luminance: 43.3333337306976
//Color.FromArgb(80, 69, 155), // luminance: 43.921571969986
//Color.FromArgb(159, 78, 68), // luminance: 44.5098042488098
//Color.FromArgb(160, 87, 163), // luminance: 49.0196108818054
Color.FromArgb(92, 171, 94), // luminance: 51.5686273574829
//Color.FromArgb(137, 137, 137), // luminance: 53.7254929542542
//Color.FromArgb(106, 191, 198), // luminance: 59.6078455448151
//Color.FromArgb(203, 126, 117), // luminance: 62.7451002597809
Color.FromArgb(136, 126, 203), // luminance: 64.5098030567169
Color.FromArgb(173, 173, 173), // luminance: 67.8431391716003
//Color.FromArgb(201, 212, 135), // luminance: 68.0392146110535
Color.FromArgb(154, 226, 155), // luminance: 74.5098054409027
Color.FromArgb(255, 255, 255), // luminance: 100
        };
      }
    }

    public static Color[] C64Color
    {
      get
      {
        return new Color[] {
        //Color.FromArgb(  0,   0,   0),// black
        //Color.FromArgb(255, 255, 255),//white

        //Color.FromArgb( 98,  98,  98),//gray1
        //Color.FromArgb(137, 137, 137),// gray2
        //Color.FromArgb(173, 173, 173),//gray3

        //Color.FromArgb(159,  78,  68),// brick red
        //Color.FromArgb(203, 126, 117),// light red
        //Color.FromArgb(109,  84,  18),// dkbrown
        //Color.FromArgb(161, 104,  60),// light brown
        
        //Color.FromArgb(201, 212, 135),// yellowish
        //Color.FromArgb(154, 226, 155),// bright green
        //Color.FromArgb( 92, 171,  94),// darker green
        //Color.FromArgb(106, 191, 198),// cyan
        
        //Color.FromArgb(136, 126, 203),// light purple
        //Color.FromArgb( 80,  69, 155),// dark purple
        //Color.FromArgb(160,  87, 163),// violet

// in order of luminance.
Color.FromArgb(0, 0, 0), // luminance: 0
Color.FromArgb(109, 84, 18), // luminance: 24.9019607901573
Color.FromArgb(98, 98, 98), // luminance: 38.4313732385635
Color.FromArgb(161, 104, 60), // luminance: 43.3333337306976
Color.FromArgb(80, 69, 155), // luminance: 43.921571969986
Color.FromArgb(159, 78, 68), // luminance: 44.5098042488098
Color.FromArgb(160, 87, 163), // luminance: 49.0196108818054
Color.FromArgb(92, 171, 94), // luminance: 51.5686273574829
Color.FromArgb(137, 137, 137), // luminance: 53.7254929542542
Color.FromArgb(106, 191, 198), // luminance: 59.6078455448151
Color.FromArgb(203, 126, 117), // luminance: 62.7451002597809
Color.FromArgb(136, 126, 203), // luminance: 64.5098030567169
Color.FromArgb(173, 173, 173), // luminance: 67.8431391716003
Color.FromArgb(201, 212, 135), // luminance: 68.0392146110535
Color.FromArgb(154, 226, 155), // luminance: 74.5098054409027
Color.FromArgb(255, 255, 255), // luminance: 100
        };
      }
    }
    public static Color[] Workbench134
    {
      get
      {
        return new Color[] {
        Color.FromArgb(  0,   1,  32),
        Color.FromArgb(248, 248, 248),
        Color.FromArgb(  0,  86, 173),
        Color.FromArgb(255,  138, 0 ),
      };
      }
    }
    public static Color[] Workbench314
    {
      get
      {
        return new Color[] {
        Color.FromArgb(  0,   0,  0),
        Color.FromArgb(181,178, 181),
        Color.FromArgb(  107,  142, 198),
        Color.FromArgb(255,  255, 255),
      };
      }
    }

    public static Color[] ThreeBit
    {
      get
      {
        return new Color[] {
      Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 255),
        Color.FromArgb(0, 255, 0),
        Color.FromArgb(0, 255, 255),
        Color.FromArgb(255, 0, 0),
        Color.FromArgb(255, 0, 255),
        Color.FromArgb(255, 255, 0),
        Color.FromArgb(255, 255, 255),
        };
      }
    }

    public static Color[] RGBPrimariesHalftone16
    {
      get
      {
        return new Color[] { // RGB primaries + halftone
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0,255),
        Color.FromArgb(0, 255,0),
        Color.FromArgb(0, 255,255),
        Color.FromArgb(255, 0,0),
        Color.FromArgb(255, 0,255),
        Color.FromArgb(255, 255,0),
        Color.FromArgb(255, 255,255),

        Color.FromArgb(128, 128, 128),
        Color.FromArgb(0, 0,128),
        Color.FromArgb(0, 128,0),
        Color.FromArgb(0, 128,128),
        Color.FromArgb(128, 0,0),
        Color.FromArgb(128, 0,128),
        Color.FromArgb(128, 128,0),
        Color.FromArgb(192, 192,192),
      };

      }
    }

    // https://en.wikipedia.org/wiki/List_of_software_palettes
    public static Color[] Windows16
    {
      get
      {
        return new Color[]
        {
          ColorTranslator.FromHtml("#000000"),// black
          ColorTranslator.FromHtml("#808080"),// gray
          ColorTranslator.FromHtml("#800000"),// maroon
          ColorTranslator.FromHtml("#ff0000"),// red
          ColorTranslator.FromHtml("#008000"),// green
          ColorTranslator.FromHtml("#00ff00"),// lime
          ColorTranslator.FromHtml("#808000"),// olive
          ColorTranslator.FromHtml("#ffff00"),// yellow
          ColorTranslator.FromHtml("#000080"),// navy
          ColorTranslator.FromHtml("#0000ff"),// blue
          ColorTranslator.FromHtml("#800080"),// purple
          ColorTranslator.FromHtml("#ff00ff"),// fushsia
          ColorTranslator.FromHtml("#008080"),// teal
          ColorTranslator.FromHtml("#00ffff"),// aqua
          ColorTranslator.FromHtml("#c0c0c0"),// silver
          ColorTranslator.FromHtml("#ffffff"),// white

        };
      }
    }

    // https://en.wikipedia.org/wiki/List_of_software_palettes
    public static Color[] Windows20
    {
      get
      {
        return new Color[]
        {
          ColorTranslator.FromHtml("#000000"),// black
          ColorTranslator.FromHtml("#fffbf0"),// cream
          ColorTranslator.FromHtml("#800000"),// maroon
          ColorTranslator.FromHtml("#a0a0a4"),// medium grey
          ColorTranslator.FromHtml("#008000"),// green
          ColorTranslator.FromHtml("#808080"),// dark gray
          ColorTranslator.FromHtml("#808000"),// olive
          ColorTranslator.FromHtml("#ff0000"),// red
          ColorTranslator.FromHtml("#000080"),// navy
          ColorTranslator.FromHtml("#00ff00"),// lime
          ColorTranslator.FromHtml("#800080"),// purple
          ColorTranslator.FromHtml("#ffff00"),// yellow
          ColorTranslator.FromHtml("#008080"),// teal
          ColorTranslator.FromHtml("#0000ff"),// blue
          ColorTranslator.FromHtml("#c0c0c0"),// light grey
          ColorTranslator.FromHtml("#ff00ff"),// magenta
          ColorTranslator.FromHtml("#c0dcc0"),// money green
          ColorTranslator.FromHtml("#00ffff"),// aqua
          ColorTranslator.FromHtml("#a6caf0"),// sky blue
          ColorTranslator.FromHtml("#ffffff"),// white

        };
      }
    }




    // https://en.wikipedia.org/wiki/List_of_software_palettes
    public static Color[] Macintosh16
    {
      get
      {
        return new Color[]
        {
          ColorTranslator.FromHtml("#ffffff"),// white
          ColorTranslator.FromHtml("#1fb714"),// green
          ColorTranslator.FromHtml("#006412"),// dark green
          ColorTranslator.FromHtml("#ff6403"),// orange
          ColorTranslator.FromHtml("#562c05"),// brown
          ColorTranslator.FromHtml("#dd0907"),// red
          ColorTranslator.FromHtml("#90713a"),// tan
          ColorTranslator.FromHtml("#f20884"),// magenta
          ColorTranslator.FromHtml("#4700a5"),// purple
          ColorTranslator.FromHtml("#0000d3"),// blue
          ColorTranslator.FromHtml("#02abea"),// cyan

          ColorTranslator.FromHtml("#c0c0c0"),// lt grey
          ColorTranslator.FromHtml("#808080"),// med grey
          ColorTranslator.FromHtml("#404040"),// dark grey
          ColorTranslator.FromHtml("#000000"),// black
        };
      }
    }






    // https://en.wikipedia.org/wiki/List_of_software_palettes
    public static Color[] AcornRISC16
    {
      get
      {
        return new Color[]
        {
          ColorTranslator.FromHtml("#ffffff"),// white
          ColorTranslator.FromHtml("#dddddd"),// grey1
          ColorTranslator.FromHtml("#bbbbbb"),// grey2
          ColorTranslator.FromHtml("#999999"),// grey3
          ColorTranslator.FromHtml("#777777"),// grey4
          ColorTranslator.FromHtml("#555555"),// grey5
          ColorTranslator.FromHtml("#333333"),// grey6
          ColorTranslator.FromHtml("#000000"),// black

          ColorTranslator.FromHtml("#004499"),// dk blue
          ColorTranslator.FromHtml("#eeee00"),// yellow
          ColorTranslator.FromHtml("#00cc00"),// green
          ColorTranslator.FromHtml("#dd0000"),// red
          ColorTranslator.FromHtml("#eeeebb"),// beige
          ColorTranslator.FromHtml("#558800"),// dk green
          ColorTranslator.FromHtml("#ffbb00"),// orange
          ColorTranslator.FromHtml("#00bbff"),// lt blue
        };
      }
    }







    // https://en.wikipedia.org/wiki/List_of_software_palettes
    public static Color[] Intellivision
    {
      get
      {
        return new Color[]
        {
          ColorTranslator.FromHtml("#0c0005"), // BLACK
          ColorTranslator.FromHtml("#002dff"), // BLUE
          ColorTranslator.FromHtml("#ff3e00"), // RED
          ColorTranslator.FromHtml("#c9d464"), // TAN
          ColorTranslator.FromHtml("#00780f"), // DARK GREEN
          ColorTranslator.FromHtml("#00a720"), // GREEN
          ColorTranslator.FromHtml("#faea27"), // YELLOW
          ColorTranslator.FromHtml("#fffcff"), // WHITE
          ColorTranslator.FromHtml("#a7a8a8"), // GRAY
          ColorTranslator.FromHtml("#5acbff"), // CYAN
          ColorTranslator.FromHtml("#ffa600"), // ORANGE
          ColorTranslator.FromHtml("#3c5800"), // BROWN
          ColorTranslator.FromHtml("#ff3276"), // PINK
          ColorTranslator.FromHtml("#bd95ff"), // LIGHT BLUE
          ColorTranslator.FromHtml("#6ccd30"), // YELLOW GREEN
          ColorTranslator.FromHtml("#c81a7d"), // PURPLE
        };
      }
    }




    // https://lospec.com/palette-list/tag/endesga
    public static Color[] ENDESGA16
    {
      get
      {
        return new Color[]
        {
          ColorTranslator.FromHtml("#e4a672"),
          ColorTranslator.FromHtml("#63c64d"),

          ColorTranslator.FromHtml("#b86f50"),
          ColorTranslator.FromHtml("#327345"),

          ColorTranslator.FromHtml("#743f39"),
          ColorTranslator.FromHtml("#193d3f"),

          ColorTranslator.FromHtml("#3f2832"),
          ColorTranslator.FromHtml("#4f6781"),

          ColorTranslator.FromHtml("#9e2835"),
          ColorTranslator.FromHtml("#afbfd2"),

          ColorTranslator.FromHtml("#e53b44"),
          ColorTranslator.FromHtml("#ffffff"),
          ColorTranslator.FromHtml("#fb922b"),
          ColorTranslator.FromHtml("#2ce8f4"),
          ColorTranslator.FromHtml("#ffe762"),
          ColorTranslator.FromHtml("#0484d1"),
        };
      }
    }





    // https://lospec.com/palette-list/tag/endesga
    public static Color[] EN4
    {
      get
      {
        return new Color[]
        {
          ColorTranslator.FromHtml("#fbf7f3"),
          ColorTranslator.FromHtml("#e5b083"),

          ColorTranslator.FromHtml("#426e5d"),
          ColorTranslator.FromHtml("#20283d"),

        };
      }
    }





    // https://lospec.com/palette-list/tag/endesga
    public static Color[] ENDESGASOFT16
    {
      get
      {
        return new Color[]
        {
          ColorTranslator.FromHtml("#fefed7"),
          ColorTranslator.FromHtml("#a9aba3"),

          ColorTranslator.FromHtml("#dbbc96"),
          ColorTranslator.FromHtml("#666869"),

          ColorTranslator.FromHtml("#ddac46"),
          ColorTranslator.FromHtml("#51b1ca"),

          ColorTranslator.FromHtml("#c25940"),
          ColorTranslator.FromHtml("#1773b8"),

          ColorTranslator.FromHtml("#683d64"),
          ColorTranslator.FromHtml("#639f5b"),

          ColorTranslator.FromHtml("#9c6659"),
          ColorTranslator.FromHtml("#376e49"),

          ColorTranslator.FromHtml("#88434f"),
          ColorTranslator.FromHtml("#323441"),

          ColorTranslator.FromHtml("#4d2831"),
          ColorTranslator.FromHtml("#161323"),

        };
      }
    }







    // https://lospec.com/palette-list/tag/endesga
    public static Color[] ARQ4
    {
      get
      {
        return new Color[]
        {
          ColorTranslator.FromHtml("#ffffff"),
          ColorTranslator.FromHtml("#6772a9"),
          ColorTranslator.FromHtml("#3a3277"),
          ColorTranslator.FromHtml("#000000"),

        };
      }
    }





    // https://lospec.com/palette-list/tag/endesga
    public static Color[] ARQ16
    {
      get
      {
        return new Color[]
        {
          ColorTranslator.FromHtml("#ffffff"),
          ColorTranslator.FromHtml("#ffd19d"),

          ColorTranslator.FromHtml("#aeb5bd"),
          ColorTranslator.FromHtml("#4d80c9"),
                                    
          ColorTranslator.FromHtml("#e93841"),
          ColorTranslator.FromHtml("#100820"),
                                    
          ColorTranslator.FromHtml("#511e43"),
          ColorTranslator.FromHtml("#054494"),
                                    
          ColorTranslator.FromHtml("#f1892d"),
          ColorTranslator.FromHtml("#823e2c"),
                                    
          ColorTranslator.FromHtml("#ffa9a9"),
          ColorTranslator.FromHtml("#5ae150"),
                                    
          ColorTranslator.FromHtml("#ffe947"),
          ColorTranslator.FromHtml("#7d3ebf"),
                                    
          ColorTranslator.FromHtml("#eb6c82"),
          ColorTranslator.FromHtml("#1e8a4c"),

        };
      }
    }



    // https://lospec.com/palette-list/sweetie-16
    public static Color[] SWEETIE16
    {
      get
      {
        return new Color[]
        {

ColorTranslator.FromHtml("#1a1c2c"),
ColorTranslator.FromHtml("#5d275d"),
ColorTranslator.FromHtml("#b13e53"),
ColorTranslator.FromHtml("#ef7d57"),
ColorTranslator.FromHtml("#ffcd75"),
ColorTranslator.FromHtml("#a7f070"),
ColorTranslator.FromHtml("#38b764"),
ColorTranslator.FromHtml("#257179"),
ColorTranslator.FromHtml("#29366f"),
ColorTranslator.FromHtml("#3b5dc9"),
ColorTranslator.FromHtml("#41a6f6"),
ColorTranslator.FromHtml("#73eff7"),
ColorTranslator.FromHtml("#f4f4f4"),
ColorTranslator.FromHtml("#94b0c2"),
ColorTranslator.FromHtml("#566c86"),
ColorTranslator.FromHtml("#333c57"),

        };
      }
    }






    public static Color[] NYX8
    {
      get
      {
        return new Color[]
        {

// NYX8 PALETTE
// https://lospec.com/palette-list/nyx8

ColorTranslator.FromHtml("#08141e"),
ColorTranslator.FromHtml("#0f2a3f"),
ColorTranslator.FromHtml("#20394f"),
ColorTranslator.FromHtml("#f6d6bd"),
ColorTranslator.FromHtml("#c3a38a"),
ColorTranslator.FromHtml("#997577"),
ColorTranslator.FromHtml("#816271"),
ColorTranslator.FromHtml("#4e495f"),

        };
      }
    }



    public static Color[] SLSO8
    {
      get
      {
        return new Color[]
        {

          // blue & orange shades
// SLSO8
ColorTranslator.FromHtml("#0d2b45"),
ColorTranslator.FromHtml("#203c56"),
ColorTranslator.FromHtml("#544e68"),
ColorTranslator.FromHtml("#8d697a"),
ColorTranslator.FromHtml("#d08159"),
ColorTranslator.FromHtml("#ffaa5e"),
ColorTranslator.FromHtml("#ffd4a3"),
ColorTranslator.FromHtml("#ffecd6"),

        };
      }
    }



    public static Color[] POLLEN8
    {
      get
      {
        return new Color[]
        {

// POLLEN8
ColorTranslator.FromHtml("#73464c"),
ColorTranslator.FromHtml("#ab5675"),
ColorTranslator.FromHtml("#ee6a7c"),
ColorTranslator.FromHtml("#ffa7a5"),
ColorTranslator.FromHtml("#ffe07e"),
ColorTranslator.FromHtml("#ffe7d6"),
ColorTranslator.FromHtml("#72dcbb"),
ColorTranslator.FromHtml("#34acba"),

        };
      }
    }



    public static Color[] DAWNBRINGER8
    {
      get
      {
        return new Color[]
        {


// DAWNBRINGER'S 8 COLOR PALETTE
ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#55415f"),
ColorTranslator.FromHtml("#646964"),
ColorTranslator.FromHtml("#d77355"),
ColorTranslator.FromHtml("#508cd7"),
ColorTranslator.FromHtml("#64b964"),
ColorTranslator.FromHtml("#e6c86e"),
ColorTranslator.FromHtml("#dcf5ff"),

        };
      }
    }



    public static Color[] CGA1HIGH4
    {
      get
      {
        return new Color[]
        {

// CGA PALETTE 1 (HIGH) PALETTE
ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#ff55ff"),
ColorTranslator.FromHtml("#55ffff"),
ColorTranslator.FromHtml("#ffffff"),


        };
      }
    }


    public static Color[] FUZZY4
    {
      get
      {
        return new Color[]
        {
          // https://lospec.com/palette-list/fuzzyfour
// FUZZYFOUR PALETTE
ColorTranslator.FromHtml("#302387"),
ColorTranslator.FromHtml("#ff3796"),
ColorTranslator.FromHtml("#00faac"),
ColorTranslator.FromHtml("#fffdaf"),

        };
      }
    }



    public static Color[] RABBIT8
    {
      get
      {
        return new Color[]
        {
          // balanced-ish
// RABBIT PALETTE
ColorTranslator.FromHtml("#d47564"),
ColorTranslator.FromHtml("#e8c498"),
ColorTranslator.FromHtml("#ecece0"),
ColorTranslator.FromHtml("#4fa4a5"),
ColorTranslator.FromHtml("#aad395"),
ColorTranslator.FromHtml("#3b324a"),
ColorTranslator.FromHtml("#5c6182"),

        };
      }
    }



    public static Color[] RKBV8
    {
      get
      {
        return new Color[]
        {
// https://lospec.com/palette-list/rkbv
// RKBV PALETTE
// pink & cyan shades
ColorTranslator.FromHtml("#15191a"),
ColorTranslator.FromHtml("#8a4c58"),
ColorTranslator.FromHtml("#d96275"),
ColorTranslator.FromHtml("#e6b8c1"),

ColorTranslator.FromHtml("#456b73"),
ColorTranslator.FromHtml("#4b97a6"),
ColorTranslator.FromHtml("#a5bdc2"),
ColorTranslator.FromHtml("#fff5f7"),


        };
      }
    }



    public static Color[] FUNKYFUTURE8
    {
      get
      {
        return new Color[]
        {


//FUNKYFUTURE 8 PALETTE
ColorTranslator.FromHtml("#2b0f54"),
ColorTranslator.FromHtml("#ab1f65"),
ColorTranslator.FromHtml("#ff4f69"),
ColorTranslator.FromHtml("#fff7f8"),
ColorTranslator.FromHtml("#ff8142"),
ColorTranslator.FromHtml("#ffda45"),
ColorTranslator.FromHtml("#3368dc"),
ColorTranslator.FromHtml("#49e7ec"),


        };
      }
    }



    public static Color[] CGA2HIGH
    {
      get
      {
        return new Color[]
        {


//CGA PALETTE 2 (HIGH) PALETTE

ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#FF5555"),
ColorTranslator.FromHtml("#55FFFF"),
ColorTranslator.FromHtml("#FFFFFF"),


        };
      }
    }



    public static Color[] FANTASTIC8
    {
      get
      {
        return new Color[]
        {


// FANTASTIC 8 PALETTE

ColorTranslator.FromHtml("#dbdbdd"),
ColorTranslator.FromHtml("#9c8e8e"),
ColorTranslator.FromHtml("#524ba3"),
ColorTranslator.FromHtml("#1f1e23"),
ColorTranslator.FromHtml("#6a9254"),
ColorTranslator.FromHtml("#dcac6c"),
ColorTranslator.FromHtml("#e55656"),
ColorTranslator.FromHtml("#73493c"),

        };
      }
    }



    public static Color[] CGA0LOW4
    {
      get
      {
        return new Color[]
        {


//CGA PALETTE 0 (LOW) PALETTE

ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#00aa00"),
ColorTranslator.FromHtml("#aa0000"),
ColorTranslator.FromHtml("#aa5500"),

        };
      }
    }



    public static Color[] PICO8
    {
      get
      {
        return new Color[]
        {


//PICO-8 PALETTE
ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#1D2B53"),
ColorTranslator.FromHtml("#7E2553"),
ColorTranslator.FromHtml("#008751"),
ColorTranslator.FromHtml("#AB5236"),
ColorTranslator.FromHtml("#5F574F"),
ColorTranslator.FromHtml("#C2C3C7"),
ColorTranslator.FromHtml("#FFF1E8"),
ColorTranslator.FromHtml("#FF004D"),
ColorTranslator.FromHtml("#FFA300"),
ColorTranslator.FromHtml("#FFEC27"),
ColorTranslator.FromHtml("#00E436"),
ColorTranslator.FromHtml("#29ADFF"),
ColorTranslator.FromHtml("#83769C"),
ColorTranslator.FromHtml("#FF77A8"),
ColorTranslator.FromHtml("#FFCCAA"),


        };
      }
    }



    public static Color[] NA16
    {
      get
      {
        return new Color[]
        {
          // https://lospec.com/palette-list/na16
          // general purpose
//NA16 PALETTE
ColorTranslator.FromHtml("#8c8fae"),
ColorTranslator.FromHtml("#584563"),
ColorTranslator.FromHtml("#3e2137"),
ColorTranslator.FromHtml("#9a6348"),
ColorTranslator.FromHtml("#d79b7d"),
ColorTranslator.FromHtml("#f5edba"),
ColorTranslator.FromHtml("#c0c741"),
ColorTranslator.FromHtml("#647d34"),
ColorTranslator.FromHtml("#e4943a"),
ColorTranslator.FromHtml("#9d303b"),
ColorTranslator.FromHtml("#d26471"),
ColorTranslator.FromHtml("#70377f"),
ColorTranslator.FromHtml("#7ec4c1"),
ColorTranslator.FromHtml("#34859d"),
ColorTranslator.FromHtml("#17434b"),
ColorTranslator.FromHtml("#1f0e1c"),


        };
      }
    }



    public static Color[] STEAMLORDS
    {
      get
      {
        return new Color[]
        {
          // https://lospec.com/palette-list/steam-lords
//STEAM LORDS PALETTE
ColorTranslator.FromHtml("#213b25"),
ColorTranslator.FromHtml("#3a604a"),
ColorTranslator.FromHtml("#4f7754"),
ColorTranslator.FromHtml("#a19f7c"),

ColorTranslator.FromHtml("#77744f"),
ColorTranslator.FromHtml("#775c4f"),
ColorTranslator.FromHtml("#603b3a"),
ColorTranslator.FromHtml("#3b2137"),

ColorTranslator.FromHtml("#170e19"),
ColorTranslator.FromHtml("#2f213b"),
ColorTranslator.FromHtml("#433a60"),
ColorTranslator.FromHtml("#4f5277"),

ColorTranslator.FromHtml("#65738c"),
ColorTranslator.FromHtml("#7c94a1"),
ColorTranslator.FromHtml("#a0b9ba"),
ColorTranslator.FromHtml("#c0d1cc"),



        };
      }
    }



    public static Color[] LOSPEC_COM_COMMODORE64
    {
      get
      {
        return new Color[]
        {

// COMMODORE 64 PALETTE
ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#626262"),
ColorTranslator.FromHtml("#898989"),
ColorTranslator.FromHtml("#adadad"),
ColorTranslator.FromHtml("#ffffff"),
ColorTranslator.FromHtml("#9f4e44"),
ColorTranslator.FromHtml("#cb7e75"),
ColorTranslator.FromHtml("#6d5412"),
ColorTranslator.FromHtml("#a1683c"),
ColorTranslator.FromHtml("#c9d487"),
ColorTranslator.FromHtml("#9ae29b"),
ColorTranslator.FromHtml("#5cab5e"),
ColorTranslator.FromHtml("#6abfc6"),
ColorTranslator.FromHtml("#887ecb"),
ColorTranslator.FromHtml("#50459b"),
ColorTranslator.FromHtml("#a057a3"),


        };
      }
    }



    public static Color[] MSXJMP
    {
      get
      {
        return new Color[]
        {


//MSX / JMP (JAPANESE MACHINE PALETTE) PALETTE
ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#191028"),
ColorTranslator.FromHtml("#46af45"),
ColorTranslator.FromHtml("#a1d685"),
ColorTranslator.FromHtml("#453e78"),
ColorTranslator.FromHtml("#7664fe"),
ColorTranslator.FromHtml("#833129"),
ColorTranslator.FromHtml("#9ec2e8"),
ColorTranslator.FromHtml("#dc534b"),
ColorTranslator.FromHtml("#e18d79"),
ColorTranslator.FromHtml("#d6b97b"),
ColorTranslator.FromHtml("#e9d8a1"),
ColorTranslator.FromHtml("#216c4b"),
ColorTranslator.FromHtml("#d365c8"),
ColorTranslator.FromHtml("#afaab9"),
ColorTranslator.FromHtml("#f5f4eb"),


        };
      }
    }



    public static Color[] PSYGNOSIA
    {
      get
      {
        return new Color[]
        {



// PSYGNOSIA PALETTE
ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#1b1e29"),
ColorTranslator.FromHtml("#362747"),
ColorTranslator.FromHtml("#443f41"),
ColorTranslator.FromHtml("#52524c"),
ColorTranslator.FromHtml("#64647c"),
ColorTranslator.FromHtml("#736150"),
ColorTranslator.FromHtml("#77785b"),
ColorTranslator.FromHtml("#9ea4a7"),
ColorTranslator.FromHtml("#cbe8f7"),
ColorTranslator.FromHtml("#e08b79"),
ColorTranslator.FromHtml("#a2324e"),
ColorTranslator.FromHtml("#003308"),
ColorTranslator.FromHtml("#084a3c"),
ColorTranslator.FromHtml("#546a00"),
ColorTranslator.FromHtml("#516cbf"),


        };
      }
    }



    public static Color[] FANTASY16
    {
      get
      {
        return new Color[]
        {

// FANTASY 16 PALETTE
ColorTranslator.FromHtml("#8e6d34"),
ColorTranslator.FromHtml("#513a18"),
ColorTranslator.FromHtml("#332710"),
ColorTranslator.FromHtml("#14130c"),
ColorTranslator.FromHtml("#461820"),
ColorTranslator.FromHtml("#a63c1e"),
ColorTranslator.FromHtml("#d37b1e"),
ColorTranslator.FromHtml("#e7bc4f"),
ColorTranslator.FromHtml("#eeeefa"),
ColorTranslator.FromHtml("#d9d55b"),
ColorTranslator.FromHtml("#757320"),
ColorTranslator.FromHtml("#14210f"),
ColorTranslator.FromHtml("#040405"),
ColorTranslator.FromHtml("#1c1b2f"),
ColorTranslator.FromHtml("#435063"),
ColorTranslator.FromHtml("#60a18f"),


        };
      }
    }



    public static Color[] CGA16
    {
      get
      {
        return new Color[]
        {



//COLOR GRAPHICS ADAPTER PALETTE

ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#555555"),
ColorTranslator.FromHtml("#AAAAAA"),
ColorTranslator.FromHtml("#FFFFFF"),
ColorTranslator.FromHtml("#0000AA"),
ColorTranslator.FromHtml("#5555FF"),
ColorTranslator.FromHtml("#00AA00"),
ColorTranslator.FromHtml("#55FF55"),
ColorTranslator.FromHtml("#00AAAA"),
ColorTranslator.FromHtml("#55FFFF"),
ColorTranslator.FromHtml("#AA0000"),
ColorTranslator.FromHtml("#FF5555"),
ColorTranslator.FromHtml("#AA00AA"),
ColorTranslator.FromHtml("#FF55FF"),
ColorTranslator.FromHtml("#AA5500"),
ColorTranslator.FromHtml("#FFFF55"),

        };
      }
    }



    public static Color[] AAP16
    {
      get
      {
        return new Color[]
        {

          // general purpose balanced
          // https://lospec.com/palette-list/aap-16
//AAP-16 PALETTE
ColorTranslator.FromHtml("#070708"),
ColorTranslator.FromHtml("#332222"),
ColorTranslator.FromHtml("#774433"),
ColorTranslator.FromHtml("#cc8855"),
ColorTranslator.FromHtml("#993311"),
ColorTranslator.FromHtml("#dd7711"),
ColorTranslator.FromHtml("#ffdd55"),
ColorTranslator.FromHtml("#ffff33"),
ColorTranslator.FromHtml("#55aa44"),
ColorTranslator.FromHtml("#115522"),
ColorTranslator.FromHtml("#44eebb"),
ColorTranslator.FromHtml("#3388dd"),
ColorTranslator.FromHtml("#5544aa"),
ColorTranslator.FromHtml("#555577"),
ColorTranslator.FromHtml("#aabbbb"),
ColorTranslator.FromHtml("#ffffff"),


        };
      }
    }



    public static Color[] SIMPLEJPC16_MSX_PC88
    {
      get
      {
        return new Color[]
        {


// SIMPLEJPC-16 PALETTE (inspired by MSX / PC88)
ColorTranslator.FromHtml("#050403"),
ColorTranslator.FromHtml("#221f31"),
ColorTranslator.FromHtml("#543516"),
ColorTranslator.FromHtml("#9b6e2d"),
ColorTranslator.FromHtml("#e1b047"),
ColorTranslator.FromHtml("#f5ee9b"),
ColorTranslator.FromHtml("#fefefe"),
ColorTranslator.FromHtml("#8be1e0"),
ColorTranslator.FromHtml("#7cc264"),
ColorTranslator.FromHtml("#678fcb"),
ColorTranslator.FromHtml("#316f23"),
ColorTranslator.FromHtml("#404a68"),
ColorTranslator.FromHtml("#a14d3f"),
ColorTranslator.FromHtml("#a568d4"),
ColorTranslator.FromHtml("#9a93b7"),
ColorTranslator.FromHtml("#ea9182"),


        };
      }
    }



    public static Color[] GALAXYFLAME16
    {
      get
      {
        return new Color[]
        {


          //GALAXYFLAME
          // https://lospec.com/palette-list/galaxy-flame

// GALAXY FLAME PALETTE
ColorTranslator.FromHtml("#699fad"),
ColorTranslator.FromHtml("#3a708e"),
ColorTranslator.FromHtml("#2b454f"),
ColorTranslator.FromHtml("#111215"),
ColorTranslator.FromHtml("#151d1a"),
ColorTranslator.FromHtml("#1d3230"),
ColorTranslator.FromHtml("#314e3f"),
ColorTranslator.FromHtml("#4f5d42"),
ColorTranslator.FromHtml("#9a9f87"),
ColorTranslator.FromHtml("#ede6cb"),
ColorTranslator.FromHtml("#f5d893"),
ColorTranslator.FromHtml("#e8b26f"),
ColorTranslator.FromHtml("#b6834c"),
ColorTranslator.FromHtml("#704d2b"),
ColorTranslator.FromHtml("#40231e"),
ColorTranslator.FromHtml("#151015"),


        };
      }
    }



    public static Color[] CRIMSO11
    {
      get
      {
        return new Color[]
        {


//CRIMSO 11 PALETTE
// balanced

ColorTranslator.FromHtml("#ffffe3"),
ColorTranslator.FromHtml("#f3d762"),
ColorTranslator.FromHtml("#bf9651"),
ColorTranslator.FromHtml("#769a55"),
ColorTranslator.FromHtml("#cb5e31"),
ColorTranslator.FromHtml("#8e393d"),
ColorTranslator.FromHtml("#7a4962"),
ColorTranslator.FromHtml("#5e4531"),
ColorTranslator.FromHtml("#8ec3cf"),
ColorTranslator.FromHtml("#867696"),
ColorTranslator.FromHtml("#456e51"),
ColorTranslator.FromHtml("#3d6286"),
ColorTranslator.FromHtml("#353d5a"),
ColorTranslator.FromHtml("#232e32"),
ColorTranslator.FromHtml("#41292d"),
ColorTranslator.FromHtml("#110b11"),

        };
      }
    }



    public static Color[] EUROPA16
    {
      get
      {
        return new Color[]
        {



//EUROPA 16 PALETTE
ColorTranslator.FromHtml("#ffffff"),
ColorTranslator.FromHtml("#75ceea"),
ColorTranslator.FromHtml("#317ad7"),
ColorTranslator.FromHtml("#283785"),
ColorTranslator.FromHtml("#1a1b35"),
ColorTranslator.FromHtml("#2e354e"),
ColorTranslator.FromHtml("#4f6678"),
ColorTranslator.FromHtml("#a4bcc2"),
ColorTranslator.FromHtml("#ecf860"),
ColorTranslator.FromHtml("#94d446"),
ColorTranslator.FromHtml("#3b7850"),
ColorTranslator.FromHtml("#20322e"),
ColorTranslator.FromHtml("#512031"),
ColorTranslator.FromHtml("#a43e4b"),
ColorTranslator.FromHtml("#dc7d5e"),
ColorTranslator.FromHtml("#f0cc90"),



        };
      }
    }



    public static Color[] NES_FULL
    {
      get
      {
        return new Color[]
        {





// https://lospec.com/palette-list/nintendo-entertainment-system
// NES
ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#fcfcfc"),
ColorTranslator.FromHtml("#f8f8f8"),
ColorTranslator.FromHtml("#bcbcbc"),
ColorTranslator.FromHtml("#7c7c7c"),
ColorTranslator.FromHtml("#a4e4fc"),
ColorTranslator.FromHtml("#3cbcfc"),
ColorTranslator.FromHtml("#0078f8"),
ColorTranslator.FromHtml("#0000fc"),
ColorTranslator.FromHtml("#b8b8f8"),
ColorTranslator.FromHtml("#6888fc"),
ColorTranslator.FromHtml("#0058f8"),
ColorTranslator.FromHtml("#0000bc"),
ColorTranslator.FromHtml("#d8b8f8"),
ColorTranslator.FromHtml("#9878f8"),
ColorTranslator.FromHtml("#6844fc"),
ColorTranslator.FromHtml("#4428bc"),
ColorTranslator.FromHtml("#f8b8f8"),
ColorTranslator.FromHtml("#f878f8"),
ColorTranslator.FromHtml("#d800cc"),
ColorTranslator.FromHtml("#940084"),
ColorTranslator.FromHtml("#f8a4c0"),
ColorTranslator.FromHtml("#f85898"),
ColorTranslator.FromHtml("#e40058"),
ColorTranslator.FromHtml("#a80020"),
ColorTranslator.FromHtml("#f0d0b0"),
ColorTranslator.FromHtml("#f87858"),
ColorTranslator.FromHtml("#f83800"),
ColorTranslator.FromHtml("#a81000"),
ColorTranslator.FromHtml("#fce0a8"),
ColorTranslator.FromHtml("#fca044"),
ColorTranslator.FromHtml("#e45c10"),
ColorTranslator.FromHtml("#881400"),
ColorTranslator.FromHtml("#f8d878"),
ColorTranslator.FromHtml("#f8b800"),
ColorTranslator.FromHtml("#ac7c00"),
ColorTranslator.FromHtml("#503000"),
ColorTranslator.FromHtml("#d8f878"),
ColorTranslator.FromHtml("#b8f818"),
ColorTranslator.FromHtml("#00b800"),
ColorTranslator.FromHtml("#007800"),
ColorTranslator.FromHtml("#b8f8b8"),
ColorTranslator.FromHtml("#58d854"),
ColorTranslator.FromHtml("#00a800"),
ColorTranslator.FromHtml("#006800"),
ColorTranslator.FromHtml("#b8f8d8"),
ColorTranslator.FromHtml("#58f898"),
ColorTranslator.FromHtml("#00a844"),
ColorTranslator.FromHtml("#005800"),
ColorTranslator.FromHtml("#00fcfc"),
ColorTranslator.FromHtml("#00e8d8"),
ColorTranslator.FromHtml("#008888"),
ColorTranslator.FromHtml("#004058"),
ColorTranslator.FromHtml("#f8d8f8"),
ColorTranslator.FromHtml("#787878"),


        };
      }
    }



    public static Color[] ENDESGA32
    {
      get
      {
        return new Color[]
        {


//ENDESGA 32 PALETTE
ColorTranslator.FromHtml("#be4a2f"),
ColorTranslator.FromHtml("#d77643"),
ColorTranslator.FromHtml("#ead4aa"),
ColorTranslator.FromHtml("#e4a672"),
ColorTranslator.FromHtml("#b86f50"),
ColorTranslator.FromHtml("#733e39"),
ColorTranslator.FromHtml("#3e2731"),
ColorTranslator.FromHtml("#a22633"),
ColorTranslator.FromHtml("#e43b44"),
ColorTranslator.FromHtml("#f77622"),
ColorTranslator.FromHtml("#feae34"),
ColorTranslator.FromHtml("#fee761"),
ColorTranslator.FromHtml("#63c74d"),
ColorTranslator.FromHtml("#3e8948"),
ColorTranslator.FromHtml("#265c42"),
ColorTranslator.FromHtml("#193c3e"),
ColorTranslator.FromHtml("#124e89"),
ColorTranslator.FromHtml("#0099db"),
ColorTranslator.FromHtml("#2ce8f5"),
ColorTranslator.FromHtml("#ffffff"),
ColorTranslator.FromHtml("#c0cbdc"),
ColorTranslator.FromHtml("#8b9bb4"),
ColorTranslator.FromHtml("#5a6988"),
ColorTranslator.FromHtml("#3a4466"),
ColorTranslator.FromHtml("#262b44"),
ColorTranslator.FromHtml("#181425"),
ColorTranslator.FromHtml("#ff0044"),
ColorTranslator.FromHtml("#68386c"),
ColorTranslator.FromHtml("#b55088"),
ColorTranslator.FromHtml("#f6757a"),
ColorTranslator.FromHtml("#e8b796"),
ColorTranslator.FromHtml("#c28569"),

        };
      }
    }



    public static Color[] ENDESGA64
    {
      get
      {
        return new Color[]
        {


// ENDESGA 64 PALETTE
ColorTranslator.FromHtml("#ff0040"),
ColorTranslator.FromHtml("#131313"),
ColorTranslator.FromHtml("#1b1b1b"),
ColorTranslator.FromHtml("#272727"),
ColorTranslator.FromHtml("#3d3d3d"),
ColorTranslator.FromHtml("#5d5d5d"),
ColorTranslator.FromHtml("#858585"),
ColorTranslator.FromHtml("#b4b4b4"),
ColorTranslator.FromHtml("#ffffff"),
ColorTranslator.FromHtml("#c7cfdd"),
ColorTranslator.FromHtml("#92a1b9"),
ColorTranslator.FromHtml("#657392"),
ColorTranslator.FromHtml("#424c6e"),
ColorTranslator.FromHtml("#2a2f4e"),
ColorTranslator.FromHtml("#1a1932"),
ColorTranslator.FromHtml("#0e071b"),
ColorTranslator.FromHtml("#1c121c"),
ColorTranslator.FromHtml("#391f21"),
ColorTranslator.FromHtml("#5d2c28"),
ColorTranslator.FromHtml("#8a4836"),
ColorTranslator.FromHtml("#bf6f4a"),
ColorTranslator.FromHtml("#e69c69"),
ColorTranslator.FromHtml("#f6ca9f"),
ColorTranslator.FromHtml("#f9e6cf"),
ColorTranslator.FromHtml("#edab50"),
ColorTranslator.FromHtml("#e07438"),
ColorTranslator.FromHtml("#c64524"),
ColorTranslator.FromHtml("#8e251d"),
ColorTranslator.FromHtml("#ff5000"),
ColorTranslator.FromHtml("#ed7614"),
ColorTranslator.FromHtml("#ffa214"),
ColorTranslator.FromHtml("#ffc825"),
ColorTranslator.FromHtml("#ffeb57"),
ColorTranslator.FromHtml("#d3fc7e"),
ColorTranslator.FromHtml("#99e65f"),
ColorTranslator.FromHtml("#5ac54f"),
ColorTranslator.FromHtml("#33984b"),
ColorTranslator.FromHtml("#1e6f50"),
ColorTranslator.FromHtml("#134c4c"),
ColorTranslator.FromHtml("#0c2e44"),
ColorTranslator.FromHtml("#00396d"),
ColorTranslator.FromHtml("#0069aa"),
ColorTranslator.FromHtml("#0098dc"),
ColorTranslator.FromHtml("#00cdf9"),
ColorTranslator.FromHtml("#0cf1ff"),
ColorTranslator.FromHtml("#94fdff"),
ColorTranslator.FromHtml("#fdd2ed"),
ColorTranslator.FromHtml("#f389f5"),
ColorTranslator.FromHtml("#db3ffd"),
ColorTranslator.FromHtml("#7a09fa"),
ColorTranslator.FromHtml("#3003d9"),
ColorTranslator.FromHtml("#0c0293"),
ColorTranslator.FromHtml("#03193f"),
ColorTranslator.FromHtml("#3b1443"),
ColorTranslator.FromHtml("#622461"),
ColorTranslator.FromHtml("#93388f"),
ColorTranslator.FromHtml("#ca52c9"),
ColorTranslator.FromHtml("#c85086"),
ColorTranslator.FromHtml("#f68187"),
ColorTranslator.FromHtml("#f5555d"),
ColorTranslator.FromHtml("#ea323c"),
ColorTranslator.FromHtml("#c42430"),
ColorTranslator.FromHtml("#891e2b"),
ColorTranslator.FromHtml("#571c27"),


        };
      }
    }



    public static Color[] AAP64
    {
      get
      {
        return new Color[]
        {

// AAP-64 PALETTE
ColorTranslator.FromHtml("#060608"),
ColorTranslator.FromHtml("#141013"),
ColorTranslator.FromHtml("#3b1725"),
ColorTranslator.FromHtml("#73172d"),
ColorTranslator.FromHtml("#b4202a"),
ColorTranslator.FromHtml("#df3e23"),
ColorTranslator.FromHtml("#fa6a0a"),
ColorTranslator.FromHtml("#f9a31b"),
ColorTranslator.FromHtml("#ffd541"),
ColorTranslator.FromHtml("#fffc40"),
ColorTranslator.FromHtml("#d6f264"),
ColorTranslator.FromHtml("#9cdb43"),
ColorTranslator.FromHtml("#59c135"),
ColorTranslator.FromHtml("#14a02e"),
ColorTranslator.FromHtml("#1a7a3e"),
ColorTranslator.FromHtml("#24523b"),
ColorTranslator.FromHtml("#122020"),
ColorTranslator.FromHtml("#143464"),
ColorTranslator.FromHtml("#285cc4"),
ColorTranslator.FromHtml("#249fde"),
ColorTranslator.FromHtml("#20d6c7"),
ColorTranslator.FromHtml("#a6fcdb"),
ColorTranslator.FromHtml("#ffffff"),
ColorTranslator.FromHtml("#fef3c0"),
ColorTranslator.FromHtml("#fad6b8"),
ColorTranslator.FromHtml("#f5a097"),
ColorTranslator.FromHtml("#e86a73"),
ColorTranslator.FromHtml("#bc4a9b"),
ColorTranslator.FromHtml("#793a80"),
ColorTranslator.FromHtml("#403353"),
ColorTranslator.FromHtml("#242234"),
ColorTranslator.FromHtml("#221c1a"),
ColorTranslator.FromHtml("#322b28"),
ColorTranslator.FromHtml("#71413b"),
ColorTranslator.FromHtml("#bb7547"),
ColorTranslator.FromHtml("#dba463"),
ColorTranslator.FromHtml("#f4d29c"),
ColorTranslator.FromHtml("#dae0ea"),
ColorTranslator.FromHtml("#b3b9d1"),
ColorTranslator.FromHtml("#8b93af"),
ColorTranslator.FromHtml("#6d758d"),
ColorTranslator.FromHtml("#4a5462"),
ColorTranslator.FromHtml("#333941"),
ColorTranslator.FromHtml("#422433"),
ColorTranslator.FromHtml("#5b3138"),
ColorTranslator.FromHtml("#8e5252"),
ColorTranslator.FromHtml("#ba756a"),
ColorTranslator.FromHtml("#e9b5a3"),
ColorTranslator.FromHtml("#e3e6ff"),
ColorTranslator.FromHtml("#b9bffb"),
ColorTranslator.FromHtml("#849be4"),
ColorTranslator.FromHtml("#588dbe"),
ColorTranslator.FromHtml("#477d85"),
ColorTranslator.FromHtml("#23674e"),
ColorTranslator.FromHtml("#328464"),
ColorTranslator.FromHtml("#5daf8d"),
ColorTranslator.FromHtml("#92dcba"),
ColorTranslator.FromHtml("#cdf7e2"),
ColorTranslator.FromHtml("#e4d2aa"),
ColorTranslator.FromHtml("#c7b08b"),
ColorTranslator.FromHtml("#a08662"),
ColorTranslator.FromHtml("#796755"),
ColorTranslator.FromHtml("#5a4e44"),
ColorTranslator.FromHtml("#423934"),




        };
      }
    }



    public static Color[] ENDESGA36
    {
      get
      {
        return new Color[]
        {







//ENDESGA 36 PALETTE
ColorTranslator.FromHtml("#dbe0e7"),
ColorTranslator.FromHtml("#a3acbe"),
ColorTranslator.FromHtml("#67708b"),
ColorTranslator.FromHtml("#4e5371"),
ColorTranslator.FromHtml("#393a56"),
ColorTranslator.FromHtml("#26243a"),
ColorTranslator.FromHtml("#141020"),
ColorTranslator.FromHtml("#7bcf5c"),
ColorTranslator.FromHtml("#509b4b"),
ColorTranslator.FromHtml("#2e6a42"),
ColorTranslator.FromHtml("#1a453b"),
ColorTranslator.FromHtml("#0f2738"),
ColorTranslator.FromHtml("#0d2f6d"),
ColorTranslator.FromHtml("#0f4da3"),
ColorTranslator.FromHtml("#0e82ce"),
ColorTranslator.FromHtml("#13b2f2"),
ColorTranslator.FromHtml("#41f3fc"),
ColorTranslator.FromHtml("#f0d2af"),
ColorTranslator.FromHtml("#e5ae78"),
ColorTranslator.FromHtml("#c58158"),
ColorTranslator.FromHtml("#945542"),
ColorTranslator.FromHtml("#623530"),
ColorTranslator.FromHtml("#46211f"),
ColorTranslator.FromHtml("#97432a"),
ColorTranslator.FromHtml("#e57028"),
ColorTranslator.FromHtml("#f7ac37"),
ColorTranslator.FromHtml("#fbdf6b"),
ColorTranslator.FromHtml("#fe979b"),
ColorTranslator.FromHtml("#ed5259"),
ColorTranslator.FromHtml("#c42c36"),
ColorTranslator.FromHtml("#781f2c"),
ColorTranslator.FromHtml("#351428"),
ColorTranslator.FromHtml("#4d2352"),
ColorTranslator.FromHtml("#7f3b86"),
ColorTranslator.FromHtml("#b45eb3"),
ColorTranslator.FromHtml("#e38dd6"),



        };
      }
    }



    public static Color[] RGB6BIT_64
    {
      get
      {
        return new Color[]
        {



// https://lospec.com/palette-list/6-bit-rgb
//6-BIT RGB PALETTE
ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#000055"),
ColorTranslator.FromHtml("#0000aa"),
ColorTranslator.FromHtml("#0000ff"),
ColorTranslator.FromHtml("#550000"),
ColorTranslator.FromHtml("#550055"),
ColorTranslator.FromHtml("#5500aa"),
ColorTranslator.FromHtml("#5500ff"),
ColorTranslator.FromHtml("#aa0000"),
ColorTranslator.FromHtml("#aa0055"),
ColorTranslator.FromHtml("#aa00aa"),
ColorTranslator.FromHtml("#aa00ff"),
ColorTranslator.FromHtml("#ff0000"),
ColorTranslator.FromHtml("#ff0055"),
ColorTranslator.FromHtml("#ff00aa"),
ColorTranslator.FromHtml("#ff00ff"),
ColorTranslator.FromHtml("#005500"),
ColorTranslator.FromHtml("#005555"),
ColorTranslator.FromHtml("#0055aa"),
ColorTranslator.FromHtml("#0055ff"),
ColorTranslator.FromHtml("#555500"),
ColorTranslator.FromHtml("#555555"),
ColorTranslator.FromHtml("#5555aa"),
ColorTranslator.FromHtml("#5555ff"),
ColorTranslator.FromHtml("#aa5500"),
ColorTranslator.FromHtml("#aa5555"),
ColorTranslator.FromHtml("#aa55aa"),
ColorTranslator.FromHtml("#aa55ff"),
ColorTranslator.FromHtml("#ff5500"),
ColorTranslator.FromHtml("#ff5555"),
ColorTranslator.FromHtml("#ff55aa"),
ColorTranslator.FromHtml("#ff55ff"),
ColorTranslator.FromHtml("#00aa00"),
ColorTranslator.FromHtml("#00aa55"),
ColorTranslator.FromHtml("#00aaaa"),
ColorTranslator.FromHtml("#00aaff"),
ColorTranslator.FromHtml("#55aa00"),
ColorTranslator.FromHtml("#55aa55"),
ColorTranslator.FromHtml("#55aaaa"),
ColorTranslator.FromHtml("#55aaff"),
ColorTranslator.FromHtml("#aaaa00"),
ColorTranslator.FromHtml("#aaaa55"),
ColorTranslator.FromHtml("#aaaaaa"),
ColorTranslator.FromHtml("#aaaaff"),
ColorTranslator.FromHtml("#ffaa00"),
ColorTranslator.FromHtml("#ffaa55"),
ColorTranslator.FromHtml("#ffaaaa"),
ColorTranslator.FromHtml("#ffaaff"),
ColorTranslator.FromHtml("#00ff00"),
ColorTranslator.FromHtml("#00ff55"),
ColorTranslator.FromHtml("#00ffaa"),
ColorTranslator.FromHtml("#00ffff"),
ColorTranslator.FromHtml("#55ff00"),
ColorTranslator.FromHtml("#55ff55"),
ColorTranslator.FromHtml("#55ffaa"),
ColorTranslator.FromHtml("#55ffff"),
ColorTranslator.FromHtml("#aaff00"),
ColorTranslator.FromHtml("#aaff55"),
ColorTranslator.FromHtml("#aaffaa"),
ColorTranslator.FromHtml("#aaffff"),
ColorTranslator.FromHtml("#ffff00"),
ColorTranslator.FromHtml("#ffff55"),
ColorTranslator.FromHtml("#ffffaa"),
ColorTranslator.FromHtml("#ffffff"),


        };
      }
    }



    public static Color[] MSX
    {
      get
      {
        return new Color[]
        {




// https://lospec.com/palette-list/msx
// MSX PALETTE
ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#cacaca"),
ColorTranslator.FromHtml("#ffffff"),
ColorTranslator.FromHtml("#b75e51"),
ColorTranslator.FromHtml("#d96459"),
ColorTranslator.FromHtml("#fe877c"),
ColorTranslator.FromHtml("#cac15e"),
ColorTranslator.FromHtml("#ddce85"),
ColorTranslator.FromHtml("#3ca042"),
ColorTranslator.FromHtml("#40b64a"),
ColorTranslator.FromHtml("#73ce7c"),
ColorTranslator.FromHtml("#5955df"),
ColorTranslator.FromHtml("#7e75f0"),
ColorTranslator.FromHtml("#64daee"),
ColorTranslator.FromHtml("#b565b3"),



        };
      }
    }



    public static Color[] APPLEII
    {
      get
      {
        return new Color[]
        {


// https://lospec.com/palette-list/apple-ii
// APPLE II PALETTE
ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#515c16"),
ColorTranslator.FromHtml("#843d52"),
ColorTranslator.FromHtml("#ea7d27"),
ColorTranslator.FromHtml("#514888"),
ColorTranslator.FromHtml("#e85def"),
ColorTranslator.FromHtml("#f5b7c9"),
ColorTranslator.FromHtml("#006752"),
ColorTranslator.FromHtml("#00c82c"),
ColorTranslator.FromHtml("#919191"),
ColorTranslator.FromHtml("#c9d199"),
ColorTranslator.FromHtml("#00a6f0"),
ColorTranslator.FromHtml("#98dbc9"),
ColorTranslator.FromHtml("#c8c1f7"),
ColorTranslator.FromHtml("#ffffff"),

        };
}
    }



    public static Color[] ZXSPECTRUM
{
  get
  {
    return new Color[]
    {


// https://lospec.com/palette-list/zx-spectrum
//ZX SPECTRUM PALETTE
ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#0022c7"),
ColorTranslator.FromHtml("#002bfb"),
ColorTranslator.FromHtml("#d62816"),
ColorTranslator.FromHtml("#ff331c"),
ColorTranslator.FromHtml("#d433c7"),
ColorTranslator.FromHtml("#ff40fc"),
ColorTranslator.FromHtml("#00c525"),
ColorTranslator.FromHtml("#00f92f"),
ColorTranslator.FromHtml("#00c7c9"),
ColorTranslator.FromHtml("#00fbfe"),
ColorTranslator.FromHtml("#ccc82a"),
ColorTranslator.FromHtml("#fffc36"),
ColorTranslator.FromHtml("#cacaca"),
ColorTranslator.FromHtml("#ffffff"),

    };
  }
}



public static Color[] THOMSONM05_16
{
  get
  {
    return new Color[]
    {


// https://lospec.com/palette-list/thomson-m05
//THOMSON M05 PALETTE
ColorTranslator.FromHtml("#000000"),
ColorTranslator.FromHtml("#bbbbbb"),
ColorTranslator.FromHtml("#ff0000"),
ColorTranslator.FromHtml("#dd7777"),

ColorTranslator.FromHtml("#eebb00"),
ColorTranslator.FromHtml("#ffff00"),
ColorTranslator.FromHtml("#dddd77"),
ColorTranslator.FromHtml("#00ff00"),

ColorTranslator.FromHtml("#77dd77"),
ColorTranslator.FromHtml("#00ffff"),
ColorTranslator.FromHtml("#bbffff"),
ColorTranslator.FromHtml("#ffffff"),

ColorTranslator.FromHtml("#ff00ff"),
ColorTranslator.FromHtml("#dd77ee"),
ColorTranslator.FromHtml("#0000ff"),
ColorTranslator.FromHtml("#7777dd"),


    };
  }
}



public static Color[] AMSTRADCPC
{
  get
  {
    return new Color[]
    {


// https://lospec.com/palette-list/amstrad-cpc
//AMSTRAD CPC PALETTE
ColorTranslator.FromHtml("#040404"),
ColorTranslator.FromHtml("#808080"),
ColorTranslator.FromHtml("#ffffff"),
ColorTranslator.FromHtml("#800000"),
ColorTranslator.FromHtml("#ff0000"),
ColorTranslator.FromHtml("#ff8080"),
ColorTranslator.FromHtml("#ff7f00"),
ColorTranslator.FromHtml("#ffff80"),
ColorTranslator.FromHtml("#ffff00"),
ColorTranslator.FromHtml("#808000"),
ColorTranslator.FromHtml("#008000"),
ColorTranslator.FromHtml("#01ff00"),
ColorTranslator.FromHtml("#80ff00"),
ColorTranslator.FromHtml("#80ff80"),
ColorTranslator.FromHtml("#01ff80"),
ColorTranslator.FromHtml("#008080"),
ColorTranslator.FromHtml("#01ffff"),
ColorTranslator.FromHtml("#80ffff"),
ColorTranslator.FromHtml("#0080ff"),
ColorTranslator.FromHtml("#0000ff"),
ColorTranslator.FromHtml("#00007f"),
ColorTranslator.FromHtml("#7f00ff"),
ColorTranslator.FromHtml("#8080ff"),
ColorTranslator.FromHtml("#ff80ff"),
ColorTranslator.FromHtml("#ff00ff"),
ColorTranslator.FromHtml("#ff0080"),
ColorTranslator.FromHtml("#800080"),




    };
  }
}




  }
}



