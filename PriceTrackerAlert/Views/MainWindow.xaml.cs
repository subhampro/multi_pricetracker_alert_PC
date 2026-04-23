using Hardcodet.Wpf.TaskbarNotification;
using PriceTrackerAlert.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PriceTrackerAlert.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly TaskbarIcon _trayIcon;
    private bool _forceClose = false;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        _trayIcon = new TaskbarIcon
        {
            IconSource = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Assets/icon.ico")),
            ToolTipText = "Price Tracker Alert — Running"
        };
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowWindow();

        var menu = new ContextMenu();
        var openItem = new MenuItem { Header = "📈  Open Price Tracker" };
        openItem.Click += (_, _) => ShowWindow();
        var exitItem = new MenuItem { Header = "✖  Exit" };
        exitItem.Click += (_, _) => ForceExit();
        menu.Items.Add(openItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);
        _trayIcon.ContextMenu = menu;
    }

    private void ToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is AlertItem item)
            _vm.ToggleAlertCommand.Execute(item);
    }

    private void ResetBtn_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is AlertItem item)
            _vm.ResetAlertCommand.Execute(item);
    }

    private void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is AlertItem item)
            _vm.DeleteAlertCommand.Execute(item);
    }

    // Minimize button → hide to tray silently
    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            Hide();
    }

    // X button → hide to tray (keep running in background)
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_forceClose)
        {
            e.Cancel = true;
            Hide();
            _trayIcon.ShowBalloonTip(
                "Still Running",
                "Price Tracker Alert is monitoring prices in the background.\nRight-click the tray icon to exit.",
                BalloonIcon.Info);
        }
        else
        {
            _trayIcon.Dispose();
        }
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    private void ForceExit()
    {
        _forceClose = true;
        _trayIcon.Dispose();
        Application.Current.Shutdown();
    }
}
