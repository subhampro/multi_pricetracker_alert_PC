# 📈 Price Tracker Alert

A Windows desktop app that monitors live prices for crypto, gold, and oil — and fires a loud popup alert with looping sound the moment your target price is hit.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)
![Version](https://img.shields.io/badge/version-1.6.0-brightgreen)

---

## ✨ Features

| Feature | Details |
|---|---|
| 🔔 Price Alerts | Set Above / Below triggers for any symbol |
| 📊 Live Prices | Real-time polling — updates every second |
| 🔊 Looping Sound | Alert sound loops until you acknowledge |
| 🪟 Popup Window | Big visible popup with Acknowledge + Snooze 5 min |
| 💾 Persistent Storage | SQLite — alerts survive app restarts |
| 🖥 System Tray | Minimize to tray, runs silently in background |
| ⚙ Settings | Interval, volume, sound file, auto-start |
| 🧪 Test Mode | Simulate alerts without live API |
| 🔄 Auto-Update | Checks GitHub for updates, installs with one click |
| 📺 TradingView Mode | BTCUSDT with TradingView price offset |

---

## 📦 Supported Symbols

| Symbol | Market | API Used | Key Required |
|---|---|---|---|
| `BTCUSDT` | Bitcoin / USD | Binance | ❌ Free |
| `ETHUSDT` | Ethereum / USD | Binance | ❌ Free |
| `BNBUSDT` | BNB / USD | Binance | ❌ Free |
| `SOLUSDT` | Solana / USD | Binance | ❌ Free |
| `XRPUSDT` | XRP / USD | Binance | ❌ Free |
| `XAUUSD` | Gold / USD | [Frankfurter.app](https://frankfurter.app) | ❌ Free |
| `XAGUSD` | Silver / USD | [Frankfurter.app](https://frankfurter.app) | ❌ Free |
| `USOIL` | WTI Crude Oil | [EIA.gov](https://www.eia.gov/opendata/) | ❌ Free |

> **All APIs are completely free — no account or API key required.**

---

## 🚀 Quick Start

### Option A — Download Release (recommended)
1. Go to [Releases](../../releases) and download `PriceTrackerAlert.exe`
2. Run it — no install needed, single `.exe`
3. Future updates happen automatically inside the app

### Option B — Build from source
```
Requirements: .NET 8 SDK  →  https://dotnet.microsoft.com/download/dotnet/8.0
```
```bash
git clone https://github.com/subhampro/multi_pricetracker_alert_PC.git
cd multi_pricetracker_alert_PC

# Generate assets (icon + sound)
dotnet run --project GenWav/GenWav.csproj

# Build and run
cd PriceTrackerAlert
dotnet run --source https://api.nuget.org/v3/index.json
```

---

## 🧪 Test Mode

Enable **Test Mode** in the top bar to simulate alerts without any API calls.

Default test prices:
- `BTCUSDT` = 100,000
- `ETHUSDT` = 3,500
- `XAUUSD` = 2,400
- `USOIL` = 85

---

## 📺 TradingView Mode

For `BTCUSDT`, you can select **TradingView** as the source in the Source dropdown.
This fetches the Binance price and applies an offset to match TradingView's displayed price.

---

## 🏗 Architecture

```
PriceTrackerAlert/
├── Models/
│   ├── Alert.cs            # Alert rule model + AlertCondition/PriceSource enums
│   └── AppSettings.cs      # Settings model
├── ViewModels/
│   ├── MainViewModel.cs    # Main dashboard logic
│   ├── AlertItem.cs        # Observable wrapper for Alert
│   └── RelayCommand.cs     # ICommand implementations
├── Views/
│   ├── MainWindow.xaml     # Dashboard UI
│   ├── AlertPopupWindow.xaml  # Popup alert window
│   └── SettingsWindow.xaml    # Settings dialog
├── Services/
│   ├── PriceService.cs     # Binance / Frankfurter / EIA
│   ├── AlertEngine.cs      # Background polling + trigger detection
│   ├── StorageService.cs   # SQLite persistence
│   ├── AudioService.cs     # NAudio looping playback
│   ├── AutoStartService.cs # Windows registry autostart
│   └── UpdateService.cs    # GitHub auto-update
├── Converters/
│   └── Converters.cs       # WPF value converters
└── Assets/
    ├── alert.mp3           # Default alert sound
    ├── alert.wav           # Fallback alert sound
    └── icon.ico            # App icon
```

**Stack:** C# · WPF · MVVM · .NET 8 · SQLite · NAudio · Hardcodet.NotifyIcon

---

## 📋 Alert Logic

- Each alert has: **Symbol**, **Target Price**, **Condition** (Above/Below), **Source**, **Note**
- The engine polls every N seconds (configurable, default 1s)
- When `price >= target` (Above) or `price <= target` (Below) → alert fires **once**
- After acknowledging, click 🔄 Reset to re-arm the alert
- Snooze re-arms automatically after 5 minutes

---

## 🔨 Build for Release

```bash
dotnet publish PriceTrackerAlert/PriceTrackerAlert.csproj \
  -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=none \
  -o ./publish \
  --source https://api.nuget.org/v3/index.json
```

Output: `publish/PriceTrackerAlert.exe` — single file, no .NET install required.

---

## 📄 License

MIT — free to use, modify, and distribute.
