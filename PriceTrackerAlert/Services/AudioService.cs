using NAudio.Wave;
using System.IO;

namespace PriceTrackerAlert.Services;

public class AudioService : IDisposable
{
    private IWavePlayer? _player;
    private AudioFileReader? _reader;
    private bool _disposed;

    public double Volume { get; set; } = 1.0;

    // Returns the resolved absolute path for display in Settings
    public static string ResolveDisplayPath(string soundFile)
    {
        if (soundFile is "default" or "default_mp3")
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "alert.mp3");
        return soundFile;
    }

    public void PlayLoop(string soundFile)
    {
        Stop();
        try
        {
            string path = soundFile is "default" or "default_mp3"
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "alert.mp3")
                : soundFile;

            // Fallback chain: alert.mp3 → alert.wav → beep
            if (!File.Exists(path))
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "alert.wav");

            if (!File.Exists(path)) { PlayBeep(); return; }

            _reader = new AudioFileReader(path) { Volume = (float)Volume };
            _player = new WaveOutEvent();
            _player.Init(new LoopStream(_reader));
            _player.Play();
        }
        catch { PlayBeep(); }
    }

    public void Stop()
    {
        _player?.Stop();
        _player?.Dispose();
        _reader?.Dispose();
        _player = null;
        _reader = null;
    }

    private static void PlayBeep() =>
        Task.Run(() => { for (int i = 0; i < 3; i++) { Console.Beep(880, 400); Thread.Sleep(200); } });

    public void Dispose() { if (!_disposed) { Stop(); _disposed = true; } }
}

internal class LoopStream(WaveStream source) : WaveStream
{
    public override WaveFormat WaveFormat => source.WaveFormat;
    public override long Length => long.MaxValue;
    public override long Position { get => source.Position; set => source.Position = value; }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = 0;
        while (read < count)
        {
            int r = source.Read(buffer, offset + read, count - read);
            if (r == 0) source.Position = 0;
            else read += r;
        }
        return read;
    }
}
