using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

string outPath = @"f:\GitRepo\multi_pricetracker_alert_PC\PriceTrackerAlert\Assets\icon.ico";

// Windows Explorer needs: 16, 24, 32, 48 as BMP (32bpp), 256 as PNG
int[] sizes = { 16, 24, 32, 48, 256 };

var bitmaps = new Bitmap[sizes.Length];
for (int s = 0; s < sizes.Length; s++)
    bitmaps[s] = DrawIcon(sizes[s]);

using var fs = new FileStream(outPath, FileMode.Create);
using var bw = new BinaryWriter(fs);

// Prepare image data — BMP for <=48, PNG for 256
var imgData = new byte[sizes.Length][];
for (int i = 0; i < sizes.Length; i++)
{
    if (sizes[i] <= 48)
        imgData[i] = ToBmpBytes(bitmaps[i]);
    else
    {
        using var ms = new MemoryStream();
        bitmaps[i].Save(ms, ImageFormat.Png);
        imgData[i] = ms.ToArray();
    }
}

// ICO header
bw.Write((short)0);                  // reserved
bw.Write((short)1);                  // type: 1 = ICO
bw.Write((short)sizes.Length);       // image count

// Directory entries (each 16 bytes)
int offset = 6 + 16 * sizes.Length;
for (int i = 0; i < sizes.Length; i++)
{
    int sz = sizes[i];
    bw.Write((byte)(sz >= 256 ? 0 : sz));   // width  (0 means 256)
    bw.Write((byte)(sz >= 256 ? 0 : sz));   // height (0 means 256)
    bw.Write((byte)0);                       // color count (0 = true color)
    bw.Write((byte)0);                       // reserved
    bw.Write((short)1);                      // planes
    bw.Write((short)32);                     // bits per pixel
    bw.Write(imgData[i].Length);             // size of image data
    bw.Write(offset);                        // offset to image data
    offset += imgData[i].Length;
}

// Image data
foreach (var d in imgData)
    bw.Write(d);

foreach (var b in bitmaps) b.Dispose();
Console.WriteLine("icon.ico written: " + outPath);

// ── helpers ────────────────────────────────────────────────────────────────

static Bitmap DrawIcon(int sz)
{
    var bmp = new Bitmap(sz, sz, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode      = SmoothingMode.AntiAlias;
    g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
    g.Clear(Color.Transparent);

    // Background circle — dark green
    using var bg = new SolidBrush(Color.FromArgb(255, 22, 80, 28));
    g.FillEllipse(bg, 0, 0, sz - 1, sz - 1);

    // Subtle inner glow ring
    using var ring = new Pen(Color.FromArgb(60, 76, 175, 80), Math.Max(1f, sz * 0.04f));
    float ri = sz * 0.06f;
    g.DrawEllipse(ring, ri, ri, sz - 1 - ri * 2, sz - 1 - ri * 2);

    // Rising price chart line
    float pad = sz * 0.20f;
    float w   = sz - pad * 2;
    float h   = sz - pad * 2;

    PointF[] pts = {
        new(pad,           pad + h * 0.75f),
        new(pad + w*0.22f, pad + h * 0.52f),
        new(pad + w*0.44f, pad + h * 0.68f),
        new(pad + w*0.66f, pad + h * 0.28f),
        new(pad + w,       pad + h * 0.08f),
    };

    // Shadow line for depth
    float penW = Math.Max(1.5f, sz * 0.075f);
    using var shadow = new Pen(Color.FromArgb(80, 0, 0, 0), penW + 1f)
        { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
    var shadowPts = Array.ConvertAll(pts, p => new PointF(p.X + 0.5f, p.Y + 0.8f));
    g.DrawLines(shadow, shadowPts);

    // Main white line
    using var pen = new Pen(Color.White, penW)
        { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
    g.DrawLines(pen, pts);

    // Green dot at latest price point
    float dotR = penW * 1.4f;
    var tip = pts[^1];
    using var dotFill = new SolidBrush(Color.FromArgb(255, 76, 175, 80));
    g.FillEllipse(dotFill, tip.X - dotR, tip.Y - dotR, dotR * 2, dotR * 2);
    using var dotBorder = new Pen(Color.White, Math.Max(1f, penW * 0.55f));
    g.DrawEllipse(dotBorder, tip.X - dotR, tip.Y - dotR, dotR * 2, dotR * 2);

    return bmp;
}

// Convert Bitmap to raw BMP bytes (BITMAPINFOHEADER + pixels, no file header)
// ICO BMP stores XOR mask + AND mask, height is doubled in header
static byte[] ToBmpBytes(Bitmap bmp)
{
    int w = bmp.Width;
    int h = bmp.Height;
    int rowBytes = w * 4;                    // 32bpp, no padding needed (multiple of 4)
    int xorSize  = rowBytes * h;
    int andSize  = ((w + 31) / 32) * 4 * h; // 1bpp AND mask, padded to DWORD

    using var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms);

    // BITMAPINFOHEADER (40 bytes)
    bw.Write(40);           // biSize
    bw.Write(w);            // biWidth
    bw.Write(h * 2);        // biHeight — doubled for ICO (XOR + AND)
    bw.Write((short)1);     // biPlanes
    bw.Write((short)32);    // biBitCount
    bw.Write(0);            // biCompression (BI_RGB)
    bw.Write(xorSize);      // biSizeImage
    bw.Write(0);            // biXPelsPerMeter
    bw.Write(0);            // biYPelsPerMeter
    bw.Write(0);            // biClrUsed
    bw.Write(0);            // biClrImportant

    // XOR mask — 32bpp BGRA, bottom-up
    for (int y = h - 1; y >= 0; y--)
    {
        for (int x = 0; x < w; x++)
        {
            var c = bmp.GetPixel(x, y);
            bw.Write(c.B);
            bw.Write(c.G);
            bw.Write(c.R);
            bw.Write(c.A);
        }
    }

    // AND mask — 1bpp, 0 = opaque, 1 = transparent, bottom-up
    int andRowBytes = ((w + 31) / 32) * 4;
    for (int y = h - 1; y >= 0; y--)
    {
        int col = 0;
        for (int x = 0; x < andRowBytes * 8; x++)
        {
            bool transparent = x >= w || bmp.GetPixel(x, y).A < 128;
            col = (col << 1) | (transparent ? 1 : 0);
            if ((x + 1) % 8 == 0) bw.Write((byte)col);
        }
    }

    return ms.ToArray();
}
