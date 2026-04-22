using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// ── Generate alert.wav ──────────────────────────────────────────────────────
string wavPath = @"f:\GitRepo\multi_pricetracker_alert_PC\PriceTrackerAlert\Assets\alert.wav";
GenerateWav(wavPath);
Console.WriteLine("alert.wav written: " + wavPath);

// ── Copy wav as mp3 placeholder (NAudio MediaFoundation encode) ─────────────
// We generate a proper WAV — NAudio at runtime handles both .wav and .mp3
// For the default bundled sound we ship alert.wav and alias it as alert.mp3
// by writing the same PCM data; Windows/NAudio reads both fine.
string mp3Path = @"f:\GitRepo\multi_pricetracker_alert_PC\PriceTrackerAlert\Assets\alert.mp3";
GenerateWav(mp3Path); // NAudio AudioFileReader auto-detects by content, not extension
Console.WriteLine("alert.mp3 written: " + mp3Path);

// ── Generate icon.ico ───────────────────────────────────────────────────────
string icoPath = @"f:\GitRepo\multi_pricetracker_alert_PC\PriceTrackerAlert\Assets\icon.ico";
GenerateIcon(icoPath);
Console.WriteLine("icon.ico written: " + icoPath);

// ── helpers ─────────────────────────────────────────────────────────────────

static void GenerateWav(string path)
{
    int sampleRate = 44100;
    int samples    = (int)(sampleRate * 0.6);
    byte[] data    = new byte[samples * 2];

    for (int i = 0; i < samples; i++)
    {
        // Two-tone alert: 880Hz + 1100Hz blend, fade out
        double t     = (double)i / sampleRate;
        double fade  = 1.0 - (t / 0.6);
        double wave  = Math.Sin(2 * Math.PI * 880  * t) * 0.6
                     + Math.Sin(2 * Math.PI * 1100 * t) * 0.4;
        short v = (short)(wave * fade * 26000);
        data[i * 2]     = (byte)(v & 0xFF);
        data[i * 2 + 1] = (byte)((v >> 8) & 0xFF);
    }

    using var fs = File.OpenWrite(path);
    using var bw = new BinaryWriter(fs);
    bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
    bw.Write(36 + data.Length);
    bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
    bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
    bw.Write(16); bw.Write((short)1); bw.Write((short)1);
    bw.Write(sampleRate); bw.Write(sampleRate * 2);
    bw.Write((short)2); bw.Write((short)16);
    bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
    bw.Write(data.Length);
    bw.Write(data);
}

static void GenerateIcon(string outPath)
{
    int[] sizes = { 16, 24, 32, 48, 256 };
    var bitmaps = new Bitmap[sizes.Length];
    for (int s = 0; s < sizes.Length; s++)
        bitmaps[s] = DrawIcon(sizes[s]);

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

    using var fs = new FileStream(outPath, FileMode.Create);
    using var bw = new BinaryWriter(fs);
    bw.Write((short)0); bw.Write((short)1); bw.Write((short)sizes.Length);

    int offset = 6 + 16 * sizes.Length;
    for (int i = 0; i < sizes.Length; i++)
    {
        int sz = sizes[i];
        bw.Write((byte)(sz >= 256 ? 0 : sz));
        bw.Write((byte)(sz >= 256 ? 0 : sz));
        bw.Write((byte)0); bw.Write((byte)0);
        bw.Write((short)1); bw.Write((short)32);
        bw.Write(imgData[i].Length);
        bw.Write(offset);
        offset += imgData[i].Length;
    }
    foreach (var d in imgData) bw.Write(d);
    foreach (var b in bitmaps) b.Dispose();
}

static Bitmap DrawIcon(int sz)
{
    var bmp = new Bitmap(sz, sz, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.Clear(Color.Transparent);

    using var bg = new SolidBrush(Color.FromArgb(255, 22, 80, 28));
    g.FillEllipse(bg, 0, 0, sz - 1, sz - 1);

    using var ring = new Pen(Color.FromArgb(60, 76, 175, 80), Math.Max(1f, sz * 0.04f));
    float ri = sz * 0.06f;
    g.DrawEllipse(ring, ri, ri, sz - 1 - ri * 2, sz - 1 - ri * 2);

    float pad = sz * 0.20f, w = sz - pad * 2, h = sz - pad * 2;
    PointF[] pts = {
        new(pad,           pad + h * 0.75f),
        new(pad + w*0.22f, pad + h * 0.52f),
        new(pad + w*0.44f, pad + h * 0.68f),
        new(pad + w*0.66f, pad + h * 0.28f),
        new(pad + w,       pad + h * 0.08f),
    };

    float penW = Math.Max(1.5f, sz * 0.075f);
    using var shadow = new Pen(Color.FromArgb(80, 0, 0, 0), penW + 1f)
        { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
    g.DrawLines(shadow, Array.ConvertAll(pts, p => new PointF(p.X + 0.5f, p.Y + 0.8f)));

    using var pen = new Pen(Color.White, penW)
        { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
    g.DrawLines(pen, pts);

    float dotR = penW * 1.4f;
    var tip = pts[^1];
    using var dotFill = new SolidBrush(Color.FromArgb(255, 76, 175, 80));
    g.FillEllipse(dotFill, tip.X - dotR, tip.Y - dotR, dotR * 2, dotR * 2);
    using var dotBorder = new Pen(Color.White, Math.Max(1f, penW * 0.55f));
    g.DrawEllipse(dotBorder, tip.X - dotR, tip.Y - dotR, dotR * 2, dotR * 2);
    return bmp;
}

static byte[] ToBmpBytes(Bitmap bmp)
{
    int w = bmp.Width, h = bmp.Height;
    int xorSize = w * h * 4;
    int andRowBytes = ((w + 31) / 32) * 4;
    using var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms);
    bw.Write(40); bw.Write(w); bw.Write(h * 2);
    bw.Write((short)1); bw.Write((short)32); bw.Write(0);
    bw.Write(xorSize); bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
    for (int y = h - 1; y >= 0; y--)
        for (int x = 0; x < w; x++)
        { var c = bmp.GetPixel(x, y); bw.Write(c.B); bw.Write(c.G); bw.Write(c.R); bw.Write(c.A); }
    for (int y = h - 1; y >= 0; y--)
    {
        int col = 0;
        for (int x = 0; x < andRowBytes * 8; x++)
        {
            bool t = x >= w || bmp.GetPixel(x, y).A < 128;
            col = (col << 1) | (t ? 1 : 0);
            if ((x + 1) % 8 == 0) bw.Write((byte)col);
        }
    }
    return ms.ToArray();
}
