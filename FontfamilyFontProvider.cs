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
  public class FontFamilyFontProvider : IFontProvider
  {
    public Size CharSizeNoPadding { get; private set; }
    public Bitmap Bitmap { get { return charMap.bmp; } }
    public int CharCount { get { return charMap.AllCells.Length; } }

    EmojiTest.Utils.GenerateEmojiBitmapResults charMap;

    public string FontFamily { get; private set; }
    public string UnicodeGlyphTextFile { get; private set; }
    public Color BackgroundColor { get; private set; }
    public Color ForegroundColor { get; private set; }
    public float Scale { get; private set; }
    public Size Shift { get; private set; }
    public float AspectTolerance { get; private set; }

    public FontFamilyFontProvider(string fontFamily, Size charSize, string unicodeGlyphTextFile,
      Color bgColor, Color fgColor, float scale, Size shift, float aspectTolerance)
    {
      this.CharSizeNoPadding = charSize;
      this.FontFamily = fontFamily;
      this.UnicodeGlyphTextFile = unicodeGlyphTextFile;
      this.BackgroundColor = bgColor;
      this.ForegroundColor = fgColor;
      this.Scale = scale;
      this.Shift = shift;
      this.AspectTolerance = aspectTolerance;
    }

    public string DisplayName
    {
      get
      {
        return "emoji";
        //return string.Format("{0}-{1}", System.IO.Path.GetFileNameWithoutExtension(FontFileName), PaletteName);
      }
    }

    public static FontFamilyFontProvider ProcessArgs(string[] args)
    {
      // -fonttype fontfamily
      // -fontfamily "Segoe UI emoji"
      // -charsize 8x8
      // -UnicodeGlyphTextFile emoji-data-v12.txt
      // -bgcolor #000000
      // -fgcolor #ffffff
      // -scale 1.2
      // -shift 2x2

      string fontFamily = "";
      Size charSize = new Size(8, 8);
      string unicodeGlyphTextFile = "";
      Color bgColor = Color.White;
      Color fgColor = Color.Black;
      float scale = 1.0f;
      Size shift = new Size(0, 0);
      float aspectTolerance = 1;

      args.ProcessArg("-fontfamily", s =>
      {
        fontFamily = s;
      });
      args.ProcessArg("-charsize", s =>
      {
        charSize = new Size(int.Parse(s.Split('x')[0]), int.Parse(s.Split('x')[1]));
      });
      args.ProcessArg("-UnicodeGlyphTextFile", s =>
      {
        unicodeGlyphTextFile = s;
      });
      args.ProcessArg("-bgcolor", s =>
      {
        bgColor = System.Drawing.ColorTranslator.FromHtml(s);
      });
      args.ProcessArg("-fgcolor", s =>
      {
        fgColor = System.Drawing.ColorTranslator.FromHtml(s);
      });
      args.ProcessArg("-scale", s =>
      {
        scale = float.Parse(s);
      });
      args.ProcessArg("-shift", s =>
      {
        shift = new Size(int.Parse(s.Split('x')[0]), int.Parse(s.Split('x')[1]));
      });
      args.ProcessArg("-aspectTolerance", s =>
      {
        aspectTolerance = float.Parse(s);
      });

      return new FontFamilyFontProvider(fontFamily, charSize, unicodeGlyphTextFile, bgColor, fgColor, scale, shift, aspectTolerance);
    }

    public void Init(int DiscreteTargetValues)
    {
      var cps = EmojiTest.Utils.AllEmojiCodepoints(UnicodeGlyphTextFile);
      this.charMap = EmojiTest.Utils.GenerateEmojiBitmap(FontFamily,
        this.CharSizeNoPadding.Width, this.CharSizeNoPadding.Height,
        Scale, Shift.Width, Shift.Height, cps, BackgroundColor, ForegroundColor, AspectTolerance);
    }

    public void SaveFontImage(string path)
    {
      this.charMap.bmp.Save(path);
    }

    public Point GetCharPosInChars(int ichar)
    {
      int y = ichar / this.charMap.columns;
      int x = ichar % this.charMap.columns;
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
      return chx + (this.charMap.columns * chy);
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
          data.SetPixel(destX + x, destY + y, rgb);
        }
      }
    }


    public string ConvertToText(IDictionary<Point, int> cellsMapped)
    {
      StringBuilder sb = new StringBuilder();
      if (!cellsMapped.Any())
      {
        return "";
      }

      // figure out the # of columns. yea i could pass it in.
      int columns = cellsMapped.Max(o => o.Key.X) + 1;
      int rows = cellsMapped.Max(o => o.Key.Y) + 1;

      //cellsMapped = cellsMapped.OrderBy(c => c.Key.X + c.Key.Y * columns);
      for (int y = 0; y < rows; ++ y)
      {
        for (int x = 0; x < columns; ++ x)
        {
          if (!cellsMapped.TryGetValue(new Point(x,y), out int ichar))
          {
            sb.Append(' ');
            continue;
          }
          sb.Append(this.charMap.AllCells[ichar].str);
        }
        sb.Append("\r\n");
      }

      return sb.ToString();
    }
  }
}

