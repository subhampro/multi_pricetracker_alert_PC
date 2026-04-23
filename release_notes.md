## v1.6.7 - Symbol Icons Fixed

### 🐛 Root Cause Fixed
- Symbol dropdown was showing "PriceTrackerAlert.ViewModels.MainViewModel" text and "P" icon
- Root cause: ComboBox used hardcoded ComboBoxItem elements, so {Binding} resolved to MainViewModel instead of the symbol string
- Fix: Symbol list now bound to AvailableSymbols collection — {Binding} correctly gets "BTCUSDT", "XAUUSD" etc.
- TradingView SVG icons now load correctly for all symbols
- Colored circle background kept behind SVG (TradingView icons have transparent backgrounds)

### 📦 Install
Single `.exe` — no install needed.
If you have v1.5.2+, the app will show the update button automatically.
