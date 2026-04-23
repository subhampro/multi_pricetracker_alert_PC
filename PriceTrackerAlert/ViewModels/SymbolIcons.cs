namespace PriceTrackerAlert.ViewModels;

public static class SymbolIcons
{
    // Icon letter/symbol shown inside the colored circle
    private static readonly Dictionary<string, string> Icons = new()
    {
        ["BTCUSDT"] = "₿",
        ["ETHUSDT"] = "Ξ",
        ["BNBUSDT"] = "B",
        ["SOLUSDT"] = "◎",
        ["XRPUSDT"] = "✕",
        ["XAUUSD"]  = "Au",
        ["XAGUSD"]  = "Ag",
        ["USOIL"]   = "🛢",
    };

    // Colors matching TradingView's brand colors for each symbol
    private static readonly Dictionary<string, string> Colors = new()
    {
        ["BTCUSDT"] = "#F7931A",   // Bitcoin orange
        ["ETHUSDT"] = "#627EEA",   // Ethereum purple-blue
        ["BNBUSDT"] = "#F3BA2F",   // BNB yellow
        ["SOLUSDT"] = "#9945FF",   // Solana purple
        ["XRPUSDT"] = "#346AA9",   // XRP blue
        ["XAUUSD"]  = "#D4AF37",   // Gold
        ["XAGUSD"]  = "#A8A9AD",   // Silver
        ["USOIL"]   = "#2C5F2E",   // Oil dark green
    };

    public static string GetIcon(string symbol) =>
        Icons.TryGetValue(symbol.ToUpper(), out var i) ? i : symbol[..1].ToUpper();

    public static string GetColor(string symbol) =>
        Colors.TryGetValue(symbol.ToUpper(), out var c) ? c : "#546E7A";
}
