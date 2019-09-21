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
using System.Drawing.Text;

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
    public string FontName { get; private set; }
    public string FontFile { get; private set; }
    public bool TryToFit { get; private set; }

    public FontFamilyFontProvider(string fontFamily, string fontFile, Size charSize, string unicodeGlyphTextFile,
      Color bgColor, Color fgColor, float scale, Size shift, float aspectTolerance, string fontName, bool tryToFit)
    {
      this.CharSizeNoPadding = charSize;
      this.FontFamily = fontFamily;
      this.FontFile = fontFile;
      this.UnicodeGlyphTextFile = unicodeGlyphTextFile;
      this.BackgroundColor = bgColor;
      this.ForegroundColor = fgColor;
      this.Scale = scale;
      this.Shift = shift;
      this.AspectTolerance = aspectTolerance;
      this.FontName = fontName;
      this.TryToFit = tryToFit;

      //var cps = EmojiTest.Utils.AllEmojisWithModifiers(UnicodeGlyphTextFile);
      var cps = EmojiTest.Utils.AllEmojiCodepoints(UnicodeGlyphTextFile);
      PetsciiMapgen.Log.WriteLine("Total fontfamily codepoint sequences to process: {0:N0}", cps.Count());

      if (!string.IsNullOrEmpty(FontFile))
      {
        _fontCollection = new PrivateFontCollection();
        _fontCollection.AddFontFile(FontFile);
        Log.WriteLine("Loaded font {0}", FontFile);
        foreach (var f in _fontCollection.Families)
        {
          Log.WriteLine(" -> font family: {0}", f.Name);
        }
        if (string.IsNullOrEmpty(this.FontFamily))
        {
          this.FontFamily = _fontCollection.Families[0].Name;
          Log.WriteLine("AUTO-SELECTING font family {0}", this.FontFamily);
        }
      }

      this.charMap = EmojiTest.Utils.GenerateEmojiBitmap(FontFamily,
        this.CharSizeNoPadding.Width, this.CharSizeNoPadding.Height,
        Scale, Shift.Width, Shift.Height, cps, BackgroundColor, ForegroundColor, AspectTolerance, tryToFit);

    }

    public string DisplayName
    {
      get
      {
        return string.Format("{0}-{1}", FontName, this.charMap.AllCells.Length);
      }
    }

    public static FontFamilyFontProvider ProcessArgs(string[] args)
    {
      // -fonttype fontfamily
      // -fontfamily "Segoe UI emoji"
      // -fontfile "blah.wtf"
      // -charsize 8x8
      // -UnicodeGlyphTextFile emoji-data-v12.txt
      // -bgcolor #000000
      // -fgcolor #ffffff
      // -scale 1.2
      // -shift 2x2
      // -fontname "Segoe"  <-- just to help name dirs/files

      string fontFamily = "";
      string fontFile = "";
      Size charSize = new Size(8, 8);
      string unicodeGlyphTextFile = "";
      Color bgColor = Color.White;
      Color fgColor = Color.Black;
      float scale = 1.0f;
      Size shift = new Size(0, 0);
      string fontName = "";
      float aspectTolerance = 1;
      bool tryToFit = false;

      args.ProcessArg("-fontfamily", s =>
      {
        fontFamily = s;
      });
      args.ProcessArg("-tryToFit", s =>
      {
        tryToFit = Utils.ToBool(s);
      });
      args.ProcessArg("-fontFile", s =>
      {
        fontFile = s;
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
      args.ProcessArg("-fontname", s =>
      {
        fontName = s;
      });

      return new FontFamilyFontProvider(fontFamily, fontFile, charSize, unicodeGlyphTextFile, bgColor, fgColor, scale, shift, aspectTolerance, fontName, tryToFit);
    }

    private PrivateFontCollection _fontCollection;

    public void Init(int DiscreteTargetValues)
    {
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
          sb.Append(this.charMap.AllCells[ichar].info.str);
        }
        sb.Append("\r\n");
      }

      return sb.ToString();
    }
  }
}

