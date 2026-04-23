## v1.6.5 - Real TradingView Symbol Icons + Update Fix

### ✨ Real Symbol Icons
- Actual TradingView SVG icons now shown for every symbol — exactly like TradingView's watchlist
  - BTC → crypto/XTVCBTC (orange Bitcoin logo)
  - ETH → crypto/XTVCETH (Ethereum diamond)
  - BNB → crypto/XTVCBNB
  - SOL → crypto/XTVCSOL
  - XRP → crypto/XTVCXRP
  - Gold → metal/gold
  - Silver → metal/silver
  - Oil → crude-oil
- Icons downloaded from TradingView CDN on first launch and cached locally
- Colored circle fallback shown instantly while SVG loads
- Icons appear in both the symbol dropdown and the alert list

### 🔧 Auto-Update Fix
- Updater now waits for the exact process PID to exit before replacing the file
- New exe moves directly over old exe — no leftover `_old.exe` files
- Only `PriceTrackerAlert.exe` remains after update, nothing else

### 📦 Install
Single `.exe` — no install needed.
If you have v1.5.2+, the app will show the update button automatically.
