// https://gist.github.com/ksasao/b4f5f7bef56e1cacddee4fd10204fec4
using System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

using d2 = SharpDX.Direct2D1;
using d3d = SharpDX.Direct3D11;
using dxgi = SharpDX.DXGI;
using wic = SharpDX.WIC;
using dw = SharpDX.DirectWrite;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

// Add System.Drawing and
// nuget SharpDX.Direct3D11, SharpDX.Direct2D1
namespace EmojiTest
{
  public static class Utils
  {
    public struct EmojiInfo
    {
      public int[] cps; // codepoints contributing to this glyph. typically 1 codepoint, but there can be unicode modifiers after.
      public bool forceInclude;
      public string str;
      public string attribute;
    }
    // returns codepoints and attributes. not permutated with modifiers.
    public static IEnumerable<EmojiInfo> AllEmojiCodepoints(string unicodeDataTextfilePath)
    {
      EmojiInfo emspace;
      emspace.attribute = "(extra)";
      emspace.cps = new int[] { 8195 };
      emspace.str = char.ConvertFromUtf32(emspace.cps[0]);
      emspace.forceInclude = true;
      yield return emspace;

      //var ret = new List<string>(10000);
      var el = System.IO.File.ReadAllLines(unicodeDataTextfilePath);
      foreach (var l in el)
      {
        // https://blog.mzikmund.com/2017/01/working-with-emoji-skin-tones-in-apps/
        // 
        var beforeComment = l.Split('#')[0];
        if (!beforeComment.Contains(';'))
          continue;
        var attribute = beforeComment.Split(';')[1].Trim();
        if (attribute != "fully-qualified" && attribute != "minimally-qualified")
        {
          continue;
        }
        beforeComment = beforeComment.Split(';')[0];
        beforeComment = beforeComment.Trim();
        if (!beforeComment.Any())
          continue;
        var r = beforeComment.Split(new string[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
        //int cp = Convert.ToInt32(r.First(), 16);
        if (r.Count() == 1)
        {
          EmojiInfo rv;
          rv.cps = r.First()
            .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(cp => Convert.ToInt32(cp.Trim(), 16)).ToArray();
          rv.str = string.Join("", rv.cps.Select(cp => char.ConvertFromUtf32(cp)));
          rv.attribute = attribute;
          rv.forceInclude = false;
          yield return rv;
        }
        else
        {
          throw new Exception("don't support N..N ranges anymore.");
          //int cp2 = Convert.ToInt32(r.Last(), 16);
          //for (int i = cp; i <= cp2; ++i)
          //{
          //  EmojiInfo rv;
          //  rv.attribute = attribute;
          //  rv.cp = i;
          //  rv.str = char.ConvertFromUtf32(i);
          //  rv.modifier = -1;
          //  yield return rv;
          //}
        }
      }
    }

    //public static IEnumerable<EmojiInfo> AllEmojisWithModifiers(string unicodeDataTextfilePath)
    //{
    //  EmojiInfo emspace;
    //  emspace.attribute = "(extra)";
    //  emspace.cps = new int[] { 8195 };
    //  //emspace.modifier = -1;
    //  emspace.str = char.ConvertFromUtf32(emspace.cps[0]);
    //  var emoji = AllEmojiCodepoints(unicodeDataTextfilePath).Append(emspace);

    //  var modifiers = emoji.Where(e => e.attribute == "Emoji_Modifier").ToArray();
    //  var rv = new List<EmojiInfo>(emoji.Count());
    //  foreach(var e in emoji)
    //  {
    //    if (e.attribute == "Emoji_Modifier_Base")
    //    {
    //      rv.AddRange(modifiers.Select(m =>
    //      {
    //        EmojiInfo modified;
    //        modified.attribute = "(modified)";
    //        modified.cps = e.cps;
    //        modified.modifier = m.cp;
    //        modified.str = e.str + m.str;
    //        return modified;
    //      }));
    //    } else
    //    {
    //      rv.Add(e);
    //    }
    //  }
    //  return rv;
    //}

    public class GlyphData
    {
      public GlyphData()
      {
      }
      public GlyphData(GlyphData rhs)
      {
        info = rhs.info;
        width = rhs.width;
        height = rhs.height;
        scaleNeeded = rhs.scaleNeeded;
        // don't support copying bitmap stuff yet.
        System.Diagnostics.Debug.Assert(rhs.bmp == null);
      }
      //public int cp;
      //public string str;
      public EmojiInfo info;
      public float width;
      public float height;
      public float scaleNeeded;

      public System.Drawing.Bitmap bmp = null;
      public SharpDX.Size2F bmpSize;
      public System.Drawing.Rectangle blitSourcRect;
      public System.Drawing.Color bgColor;
      public System.Drawing.Color textColor;
    }
    public class GenerateEmojiBitmapResults
    {
      //public System.Drawing.Bitmap bmp;
      public int columns;
      public int rows;
      public GlyphData[] AllCells;
    }

    public static GenerateEmojiBitmapResults GenerateEmojiBitmap(string fontName, int cellWidth, int cellHeight,
      float additionalScale, int shiftX, int shiftY, IEnumerable<EmojiInfo> codepointsToInclude,
      System.Drawing.Color[] backgroundPalette, System.Drawing.Color[] textPalette, float? aspectToleranceFromTarget, bool tryToFit,
      System.Windows.FontStyle fontStyle, System.Windows.FontWeight fontWeight, System.Windows.FontStretch fontStretch,
      bool strictGlyphChecking, d2.TextAntialiasMode aaMode)
    {
      System.Windows.Media.FontFamily fm = new System.Windows.Media.FontFamily(fontName);
      System.Windows.Media.Typeface tf = new System.Windows.Media.Typeface(fm, fontStyle, fontWeight, fontStretch);
      PetsciiMapgen.ProgressReporter pr = new PetsciiMapgen.ProgressReporter((ulong)codepointsToInclude.Count() * (ulong)backgroundPalette.Length * (ulong)textPalette.Length);
      if (!tf.TryGetGlyphTypeface(out System.Windows.Media.GlyphTypeface gtf))
      {
        PetsciiMapgen.Log.WriteLine("!!!!!!!!!! FONT FAMILY HAS NO GLYPH MAP; you will end up with unsupported glyphs in the map.");
        // throw new Exception();
      }

      // select codepoints to actually use
      PetsciiMapgen.Utils.ValueRangeInspector rangeX = new PetsciiMapgen.Utils.ValueRangeInspector();
      PetsciiMapgen.Utils.ValueRangeInspector rangeY = new PetsciiMapgen.Utils.ValueRangeInspector();
      PetsciiMapgen.Utils.ValueRangeInspector allAspects = new PetsciiMapgen.Utils.ValueRangeInspector();
      PetsciiMapgen.Utils.ValueRangeInspector selectedAspects = new PetsciiMapgen.Utils.ValueRangeInspector();
      int rejectedBecauseNotInTypeface = 0;
      int rejectedBecauseAspect = 0;

      int targetWidth = cellWidth;
      int targetHeight = cellHeight;
      float targetAspect = (float)targetWidth / targetHeight;

      EmojiTest.Direct2DText dt = new EmojiTest.Direct2DText();
      dt.SetFont(fontName, targetHeight);

      var emoji = codepointsToInclude.Select(e =>
      {
        pr.Visit();
        GlyphData ret = new GlyphData();
        ret.info = e;
        //ret.str = char.ConvertFromUtf32(cp);
        var sz = dt.GetTextSize(ret.info.str);
        ret.width = sz.Width;
        ret.height = sz.Height;
        rangeX.Visit(ret.width);
        rangeY.Visit(ret.height);
        allAspects.Visit(ret.width / ret.height);
        ret.scaleNeeded = 1;
        if (tryToFit)
        {
          if (ret.height > 0 && ret.width > 0)
          {
            float scaleNeededY = (float)targetHeight / ret.height;// factor to match target
            float scaleNeededX = (float)targetWidth / ret.width;
            ret.scaleNeeded = Math.Min(scaleNeededX, scaleNeededY);
          }
          else
          {
            ret.scaleNeeded = -1;
          }
        }
        return ret;
      })
      .Where(o =>
      {
        if (o.info.forceInclude)
          return true;
        if (o.info.str == "\r" || o.info.str == "\n")
          return false;
        if (gtf != null)
        {
          if (!gtf.CharacterToGlyphMap.ContainsKey(o.info.cps[0]))
          {
            rejectedBecauseNotInTypeface++;
            if (strictGlyphChecking)
              return false;
          }
        }
        float aspect = o.width / o.height;
        float da = Math.Abs(aspect - targetAspect);
        if (aspectToleranceFromTarget.HasValue && da > aspectToleranceFromTarget)
        {
          rejectedBecauseAspect++;
          return false;
        }

        selectedAspects.Visit(aspect);
        return true;
      })
      .OrderBy(o => o.scaleNeeded).ToArray();

      PetsciiMapgen.Log.WriteLine("EMOJI font encountered aspect ratios between {0}", allAspects);
      PetsciiMapgen.Log.WriteLine("Chars rejected because they don't have glyphs in this typeface: {0:N0}", rejectedBecauseNotInTypeface);
      PetsciiMapgen.Log.WriteLine("Chars rejected because aspect ratio out of range: {0:N0}", rejectedBecauseAspect);
      if (aspectToleranceFromTarget.HasValue)
      {
        PetsciiMapgen.Log.WriteLine("EMOJI font allowed aspect ratios between {0}", selectedAspects);
      }

      int totalCharCount = emoji.Count() * backgroundPalette.Length * textPalette.Length;
      int scaleChanges = 0;

      int columns = (int)Math.Ceiling(Math.Sqrt(totalCharCount));
      int rows = columns;// we're aiming for square bitmap.
      int imgWidth = columns * targetWidth;
      int imgHeight = rows * targetHeight;

      List<GlyphData> fullEmoji = new List<GlyphData>(totalCharCount);
      if (emoji.Length < 2)
      {
        throw new Exception("NOt enough glyphs to generate anything meaningful.");
      }

      foreach (var backgroundColor in backgroundPalette)
      {
        foreach (var textColor in textPalette)
        {
          var bmp = new System.Drawing.Bitmap(imgWidth, imgHeight);
          using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
          {
            float lastScale = 0;
            //int iemoji = 0;
            var pr2 = new PetsciiMapgen.ProgressReporter((ulong)emoji.Count());
            //foreach (var e in emoji)
            for (int iemoji = 0; iemoji < emoji.Length; ++ iemoji)
            {
              var e = emoji[iemoji];
              pr2.Visit();
              if (e.scaleNeeded <= 0)
                continue;
              if (Math.Abs(lastScale - e.scaleNeeded) > 0.001)
              {
                scaleChanges++;
                dt.SetFont(fontName, targetHeight * e.scaleNeeded * additionalScale);
              }

              //SharpDX.Size2F sz;
              var n = new GlyphData(e);
              fullEmoji.Add(n);

              RawColor4 bg = new RawColor4(backgroundColor.R / 255.0f, backgroundColor.G / 255.0f, backgroundColor.B / 255.0f, 1);
              dt.SetColor(textColor);
              n.bmp = dt.TextToBitmap(n.info.str, out n.bmpSize, bg, aaMode);

              // offset where to blit from, so it's centered.
              int ox = (int)((n.bmpSize.Width - targetWidth) / 2);
              int oy = (int)((n.bmpSize.Height - targetHeight) / 2);
              n.blitSourcRect = new System.Drawing.Rectangle(ox - shiftX, oy - shiftY, targetWidth, targetHeight);
              n.bgColor = backgroundColor;
              n.textColor = textColor;

              //int y = iemoji / columns;
              //int x = iemoji % columns;
              //g.DrawImage(bmpChar,
              //  new System.Drawing.Rectangle(x * targetWidth, y * targetHeight, targetWidth, targetHeight),
              //  new System.Drawing.Rectangle(ox - shiftX, oy - shiftY, targetWidth, targetHeight), System.Drawing.GraphicsUnit.Pixel
              //  );

              //bmpChar.Dispose();
              //iemoji++;
            }
          }
        }
      }

      PetsciiMapgen.Log.WriteLine("Scale changes: {0}", scaleChanges);
      PetsciiMapgen.Log.WriteLine("Total char count: {0}", fullEmoji.Count);
      PetsciiMapgen.Log.WriteLine("Image size to hold entire colored charset: {0}, {1}", imgWidth, imgHeight);

      GenerateEmojiBitmapResults rv = new GenerateEmojiBitmapResults();
      //rv.bmp = bmp;
      rv.columns = columns;
      rv.rows = rows;
      rv.AllCells = fullEmoji.ToArray();
      return rv;
    }
  }
  public class Direct2DText : IDisposable
  {
    // initialize the D3D device which will allow to render to image any graphics - 3D or 2D
    d3d.Device defaultDevice;
    d3d.Device1 d3dDevice;
    dxgi.Device dxgiDevice;
    Device d2dDevice;
    wic.ImagingFactory2 imagingFactory = new wic.ImagingFactory2(); // initialize the WIC factory

    // initialize the DeviceContext - it will be the D2D render target and will allow all rendering operations
    DeviceContext d2dContext;
    dw.Factory dwFactory;

    // specify a pixel format that is supported by both D2D and WIC
    //PixelFormat d2PixelFormat = new d2.PixelFormat(dxgi.Format.R8G8B8A8_UNorm, d2.AlphaMode.Premultiplied);
    PixelFormat d2PixelFormat = new d2.PixelFormat(dxgi.Format.R8G8B8A8_UNorm, d2.AlphaMode.Ignore);

    // if in D2D was specified an R-G-B-A format - use the same for wic
    Guid wicPixelFormat = wic.PixelFormat.Format32bppPRGBA;
    TextFormat textFormat;
    SolidColorBrush textBrush;
    BitmapProperties1 d2dBitmapProps;

    float dpi = 96;

    public Direct2DText()
    {
      defaultDevice = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware,
                                                    d3d.DeviceCreationFlags.VideoSupport
                                                    | d3d.DeviceCreationFlags.BgraSupport
                                                    | d3d.DeviceCreationFlags.None); // take out the Debug flag for better performance
      d3dDevice = defaultDevice.QueryInterface<d3d.Device1>(); // get a reference to the Direct3D 11.1 device
      dxgiDevice = d3dDevice.QueryInterface<dxgi.Device>(); // get a reference to DXGI device
      d2dDevice = new d2.Device(dxgiDevice); // initialize the D2D device
      imagingFactory = new wic.ImagingFactory2(); // initialize the WIC factory
      d2dContext = new d2.DeviceContext(d2dDevice, d2.DeviceContextOptions.None);
      dwFactory = new dw.Factory();
      d2dBitmapProps = new BitmapProperties1(d2PixelFormat, dpi, dpi, BitmapOptions.Target | BitmapOptions.CannotDraw);
    }

    public void SetFont(string fontName, float fontSize)
    {
      if (textFormat != null)
      {
        textFormat.Dispose();
      }
      textFormat = new TextFormat(dwFactory, fontName, fontSize);
    }
    public void SetColor(System.Drawing.Color color)
    {
      if (textBrush != null)
      {
        textBrush.Dispose();
      }
      textBrush = new SolidColorBrush(d2dContext, new RawColor4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255));
    }
    public Size2F GetTextSize(string text, int width = 1000, int height = 1000)
    {
      //TextLayout tl = new TextLayout(dwFactory, text, textFormat, width, height);
      //var cm = tl.GetClusterMetrics();
      //float mw = tl.DetermineMinWidth();
      //Size2F ret = new Size2F(tl.Metrics.Width, tl.Metrics.Height);
      //tl.Dispose();
      //return ret;
      // measure text width including white spaces
      TextLayout tl0 = new TextLayout(dwFactory, "A", textFormat, width, height);
      TextLayout tl1 = new TextLayout(dwFactory, text + "A", textFormat, width, height);
      int w = (int)(tl1.Metrics.Width - tl0.Metrics.Width);
      int h = (int)(tl1.Metrics.Height);
      tl0.Dispose();
      tl1.Dispose();
      //return result > width ? width : result;
      return new Size2F(w, h);
    }


    public System.Drawing.Bitmap TextToBitmap(string text, out Size2F size, RawColor4 bgcolor, d2.TextAntialiasMode aamode, int maxWidth = 1000, int maxHeight = 1000)
    {
      var sz = GetTextSize(text, maxWidth, maxHeight);
      int pixelWidth = (int)(sz.Width * 2);
      int pixelHeight = (int)(sz.Height * 2);

      var d2dRenderTarget = new Bitmap1(d2dContext, new Size2(pixelWidth, pixelHeight), d2dBitmapProps);
      if (d2dContext.Target != null)
      {
        d2dContext.Target.Dispose();
      }
      d2dContext.Target = d2dRenderTarget; // associate bitmap with the d2d context

      // Draw Text
      TextLayout textLayout = new TextLayout(dwFactory, text, textFormat, pixelWidth, pixelHeight);
      //d2dContext.TextRenderingParams = new RenderingParams(dwFactory, 1, 0, 0, PixelGeometry.Flat, renderingMode);
      d2dContext.TextAntialiasMode = aamode;

      d2dContext.BeginDraw();
      d2dContext.Clear(bgcolor);
      d2dContext.DrawTextLayout(new RawVector2(0, 0), textLayout, textBrush, DrawTextOptions.EnableColorFont);
      d2dContext.EndDraw();

      size = new Size2F(textLayout.Metrics.Width, textLayout.Metrics.Height);

      textLayout.Dispose();

      // Copy to MemoryStream
      var stream = new MemoryStream();
      var encoder = new wic.PngBitmapEncoder(imagingFactory);
      encoder.Initialize(stream);

      var bitmapFrameEncode = new wic.BitmapFrameEncode(encoder);
      bitmapFrameEncode.Initialize();
      bitmapFrameEncode.SetSize(pixelWidth, pixelHeight);
      bitmapFrameEncode.SetPixelFormat(ref wicPixelFormat);

      // this is the trick to write D2D1 bitmap to WIC
      var imageEncoder = new wic.ImageEncoder(imagingFactory, d2dDevice);
      var imageParam = new wic.ImageParameters(d2PixelFormat, dpi, dpi, 0, 0, pixelWidth, pixelHeight);
      imageEncoder.WriteFrame(d2dRenderTarget, bitmapFrameEncode, imageParam);
      bitmapFrameEncode.Commit();
      encoder.Commit();

      imageEncoder.Dispose();
      encoder.Dispose();
      bitmapFrameEncode.Dispose();
      d2dRenderTarget.Dispose();

      // Convert To Bitmap
      byte[] data = stream.ToArray();
      stream.Seek(0, SeekOrigin.Begin);
      var bmp = new System.Drawing.Bitmap(stream);
      stream.Dispose();

      return bmp;
    }
    #region IDisposable Support
    private bool disposedValue = false;

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          d2dContext.Dispose();
          dwFactory.Dispose();
          imagingFactory.Dispose();
          d2dDevice.Dispose();
          dxgiDevice.Dispose();
          d3dDevice.Dispose();
          defaultDevice.Dispose();
          textBrush.Dispose();
          textFormat.Dispose();
        }
        disposedValue = true;
      }
    }

    public void Dispose()
    {
      Dispose(true);
    }
    #endregion
  }
}

