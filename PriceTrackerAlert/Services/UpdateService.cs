using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace PriceTrackerAlert.Services;

public class UpdateService
{
    private const string ApiUrl    = "https://api.github.com/repos/subhampro/multi_pricetracker_alert_PC/releases/latest";
    private const string UserAgent = "PriceTrackerAlert-Updater";

    private readonly HttpClient _http = new();

    public string CurrentVersion { get; } =
        System.Diagnostics.FileVersionInfo
              .GetVersionInfo(Environment.ProcessPath!)
              .ProductVersion
              ?.Split('+')[0]
              ?? "1.0.0";

    public event Action<string, string>? UpdateAvailable;

    public UpdateService() => _http.DefaultRequestHeaders.Add("User-Agent", UserAgent);

    public async Task CheckAsync()
    {
        try
        {
            var json = await _http.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var tag  = root.GetProperty("tag_name").GetString()!.TrimStart('v');

            string? downloadUrl = null;
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }

            if (downloadUrl != null && IsNewer(tag, CurrentVersion))
                UpdateAvailable?.Invoke(tag, downloadUrl);
        }
        catch { }
    }

    public async Task DownloadAndInstallAsync(string downloadUrl, Action<int> onProgress)
    {
        var currentExe = Environment.ProcessPath!;
        var dir        = Path.GetDirectoryName(currentExe)!;
        var newExe     = Path.Combine(dir, "PriceTrackerAlert_new.exe");
        var updater    = Path.Combine(dir, "updater.bat");

        // Download new exe with progress reporting
        using var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        var total      = response.Content.Headers.ContentLength ?? -1L;
        var buffer     = new byte[81920];
        long downloaded = 0;

        await using var fs     = File.Create(newExe);
        await using var stream = await response.Content.ReadAsStreamAsync();
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, read));
            downloaded += read;
            if (total > 0) onProgress((int)(downloaded * 100 / total));
        }
        fs.Close();

        // Write updater batch — waits for this process to exit by PID,
        // then moves new exe over current exe (no leftover files), launches it, deletes self
        int pid = Environment.ProcessId;
        var lines = new[]
        {
            "@echo off",
            ":waitloop",
            $"tasklist /FI \"PID eq {pid}\" 2>nul | find \"{pid}\" >nul",
            "if not errorlevel 1 (",
            "    timeout /t 1 /nobreak >nul",
            "    goto waitloop",
            ")",
            $"move /y \"{newExe}\" \"{currentExe}\"",
            $"start \"\" \"{currentExe}\"",
            "del \"%~f0\""
        };
        File.WriteAllLines(updater, lines);

        Process.Start(new ProcessStartInfo
        {
            FileName        = updater,
            CreateNoWindow  = true,
            UseShellExecute = true
        });

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
            System.Windows.Application.Current.Shutdown());
    }

    private static bool IsNewer(string latest, string current)
    {
        if (Version.TryParse(latest, out var l) && Version.TryParse(current, out var c))
            return l > c;
        return false;
    }
}
