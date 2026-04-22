using PriceTrackerAlert.Models;

namespace PriceTrackerAlert.Services;

public class AlertEngine
{
    private readonly PriceService _prices;
    private readonly StorageService _storage;
    private CancellationTokenSource? _cts;

    public event Action<Alert, double>? AlertTriggered;
    public event Action<string, string>? PriceUpdated;   // symbol, formatted price
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
        var symbols = alerts.Select(a => a.Symbol.ToUpper()).Distinct();

        foreach (var symbol in symbols)
        {
            var (price, error) = await _prices.GetPriceAsync(symbol);

            if (!string.IsNullOrEmpty(error))
            {
                StatusChanged?.Invoke($"{symbol}: {error}");
                continue;
            }

            PriceUpdated?.Invoke(symbol, FormatPrice(symbol, price));

            foreach (var alert in alerts.Where(a => a.Symbol.ToUpper() == symbol && !a.IsTriggered))
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
        var alerts = _storage.GetAlerts();
        var alert = alerts.FirstOrDefault(a => a.Id == alertId);
        if (alert == null) return;
        alert.IsTriggered = false;
        _storage.UpdateAlert(alert);
    }

    private static string FormatPrice(string symbol, double price) =>
        symbol is "BTCUSDT" ? $"${price:N0}" : $"${price:N2}";
}
