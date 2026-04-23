using System;
using System.IO;

int sampleRate = 44100;
double duration = 0.5;
double freq = 880.0;
int samples = (int)(sampleRate * duration);
byte[] data = new byte[samples * 2];

for (int i = 0; i < samples; i++)
{
    short v = (short)(Math.Sin(2 * Math.PI * freq * i / sampleRate) * 28000);
    data[i * 2]     = (byte)(v & 0xFF);
    data[i * 2 + 1] = (byte)((v >> 8) & 0xFF);
}

using var fs = File.OpenWrite(@"f:\GitRepo\multi_pricetracker_alert_PC\PriceTrackerAlert\Assets\alert.wav");
using var bw = new BinaryWriter(fs);
bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
bw.Write(36 + data.Length);
bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
bw.Write(16);
bw.Write((short)1);
bw.Write((short)1);
bw.Write(sampleRate);
bw.Write(sampleRate * 2);
bw.Write((short)2);
bw.Write((short)16);
bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
bw.Write(data.Length);
bw.Write(data);
Console.WriteLine("alert.wav created");
