# 📈 Price Tracker Alert

A Windows desktop app that monitors live prices for crypto, gold, and oil — and fires a loud popup alert with looping sound the moment your target price is hit.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

---

## ✨ Features

| Feature | Details |
|---|---|
| 🔔 Price Alerts | Set Above / Below triggers for any symbol |
| 📊 Live Prices | Real-time polling via Binance, Metals-API, Alpha Vantage |
| 🔊 Looping Sound | Alert sound loops until you acknowledge |
| 🪟 Popup Window | Big visible popup with Acknowledge + Snooze 5 min |
| 💾 Persistent Storage | SQLite — alerts survive app restarts |
| 🖥 System Tray | Minimize to tray, runs silently in background |
| ⚙ Settings | Interval, volume, sound file, auto-start, API keys |
| 🧪 Test Mode | Simulate alerts without live API |

---

## 📦 Supported Symbols

| Symbol | Market | API Used |
|---|---|---|
| `BTCUSDT` | Bitcoin / USD | Binance (free, no key) |
| `ETHUSDT` | Ethereum / USD | Binance (free, no key) |
| `BNBUSDT` | BNB / USD | Binance (free, no key) |
| `SOLUSDT` | Solana / USD | Binance (free, no key) |
| `XRPUSDT` | XRP / USD | Binance (free, no key) |
| `XAUUSD` | Gold / USD | [metals-api.com](https://metals-api.com) (free tier) |
| `XAGUSD` | Silver / USD | [metals-api.com](https://metals-api.com) (free tier) |
| `USOIL` | WTI Crude Oil | [alphavantage.co](https://www.alphavantage.co) (free tier) |

---

## 🚀 Quick Start

### Option A — Download Release (recommended)
1. Go to [Releases](../../releases) and download `PriceTrackerAlert.exe`
2. Run it — no install needed, single `.exe`

### Option B — Build from source
```
Requirements: .NET 8 SDK  →  https://dotnet.microsoft.com/download/dotnet/8.0
```
```bash
git clone https://github.com/YOUR_USERNAME/multi_pricetracker_alert_PC.git
cd multi_pricetracker_alert_PC/PriceTrackerAlert
dotnet restore --source https://api.nuget.org/v3/index.json
dotnet build
dotnet run
```

---

## ⚙ API Keys Setup

Crypto (Binance) works with **no API key**.

For Gold and Oil, get free keys:

| Provider | Sign up | Used for |
|---|---|---|
| [metals-api.com](https://metals-api.com/register) | Free tier: 50 req/month | XAUUSD, XAGUSD |
| [alphavantage.co](https://www.alphavantage.co/support/#api-key) | Free: 25 req/day | USOIL / WTI |

Then open **⚙ Settings** in the app and paste your keys.

---

## 🧪 Test Mode

Enable **Test Mode** in the top bar to simulate alerts without any API calls.

Default test prices:
- `BTCUSDT` = 100,000
- `ETHUSDT` = 3,500
- `XAUUSD` = 2,400
- `USOIL` = 85

Add an alert below those values to immediately trigger a popup and hear the sound.

---

## 🏗 Architecture

```
PriceTrackerAlert/
├── Models/
│   ├── Alert.cs            # Alert rule model + AlertCondition enum
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
│   ├── PriceService.cs     # Binance / Metals-API / Alpha Vantage
│   ├── AlertEngine.cs      # Background polling + trigger detection
│   ├── StorageService.cs   # SQLite persistence
│   ├── AudioService.cs     # NAudio looping playback
│   └── AutoStartService.cs # Windows registry autostart
├── Converters/
│   └── Converters.cs       # WPF value converters
└── Assets/
    ├── alert.wav           # Default alert sound (880Hz sine)
    └── icon.ico            # App icon
```

**Stack:** C# · WPF · MVVM · .NET 8 · SQLite · NAudio · Hardcodet.NotifyIcon

---

## 📋 Alert Logic

- Each alert has: **Symbol**, **Target Price**, **Condition** (Above/Below), **Note**
- The engine polls every N seconds (configurable)
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
  -o ./publish \
  --source https://api.nuget.org/v3/index.json
```

Output: `publish/PriceTrackerAlert.exe` — single file, no .NET install required on target machine.

---

## 📄 License

MIT — free to use, modify, and distribute.
