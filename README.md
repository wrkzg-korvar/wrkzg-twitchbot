<div align="center">

# 🎮 Wrkzg

### The open-source, local-first Twitch bot with a modern dashboard.

**Your stream. Your data. Your machine.**

Wrkzg runs directly on your computer — no cloud services, no subscriptions, no data leaving your machine.
A full-featured Twitch bot with a built-in dashboard for chat commands, moderation, polls, raffles, points, and more.

[Download Latest Release](https://github.com/wrkzg-korvar/wrkzg-twitchbot/releases) · [Report Bug](https://github.com/wrkzg-korvar/wrkzg-twitchbot/issues) · [Request Feature](https://github.com/wrkzg-korvar/wrkzg-twitchbot/discussions)

---

[![Build](https://img.shields.io/github/actions/workflow/status/wrkzg-korvar/wrkzg-twitchbot/ci.yml?branch=main&style=flat-square&label=build)](https://github.com/wrkzg-korvar/wrkzg-twitchbot/actions)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple?style=flat-square)](https://dotnet.microsoft.com)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS-lightgrey?style=flat-square)](#installation)
[![Latest Release](https://img.shields.io/github/v/release/wrkzg-korvar/wrkzg-twitchbot?style=flat-square&color=green)](https://github.com/wrkzg-korvar/wrkzg-twitchbot/releases)
[![GitHub Stars](https://img.shields.io/github/stars/wrkzg-korvar/wrkzg-twitchbot?style=flat-square)](https://github.com/wrkzg-korvar/wrkzg-twitchbot/stargazers)

</div>

---

<div align="center">
    <img src="_docs/screenshots/dashboard-overview.png" alt="Wrkzg Dashboard" width="800">
    <br>
    <sub>Dashboard with live chat, bot status, and command management — shown in Dark & Light Mode</sub>
</div>

## Why Wrkzg?

Most Twitch bots either live in the cloud (your data on someone else's server) or are stuck on Windows only.
Wrkzg is different:

- **100% local** — Everything runs on your machine. No cloud, no external servers, no tracking.
- **Real desktop app** — Not a browser tab. A native window with a modern React dashboard built in.
- **Windows & macOS** — One of the only open-source Twitch bots that runs natively on both platforms.
- **Secure by default** — Your Twitch credentials are stored in your OS keychain (Windows DPAPI / macOS Keychain), never in config files.
- **Zero cost, forever** — Open source under MIT license. No subscriptions, no premium tiers, no ads.
- **Setup in minutes** — A built-in wizard walks you through everything. No config files, no terminal commands.

---

## How It Compares

> Choosing a Twitch bot? Here's how Wrkzg stacks up against the most popular alternatives.

| | **Wrkzg** | **Nightbot** | **Firebot** | **PhantomBot** | **Streamlabs Bot** |
|---|:---:|:---:|:---:|:---:|:---:|
| **Runs locally** | ✅ | ❌ Cloud | ✅ | ✅ | ❌ Cloud |
| **Desktop app** | ✅ Native | ❌ Browser | ✅ Electron | ❌ Web Panel | ❌ Browser |
| **Windows** | ✅ | ✅ Cloud | ✅ | ✅ | ✅ Cloud |
| **macOS** | ✅ | ✅ Cloud | ❌ | ✅ (via Java) | ❌ |
| **Modern dashboard** | ✅ React | ⚠️ Dated | ⚠️ Angular | ⚠️ Dated | ✅ |
| **Setup wizard** | ✅ | ✅ | ✅ | ❌ Manual | ✅ |
| **Secure credential storage** | ✅ OS Keychain | N/A | ❌ Config file | ❌ Config file | N/A |
| **Custom commands** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Points system** | ✅ | ❌ | ✅ | ✅ | ✅ |
| **Polls & raffles** | ✅ | ✅ Basic | ✅ | ✅ | ✅ |
| **Spam filter** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Timed messages** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Chat games** | ✅ 5 games | ❌ | ✅ | ✅ | ❌ |
| **Song requests** | ✅ YouTube | ❌ | ✅ | ✅ | ✅ |
| **Automation system** | ✅ Visual | ❌ | ✅ | ❌ | ❌ |
| **Discord integration** | ✅ Webhooks | ❌ | ❌ | ✅ | ❌ |
| **Stream analytics** | ✅ | ❌ | ✅ | ✅ | ❌ |
| **Overlay editor** | ✅ Visual | ❌ | ✅ | ❌ | ✅ |
| **Custom overlays (HTML/CSS/JS)** | ✅ Local | ✅ Cloud | ❌ | ❌ | ❌ |
| **Custom sounds/images** | ✅ Local | ✅ Cloud | ✅ | ❌ | ✅ Cloud |
| **OBS WebSocket** | ✅ Native | ❌ | ✅ | ❌ | ❌ |
| **Data import** | ✅ Deepbot/CSV | ❌ | ✅ | ✅ | ❌ |
| **Open source** | ✅ MIT | ❌ | ✅ GPL-3 | ✅ GPL-3 | ❌ |
| **No account required** | ✅ | ❌ | ✅ | ✅ | ❌ |
| **Price** | **Free** | Free (limited) | **Free** | **Free** | Freemium |

---

## Permissions & Requirements

### Twitch App Scopes

Wrkzg requests specific OAuth scopes for each connected account. These are required for the bot to function correctly.

**Bot Account:**

| Scope | Required For |
|---|---|
| `chat:read` | Reading chat messages |
| `chat:edit` | Sending chat messages via IRC |
| `user:write:chat` | Sending messages via Helix API |
| `moderator:manage:announcements` | Colored timer announcements |
| `moderator:manage:banned_users` | Spam filter timeouts |
| `user:read:emotes` | Loading all available emotes (subscribed channels, bits, follower) |

**Broadcaster Account:**

| Scope | Required For |
|---|---|
| `moderator:read:followers` | Follow event notifications |
| `channel:read:polls` | Reading poll status |
| `channel:manage:polls` | Creating and ending polls |
| `bits:read` | Bit event notifications |
| `channel:read:subscriptions` | Subscribe/gift event notifications |
| `moderator:manage:shoutouts` | Auto-shoutout on raids |
| `user:write:chat` | Sending messages as broadcaster |
| `channel:read:redemptions` | Reading channel point redemptions |
| `channel:manage:redemptions` | Managing channel point rewards |
| `channel:manage:broadcast` | `!title` and `!game` mod commands |
| `user:read:emotes` | Loading broadcaster's available emotes |

### Moderator Requirement

Some features require the **Bot Account** to be a **Moderator** in the Broadcaster's channel. Without mod status, these features will fail silently or return errors:

| Feature | Why Mod is Required |
|---|---|
| **Spam Filter** (timeouts) | `POST /helix/moderation/bans` requires the bot to be a moderator |
| **Timer Announcements** (colored) | `POST /helix/chat/announcements` requires moderator permissions |
| **Auto-Shoutout** (raids) | `POST /helix/chat/shoutouts` requires moderator permissions |

> **How to make the bot a moderator:** Open your Twitch chat and type `/mod wrkzg` (replace `wrkzg` with your bot's Twitch username).

---

## Features

### Feature Highlights

<div align="center">

<table>
    <tr>
        <td align="center" width="50%">
            <img src="_docs/screenshots/event-system.png" alt="Automations" width="100%">
            <br>
            <sub><b>Automations</b> — Visual Trigger → Conditions → Effects chains with Discord, Chat, Counters, and more</sub>
        </td>
            <td align="center" width="50%">
            <img src="_docs/screenshots/standard-overlay-editor-split-view.png" alt="Overlay Editor" width="100%">
            <br>
            <sub><b>Overlay Editor</b> — Live preview, 14 animations, Google Fonts, per-event customization</sub>
        </td>
    </tr>
    <tr>
        <td align="center" width="50%">
            <img src="_docs/screenshots/stream-analytics.png" alt="Stream Analytics" width="100%">
            <br>
            <sub><b>Stream Analytics</b> — Viewer trends, category tracking, session history</sub>
        </td>
        <td align="center" width="50%">
            <img src="_docs/screenshots/import-data-wizard.png" alt="Bot Data Import" width="100%">
            <br>
            <sub><b>Bot Data Import</b> — Migrate from Deepbot, Streamlabs Chatbot, or any CSV</sub>
        </td>
    </tr>
    <tr>
        <td align="center" width="50%">
            <img src="_docs/screenshots/chat-games-overview.png" alt="Chat Games" width="100%">
            <br>
            <sub><b>Chat Games</b> — Heist, Duel, Slots, Roulette, Trivia — all points-based</sub>
        </td>
            <td align="center" width="50%">
            <img src="_docs/screenshots/integrations.png" alt="Discord Integration" width="100%">
            <br>
            <sub><b>Discord Integration</b> — Webhooks for go-live alerts, raid notifications, and more</sub>
        </td>
    </tr>
</table>

<details>
<summary><b>More screenshots</b> — Automation Editor, Custom Overlays, Game Messages</summary>
<br>
<table>
    <tr>
        <td align="center" width="50%">
            <img src="_docs/screenshots/event-system-detail.png" alt="Automation Editor" width="100%">
            <br>
            <sub><b>Automation Editor</b> — Configure triggers, conditions, and effect chains</sub>
        </td>
        <td align="center" width="50%">
            <img src="_docs/screenshots/custom-overlay-editor-split-view.png" alt="Custom Overlays" width="100%">
            <br>
            <sub><b>Custom Overlays</b> — Write HTML/CSS/JS with full SignalR event access</sub>
        </td>
    </tr>
    <tr>
        <td align="center" width="50%">
            <img src="_docs/screenshots/chat-games-custom-messages.png" alt="Custom Game Messages" width="100%">
            <br>
            <sub><b>Custom Messages</b> — Every bot response is fully customizable with variables</sub>
        </td>
        <td align="center" width="50%">
        </td>
    </tr>
</table>
</details>

</div>

---

### ✅ Implemented

**Chat & Commands**
- **Custom Commands** — Create commands like `!discord`, `!socials` with variables: `{user}`, `{points}`, `{random:1:6}`
- **16 System Commands** — Built-in `!poll`, `!vote`, `!raffle`, `!join`, `!draw`, `!editcmd`, `!quote`, `!so`, `!uptime` and more — all with enable/disable toggle and custom response templates
- **Command Aliases** — Multiple triggers per command with badge display

**Community Engagement**
- **Points System** — Automatic rewards per minute while live, with subscriber multiplier
- **Polls & Votes** — `!poll` with live bar chart, countdown timer, and customizable templates
- **Raffles & Giveaways** — Keyword entry, animated draw, winner verification, multi-winner support
- **Counters** — Dashboard +/- buttons, chat commands (`!deaths`, `!deaths+`), custom templates

**Moderation**
- **Spam Filter** — Links, caps, banned words, emote spam, repetition detection — mods and subs exempt
- **Timed Messages** — Recurring messages with cycling, minimum chat line threshold, online/offline modes

**Quotes & Chat Tools**
- **Quotes System** — Save memorable chat moments with `!quote add`, retrieve with `!quote` or `!quote #`, dashboard management
- **Shoutout Command** — `!so @user` with live game/channel lookup via Twitch Helix API
- **Uptime Command** — `!uptime` / `!live` with smart time formatting

**OBS Overlays**
- **7 Built-in Overlays** — Alert Box, Chat Box, Poll, Raffle, Counter, Event List, Song Player — all real-time via SignalR
- **Full Overlay Editor** — Visual editor with live preview, per-event customization, 14 animations, 30+ Google Fonts, Custom CSS
- **Custom Sounds & Images** — Upload your own alert sounds (.mp3/.wav/.ogg) and images (.png/.gif/.webp) — stored locally, no cloud
- **Per-Event Alerts** — Individual image, sound, message, and animation for each event type (follow, sub, raid, etc.)
- **Custom Overlays (Developer Mode)** — Create fully custom overlays with HTML, CSS, and JavaScript with full SignalR access
- **5 Templates** — Follow Goal Bar, Follower Ticker, Stream Clock, Sub Counter, Raid Banner — ready to use and customize

**Automation & Integrations**
- **Effect System** — Trigger → Conditions → Effects automation chains. Combine any trigger (commands, events, hotkeys, keywords, channel points) with any effect (chat messages, counter updates, alerts, variables, Discord messages, OBS scene switches)
- **Visual Automation Builder** — Dual-mode editor: Visual mode with dynamic fields, variable chips, and inline help — or JSON mode for power users
- **OBS WebSocket 5.x** — Switch scenes and toggle sources via hotkeys and automations. Password stored securely in OS keychain.
- **Discord Integration** — Send messages and rich embeds to Discord via webhooks — no bot token needed
- **Stream Online Events** — All EventSub events (follow, sub, raid, stream online) are routed through the Effect System for custom automations
- **Mod Commands** — `!title`/`!titel` and `!game`/`!category` for live stream management (requires Moderator role)

**Dashboard & UX**
- **Live Dashboard** — Real-time chat feed, bot status, viewer count, activity feed, command management
- **Live Chat** — Send messages as bot or broadcaster, auto-scroll, message history, Twitch emote rendering with EmotePicker
- **Notification Center** — Sidebar bell with notification drawer, replaces ephemeral toasts for import progress and system events
- **User Tracking** — Message count, watch time, points, mod/sub/broadcaster status sync
- **Setup Wizard** — Guided first-time setup with direct links to Twitch Developer Console
- **Design System** — Light and Dark theme with consistent CSS custom properties
- **Cross-Platform** — Native desktop app on Windows 10/11 and macOS 12+ with platform-specific title bars
- **Update Check** — Automatic check for new releases with dismissable banner

### 🚧 Coming Soon

| Version | Features |
|---|---|
| **v2.5.0** | Linux support |
| **v2.6.0** | Automatic updater |

---

## Architecture

Wrkzg runs as a **single desktop process** — no separate server, no Docker, no external dependencies.
It embeds a Kestrel HTTP server and a Photino browser window into one self-contained application.

```
┌─────────────────────────────────────────────┐
│           Photino Window                     │
│        (Chromium on Windows / WebKit on Mac) │
│                                              │
│   ┌─────────────────────────────────────┐    │
│   │  React 19 Dashboard (TypeScript)    │    │
│   │  Tailwind CSS v4 · Vite · SignalR   │    │
│   └──────────────┬──────────────────────┘    │
└──────────────────┼───────────────────────────┘
                   │ HTTP + WebSocket
┌──────────────────┼───────────────────────────┐
│  ASP.NET Core Kestrel Server                 │
│                                              │
│  ┌──────────┐  ┌────────────┐  ┌──────────┐ │
│  │   Core   │  │ Infra-     │  │ SignalR  │ │
│  │  (Logic, │  │ structure  │  │  Hubs    │ │
│  │ Commands,│  │ (EF Core,  │  │ (Real-   │ │
│  │  Events) │  │  Twitch    │  │  time)   │ │
│  │          │  │  API, IRC) │  │          │ │
│  └──────────┘  └─────┬──────┘  └──────────┘ │
│                      │                       │
│               ┌──────┴──────┐                │
│               │   SQLite    │                │
│               └─────────────┘                │
└──────────────────────────────────────────────┘
```

**Key design decisions:**
- **Clean Architecture** — Core logic has zero dependencies on infrastructure
- **OS Keychain** — Twitch secrets stored via Windows DPAPI / macOS Keychain (never in config files)
- **SignalR** — Real-time push from backend to dashboard (chat messages, poll updates, viewer count)
- **Single-file publish** — One executable, no runtime installation needed

For the full architecture breakdown, see [ARCHITECTURE.md](_docs/ARCHITECTURE.md).

---

## Tech Stack

| Layer | Technology |
|---|---|
| Language & Runtime | C# · .NET 10 |
| Desktop Host | [Photino.NET](https://tryphotino.io/) |
| Backend / API | ASP.NET Core · Kestrel · SignalR |
| Frontend | React 19 · TypeScript · Vite · Tailwind CSS v4 |
| Database | SQLite · Entity Framework Core 10 |
| Twitch Integration | TwitchLib · Twitch Helix API · EventSub WebSocket |

---

## Installation

Download the latest release for your platform from the **[Releases page](https://github.com/wrkzg-korvar/wrkzg-twitchbot/releases)**.

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
| WebView | WebView2 (pre-installed with Edge) | WebKit (built-in) |

### First-Time Setup

When you start Wrkzg for the first time, a **Setup Wizard** guides you through everything:

1. **Create a Twitch App** — The wizard links you directly to the Twitch Developer Console and provides copy-paste values
2. **Enter Credentials** — Paste your Client ID and Client Secret (encrypted in your OS keychain)
3. **Connect Bot Account** — OAuth flow opens in your system browser
4. **Connect Broadcaster Account** — Same flow with your main streamer account
5. **Set Channel** — Enter your channel name — done

No manual config file editing. No terminal. No JSON.

---

## Building from Source

<details>
<summary>Click to expand build instructions</summary>

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

### Development Setup (Contributors)

Contributors can optionally use `appsettings.Development.json` as a **fallback** for local development. The app always checks the OS keychain first — this file is only used if no keychain credentials are found.

```json
{
  "Bot": {
    "Port": 5050
  }
}
```

> ⚠️ Never put secrets in config files. Use the Setup Wizard to store credentials in the OS keychain. The config file is only for non-sensitive settings like the port number. `appsettings.Development.json` is listed in `.gitignore`.

</details>

---

## Contributing

Contributions are welcome! Wrkzg is open source and built in the open.

Please read **[CONTRIBUTING.md](_docs/CONTRIBUTING.md)** before opening a pull request. It covers the development environment, project structure, architecture rules, coding conventions, and commit message format.

For questions or ideas, open a **[GitHub Discussion](https://github.com/wrkzg-korvar/wrkzg-twitchbot/discussions)**.

---

## Roadmap

<details>
<summary><strong>v1.0.0 — MVP ✅</strong></summary>

- Twitch OAuth (bot + broadcaster account)
- IRC connection + custom commands
- System commands (!commands, !points, !watchtime, !followage)
- User tracking (messages, watch time, points, mod/sub/broadcaster sync)
- Points system (automatic rewards, sub multiplier)
- Dashboard (live chat, commands CRUD, user table, settings)
- Setup Wizard
- Custom title bar with OS-native window controls
- Design system with Light/Dark theme

</details>

<details>
<summary><strong>v1.0.1 — Polish ✅</strong></summary>

- Windows blank screen fix (STA threading for WebView2)
- macOS .app bundle with ad-hoc code signing
- Release artifact cleanup
- Chromeless window resize border

</details>

<details>
<summary><strong>v1.1.0 — Community Features ✅</strong></summary>

- Polls & Votes (live bar chart, countdown, templates)
- Raffles & Giveaways (animated draw, multi-winner, templates)
- Timed Messages (message cycling, min chat lines, online/offline)
- Spam Filter (links, caps, banned words, emotes, repetition)
- Counters (dashboard +/-, chat commands, templates)
- Editable System Commands (enable/disable, custom responses)
- Live Chat improvements (send as bot/broadcaster, auto-scroll)

</details>

<details>
<summary><strong>v1.2.0 — Chat Tools ✅</strong></summary>

- Quotes System (`!quote add`, `!quote`, `!quote #`, `!quote delete #`, dashboard page)
- Shoutout Command (`!so @user` with Helix game/channel lookup)
- Uptime Command (`!uptime` / `!live`)
- Command Aliases (frontend editor, badge display in command table)

</details>

<details>
<summary><strong>v1.3.0 — Live Events ✅</strong></summary>

- EventSub WebSocket integration (TwitchLib.EventSub.Websockets)
- Follow / Subscribe / Gift Sub / Resub / Raid notifications with customizable templates
- Dashboard notification settings page with per-event toggles and test buttons
- Dashboard activity feed with real-time event display
- Auto-shoutout for raiders via Twitch Helix API

</details>

<details>
<summary><strong>v1.4.1 — OBS Overlays ✅</strong></summary>

- OBS Browser Source overlays (Alert Box, Chat, Poll, Raffle, Counter, Event List)
- Overlay settings dashboard with live preview and customizable properties
- SignalR dual groups (dashboard + overlay), overlay auto-reconnect with health polling
- Twitch emote rendering via CDN in live chat and chat overlay
- Frontend component library and full reorganization
- Port changed from 5000 to 5050 (avoids macOS AirPlay Receiver conflict)
- Multiple WKWebView compatibility fixes (Headers, empty response bodies)

</details>

<details>
<summary><strong>v1.5.0 — Community & Rewards ✅</strong></summary>

- Channel Point Rewards (sync from Twitch, configure actions: chat message, counter update, overlay alert)
- Roles & Ranks (auto-assign by watch time, points, messages, subscriber status)
- Priority-based role system with custom colors

</details>

<details>
<summary><strong>v1.6.0 — Chat Games ✅</strong></summary>

- 5 points-based games: Heist, Duel, Slots, Roulette, Trivia
- Fully configurable settings per game (cooldowns, bet limits, multipliers)
- Customizable bot messages with variable support
- Custom trivia questions
- Role-based access control

</details>

<details>
<summary><strong>v1.7.0 — Stream Analytics ✅</strong></summary>

- Automatic stream session tracking with minute-by-minute viewer snapshots
- Category change detection with time segments
- Overview dashboard with KPIs, viewer trend charts, stream hour charts
- Individual session explorer with viewer charts and category timelines

</details>

<details>
<summary><strong>v1.8.0 — Song Requests ✅</strong></summary>

- YouTube song requests via `!sr <URL>`
- Queue management (open/close, skip, clear)
- OBS Song Player overlay (full and slim mode)
- Points cost, max duration, per-user limits
- Customizable bot messages

</details>

<details>
<summary><strong>v1.9.0 — Hotkey Triggers ✅</strong></summary>

- Global keyboard hotkeys with key recorder
- Actions: chat message, counter increment/decrement/reset
- Auth-free API trigger for Stream Deck integration
- macOS Accessibility permission handling

</details>

<details>
<summary><strong>v2.0.0 — Effect System ✅</strong></summary>

- Visual automation editor: Trigger → Conditions → Effects chains
- 5 trigger types: command, event, keyword, hotkey, channel point
- 4 condition types: role check, points check, random chance, stream status
- 5 effect types: chat message, wait, counter update, alert, variable
- Quick-start examples and test button
- JSON-based configuration with cooldown management

</details>

<details>
<summary><strong>v2.1.0 — Third-Party Integrations ✅</strong></summary>

- Discord webhook integration (send messages and rich embeds)
- Discord effects in Effect System (`discord.send_message`, `discord.send_embed`)
- All EventSub events routed to Effect System (follow, sub, raid, gift, resub, stream online)
- `stream.online` EventSub subscription for live notifications
- Integrations dashboard page with webhook management and test button

</details>

<details>
<summary><strong>v2.2.0 — Bot Data Import ✅</strong></summary>

- Data import from Deepbot (CSV + JSON), Streamlabs Chatbot, and generic CSV files
- 4-step import wizard with preview, column mapping, and conflict strategies
- Imported user ID auto-resolution when users first chat
- VIP-to-Role mapping for Deepbot JSON imports

</details>

<details>
<summary><strong>v2.3.0 — Overlay Editor ✅</strong></summary>

- Full visual overlay editor with live preview replacing the config modal
- Per-event alert customization (custom image, sound, message, animation per event type)
- Asset management system — upload sounds (.mp3/.wav/.ogg) and images (.png/.gif/.webp) locally
- 30+ Google Fonts with live font picker
- 14 animations (slideDown, slideUp, bounceIn, flipIn, jackInTheBox, heartBeat, tada, and more)
- Custom CSS per overlay type — no !important needed
- Custom Overlays (Developer Mode) — HTML/CSS/JS editor with full SignalR access
- 5 ready-to-use templates (Follow Goal Bar, Follower Ticker, Stream Clock, Sub Counter, Raid Banner)
- JSON Field Definitions for configurable overlay settings

</details>

<details>
<summary><strong>v2.4.0 — Visual Automation Builder, OBS WebSocket, Emotes ✅</strong></summary>

- Visual Automation Builder with dynamic fields, variable chips, inline help (JSON mode still available)
- OBS WebSocket 5.x integration — switch scenes, toggle sources via hotkeys and automations
- Twitch Emotes — EmoteService caches global + user emotes (subscriptions, bits, follower), EmotePicker in dashboard chat
- Dual Helix Client: ITwitchHelixClient split into IBroadcasterHelixClient + IBotHelixClient
- Async background imports with SignalR progress notifications and module locking
- Notification Center — sidebar bell with notification drawer
- Mod Commands — `!title`/`!titel`, `!game`/`!category` for stream management
- SmartDataTable standardized across all list pages (search, sort, pagination)
- Timer announcements with configurable colors via Helix API
- RFC 7807 Problem Details on all API error responses
- Server-side paginated user list with user detail modal (points editing, ban/unban)
- Channel Point redemptions now trigger automations
- ISecureStorage extended with generic secret storage for integration credentials

</details>

### Future

| Version | Features |
|---|---|
| **v2.5.0** | Linux Support |
| **v2.6.0** | Automatic Updater — GitHub Releases download + install |

---

## License

Wrkzg is licensed under the [MIT License](LICENSE). © 2026 wrkzg.io

---

<div align="center">

**If Wrkzg is useful to you, consider giving it a ⭐ on GitHub — it helps others discover the project.**

</div>
