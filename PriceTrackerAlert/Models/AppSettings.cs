namespace PriceTrackerAlert.Models;

public class AppSettings
{
    public int CheckIntervalSeconds { get; set; } = 5;
    public bool SoundEnabled { get; set; } = true;
    public string SoundFile { get; set; } = "default_mp3";
    public bool PopupAlwaysOnTop { get; set; } = true;
    public bool AutoStartWithWindows { get; set; } = false;
    public string Theme { get; set; } = "Dark";
    public double Volume { get; set; } = 1.0;
    public string GoldApiKey { get; set; } = "";
    public string OilApiKey { get; set; } = "";
}
