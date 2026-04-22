namespace PriceTrackerAlert.Models;

public class Alert
{
    public int Id { get; set; }
    public string Symbol { get; set; } = "";
    public double TargetPrice { get; set; }
    public AlertCondition Condition { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsTriggered { get; set; } = false;
    public string SoundFile { get; set; } = "default_mp3";
    public string Note { get; set; } = "";
    public PriceSource Source { get; set; } = PriceSource.Binance;
}

public enum AlertCondition { Above, Below }

public enum PriceSource
{
    Binance     = 0,
    TradingView = 1   // Binance price + symbol offset
}
