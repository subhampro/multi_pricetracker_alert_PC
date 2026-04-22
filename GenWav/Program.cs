using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

string outPath = @"f:\GitRepo\multi_pricetracker_alert_PC\PriceTrackerAlert\Assets\icon.ico";

// Generate bitmaps at 16, 32, 48, 256
int[] sizes = { 16, 32, 48, 256 };
var bitmaps = new Bitmap[sizes.Length];

for (int s = 0; s < sizes.Length; s++)
{
    int sz = sizes[s];
    var bmp = new Bitmap(sz, sz, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.Clear(Color.Transparent);

    // Background circle - dark green
    using var bgBrush = new SolidBrush(Color.FromArgb(255, 27, 94, 32));
    g.FillEllipse(bgBrush, 0, 0, sz - 1, sz - 1);

    // Chart line - white zigzag representing price movement
    float pad = sz * 0.18f;
    float w = sz - pad * 2;
    float h = sz - pad * 2;
    float mid = sz / 2f;

    // Points for a rising price chart line
    PointF[] pts = {
        new(pad,           pad + h * 0.7f),
        new(pad + w*0.25f, pad + h * 0.5f),
        new(pad + w*0.45f, pad + h * 0.65f),
        new(pad + w*0.65f, pad + h * 0.25f),
        new(pad + w,       pad + h * 0.1f),
    };

    float penW = Math.Max(1.5f, sz * 0.07f);
    using var pen = new Pen(Color.White, penW) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
    g.DrawLines(pen, pts);

    // Small dot at the end (latest price)
    float dotR = penW * 1.2f;
    using var dotBrush = new SolidBrush(Color.FromArgb(255, 76, 175, 80));
    g.FillEllipse(dotBrush, pts[^1].X - dotR, pts[^1].Y - dotR, dotR * 2, dotR * 2);
    using var dotPen = new Pen(Color.White, penW * 0.6f);
    g.DrawEllipse(dotPen, pts[^1].X - dotR, pts[^1].Y - dotR, dotR * 2, dotR * 2);

    bitmaps[s] = bmp;
}

// Write ICO format manually
using var fs = new FileStream(outPath, FileMode.Create);
using var bw = new BinaryWriter(fs);

// ICO header
bw.Write((short)0);       // reserved
bw.Write((short)1);       // type: ICO
bw.Write((short)sizes.Length); // count

// Calculate offsets - header(6) + directory(16 * count)
int dataOffset = 6 + 16 * sizes.Length;
var pngDatas = new byte[sizes.Length][];

for (int i = 0; i < sizes.Length; i++)
{
    using var ms = new MemoryStream();
    bitmaps[i].Save(ms, ImageFormat.Png);
    pngDatas[i] = ms.ToArray();
}

// Directory entries
for (int i = 0; i < sizes.Length; i++)
{
    int sz = sizes[i];
    bw.Write((byte)(sz >= 256 ? 0 : sz));  // width (0 = 256)
    bw.Write((byte)(sz >= 256 ? 0 : sz));  // height
    bw.Write((byte)0);   // color count
    bw.Write((byte)0);   // reserved
    bw.Write((short)1);  // planes
    bw.Write((short)32); // bit count
    bw.Write(pngDatas[i].Length);
    bw.Write(dataOffset);
    dataOffset += pngDatas[i].Length;
}

// PNG data
foreach (var png in pngDatas)
    bw.Write(png);

foreach (var b in bitmaps) b.Dispose();
Console.WriteLine("icon.ico created: " + outPath);
