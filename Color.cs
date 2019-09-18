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
  public interface IDitherProvider
  {
    ColorF TransformColor(int cellX, int cellY, ColorF c);
    int DiscreteTargetValues { get; set; }
  }

  public class Bayer8DitherProvider : IDitherProvider
  {
    public int DiscreteTargetValues { get; set; }
    private double strength;
    private double[,] matrix = new double[,]
    {{0.0,       0.5,      0.125,    0.625,    0.03125,  0.53125,  0.15625,  0.65625 },
 {0.75,     0.25,     0.875,    0.375,    0.78125,  0.28125,  0.90625,  0.40625 },
 {0.1875,   0.6875,   0.0625,   0.5625,   0.21875,  0.71875,  0.09375,  0.59375 },
 {0.9375,   0.4375,   0.8125,   0.3125,   0.96875,  0.46875,  0.84375,  0.34375 },
 {0.046875, 0.546875, 0.171875, 0.671875, 0.015625, 0.515625, 0.140625, 0.640625},
 {0.796875, 0.296875, 0.921875, 0.421875, 0.765625, 0.265625, 0.890625, 0.390625},
 {0.234375, 0.734375, 0.109375, 0.609375, 0.203125, 0.703125, 0.078125, 0.578125},
 {0.984375, 0.484375, 0.859375, 0.359375, 0.953125, 0.453125, 0.828125, 0.328125}};

    public Bayer8DitherProvider(double strength)
    {
      this.strength = strength;
    }

    public ColorF TransformColor(int cellX, int cellY, ColorF c)
    {
      if (c.IsBlackOrWhite())// == Color.Black || c == Color.White)
        return c;// don't dither these; they're useful to be pure!
      cellX &= 7;
      cellY &= 7;
      double p = (matrix[cellX, cellY] - .5) * strength;
      double i = (255 * (p / DiscreteTargetValues));

      c = c.Add(i);
      c = c.Clamp();
      return c;
    }
  }

  public interface IPaletteProvider
  {
    // takes a given color and returns an array of colors to use instead.
    // the idea is that you use a color key, and replace the key with various palette colors.
    Color[] GetColors(Color c);
  }
}

