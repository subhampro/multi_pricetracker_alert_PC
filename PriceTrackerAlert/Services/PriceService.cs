using PriceTrackerAlert.Models;
using System.Net.Http;
using System.Text.Json;

namespace PriceTrackerAlert.Services;

public class PriceService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private string _goldApiKey = "";
    private string _oilApiKey  = "";

    // TradingView offset per symbol: TV_price = Binance_price + offset
    // Key is the SAME symbol user selects — no separate pair needed
    public static readonly Dictionary<string, (string BinanceSymbol, double Offset)> TradingViewMap = new()
    {
        ["BTCUSDT"] = ("BTCUSDT", +18.0),
    };

    private readonly Dictionary<string, double> _testPrices = new()
    {
        ["BTCUSDT"] = 100000,
        ["ETHUSDT"] = 3500,
        ["XAUUSD"]  = 2400,
        ["USOIL"]   = 85
    };

    public bool TestMode { get; set; } = false;

    public void Configure(string goldApiKey, string oilApiKey)
    {
        _goldApiKey = goldApiKey;
        _oilApiKey  = oilApiKey;
    }

    /// <summary>
    /// Fetches price for the given symbol + source.
    /// For TradingView source: fetches the mapped Binance symbol and applies the offset.
    /// </summary>
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
                "XAUUSD" or "XAGUSD"
                    => await FetchGoldAsync(symbol.ToUpper()),
                "USOIL" or "WTIUSD"
                    => await FetchOilAsync(),
                _ => (0, $"Unsupported symbol: {symbol}")
            };
        }
        catch (HttpRequestException) { return (0, "Network error"); }
        catch (TaskCanceledException)  { return (0, "Request timed out"); }
        catch (Exception ex)           { return (0, ex.Message); }
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

    private async Task<(double, string)> FetchGoldAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(_goldApiKey))
            return (0, "Gold API key not set. Add it in Settings.");
        var metal = symbol == "XAUUSD" ? "XAU" : "XAG";
        var url   = $"https://metals-api.com/api/latest?access_key={_goldApiKey}&base=USD&symbols={metal}";
        var json  = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.GetProperty("success").GetBoolean())
            return (0, "Metals API error");
        var rate = doc.RootElement.GetProperty("rates").GetProperty(metal).GetDouble();
        return (1.0 / rate, "");
    }

    private async Task<(double, string)> FetchOilAsync()
    {
        if (string.IsNullOrWhiteSpace(_oilApiKey))
            return (0, "Oil API key not set. Add it in Settings.");
        var url  = $"https://www.alphavantage.co/query?function=WTI&interval=daily&apikey={_oilApiKey}";
        var json = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var latest = doc.RootElement.GetProperty("data").EnumerateArray().First();
        var price  = double.Parse(latest.GetProperty("value").GetString()!,
            System.Globalization.CultureInfo.InvariantCulture);
        return (price, "");
    }

    public void SetTestPrice(string symbol, double price) =>
        _testPrices[symbol.ToUpper()] = price;
}
