using PriceTrackerAlert.Models;
using System.Net.Http;
using System.Text.Json;

namespace PriceTrackerAlert.Services;

public class PriceService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    // TradingView offset per symbol: TV_price = Binance_price + offset
    public static readonly Dictionary<string, (string BinanceSymbol, double Offset)> TradingViewMap = new()
    {
        ["BTCUSDT"] = ("BTCUSDT", +18.0),
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

    // No longer needed — kept for backward compat with AppSettings
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
                "XAUUSD" or "XAGUSD"
                    => await FetchGoldSilverAsync(symbol.ToUpper()),
                "USOIL" or "WTIUSD"
                    => await FetchOilAsync(),
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

    // Frankfurter.app — completely free, no API key, uses ECB/forex data
    // XAU and XAG are priced per troy ounce in USD
    private async Task<(double, string)> FetchGoldSilverAsync(string symbol)
    {
        // Frankfurter returns how many XAU/XAG per 1 USD
        // We need USD per 1 XAU/XAG, so we invert
        var metal = symbol == "XAUUSD" ? "XAU" : "XAG";
        var url   = $"https://api.frankfurter.app/latest?from=USD&to={metal}";
        var json  = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var rate  = doc.RootElement.GetProperty("rates").GetProperty(metal).GetDouble();
        // rate = XAU per 1 USD, so USD per XAU = 1/rate
        return (1.0 / rate, "");
    }

    // EIA.gov — US Energy Information Administration, completely free, no key needed
    // Returns latest WTI crude oil spot price
    private async Task<(double, string)> FetchOilAsync()
    {
        // EIA open data — WTI daily spot price, no API key required for this endpoint
        var url  = "https://api.eia.gov/v2/petroleum/pri/spt/data/?api_key=DEMO_KEY&frequency=daily&data[0]=value&facets[series][]=RWTC&sort[0][column]=period&sort[0][direction]=desc&length=1";
        var json = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var data  = doc.RootElement
                       .GetProperty("response")
                       .GetProperty("data");
        var first = data.EnumerateArray().First();
        var price = first.GetProperty("value").GetDouble();
        return (price, "");
    }

    public void SetTestPrice(string symbol, double price) =>
        _testPrices[symbol.ToUpper()] = price;
}
