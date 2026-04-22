using Microsoft.Win32;
using PriceTrackerAlert.Models;
using System.Windows;

namespace PriceTrackerAlert.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        // Populate controls
        var intervals = new[] { "1", "5", "10", "30" };
        IntervalCombo.SelectedIndex = Array.IndexOf(intervals, settings.CheckIntervalSeconds.ToString());
        if (IntervalCombo.SelectedIndex < 0) IntervalCombo.SelectedIndex = 1;

        VolumeSlider.Value    = settings.Volume;
        SoundCheck.IsChecked  = settings.SoundEnabled;
        SoundFileBox.Text     = settings.SoundFile == "default" ? "(default)" : settings.SoundFile;
        TopCheck.IsChecked    = settings.PopupAlwaysOnTop;
        AutoStartCheck.IsChecked = settings.AutoStartWithWindows;
        GoldKeyBox.Text       = settings.GoldApiKey;
        OilKeyBox.Text        = settings.OilApiKey;
    }

    private void BrowseSound_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Audio files|*.wav;*.mp3|All files|*.*" };
        if (dlg.ShowDialog() == true)
            SoundFileBox.Text = dlg.FileName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var intervals = new[] { 1, 5, 10, 30 };
        _settings.CheckIntervalSeconds = intervals[Math.Max(0, IntervalCombo.SelectedIndex)];
        _settings.Volume               = VolumeSlider.Value;
        _settings.SoundEnabled         = SoundCheck.IsChecked == true;
        _settings.SoundFile            = SoundFileBox.Text == "(default)" ? "default" : SoundFileBox.Text;
        _settings.PopupAlwaysOnTop     = TopCheck.IsChecked == true;
        _settings.AutoStartWithWindows = AutoStartCheck.IsChecked == true;
        _settings.GoldApiKey           = GoldKeyBox.Text.Trim();
        _settings.OilApiKey            = OilKeyBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
