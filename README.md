<div align="center">

# 🎮 Wrkzg

**A self-hosted, local Twitch community bot with a built-in dashboard.**  
Built with C# .NET 10 · Runs on Windows & macOS · Open Source

---

[![Build](https://img.shields.io/github/actions/workflow/status/wrkzg-korvar/wrkzg-twitchbot/ci.yml?branch=main&style=flat-square&label=build)](https://github.com/wrkzg-korvar/wrkzg-twitchbot/actions)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple?style=flat-square)](https://dotnet.microsoft.com)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS-lightgrey?style=flat-square)](#installation)
[![Status](https://img.shields.io/badge/status-v1.0.1-green?style=flat-square)](#roadmap)

---

> ⚠️ **This project is in early development.** Core features are functional but the UI is being refined.  
> Watch or star the repository to follow progress.

</div>

## What is Wrkzg?

Wrkzg is a **locally-run Twitch community bot** inspired by various existing bots.
It runs directly on the streamer's machine and provides a built-in browser-based dashboard for full control over your community without relying on third-party cloud services.

Everything stays on your machine. No subscriptions, no data sent to external servers, no ads.

---

## Features

| Feature | Status |
|---|---|
| **Setup Wizard** — guided first-time setup with Twitch app registration | ✅ Implemented |
| **Twitch OAuth** — bot account + broadcaster account, tokens encrypted in OS keychain | ✅ Implemented |
| **IRC Connection** — auto-connect on startup, auto token refresh, reconnect on disconnect | ✅ Implemented |
| **Custom Commands** — `!discord`, `!socials`, variables like `{user}`, `{points}`, `{random:1:6}` | ✅ Implemented |
| **System Commands** — built-in `!commands`, `!points`, `!watchtime`, `!followage` | ✅ Implemented |
| **Dashboard** — live chat feed, bot status, viewer count, command management | ✅ Implemented |
| **User Tracking** — message count, watch time, points, mod/sub/broadcaster status sync | ✅ Implemented |
| **Points System** — automatic point rewards per minute while stream is live, sub multiplier | ✅ Implemented |
| **Custom Title Bar** — OS-native window controls (macOS traffic lights / Windows buttons) | ✅ Implemented |
| **Design System** — brand colors from logo, Light/Dark theme toggle, CSS custom properties | ✅ Implemented |
| **Custom Bot Name** — use your own Twitch account as the bot | ✅ Implemented |
| **Chat Games** — Heist, Duel, Slots, Roulette, Trivia | 🚧 v1.1 Planned |
| **Raffles & Giveaways** — weighted ticket system, subscriber bonuses | 🚧 v1.1 Planned |
| **Votes & Polls** — chat-based or native Twitch polls via Helix API | 🚧 v1.1 Planned |
| **Automatic Updates** — checks GitHub Releases and updates in the background | 🚧 v1.0.1 Planned |

---

## Architecture at a Glance

Wrkzg runs as a single desktop process that embeds both a **Kestrel HTTP server** and a **Photino browser window**. The dashboard is a React SPA served locally — no external hosting needed.

```
Photino Window (Chromium / WebKit)
        │
        ▼
ASP.NET Core Kestrel  ←→  SignalR (real-time events)
        │
   ┌────┴────┐
 Core     Infrastructure
(Logic)  (DB · Twitch API)
        │
      SQLite
```

For a full breakdown, see [ARCHITECTURE.md](_docs/ARCHITECTURE.md).

---

## Tech Stack

| Layer | Technology |
|---|---|
| Language & Runtime | C# · .NET 10 |
| Desktop Host | [Photino.NET](https://tryphotino.io/) |
| Backend / API | ASP.NET Core · Kestrel · SignalR |
| Frontend | React 19 · TypeScript · Vite · Tailwind CSS v4 |
| Database | SQLite · Entity Framework Core 10 |
| Twitch | TwitchLib · Twitch Helix API · EventSub WebSocket |

---

## Installation

Download the latest release for your platform from the [Releases page](https://github.com/wrkzg-korvar/wrkzg-twitchbot/releases).

### Windows

1. Download and extract the `.zip` file
2. Run `Wrkzg.exe`
3. Windows SmartScreen may show a warning — click **"More info"** → **"Run anyway"**

> **Tip:** Right-click the ZIP before extracting → Properties → check **"Unblock"** → OK. This removes the warning for all files inside.

### macOS

1. Download and extract the `.zip` file
2. Double-click `Wrkzg.app` — macOS will block it. Click **"Done"**
3. Open **System Settings → Privacy & Security**
4. Scroll to the bottom — click **"Open Anyway"** next to the Wrkzg message
5. Enter your password — the app launches and is remembered for future starts

**Alternative (Terminal):**
```bash
xattr -cr ~/Downloads/Wrkzg.app
open ~/Downloads/Wrkzg.app
```

> **Note:** Wrkzg is not signed with an Apple Developer Certificate. Since macOS 15 (Sequoia), the right-click → Open workaround no longer works. The System Settings method above is required on macOS 15+ and macOS 26 (Tahoe).

### Prerequisites

| | Windows | macOS |
|---|---|---|
| OS Version | Windows 10 / 11 | macOS 12+ |
| Architecture | x64 | x64 · Apple Silicon (ARM64) |
| .NET Runtime | Bundled (self-contained) | Bundled (self-contained) |
| WebView | WebView2 Runtime (pre-installed with Edge) | WebKit (built-in) |

### Building from Source

**Prerequisites:**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22 LTS](https://nodejs.org/)
- Git

```bash
# 1. Clone the repository
git clone https://github.com/wrkzg-korvar/wrkzg-twitchbot.git
cd wrkzg-twitchbot

# 2. Install frontend dependencies and build
cd src/Wrkzg.Frontend
npm install
npm run build
cd ../..

# 3. Restore .NET dependencies
dotnet restore

# 4. Build
dotnet build

# 5. Run
dotnet run --project src/Wrkzg.Host
```

### First-Time Setup

When you start Wrkzg for the first time, a **Setup Wizard** guides you through the configuration:

1. **Create a Twitch App** — The wizard links you directly to the Twitch Developer Console and provides copy-paste values for the app name and redirect URI
2. **Enter Credentials** — Paste your Client ID and Client Secret (stored encrypted in your OS keychain, never in config files)
3. **Connect Bot Account** — Opens your system browser for Twitch OAuth authorization
4. **Connect Broadcaster Account** — Same flow with your main streamer account
5. **Set Channel** — Enter your channel name and you're ready to go

No manual config file editing required.

### Development Setup (Contributors)

Contributors can optionally use `appsettings.Development.json` for local development instead of the Setup Wizard:

```json
{
  "Twitch": {
    "ClientId": "your_client_id_here",
    "ClientSecret": "your_client_secret_here"
  },
  "Bot": {
    "Port": 5000
  }
}
```

> ⚠️ Never commit `appsettings.Development.json` — it is listed in `.gitignore`.

---

## Contributing

Contributions are very welcome! Wrkzg is open source and built in the open.

Please read [CONTRIBUTING.md](_docs/CONTRIBUTING.md) before opening a pull request. It covers:
- How to set up the development environment
- Project structure and architecture rules
- Coding conventions and commit message format
- How to add a new chat game or API endpoint

For questions or ideas, open a [GitHub Discussion](https://github.com/wrkzg-korvar/wrkzg-twitchbot/discussions).

---

## Roadmap

### v1.0.0 — MVP ✅
- [x] Twitch OAuth (bot account + broadcaster account)
- [x] IRC connection + custom commands
- [x] System commands (!commands, !points, !watchtime, !followage)
- [x] User tracking (messages, watch time, points, mod/sub/broadcaster sync)
- [x] Points system (automatic rewards per minute while live, sub multiplier)
- [x] Dashboard (live chat, commands CRUD, user table, settings)
- [x] Setup Wizard for first-time users
- [x] Custom title bar with OS-native window controls
- [x] Design system with Light/Dark theme support

### v1.0.1 — Polish
- [ ] Automatic updater (GitHub Releases check + download + install)
- [ ] Command aliases support in UI
- [ ] Command edit modal (inline editing)

### v1.1.0 — Community Features
- [ ] Chat games (Heist, Duel, Slots, Trivia, Roulette)
- [ ] Raffles & Giveaways
- [ ] Votes & Polls
- [ ] Analytics & Charts

### Future
- [ ] OBS overlay browser sources
- [ ] Song request queue
- [ ] EventSub integration (follows, subs, raids in real-time)
- [ ] Linux support

---

## License

Wrkzg is licensed under the [MIT License](LICENSE). © 2026 wrkzg.io
