using PriceTrackerAlert.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PriceTrackerAlert.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is true ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))
                  : new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class ConditionToStringConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is AlertCondition.Above ? "▲ Above" : "▼ Below";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is bool b && !b;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => v is bool b && !b;
}

public class ConditionIndexConverter : IValueConverter
{
    public static readonly ConditionIndexConverter Instance = new();
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is AlertCondition cond ? (int)cond : 0;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        v is int i ? (AlertCondition)i : AlertCondition.Above;
}

public class PriceSourceToStringConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is PriceSource.TradingView ? "📺 TradingView (+$18)" : "🔶 Binance";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
