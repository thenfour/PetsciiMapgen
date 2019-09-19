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
  //// defines a keycolor => multiple palette colors mapping.
  //public class ColorKey
  //{
  //  Color key;
  //  Color[] palette;
  //}

  public class MonoPaletteFontProvider : IFontProvider
  {
    public string FontFileName { get; private set; }
    public Size CharSizeNoPadding { get; private set; }
    //public Size ImageSize { get; private set; }
    public Bitmap Bitmap { get; private set; }
    public Image Image { get; private set; }
    public int CharCount { get; private set; }
    public Color[] Palette { get; private set; }

    public Size OrigSizeInChars { get; private set; }
    public int OrigCharCount { get; private set; }

    public struct CharMapping
    {
      public int origIndex;
      public int fgIdx;
      public int bgIdx;
    }

    List<CharMapping> map = new List<CharMapping>();

    //void GetCharInfo(int ichar, out int origIndex, out int fgIdx, out int bgIdx)
    //{
      
    //}
    //int ConstructCharID(int origIdx, int fgIdx, int bgIdx)
    //{
    //  //return origIdx * fgIdx * Palette.Length + 
    //}

    public MonoPaletteFontProvider(string fontFileName, Size charSize, Color[] palette)
    {
      this.FontFileName = fontFileName;
      this.Image = Image.FromFile(fontFileName);
      this.Bitmap = new Bitmap(this.Image);
      this.CharSizeNoPadding = charSize;

      this.Palette = palette;

      this.OrigSizeInChars = Utils.Div(this.Image.Size, this.CharSizeNoPadding);
      this.OrigCharCount = Utils.Product(this.OrigSizeInChars);

      int i = 0;
      for (int fgidx = 0; fgidx < Palette.Length; ++ fgidx)
      {
        for (int bgidx = 0; bgidx < Palette.Length; ++bgidx)
        {
          //if (bgidx == fgidx)
          //  continue;
          for (int ch = 0; ch < OrigCharCount; ++ ch) // important that this is the bottom of the stack so the 1st CharCount indices are all unique chars. makes reverse lookup simpler.
          {
            CharMapping m;
            m.origIndex = ch;
            m.fgIdx = fgidx;
            m.bgIdx = bgidx;
            map.Add(m);
            i++;
          }
        }
      }

      this.CharCount = this.map.Count;
    }

    public void Init(int DiscreteTargetValues) { }

    public Point GetCharPosInChars(int ichar)
    {
      var ch = this.map[ichar];
      int y = ch.origIndex / this.OrigSizeInChars.Width;
      int x = ch.origIndex % this.OrigSizeInChars.Width;
      return new Point(x, y);
    }

    public Point GetCharOriginInPixels(int ichar)
    {
      var p = GetCharPosInChars(ichar);
      p = Utils.Mul(p, CharSizeNoPadding);
      return p;
    }

    public int GetCharIndexAtPixelPos(Point charPixPosWUT)
    {
      int chx = charPixPosWUT.X / CharSizeNoPadding.Width;
      int chy = charPixPosWUT.Y / CharSizeNoPadding.Height;
      return chx + (OrigSizeInChars.Width * chy);
    }

    public Color SelectColor(int ichar, Color c)
    {
      var ch = this.map[ichar];
      if (c.R < 127)
        return this.Palette[ch.bgIdx];
      return this.Palette[ch.fgIdx];
    }

    public ColorF GetRegionColor(int ichar, Point topLeft, Size size, Size cellsPerChar, int cellOffsetX, int cellOffsetY)
    {
      Point oc = GetCharPosInChars(ichar);
      Point o = GetCharOriginInPixels(ichar);
      o = Utils.Add(o, topLeft);
      int tilePixelCount = 0;
      ColorF tileC = ColorFUtils.Init;
      for (int py = 0; py < size.Height; ++py)
      {
        for (int px = 0; px < size.Width; ++px)
        {
          var c = this.Bitmap.GetPixel(o.X + px, o.Y + py);
          c = SelectColor(ichar, c);
          tileC = tileC.Add(ColorFUtils.From(c));
          tilePixelCount++;
        }
      }
      return tileC.Div(tilePixelCount);
    }

    public void BlitCharacter(int ichar, BitmapData data, long destX, long destY)
    {
      Point o = GetCharOriginInPixels(ichar);
      for (int y = 0; y < CharSizeNoPadding.Height; ++y)
      {
        for (int x = 0; x < CharSizeNoPadding.Width; ++x)
        {
          Color rgb = this.Bitmap.GetPixel(o.X + x, o.Y + y);
          rgb = SelectColor(ichar, rgb);
          data.SetPixel(destX + x, destY + y, rgb);
        }
      }
    }

  }
}

