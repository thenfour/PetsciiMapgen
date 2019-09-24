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
  public class MonoPaletteFontProvider : IFontProvider
  {
    public string FontFileName { get; private set; }
    public Size CharSizeNoPadding { get; private set; }
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

    public virtual void WriteConfig(StringBuilder sb)
    {
      sb.AppendLine("fontType=Mono");
      sb.AppendLine(string.Format("charWidth={0}", this.CharSizeNoPadding.Width));
      sb.AppendLine(string.Format("charHeight={0}", this.CharSizeNoPadding.Height));
      sb.AppendLine(string.Format("CharCount={0}", this.CharCount));
      sb.AppendLine(string.Format("FontFileName={0}", this.FontFileName));
      sb.AppendLine(string.Format("Palette={0}", this.Palette));
    }

    List<CharMapping> map = new List<CharMapping>();

    public MonoPaletteFontProvider(string fontFileName, Size charSize, Color[] palette, string paletteName)
    {
      this.FontFileName = fontFileName;
      this.Image = Image.FromFile(fontFileName);
      this.Bitmap = new Bitmap(this.Image);
      this.CharSizeNoPadding = charSize;
      this.PaletteName = paletteName;
      this.Palette = palette;

      this.OrigSizeInChars = Utils.Div(this.Image.Size, this.CharSizeNoPadding);
      this.OrigCharCount = Utils.Product(this.OrigSizeInChars);

      int i = 0;
      for (int fgidx = 0; fgidx < Palette.Length; ++ fgidx)
      {
        for (int bgidx = 0; bgidx < Palette.Length; ++bgidx)
        {
          if (bgidx == fgidx)
            continue;
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

    public string DisplayName
    {
      get
      {
        return string.Format("{0}-{1}", System.IO.Path.GetFileNameWithoutExtension(FontFileName), PaletteName);
      }
    }

    public string PaletteName { get; private set; }

    public static MonoPaletteFontProvider ProcessArgs(string[] args)
    {
      //- fontImage "emojidark12.png"
      string fontImagePath = "";
      Size charSize = new Size(8,8);
      string paletteName = "";
      Color[] palette = Palettes.RGBPrimariesHalftone16;
      args.ProcessArg("-fontimage", s =>
      {
        fontImagePath = s;
      });
      args.ProcessArg("-charsize", s =>
      {
        charSize = new Size(int.Parse(s.Split('x')[0]), int.Parse(s.Split('x')[1]));
      });
      args.ProcessArg("-palette", s =>
      {
        paletteName = s;
        var ti = typeof(Palettes).GetProperty(s).GetValue(null);
        palette = (Color[])ti;
      });

      return new MonoPaletteFontProvider(fontImagePath, charSize, palette, paletteName);
    }

    public void Init(int DiscreteTargetValues) { }
    //public void OnImageProcessed(IEnumerable<KeyValuePair<Point, int>> cellsMapped, string outputDir, string bitmapFilename) { }

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

    public ColorF GetPixel(int ichar, int px, int py)
    {
      Point o = GetCharOriginInPixels(ichar);
      var c = ColorFUtils.From(this.Bitmap.GetPixel(o.X + px, o.Y + py));
      // TODO: dither
      return c;
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

