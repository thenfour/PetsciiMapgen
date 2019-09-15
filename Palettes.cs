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
    static Color[] BlackAndWhite
    {
      get
      {
        return new Color[] {
        Color.FromArgb(  0,   0,   0),// black
        Color.FromArgb(255, 255, 255),//white
        };
      }
    }

    static Color[] Gray3
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

    public static Color[] C64
    {
      get
      {
        return new Color[] {
        Color.FromArgb(  0,   0,   0),// black
        Color.FromArgb(255, 255, 255),//white
        Color.FromArgb(203, 126, 117),// light red

        Color.FromArgb( 98,  98,  98),//gray1
        Color.FromArgb(137, 137, 137),// gray2
        Color.FromArgb(173, 173, 173),//gray3

        Color.FromArgb(159,  78,  68),// brick red
        Color.FromArgb(109,  84,  18),// dkbrown
        Color.FromArgb(161, 104,  60),// light brown
        
        Color.FromArgb(201, 212, 135),// yellowish
        Color.FromArgb(154, 226, 155),// bright green
        Color.FromArgb( 92, 171,  94),// darker green
        Color.FromArgb(106, 191, 198),// cyan
        
        Color.FromArgb(136, 126, 203),// light purple
        Color.FromArgb( 80,  69, 155),// dark purple
        Color.FromArgb(160,  87, 163),// violet
      };
      }
    }
    public static Color[] Workbench4
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



