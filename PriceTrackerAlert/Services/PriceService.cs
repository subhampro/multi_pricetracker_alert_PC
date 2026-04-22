using System.Net.Http;
using System.Text.Json;

namespace PriceTrackerAlert.Services;

public class PriceService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private string _goldApiKey = "";
    private string _oilApiKey = "";

    // Test mode prices — used when API keys are not set
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

    public async Task<(double price, string error)> GetPriceAsync(string symbol)
    {
        if (TestMode)
            return _testPrices.TryGetValue(symbol.ToUpper(), out var tp) ? (tp, "") : (0, "Unknown symbol in test mode");

        try
        {
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
        var url = $"https://api.binance.com/api/v3/ticker/price?symbol={symbol}";
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

        // metals-api.com free tier
        var metal = symbol == "XAUUSD" ? "XAU" : "XAG";
        var url = $"https://metals-api.com/api/latest?access_key={_goldApiKey}&base=USD&symbols={metal}";
        var json = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.GetProperty("success").GetBoolean())
            return (0, "Metals API error");
        // rate is metal per USD, so price in USD = 1 / rate
        var rate = doc.RootElement.GetProperty("rates").GetProperty(metal).GetDouble();
        return (1.0 / rate, "");
    }

    private async Task<(double, string)> FetchOilAsync()
    {
        if (string.IsNullOrWhiteSpace(_oilApiKey))
            return (0, "Oil API key not set. Add it in Settings.");

        // Alpha Vantage commodity endpoint
        var url = $"https://www.alphavantage.co/query?function=WTI&interval=daily&apikey={_oilApiKey}";
        var json = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        var latest = data.EnumerateArray().First();
        var price = double.Parse(latest.GetProperty("value").GetString()!,
            System.Globalization.CultureInfo.InvariantCulture);
        return (price, "");
    }

    // Bump test price for simulation
    public void SetTestPrice(string symbol, double price) =>
        _testPrices[symbol.ToUpper()] = price;
}
