using PriceTrackerAlert.Services;
using PriceTrackerAlert.ViewModels;
using PriceTrackerAlert.Views;
using System.Threading;
using System.Windows;

namespace PriceTrackerAlert;

public partial class App : Application
{
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single-instance check — if already running, bring existing window to front and exit
        _mutex = new Mutex(true, "PriceTrackerAlert_SingleInstance", out bool isNewInstance);
        if (!isNewInstance)
        {
            // Signal the running instance to show itself via a named event
            var showEvent = EventWaitHandle.OpenExisting("PriceTrackerAlert_ShowWindow");
            showEvent.Set();
            Current.Shutdown();
            return;
        }

        // Create a named event this instance listens to
        var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "PriceTrackerAlert_ShowWindow");
        var thread = new Thread(() =>
        {
            while (true)
            {
                waitHandle.WaitOne();
                Current.Dispatcher.Invoke(() =>
                {
                    var win = Current.MainWindow;
                    if (win != null)
                    {
                        win.Show();
                        win.WindowState = WindowState.Normal;
                        win.Activate();
                    }
                });
            }
        }) { IsBackground = true };
        thread.Start();

        base.OnStartup(e);

        var storage = new StorageService();
        var prices  = new PriceService();
        var audio   = new AudioService();
        var engine  = new AlertEngine(prices, storage);
        var updater = new UpdateService();
        var vm      = new MainViewModel(storage, prices, engine, audio, updater);

        var window = new MainWindow(vm);
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
