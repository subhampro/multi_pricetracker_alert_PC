using PriceTrackerAlert.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PriceTrackerAlert.Services;

namespace PriceTrackerAlert.ViewModels;

public class AlertItem : INotifyPropertyChanged
{
    private bool _isActive;
    private bool _isTriggered;
    private string _livePrice = "--";

    public Alert Model { get; }

    public AlertItem(Alert model)
    {
        Model = model;
        _isActive    = model.IsActive;
        _isTriggered = model.IsTriggered;
    }

    public int    Id          => Model.Id;
    public string Symbol      => Model.Symbol;
    public string SymbolIcon  => SymbolIcons.GetIcon(Model.Symbol);
    public string SymbolColor => SymbolIcons.GetColor(Model.Symbol);
    public double TargetPrice => Model.TargetPrice;
    public string Condition   => Model.Condition == AlertCondition.Above ? "▲ Above" : "▼ Below";
    public string Note        => Model.Note;
    public string SourceBadge => Model.Source == PriceSource.TradingView ? "📺 TradingView" : "🔶 Binance";
    public string UiKey       => AlertEngine.UiKey(Model.Symbol, Model.Source);

    public string LivePrice
    {
        get => _livePrice;
        set { _livePrice = value; OnPropertyChanged(); }
    }

    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; Model.IsActive = value; OnPropertyChanged(); }
    }

    public bool IsTriggered
    {
        get => _isTriggered;
        set { _isTriggered = value; OnPropertyChanged(); }
    }

    public string StatusText => IsTriggered ? "🔔 TRIGGERED" : IsActive ? "✅ Active" : "⏸ Paused";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
