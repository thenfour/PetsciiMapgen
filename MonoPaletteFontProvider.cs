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

    public Color[] FGPalette { get; private set; }
    public Color[] BGPalette { get; private set; }

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
      sb.AppendLine(string.Format("FGPalette={0}", this.FGPalette));
      sb.AppendLine(string.Format("BGPalette={0}", this.BGPalette));
    }

    List<CharMapping> map = new List<CharMapping>();

    public MonoPaletteFontProvider(string fontFileName, Size charSize, Color[] fgpalette, string fgpaletteName, Color[] bgpalette, string bgpalettename)
    {
      this.FontFileName = fontFileName;
      this.Image = Image.FromFile(fontFileName);
      this.Bitmap = new Bitmap(this.Image);
      this.CharSizeNoPadding = charSize;
      this.FGPaletteName = fgpaletteName;
      this.FGPalette = fgpalette;
      this.BGPaletteName = bgpalettename;
      bool palettesEqual = (BGPaletteName == FGPaletteName);
      bool basePaletteIsSame = palettesEqual || BGPaletteName.StartsWith(this.FGPaletteName);
      if (!palettesEqual && basePaletteIsSame)
      {
        // this is the case like
        // fgpalette: C64Color
        // bgpalette: C64Color[1]
        BGPaletteName = BGPaletteName.Substring(this.FGPaletteName.Length);
      }
      this.BGPalette = bgpalette;

      this.OrigSizeInChars = Utils.Div(this.Image.Size, this.CharSizeNoPadding);
      this.OrigCharCount = Utils.Product(this.OrigSizeInChars);

      int i = 0;
      for (int fgidx = 0; fgidx < fgpalette.Length; ++ fgidx)
      {
        for (int bgidx = 0; bgidx < bgpalette.Length; ++bgidx)
        {
          if ((bgidx == fgidx) && basePaletteIsSame)
            continue;
          if (BGPalette[bgidx] == FGPalette[fgidx])
            continue;// even when base palette isn't the same, avoid same bg/fg colors
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

    public virtual string DisplayName
    {
      get
      {
        string pal = FGPaletteName;
        if (FGPaletteName != BGPaletteName)
        {
          pal = string.Format("fg_{0}_bg_{1}", FGPaletteName, BGPaletteName);
        }
        return string.Format("{0}-{1}", System.IO.Path.GetFileNameWithoutExtension(FontFileName), pal);
      }
    }

    public string FGPaletteName { get; private set; }
    public string BGPaletteName { get; private set; }

    public static MonoPaletteFontProvider ProcessArgs(string[] args)
    {
      //- fontImage "emojidark12.png"
      string fontImagePath = "";
      Size charSize = new Size(8,8);
      string fgpaletteName = "";
      Color[] fgpalette = Palettes.RGBPrimariesHalftone16;
      string bgpaletteName = "";
      Color[] bgpalette = Palettes.RGBPrimariesHalftone16;
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
        fgpaletteName = bgpaletteName = s;
        fgpalette = bgpalette = Utils.GetNamedPalette(s);
      });
      args.ProcessArg("-fgpalette", s =>
      {
        fgpaletteName = s;
        fgpalette = Utils.GetNamedPalette(s);
      });
      args.ProcessArg("-bgpalette", s =>
      {
        bgpaletteName = s;
        bgpalette = Utils.GetNamedPalette(s);
      });

      return new MonoPaletteFontProvider(fontImagePath, charSize, fgpalette, fgpaletteName, bgpalette, bgpaletteName);
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
        return this.BGPalette[ch.bgIdx];
      return this.FGPalette[ch.fgIdx];
    }

    public ColorF GetPixel(int ichar, int px, int py)
    {
      Point o = GetCharOriginInPixels(ichar);
      var c = this.Bitmap.GetPixel(o.X + px, o.Y + py);
      c = SelectColor(ichar, c);
      return ColorF.From(c);
    }

    public ColorF GetRegionColor(int ichar, Point topLeft, Size size, Size cellsPerChar, int cellOffsetX, int cellOffsetY)
    {
      Point oc = GetCharPosInChars(ichar);
      Point o = GetCharOriginInPixels(ichar);
      o = Utils.Add(o, topLeft);
      int tilePixelCount = 0;
      ColorF tileC = ColorF.Init;
      for (int py = 0; py < size.Height; ++py)
      {
        for (int px = 0; px < size.Width; ++px)
        {
          var c = this.Bitmap.GetPixel(o.X + px, o.Y + py);
          c = SelectColor(ichar, c);
          tileC = tileC.Add(ColorF.From(c));
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

