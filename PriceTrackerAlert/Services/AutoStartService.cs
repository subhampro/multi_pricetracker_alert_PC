using Microsoft.Win32;

namespace PriceTrackerAlert.Services;

public static class AutoStartService
{
    private const string Key = "PriceTrackerAlert";
    private const string RegPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static void Set(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, true)!;
        if (enable)
            key.SetValue(Key, $"\"{Environment.ProcessPath}\"");
        else
            key.DeleteValue(Key, false);
    }

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath);
        return key?.GetValue(Key) != null;
    }
}
