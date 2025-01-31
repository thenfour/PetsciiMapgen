﻿using System;
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
//using System.Windows;

namespace PetsciiMapgen
{
  public class FontFamilyFontProvider : IFontProvider
  {
    public Size CharSizeNoPadding { get; private set; }
    public Bitmap Bitmap
    {
      get
      {
        throw new NotImplementedException();
        //return charMap.bmp;
      }
    }
    public int CharCount { get { return charMap.AllCells.Length; } }

    EmojiTest.Utils.GenerateEmojiBitmapResults charMap;

    public string FontFamily { get; private set; }
    public string UnicodeGlyphTextFile { get; private set; }
    public Color[] BackgroundPalette { get; private set; }
    public Color[] ForegroundPalette { get; private set; }
    public float Scale { get; private set; }
    public Size Shift { get; private set; }
    public float? AspectTolerance { get; private set; }
    public string FontName { get; private set; }
    public string FontFile { get; private set; }
    public bool TryToFit { get; private set; }
    public string CharListTextFile { get; private set; }

    public bool StrictGlyphCheck { get; private set; }
    public System.Windows.FontWeight FontWeight { get; private set; }
    public System.Windows.FontStyle FontStyle { get; private set; }
    public System.Windows.FontStretch FontStretch { get; private set; }
    //public SharpDX.DirectWrite.RenderingMode RenderingMode { get; private set; }
    public SharpDX.Direct2D1.TextAntialiasMode TextAAMode { get; private set; }

    public FontFamilyFontProvider(string fontFamily, string fontFile, Size charSize, string unicodeGlyphTextFile,
      Color[] bgPalette, Color[] fgPalette, float scale, Size shift, float? aspectTolerance, string fontName, bool tryToFit, string charListTextFile,
      bool strictGlyphCheck, System.Windows.FontWeight fontWeight, System.Windows.FontStyle fontStyle, System.Windows.FontStretch fontStretch,
      SharpDX.Direct2D1.TextAntialiasMode textAAMode)
    {
      this.CharSizeNoPadding = charSize;
      this.FontFamily = fontFamily;
      this.FontFile = fontFile;
      this.UnicodeGlyphTextFile = unicodeGlyphTextFile;
      this.BackgroundPalette = bgPalette;
      this.ForegroundPalette = fgPalette;
      this.Scale = scale;
      this.Shift = shift;
      this.AspectTolerance = aspectTolerance;
      this.FontName = fontName;
      this.TryToFit = tryToFit;
      this.CharListTextFile = charListTextFile;

      this.StrictGlyphCheck = strictGlyphCheck;
      this.FontWeight = fontWeight;
      this.FontStyle = fontStyle;
      this.FontStretch = fontStretch;
      //this.RenderingMode = renderingMode;
      this.TextAAMode = textAAMode;

      IEnumerable<EmojiTest.Utils.EmojiInfo> cps = null;
      if (!string.IsNullOrEmpty(UnicodeGlyphTextFile))
      {
        cps = EmojiTest.Utils.AllEmojiCodepoints(UnicodeGlyphTextFile);
      }
      if (!string.IsNullOrEmpty(CharListTextFile))
      {
        string s = System.IO.File.ReadAllText(charListTextFile);
        List<EmojiTest.Utils.EmojiInfo> cps2 = new List<EmojiTest.Utils.EmojiInfo>();
        foreach (char c in s.Distinct())
        {
          EmojiTest.Utils.EmojiInfo o;
          o.attribute = null;
          o.cps = new int[] { c };
          o.forceInclude = false;
          o.str = c.ToString();
          cps2.Add(o);
        }
        cps = cps2;
      }
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
        Scale, Shift.Width, Shift.Height, cps, BackgroundPalette, ForegroundPalette, AspectTolerance, tryToFit,
        this.FontStyle, this.FontWeight, this.FontStretch, this.StrictGlyphCheck, this.TextAAMode);

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
      Color[] bgPalette = new Color[] { Color.White };
      Color[] fgPalette = new Color[] { Color.Black };
      float scale = 1.0f;
      Size shift = new Size(0, 0);
      string fontName = "";
      float? aspectTolerance = null;
      bool tryToFit = false;
      string charListTextFile = "";
      bool strictGlyphCheck = true;
      System.Windows.FontWeight fontWeight = System.Windows.FontWeights.Normal;
      System.Windows.FontStyle fontStyle = System.Windows.FontStyles.Normal;
      System.Windows.FontStretch fontStretch = System.Windows.FontStretches.Normal;
      //SharpDX.DirectWrite.RenderingMode renderingMode = SharpDX.DirectWrite.RenderingMode.Outline;
      SharpDX.Direct2D1.TextAntialiasMode textAAMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale;

      args.ProcessArg("-fontfamily", s =>
      {
        fontFamily = s;
      });
      args.ProcessArg("-charListTextFile", s =>
      {
        charListTextFile = s;
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
        bgPalette = new Color[] { System.Drawing.ColorTranslator.FromHtml(s) };
      });
      args.ProcessArg("-fgcolor", s =>
      {
        fgPalette = new Color[] { System.Drawing.ColorTranslator.FromHtml(s) };
      });
      args.ProcessArg("-bgpalette", s =>
      {
        bgPalette = Utils.GetNamedPalette(s);
      });
      args.ProcessArg("-fgpalette", s =>
      {
        fgPalette = Utils.GetNamedPalette(s);
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
        if (float.TryParse(s, out float f))
        {
          aspectTolerance = f;
        }
      });
      args.ProcessArg("-fontname", s =>
      {
        fontName = s;
      });
      args.ProcessArg("-strictGlyphCheck", s =>
      {
        strictGlyphCheck = Utils.ToBool(s);
      });
      args.ProcessArg("-fontWeight", s =>
      {
        fontWeight = System.Windows.FontWeight.FromOpenTypeWeight(int.Parse(s));
      });
      args.ProcessArg("-fontStyle", s =>
      {
        fontStyle = (s.ToLowerInvariant() == "italic") ? System.Windows.FontStyles.Italic : System.Windows.FontStyles.Normal;
      });
      args.ProcessArg("-fontStretch", s =>
      {
        fontStretch = System.Windows.FontStretch.FromOpenTypeStretch(int.Parse(s));
      });
      args.ProcessArg("-fontsmoothing", s =>
      {
        switch(s.ToLowerInvariant())
        {
          case "aliased":
            textAAMode = SharpDX.Direct2D1.TextAntialiasMode.Aliased;
            break;
          case "cleartype":
            textAAMode = SharpDX.Direct2D1.TextAntialiasMode.Cleartype;
            break;
          case "grayscale":
            textAAMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale;
            break;
        }
      });

      if (string.IsNullOrEmpty(fontName))
      {
        fontName = fontFamily;
      }

      return new FontFamilyFontProvider(fontFamily, fontFile, charSize, unicodeGlyphTextFile,
        bgPalette, fgPalette, scale, shift, aspectTolerance, fontName,
        tryToFit, charListTextFile, strictGlyphCheck, fontWeight, fontStyle, fontStretch, textAAMode);
    }

    private PrivateFontCollection _fontCollection;

    public void Init(int DiscreteTargetValues)
    {
    }

    //public void SaveFontImage(string path)
    //{
    //  this.charMap.bmp.Save(path);
    //}

    public Point GetCharPosInChars(int ichar)
    {
      throw new NotImplementedException();
      //  int y = ichar / this.charMap.columns;
      //  int x = ichar % this.charMap.columns;
      //  return new Point(x, y);
    }

    public Point GetCharOriginInPixels(int ichar)
    {
      throw new NotImplementedException();
      //  var p = GetCharPosInChars(ichar);
      //  p = Utils.Mul(p, CharSizeNoPadding);
      //  return p;
    }

    public int GetCharIndexAtPixelPos(Point charPixPosWUT)
    {
      throw new NotImplementedException();

      //  int chx = charPixPosWUT.X / CharSizeNoPadding.Width;
      //  int chy = charPixPosWUT.Y / CharSizeNoPadding.Height;
      //  return chx + (this.charMap.columns * chy);
      //}
    }

    public ColorF GetPixel(int ichar, int px, int py)
    {
      var e = charMap.AllCells[ichar];
      px += e.blitSourcRect.Left;
      py += e.blitSourcRect.Top;
      if (px < 0 || py < 0)
        return ColorF.From(e.bgColor);
      if (px >= e.bmp.Width || py >= e.bmp.Height)
        return ColorF.From(e.bgColor);
      var c = e.bmp.GetPixel(px, py);
      //Point o = GetCharOriginInPixels(ichar);
      //var c = ColorF.From(this.Bitmap.GetPixel(o.X + px, o.Y + py));
      return ColorF.From(c);
    }

    public ColorF GetRegionColor(int ichar, Point topLeft, Size size, Size cellsPerChar, int cellOffsetX, int cellOffsetY)
    {
      //Point oc = GetCharPosInChars(ichar);
      //Point o = GetCharOriginInPixels(ichar);
      //o = Utils.Add(o, topLeft);
      int tilePixelCount = 0;
      ColorF tileC = ColorF.Init;
      for (int py = 0; py < size.Height; ++py)
      {
        for (int px = 0; px < size.Width; ++px)
        {
          //var c = this.Bitmap.GetPixel(o.X + px, o.Y + py);
          var c = GetPixel(ichar, topLeft.X + px, topLeft.Y + py);
          tileC = tileC.Add(c);
          tilePixelCount++;
        }
      }
      return tileC.Div(tilePixelCount);
    }

    public void BlitCharacter(int ichar, BitmapData data, long destX, long destY)
    {
      //Point o = GetCharOriginInPixels(ichar);
      for (int y = 0; y < CharSizeNoPadding.Height; ++y)
      {
        for (int x = 0; x < CharSizeNoPadding.Width; ++x)
        {
          //Color rgb = this.Bitmap.GetPixel(o.X + x, o.Y + y);
          var c = GetPixel(ichar, x, y);
          data.SetPixel(destX + x, destY + y, c);
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
      for (int y = 0; y < rows; ++y)
      {
        for (int x = 0; x < columns; ++x)
        {
          if (!cellsMapped.TryGetValue(new Point(x, y), out int ichar))
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

