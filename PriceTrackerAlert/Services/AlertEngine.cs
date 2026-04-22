using PriceTrackerAlert.Models;

namespace PriceTrackerAlert.Services;

public class AlertEngine
{
    private readonly PriceService  _prices;
    private readonly StorageService _storage;
    private CancellationTokenSource? _cts;

    public event Action<Alert, double>? AlertTriggered;
    public event Action<string, string>? PriceUpdated;  // symbol+source key, formatted price
    public event Action<string>? StatusChanged;

    public AlertEngine(PriceService prices, StorageService storage)
    {
        _prices  = prices;
        _storage = storage;
    }

    public void Start(int intervalSeconds)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _ = RunLoop(intervalSeconds, _cts.Token);
    }

    public void Stop() => _cts?.Cancel();

    private async Task RunLoop(int intervalSeconds, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await CheckAllAlerts();
            try { await Task.Delay(intervalSeconds * 1000, ct); }
            catch (TaskCanceledException) { break; }
        }
    }

    public async Task CheckAllAlerts()
    {
        var alerts = _storage.GetAlerts().Where(a => a.IsActive).ToList();

        // Group by (Symbol, Source) so each unique feed is fetched once
        var groups = alerts
            .GroupBy(a => (Symbol: a.Symbol.ToUpper(), a.Source))
            .ToList();

        foreach (var group in groups)
        {
            var (symbol, source) = group.Key;
            var (price, error)   = await _prices.GetPriceAsync(symbol, source);

            if (!string.IsNullOrEmpty(error))
            {
                StatusChanged?.Invoke($"{symbol}: {error}");
                continue;
            }

            // Key used to match AlertItems in the UI
            string uiKey = UiKey(symbol, source);
            PriceUpdated?.Invoke(uiKey, FormatPrice(symbol, price));

            foreach (var alert in group.Where(a => !a.IsTriggered))
            {
                bool hit = alert.Condition == AlertCondition.Above
                    ? price >= alert.TargetPrice
                    : price <= alert.TargetPrice;

                if (hit)
                {
                    alert.IsTriggered = true;
                    _storage.UpdateAlert(alert);
                    AlertTriggered?.Invoke(alert, price);
                }
            }
        }

        StatusChanged?.Invoke($"Last check: {DateTime.Now:HH:mm:ss}");
    }

    public void ResetAlert(int alertId)
    {
        var alert = _storage.GetAlerts().FirstOrDefault(a => a.Id == alertId);
        if (alert == null) return;
        alert.IsTriggered = false;
        _storage.UpdateAlert(alert);
    }

    // Unique key per symbol+source for UI live price matching
    public static string UiKey(string symbol, PriceSource source) =>
        source == PriceSource.TradingView ? $"{symbol.ToUpper()}|TV" : symbol.ToUpper();

    private static string FormatPrice(string symbol, double price) =>
        symbol is "BTCUSDT" or "BTCUSD" ? $"${price:N2}" : $"${price:N2}";
}
