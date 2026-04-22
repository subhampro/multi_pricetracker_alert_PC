using PriceTrackerAlert.Models;
using System.Windows;

namespace PriceTrackerAlert.Views;

public partial class AlertPopupWindow : Window
{
    public event Action? Acknowledged;
    public event Action? Snoozed;

    public AlertPopupWindow(Alert alert, double livePrice, bool alwaysOnTop)
    {
        InitializeComponent();
        Topmost = alwaysOnTop;

        SymbolText.Text    = alert.Symbol;
        ConditionText.Text = $"Target: {(alert.Condition == AlertCondition.Above ? "▲ Above" : "▼ Below")} {alert.TargetPrice:N2}";
        LivePriceText.Text = $"Live: {livePrice:N2}";
        NoteText.Text      = string.IsNullOrWhiteSpace(alert.Note) ? "" : $"📝 {alert.Note}";
    }

    private void AckButton_Click(object sender, RoutedEventArgs e)
    {
        Acknowledged?.Invoke();
        Close();
    }

    private void SnoozeButton_Click(object sender, RoutedEventArgs e)
    {
        Snoozed?.Invoke();
        Close();
    }
}
