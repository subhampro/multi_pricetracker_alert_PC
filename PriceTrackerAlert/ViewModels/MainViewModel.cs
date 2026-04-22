using PriceTrackerAlert.Models;
using PriceTrackerAlert.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PriceTrackerAlert.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly StorageService _storage;
    private readonly PriceService   _prices;
    private readonly AlertEngine    _engine;
    private readonly AudioService   _audio;

    private string _statusText = "Ready";
    private string _newSymbol  = "BTCUSDT";
    private double _newTarget  = 100000;
    private AlertCondition _newCondition = AlertCondition.Above;
    private PriceSource    _newSource    = PriceSource.Binance;
    private string _newNote = "";

    public ObservableCollection<AlertItem> Alerts { get; } = [];
    public AppSettings Settings { get; private set; }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string NewSymbol
    {
        get => _newSymbol;
        set
        {
            _newSymbol = value.ToUpper();
            OnPropertyChanged();
            OnPropertyChanged(nameof(AvailableSources));
            // Reset to Binance if TV not available for this symbol
            if (!PriceService.TradingViewMap.ContainsKey(_newSymbol))
                NewSource = PriceSource.Binance;
        }
    }

    public double NewTarget
    {
        get => _newTarget;
        set { _newTarget = value; OnPropertyChanged(); }
    }

    public AlertCondition NewCondition
    {
        get => _newCondition;
        set { _newCondition = value; OnPropertyChanged(); }
    }

    public string NewNote
    {
        get => _newNote;
        set { _newNote = value; OnPropertyChanged(); }
    }

    public PriceSource NewSource
    {
        get => _newSource;
        set { _newSource = value; OnPropertyChanged(); }
    }

    // Available sources for the symbol currently selected
    public List<PriceSource> AvailableSources =>
        PriceService.TradingViewMap.ContainsKey(_newSymbol.ToUpper())
            ? [PriceSource.Binance, PriceSource.TradingView]
            : [PriceSource.Binance];

    public bool TestMode
    {
        get => _prices.TestMode;
        set { _prices.TestMode = value; OnPropertyChanged(); StatusText = value ? "⚠ TEST MODE" : "Ready"; }
    }

    public RelayCommand AddAlertCommand    { get; }
    public RelayCommand<AlertItem> DeleteAlertCommand  { get; }
    public RelayCommand<AlertItem> ToggleAlertCommand  { get; }
    public RelayCommand<AlertItem> ResetAlertCommand   { get; }
    public RelayCommand OpenSettingsCommand { get; }

    public MainViewModel(StorageService storage, PriceService prices, AlertEngine engine, AudioService audio)
    {
        _storage = storage;
        _prices  = prices;
        _engine  = engine;
        _audio   = audio;
        Settings = _storage.LoadSettings();

        AddAlertCommand    = new RelayCommand(AddAlert);
        DeleteAlertCommand = new RelayCommand<AlertItem>(DeleteAlert);
        ToggleAlertCommand = new RelayCommand<AlertItem>(ToggleAlert);
        ResetAlertCommand  = new RelayCommand<AlertItem>(ResetAlert);
        OpenSettingsCommand = new RelayCommand(OpenSettings);

        _engine.AlertTriggered += OnAlertTriggered;
        _engine.PriceUpdated   += OnPriceUpdated;
        _engine.StatusChanged  += s => Application.Current.Dispatcher.Invoke(() => StatusText = s);

        LoadAlerts();
        ApplySettings();
        _engine.Start(Settings.CheckIntervalSeconds);
    }

    private void LoadAlerts()
    {
        Alerts.Clear();
        foreach (var a in _storage.GetAlerts())
            Alerts.Add(new AlertItem(a));
    }

    private void AddAlert()
    {
        var alert = new Alert
        {
            Symbol      = NewSymbol,
            TargetPrice = NewTarget,
            Condition   = NewCondition,
            Source      = NewSource,
            Note        = NewNote,
            IsActive    = true
        };
        alert.Id = _storage.AddAlert(alert);
        Alerts.Add(new AlertItem(alert));
        NewNote = "";
    }

    private void DeleteAlert(AlertItem? item)
    {
        if (item == null) return;
        _storage.DeleteAlert(item.Id);
        Alerts.Remove(item);
    }

    private void ToggleAlert(AlertItem? item)
    {
        if (item == null) return;
        item.IsActive = !item.IsActive;
        _storage.UpdateAlert(item.Model);
    }

    private void ResetAlert(AlertItem? item)
    {
        if (item == null) return;
        item.Model.IsTriggered = false;
        item.IsTriggered = false;
        _storage.UpdateAlert(item.Model);
    }

    private void OnAlertTriggered(Alert alert, double livePrice)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var item = Alerts.FirstOrDefault(a => a.Id == alert.Id);
            if (item != null) item.IsTriggered = true;

            if (Settings.SoundEnabled)
                _audio.PlayLoop(Settings.SoundFile);

            var popup = new Views.AlertPopupWindow(alert, livePrice, Settings.PopupAlwaysOnTop);
            popup.Acknowledged += () =>
            {
                _audio.Stop();
                ResetAlert(item);
            };
            popup.Snoozed += () =>
            {
                _audio.Stop();
                // Re-enable after 5 min
                Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ =>
                    Application.Current.Dispatcher.Invoke(() => ResetAlert(item)));
            };
            popup.Show();
        });
    }

    private void OnPriceUpdated(string uiKey, string price)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var item in Alerts.Where(a => a.UiKey == uiKey))
                item.LivePrice = price;
        });
    }

    private void OpenSettings()
    {
        var win = new Views.SettingsWindow(Settings);
        if (win.ShowDialog() == true)
        {
            _storage.SaveSettings(Settings);
            ApplySettings();
        }
    }

    private void ApplySettings()
    {
        _audio.Volume = Settings.Volume;
        _prices.Configure(Settings.GoldApiKey, Settings.OilApiKey);
        _engine.Start(Settings.CheckIntervalSeconds);
        AutoStartService.Set(Settings.AutoStartWithWindows);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
