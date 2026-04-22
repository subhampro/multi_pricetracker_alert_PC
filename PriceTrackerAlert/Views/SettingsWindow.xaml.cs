using Microsoft.Win32;
using PriceTrackerAlert.Models;
using PriceTrackerAlert.Services;
using System.IO;
using System.Windows;

namespace PriceTrackerAlert.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private const string DefaultLabel = "alert.mp3  (default)";

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        var intervals = new[] { "1", "5", "10", "30" };
        IntervalCombo.SelectedIndex = Array.IndexOf(intervals, settings.CheckIntervalSeconds.ToString());
        if (IntervalCombo.SelectedIndex < 0) IntervalCombo.SelectedIndex = 1;

        VolumeSlider.Value       = settings.Volume;
        SoundCheck.IsChecked     = settings.SoundEnabled;
        TopCheck.IsChecked       = settings.PopupAlwaysOnTop;
        AutoStartCheck.IsChecked = settings.AutoStartWithWindows;
        GoldKeyBox.Text          = settings.GoldApiKey;
        OilKeyBox.Text           = settings.OilApiKey;

        // Show friendly name for default, full path for custom
        SoundFileBox.Text = settings.SoundFile is "default" or "default_mp3"
            ? DefaultLabel
            : settings.SoundFile;
    }

    private void BrowseSound_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select Alert Sound",
            Filter = "Audio files|*.mp3;*.wav|MP3|*.mp3|WAV|*.wav|All files|*.*"
        };
        if (dlg.ShowDialog() == true)
            SoundFileBox.Text = dlg.FileName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var intervals = new[] { 1, 5, 10, 30 };
        _settings.CheckIntervalSeconds = intervals[Math.Max(0, IntervalCombo.SelectedIndex)];
        _settings.Volume               = VolumeSlider.Value;
        _settings.SoundEnabled         = SoundCheck.IsChecked == true;
        _settings.PopupAlwaysOnTop     = TopCheck.IsChecked == true;
        _settings.AutoStartWithWindows = AutoStartCheck.IsChecked == true;
        _settings.GoldApiKey           = GoldKeyBox.Text.Trim();
        _settings.OilApiKey            = OilKeyBox.Text.Trim();

        // Save default token or validated custom path
        _settings.SoundFile = SoundFileBox.Text == DefaultLabel
            ? "default_mp3"
            : File.Exists(SoundFileBox.Text) ? SoundFileBox.Text : "default_mp3";

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
