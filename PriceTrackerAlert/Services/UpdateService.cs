using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace PriceTrackerAlert.Services;

public class UpdateService
{
    private const string ApiUrl    = "https://api.github.com/repos/subhampro/multi_pricetracker_alert_PC/releases/latest";
    private const string UserAgent = "PriceTrackerAlert-Updater";

    private readonly HttpClient _http = new();

    public string CurrentVersion { get; } =
        Assembly.GetExecutingAssembly()
                .GetName().Version?.ToString(3) ?? "1.0.0";

    public event Action<string, string>? UpdateAvailable;  // (latestVersion, downloadUrl)

    public UpdateService() => _http.DefaultRequestHeaders.Add("User-Agent", UserAgent);

    public async Task CheckAsync()
    {
        try
        {
            var json    = await _http.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(json);
            var root    = doc.RootElement;

            var tag     = root.GetProperty("tag_name").GetString()!.TrimStart('v');
            var assets  = root.GetProperty("assets");

            string? downloadUrl = null;
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }

            if (downloadUrl == null) return;

            if (IsNewer(tag, CurrentVersion))
                UpdateAvailable?.Invoke(tag, downloadUrl);
        }
        catch { /* silent — no internet or API error */ }
    }

    public async Task DownloadAndInstallAsync(string downloadUrl, Action<int> onProgress)
    {
        var currentExe = Environment.ProcessPath!;
        var dir        = Path.GetDirectoryName(currentExe)!;
        var newExe     = Path.Combine(dir, "PriceTrackerAlert_new.exe");
        var oldExe     = Path.Combine(dir, "PriceTrackerAlert_old.exe");
        var updater    = Path.Combine(dir, "updater.bat");

        // Download with progress
        using var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        var total   = response.Content.Headers.ContentLength ?? -1L;
        var buffer  = new byte[81920];
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

        // Write a batch script that:
        // 1. Waits for current process to exit
        // 2. Renames old exe, moves new exe in place
        // 3. Starts the new exe
        // 4. Deletes itself and the old exe
        File.WriteAllText(updater, $@"@echo off
timeout /t 2 /nobreak >nul
move /y ""{currentExe}"" ""{oldExe}""
move /y ""{newExe}"" ""{currentExe}""
start """" ""{currentExe}""
del ""{oldExe}"" >nul 2>&1
del ""%~f0""
");

        Process.Start(new ProcessStartInfo
        {
            FileName        = updater,
            CreateNoWindow  = true,
            UseShellExecute = false
        });

        // Exit current process so the batch can replace the file
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
