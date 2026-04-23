using PriceTrackerAlert.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PriceTrackerAlert.Services;

public class PriceService
{
    private readonly HttpClient _http;

    // TradingView offset per symbol: TV_price = Binance_price + offset
    public static readonly Dictionary<string, (string BinanceSymbol, double Offset)> TradingViewMap = new()
    {
        ["BTCUSDT"] = ("BTCUSDT", +18.0),
    };

    // TradingView scanner symbols — confirmed working with correct prices
    private static readonly Dictionary<string, string> TvSymbolMap = new()
    {
        ["XAUUSD"] = "COMEX:GC1!",   // Gold futures - real-time spot equivalent
        ["XAGUSD"] = "COMEX:SI1!",   // Silver futures
        ["USOIL"]  = "NYMEX:CL1!",   // WTI Crude Oil futures
    };

    private readonly Dictionary<string, double> _testPrices = new()
    {
        ["BTCUSDT"] = 100000,
        ["ETHUSDT"] = 3500,
        ["XAUUSD"]  = 2400,
        ["XAGUSD"]  = 28,
        ["USOIL"]   = 85
    };

    public bool TestMode { get; set; } = false;

    public PriceService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0 Safari/537.36");
        _http.DefaultRequestHeaders.Add("Origin", "https://www.tradingview.com");
        _http.DefaultRequestHeaders.Add("Referer", "https://www.tradingview.com/");
    }

    public void Configure(string goldApiKey, string oilApiKey) { }

    public async Task<(double price, string error)> GetPriceAsync(string symbol, PriceSource source = PriceSource.Binance)
    {
        if (TestMode)
            return _testPrices.TryGetValue(symbol.ToUpper(), out var tp) ? (tp, "") : (0, "Unknown symbol in test mode");

        try
        {
            if (source == PriceSource.TradingView && TradingViewMap.TryGetValue(symbol.ToUpper(), out var map))
            {
                var (rawPrice, err) = await FetchBinanceAsync(map.BinanceSymbol);
                if (!string.IsNullOrEmpty(err)) return (0, err);
                return (rawPrice + map.Offset, "");
            }

            return symbol.ToUpper() switch
            {
                "BTCUSDT" or "ETHUSDT" or "BNBUSDT" or "SOLUSDT" or "XRPUSDT"
                    => await FetchBinanceAsync(symbol.ToUpper()),
                "XAUUSD" or "XAGUSD" or "USOIL"
                    => await FetchTradingViewAsync(symbol.ToUpper()),
                _ => (0, $"Unsupported symbol: {symbol}")
            };
        }
        catch (HttpRequestException) { return (0, "Network error"); }
        catch (TaskCanceledException) { return (0, "Request timed out"); }
        catch (Exception ex)          { return (0, ex.Message); }
    }

    private async Task<(double, string)> FetchBinanceAsync(string symbol)
    {
        var url  = $"https://api.binance.com/api/v3/ticker/price?symbol={symbol}";
        var json = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var price = double.Parse(doc.RootElement.GetProperty("price").GetString()!,
            System.Globalization.CultureInfo.InvariantCulture);
        return (price, "");
    }

    // TradingView scanner API — same data source used by the Mathieu2301/Tradingview-API library
    // Completely free, no key, real-time prices
    private async Task<(double, string)> FetchTradingViewAsync(string symbol)
    {
        if (!TvSymbolMap.TryGetValue(symbol, out var tvSymbol))
            return (0, $"No TradingView mapping for {symbol}");

        var payload = JsonSerializer.Serialize(new
        {
            symbols = new { tickers = new[] { tvSymbol }, query = new { types = Array.Empty<string>() } },
            columns = new[] { "close", "lp" }  // lp = last price
        });

        var response = await _http.PostAsync(
            "https://scanner.tradingview.com/global/scan",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var data = doc.RootElement.GetProperty("data");
        foreach (var item in data.EnumerateArray())
        {
            var d = item.GetProperty("d");
            // Try lp (last price) first, then close
            for (int i = 0; i < d.GetArrayLength(); i++)
            {
                var el = d[i];
                if (el.ValueKind == JsonValueKind.Number)
                    return (el.GetDouble(), "");
            }
        }

        return (0, $"No price data returned for {symbol}");
    }

    public void SetTestPrice(string symbol, double price) =>
        _testPrices[symbol.ToUpper()] = price;
}
