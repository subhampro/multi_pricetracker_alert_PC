using PriceTrackerAlert.Services;
using PriceTrackerAlert.ViewModels;
using PriceTrackerAlert.Views;
using System.Windows;

namespace PriceTrackerAlert;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var storage = new StorageService();
        var prices  = new PriceService();
        var audio   = new AudioService();
        var engine  = new AlertEngine(prices, storage);
        var vm      = new MainViewModel(storage, prices, engine, audio);

        var window = new MainWindow(vm);
        window.Show();
    }
}
