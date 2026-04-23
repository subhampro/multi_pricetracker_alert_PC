using PriceTrackerAlert.Services;
using PriceTrackerAlert.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PriceTrackerAlert.Views;

public partial class SymbolIconControl : UserControl
{
    public static readonly DependencyProperty SymbolProperty =
        DependencyProperty.Register(nameof(Symbol), typeof(string), typeof(SymbolIconControl),
            new PropertyMetadata("", OnSymbolChanged));

    public string Symbol
    {
        get => (string)GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    public SymbolIconControl() => InitializeComponent();

    private static void OnSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SymbolIconControl ctrl)
            ctrl.LoadIcon((string)e.NewValue);
    }

    private void LoadIcon(string symbol)
    {
        if (string.IsNullOrEmpty(symbol)) return;

        // Set colored background and fallback text immediately
        var colorHex = SymbolIcons.GetColor(symbol);
        var color    = (Color)ColorConverter.ConvertFromString(colorHex);
        BgCircle.Fill    = new SolidColorBrush(color);
        FallbackText.Text = SymbolIcons.GetIcon(symbol);
        SvgImage.Source  = null;

        // Load SVG async
        _ = LoadSvgAsync(symbol);
    }

    private async System.Threading.Tasks.Task LoadSvgAsync(string symbol)
    {
        var drawing = await SymbolIconService.GetIconAsync(symbol);
        if (drawing == null) return;

        await Dispatcher.InvokeAsync(() =>
        {
            try
            {
                var img = new DrawingImage(drawing);
                img.Freeze();
                SvgImage.Source   = img;
                FallbackText.Text = "";  // hide letter fallback
                // Keep BgCircle colored — TradingView icons have transparent bg
            }
            catch { }
        });
    }
}
