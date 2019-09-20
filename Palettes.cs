// define some palettes to use

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

    public static Color[] C64Gray8
    {
      get
      {
        return new Color[] {
            Color.FromArgb(0, 0, 0), // luminance: 0
            Color.FromArgb(109, 84, 18), // luminance: 24.9019607901573
            //Color.FromArgb(98, 98, 98), // luminance: 38.4313732385635
            Color.FromArgb(161, 104, 60), // luminance: 43.3333337306976
            //Color.FromArgb(80, 69, 155), // luminance: 43.921571969986
            //Color.FromArgb(159, 78, 68), // luminance: 44.5098042488098
            //Color.FromArgb(160, 87, 163), // luminance: 49.0196108818054
            //Color.FromArgb(92, 171, 94), // luminance: 51.5686273574829
            //Color.FromArgb(137, 137, 137), // luminance: 53.7254929542542
            Color.FromArgb(106, 191, 198), // luminance: 59.6078455448151
            //Color.FromArgb(203, 126, 117), // luminance: 62.7451002597809
            //Color.FromArgb(136, 126, 203), // luminance: 64.5098030567169
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
  }
}



