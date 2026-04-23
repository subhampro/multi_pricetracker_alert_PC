namespace PriceTrackerAlert.Models;

public class AppSettings
{
    public int    CheckIntervalSeconds  { get; set; } = 1;
    public bool   SoundEnabled          { get; set; } = true;
    public string SoundFile             { get; set; } = "default_mp3";
    public bool   PopupAlwaysOnTop      { get; set; } = true;
    public bool   AutoStartWithWindows  { get; set; } = false;
    public double Volume                { get; set; } = 1.0;
}
