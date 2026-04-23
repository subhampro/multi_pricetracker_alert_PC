using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;

namespace PriceTrackerAlert.Services;

public class SymbolIconService
{
    private static readonly HttpClient _http = new();
    private static readonly string _cacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PriceTrackerAlert", "icons");

    // TradingView SVG URLs for each symbol
    private static readonly Dictionary<string, string> SvgUrls = new()
    {
        ["BTCUSDT"] = "https://s3-symbol-logo.tradingview.com/crypto/XTVCBTC--big.svg",
        ["ETHUSDT"] = "https://s3-symbol-logo.tradingview.com/crypto/XTVCETH--big.svg",
        ["BNBUSDT"] = "https://s3-symbol-logo.tradingview.com/crypto/XTVCBNB--big.svg",
        ["SOLUSDT"] = "https://s3-symbol-logo.tradingview.com/crypto/XTVCSOL--big.svg",
        ["XRPUSDT"] = "https://s3-symbol-logo.tradingview.com/crypto/XTVCXRP--big.svg",
        ["XAUUSD"]  = "https://s3-symbol-logo.tradingview.com/metal/gold--big.svg",
        ["XAGUSD"]  = "https://s3-symbol-logo.tradingview.com/metal/silver--big.svg",
        ["USOIL"]   = "https://s3-symbol-logo.tradingview.com/crude-oil--big.svg",
    };

    private static readonly Dictionary<string, DrawingGroup?> _cache = new();
    private static readonly SemaphoreSlim _lock = new(1, 1);

    static SymbolIconService()
    {
        Directory.CreateDirectory(_cacheDir);
        _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
    }

    public static async Task<DrawingGroup?> GetIconAsync(string symbol)
    {
        symbol = symbol.ToUpper();
        if (_cache.TryGetValue(symbol, out var cached)) return cached;

        await _lock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(symbol, out cached)) return cached;
            if (!SvgUrls.TryGetValue(symbol, out var url)) return null;

            var svgPath = Path.Combine(_cacheDir, $"{symbol}.svg");

            // Download if not cached on disk
            if (!File.Exists(svgPath))
            {
                try
                {
                    var svg = await _http.GetStringAsync(url);
                    await File.WriteAllTextAsync(svgPath, svg);
                }
                catch { _cache[symbol] = null; return null; }
            }

            // Parse SVG to WPF DrawingGroup
            var drawing = LoadSvg(svgPath);
            _cache[symbol] = drawing;
            return drawing;
        }
        finally { _lock.Release(); }
    }

    private static DrawingGroup? LoadSvg(string path)
    {
        try
        {
            var settings = new WpfDrawingSettings
            {
                IncludeRuntime = false,
                TextAsGeometry = false,
            };
            using var converter = new FileSvgConverter(settings);
            bool ok = converter.Convert(path);
            return ok ? converter.Drawing : null;
        }
        catch { return null; }
    }

    // Synchronous version — returns cached or null (triggers async load in background)
    public static DrawingGroup? GetIconSync(string symbol)
    {
        symbol = symbol.ToUpper();
        if (_cache.TryGetValue(symbol, out var cached)) return cached;
        // Trigger background load
        _ = GetIconAsync(symbol);
        return null;
    }
}
