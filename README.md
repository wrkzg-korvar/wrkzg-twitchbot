<div align="center">

# 🎮 Wrkzg

**A self-hosted, local Twitch community bot with a built-in dashboard.**  
Built with C# .NET 10 · Runs on Windows & macOS · Open Source

---

[![Build](https://img.shields.io/github/actions/workflow/status/wrkzg-korvar/wrkzg-twitchbot/ci.yml?branch=main&style=flat-square&label=build)](https://github.com/wrkzg-korvar/wrkzg-twitchbot/actions)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple?style=flat-square)](https://dotnet.microsoft.com)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS-lightgrey?style=flat-square)](#installation)
[![Status](https://img.shields.io/badge/status-early%20development-orange?style=flat-square)](#roadmap)

---

> ⚠️ **This project is in early development.** Core features are not yet implemented.  
> Watch or star the repository to follow progress.

---

<!-- Screenshot placeholder – replace once dashboard UI exists -->
<!--
<img src=".github/assets/dashboard-preview.png" alt="Wrkzg Dashboard Preview" width="860" />
-->

</div>

## What is Wrkzg?

Wrkzg is a **locally-run Twitch community bot** inspired by various existing Bots. 
It runs directly on the streamer's machine and provides a built-in browser-based dashboard for full control over your community without relying on third-party cloud services.

Everything stays on your machine. No subscriptions, no data sent to external servers, no ads.

---

## Features

> Features marked 🚧 are planned but not yet implemented.

| Feature | Status |
|---|---|
| **Custom Bot Name** — use your own Twitch account as the bot | 🚧 Planned |
| **Custom Commands** — `!discord`, `!socials`, variables like `{user}`, `{uptime}` | 🚧 Planned |
| **User Management** — track watch time, points, message count, subscriber status | 🚧 Planned |
| **Points System** — automatic point rewards per minute while stream is live | 🚧 Planned |
| **Chat Games** — Heist, Duel, Slots, Roulette, Trivia | 🚧 Planned |
| **Raffles & Giveaways** — weighted ticket system, subscriber bonuses | 🚧 Planned |
| **Votes & Polls** — chat-based or native Twitch polls via Helix API | 🚧 Planned |
| **Dashboard** — live chat feed, analytics, user management, all in one UI | 🚧 Planned |
| **Automatic Updates** — checks GitHub Releases and updates in the background | 🚧 Planned |

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
| Frontend | React · TypeScript · Vite · Tailwind CSS |
| Database | SQLite · Entity Framework Core 10 |
| Twitch | TwitchLib · Twitch Helix API · EventSub WebSocket |

---

## Installation

> ⚠️ No installer is available yet. See [Building from Source](#building-from-source) below.

### System Requirements

| | Windows | macOS |
|---|---|---|
| OS Version | Windows 10 / 11 | macOS 12+ |
| Architecture | x64 | x64 · Apple Silicon (ARM64) |
| .NET Runtime | Bundled (self-contained) | Bundled (self-contained) |

### Building from Source

**Prerequisites:**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22 LTS](https://nodejs.org/)
- Git

```bash
# 1. Clone the repository
git clone https://github.com/wrkzg-korvar/wrkzg-twitchbot.git
cd wrkzg-twitchbot

# 2. Install frontend dependencies
cd src/Wrkzg.Frontend
npm install
cd ../..

# 3. Restore .NET dependencies
dotnet restore

# 4. Build
dotnet build

# 5. Run
dotnet run --project src/Wrkzg.Host
```

### Twitch Application Setup

Wrkzg requires a Twitch Developer Application for OAuth authentication:

1. Go to [dev.twitch.tv/console](https://dev.twitch.tv/console) and log in
2. Click **Register Your Application**
3. Set the redirect URI to `http://localhost:5000/auth/callback`
4. Category: **Chat Bot**
5. Copy the **Client ID** into `src/Wrkzg.Api/appsettings.Development.json`:

```json
{
  "Twitch": {
    "ClientId": "your_client_id_here"
  }
}
```

> ⚠️ Never commit `appsettings.Development.json` — it is listed in `.gitignore`.

---

## Contributing

Contributions are very welcome! Wrkzg is open source and built in the open.

Please read [CONTRIBUTING.md](docs/CONTRIBUTING.md) before opening a pull request. It covers:
- How to set up the development environment
- Project structure and architecture rules
- Coding conventions and commit message format
- How to add a new chat game

For questions or ideas, open a [GitHub Discussion](https://github.com/wrkzg-korvar/wrkzg-twitchbot/discussions).

---

## Roadmap

### v1.0.0 — MVP
- [ ] Twitch OAuth (bot account + broadcaster account)
- [ ] IRC connection + custom commands
- [ ] User tracking (watch time, points, message count)
- [ ] Points system (automatic rewards)
- [ ] Dashboard (overview, user management, command editor, logs)
- [ ] Automatic updater

### v1.1.0 — Community Features
- [ ] Chat games (Heist, Duel, Slots, Trivia, Roulette)
- [ ] Raffles & Giveaways
- [ ] Votes & Polls
- [ ] Analytics & Charts

### Future
- [ ] OBS overlay browser sources
- [ ] Song request queue
- [ ] Linux support

---

## License

Wrkzg is licensed under the [MIT License](LICENSE). © 2026 wrkzg.io
