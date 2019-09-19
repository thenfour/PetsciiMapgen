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
  public interface IFontProvider
  {
    string FontFileName { get; }
    void Init(int DiscreteTargetValues);
    int CharCount { get; }
    Point GetCharPosInChars(int ichar);
    Point GetCharOriginInPixels(int ichar);
    int GetCharIndexAtPixelPos(Point charPixPosWUT);
    Size CharSizeNoPadding { get; }
    void BlitCharacter(int ichar, BitmapData data, long destX, long destY);
    ColorF GetRegionColor(int ichar, Point topLeft, Size size, Size cellsPerChar, int cellOffsetX, int cellOffsetY);
  }

  public class FontProvider : IFontProvider
  {
    public string FontFileName { get; private set; }
    public Size CharSizeNoPadding { get; private set; }
    public int LeftTopPadding { get; private set; }
    public Size ImageSize { get; private set; }
    public Bitmap Bitmap { get; private set; }
    public Image Image { get; private set; }

    public Size CharSizeWithPadding { get; private set; }
    public Size SizeInChars { get; private set; }
    public int CharCount { get; private set; }

    public IDitherProvider DitherProvider { get; private set; }

    public FontProvider(string fontFileName, Size charSize, int leftTopPadding = 0, IDitherProvider dither = null)
    {
      this.FontFileName = fontFileName;
      this.CharSizeNoPadding = charSize;
      this.LeftTopPadding = LeftTopPadding;
      this.DitherProvider = dither;

      this.Image = System.Drawing.Image.FromFile(fontFileName);
      this.Bitmap = new Bitmap(this.Image);
      this.CharSizeWithPadding = new Size(charSize.Width + leftTopPadding, charSize.Height + leftTopPadding);
      this.SizeInChars = Utils.Div(this.Image.Size, this.CharSizeWithPadding);
      this.CharCount = Utils.Product(this.SizeInChars);
    }

    public void Init(int DiscreteTargetValues)
    {
      if (this.DitherProvider != null)
        this.DitherProvider.DiscreteTargetValues = DiscreteTargetValues;
    }

    public Point GetCharPosInChars(int ichar)
    {
      int y = ichar / this.SizeInChars.Width;
      int x = ichar % this.SizeInChars.Width;
      return new Point(x, y);
    }

    public Point GetCharOriginInPixels(int ichar)
    {
      var p = GetCharPosInChars(ichar);
      p = Utils.Mul(p, CharSizeWithPadding);
      return Utils.Add(p, LeftTopPadding);
    }

    public int GetCharIndexAtPixelPos(Point charPixPosWUT)
    {
      int chx = charPixPosWUT.X / CharSizeWithPadding.Width;
      int chy = charPixPosWUT.Y / CharSizeWithPadding.Height;
      return chx + (SizeInChars.Width * chy);
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
          var c = ColorFUtils.From(this.Bitmap.GetPixel(o.X + px, o.Y + py));

          if (this.DitherProvider != null)
          {
            int absoluteCellX = cellsPerChar.Width * oc.X + cellOffsetX;
            int absoluteCellY = cellsPerChar.Height * oc.Y + cellOffsetY;
            c = this.DitherProvider.TransformColor(absoluteCellX, absoluteCellY, c);
          }

          tileC = tileC.Add(c);
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
  }
}

