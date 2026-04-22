using NAudio.Wave;
using System.IO;

namespace PriceTrackerAlert.Services;

public class AudioService : IDisposable
{
    private IWavePlayer? _player;
    private AudioFileReader? _reader;
    private bool _disposed;

    public double Volume { get; set; } = 1.0;

    public void PlayLoop(string soundFile)
    {
        Stop();
        try
        {
            var path = soundFile == "default"
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "alert.wav")
                : soundFile;

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

// Wraps a stream to loop it indefinitely
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
