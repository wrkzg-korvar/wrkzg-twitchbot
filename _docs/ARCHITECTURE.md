# Architecture

This document describes the technical architecture of Wrkzg. It is intended for contributors who want to understand how the system is structured before making changes.

## Table of Contents

- [Overview](#overview)
- [Layer Model](#layer-model)
- [Project Responsibilities](#project-responsibilities)
- [Data Flow](#data-flow)
- [Key Design Decisions](#key-design-decisions)
- [Dependency Injection](#dependency-injection)
- [Database](#database)
- [Twitch Integration](#twitch-integration)
- [Frontend Integration](#frontend-integration)
- [Real-Time Communication](#real-time-communication)
- [Security](#security)
- [System Commands](#system-commands)
- [Custom Title Bar](#custom-title-bar)
- [Design System](#design-system)
- [Auto-Updater](#auto-updater)

---

## Overview

Wrkzg is a **locally-run desktop application** that combines an embedded web server with a native browser window. There is no cloud component — everything runs on the streamer's machine.

```
┌─────────────────────────────────────────────────────────┐
│              Wrkzg.Host  (single OS process)            │
│                                                         │
│   ┌─────────────────┐   ┌───────────────────────────┐   │
│   │  Photino Window  │   │   ASP.NET Core (Kestrel) │   │
│   │  Chromium/WebKit │◄──│   localhost:{PORT}       │   │
│   │  (Dashboard UI)  │   │   REST API + SignalR     │   │
│   └─────────────────┘   └──────────────┬────────────┘   │
│                                         │               │
│                          ┌──────────────┴────────────┐  │
│                          │       Core Services       │  │
│                          │  CommandProcessor         │  │
│                          │  ChatMessagePipeline      │  │
│                          └──────────────┬────────────┘  │
│                                         │               │
│                          ┌──────────────┴────────────┐  │
│                          │      Infrastructure       │  │
│                          │  EF Core + SQLite         │  │
│                          │  TwitchChatClient (IRC)   │  │
│                          │  TwitchOAuthService       │  │
│                          │  SecureStorage (DPAPI/KC) │  │
│                          │  BotConnectionService     │  │
│                          └───────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
         │                              │
         ▼                              ▼
  Twitch IRC (Chat)            Twitch Helix API + EventSub
```

---

## Layer Model

Wrkzg follows **Clean Architecture**. The single most important rule is:

> **Dependencies always point inward. The Core layer has no knowledge of any outer layer.**

```
┌──────────────────────────────────────────┐
│           Host  /  Api  /  Frontend      │  ← Presentation
├──────────────────────────────────────────┤
│              Infrastructure              │  ← Data Access / External Services
├──────────────────────────────────────────┤
│                  Core                    │  ← Business Logic (no dependencies)
└──────────────────────────────────────────┘
```

Allowed dependency directions:

| Project | May reference |
|---|---|
| `Wrkzg.Host` | `Wrkzg.Api`, `Wrkzg.Core`, `Wrkzg.Infrastructure` |
| `Wrkzg.Api` | `Wrkzg.Core`, `Wrkzg.Infrastructure` |
| `Wrkzg.Infrastructure` | `Wrkzg.Core` |
| `Wrkzg.Core` | nothing (only BCL + Microsoft.Extensions abstractions) |

---

## Project Responsibilities

### `Wrkzg.Host`

**Type:** Executable (`WinExe`) with Web SDK  
**Entry point** of the entire application.

- Builds the DI container (`WebApplication.CreateBuilder`)
- Starts ASP.NET Core / Kestrel
- Resolves `wwwroot/` path for serving the React SPA (from `Wrkzg.Api/wwwroot/`)
- Opens the Photino browser window pointing at `localhost:{PORT}`
- In dev mode: detects Vite Dev Server on `:5173` and uses it if running
- Manages the application lifecycle

### `Wrkzg.Api`

**Type:** Class Library with `Microsoft.NET.Sdk.Web`  
No standalone entry point — embedded into the Host.

- Defines all REST endpoints (Minimal API pattern) in `Endpoints/`
- Hosts SignalR hubs (`ChatHub`) for real-time dashboard communication
- Stores React SPA build output in `wwwroot/` (built by Vite)
- Contains DTOs and request records
- Registers SignalR, `IAuthStateNotifier`, `IChatEventBroadcaster`

### `Wrkzg.Core`

**Type:** Class Library (standard SDK)  
The heart of the application. Has zero framework dependencies.

- All business logic: `CommandProcessor`, `ChatMessagePipeline`, `PollService`, `RaffleService`, `SpamFilterService`, `TimedMessageService`
- Domain models: `User`, `Command`, `Poll`, `PollVote`, `Raffle`, `RaffleEntry`, `RaffleDraw`, `TimedMessage`, `Counter`, `Setting`, `SystemCommandOverride`, `SpamFilterConfig`, `ChatMessage`, `BotStatus`
- Service interfaces: `ICommandProcessor`, `ITwitchChatClient`, `IUserRepository`, `IPollRepository`, `IRaffleRepository`, `ITimedMessageRepository`, `ICounterRepository`, `ISettingsRepository`, etc.
- System commands: 13 built-in commands implementing `ISystemCommand`
- DI registration via `AddCoreServices()`

### `Wrkzg.Infrastructure`

**Type:** Class Library (standard SDK)  
Implements all interfaces defined in Core using real external dependencies.

- `BotDbContext` (EF Core + SQLite) with automatic migrations on startup
- Repository implementations: `UserRepository`, `CommandRepository`, `PollRepository`, `RaffleRepository`, `TimedMessageRepository`, `CounterRepository`, `SettingsRepository`, `SystemCommandOverrideRepository`
- `TwitchChatClient` (TwitchLib IRC) — Singleton, auto token refresh
- `TwitchOAuthService` — Authorization Code Flow, credential resolution (Keystore → Config fallback)
- `TwitchAuthHandler` — DelegatingHandler for automatic Bearer token injection + 401 refresh
- `BotConnectionService` — IHostedService managing IRC lifecycle
- `WindowsSecureStorage` (DPAPI) / `MacOsSecureStorage` (Keychain) — encrypted credential storage

### `Wrkzg.Updater`

**Type:** Executable — fully standalone, no references to other projects.

Launched as a separate OS process when an update is ready. Waits for the main process to exit, then replaces application files and restarts the app.

### `Wrkzg.Frontend`

**Type:** Node.js / Vite project — not part of the .NET solution.

React 19 + TypeScript SPA with Tailwind CSS v4. Pages:
- **Setup Wizard** — 5-step guided first-time configuration
- **Dashboard** — live chat feed via SignalR, bot status, viewer count, chat send
- **Commands** — custom command CRUD + system command enable/disable + custom response override
- **Users** — sortable viewer table with roles and stats
- **Polls** — create polls, live bar chart, countdown, history, customizable templates
- **Raffles** — create raffles, keyword entry, draw animation, winner verification, multi-winner, history
- **Timers** — timed message CRUD, multi-message editor, online/offline toggle
- **Counters** — counter cards with +/- buttons, chat trigger display, create/edit
- **Spam Filter** — per-filter toggle + configuration (links, caps, banned words, emotes, repetition)
- **Settings** — Twitch account connections, credential management

In development, runs on `:5173` with a proxy to Kestrel. In production, built to `Wrkzg.Api/wwwroot/`.

---

## Data Flow

### Chat Message Pipeline

Every incoming chat message passes through the pipeline in this order:

1. **UpdateUserStats** — increment message count, update LastSeenAt, sync roles
2. **MarkUserActive** — flag user for watch time tracking
3. **IncrementChatLineCounter** — notify TimedMessageService of chat activity
4. **TryKeywordEntry** — check if message matches active raffle keyword
5. **SpamFilter.CheckAsync** — run spam checks (links, caps, banned words, emotes, repetition); if spam → timeout + return
6. **Counter Commands** — check for dynamic counter triggers (`!trigger`, `!trigger+`, `!trigger-`)
7. **CommandProcessor** — match against system commands and custom commands

### Chat Message → Command Response

```
Twitch IRC
   │
   ▼
TwitchChatClient.OnMessageReceived
   │
   ▼
BotConnectionService.HandleChatMessage
   │
   ├──→ IChatEventBroadcaster (SignalR → Dashboard live chat feed)
   │
   ▼
ChatMessagePipeline.ProcessAsync
   │
   ├── 1. UpdateUserStats
   ├── 2. MarkUserActive (Watchtime)
   ├── 3. IncrementChatLineCounter (Timed Messages)
   ├── 4. TryKeywordEntry (Raffle)
   ├── 5. SpamFilter.CheckAsync → return if spam
   ├── 6. Counter Commands (dynamic !trigger/!trigger+/!trigger-)
   ├── 7. CommandProcessor (System + Custom Commands)
   │
   ▼
TwitchChatClient.SendMessageAsync → Twitch IRC
```

### Dashboard Action → Bot Response

```
Dashboard (React)
   │  HTTP POST /api/commands
   ▼
CommandEndpoints (Wrkzg.Api)
   │  validation
   ▼
ICommandRepository.CreateAsync  →  SQLite
   │
   ▼
HTTP 201 Created → Dashboard UI updates
```

### OAuth Flow (System Browser)

```
Dashboard: "Connect Bot Account" button
   │
   ▼
POST /auth/open-browser/bot → Server opens OS default browser
   │
   ▼
System Browser → https://id.twitch.tv/oauth2/authorize
   │  User authorizes
   ▼
Twitch → http://localhost:5000/auth/callback?code=XXX&state=YYY
   │
   ▼
Server: validates state, exchanges code for tokens, saves encrypted
   │
   ▼
SignalR: AuthStateChanged → Dashboard updates automatically
   │
   ▼
Browser: "You can close this tab and return to Wrkzg"
```

---

## Key Design Decisions

### Why Photino.NET instead of Electron?

Photino.NET wraps the native OS browser engine (WebView2 on Windows, WKWebView on macOS). This means no bundled Chromium binary, significantly smaller install size, no Node.js runtime required. The trade-off: macOS uses WebKit, so popups (`window.open()`) are silently blocked. OAuth flows therefore open the system browser via `Process.Start` from the server side.

### Why SQLite instead of a server database?

Single-user, locally-run application. SQLite needs no installation, is file-based (easy backup), and fully supported by EF Core with automatic migrations.

### Why Authorization Code Flow instead of PKCE?

Twitch does not support PKCE (as of March 2026, despite community requests since 2020). We use the standard Authorization Code Flow with Client Secret. Each user registers their own Twitch Developer App. The Client Secret is stored encrypted in the OS keychain (DPAPI on Windows, macOS Keychain). The `ITwitchOAuthService` interface is designed so that a migration to PKCE requires no interface changes once Twitch adds support.

### Why system browser for OAuth instead of in-app?

Photino's WKWebView on macOS silently blocks all `window.open()` calls. Instead, the server opens the OS default browser via `Process.Start("open", url)` on macOS / `UseShellExecute = true` on Windows. The user is likely already logged into Twitch in their browser. The callback page tells the user to close the tab, and the app updates via SignalR.

### Why Minimal API instead of MVC Controllers?

Minimal API reduces boilerplate and keeps endpoint definitions close to their route definitions. Appropriate for a project of this size.

### Why credentials in OS keychain instead of config files?

End users should not need to edit JSON config files. The Setup Wizard stores Client ID, Client Secret, and OAuth tokens exclusively in the OS-native secure storage (DPAPI / Keychain). `appsettings.Development.json` is only used as a fallback by contributors during local development.

---

## Dependency Injection

All services are registered using extension methods called from `Wrkzg.Host/Program.cs`:

```csharp
builder.Services.AddCoreServices();          // Wrkzg.Core
builder.Services.AddInfrastructure(config);  // Wrkzg.Infrastructure
builder.Services.AddApiServices();           // Wrkzg.Api
```

**Lifetimes:**

| Service type | Lifetime | Reason |
|---|---|---|
| `CommandProcessor` | Singleton | Cooldown state in-memory (ConcurrentDictionary) |
| `ChatMessagePipeline` | Singleton | Orchestrates message processing |
| `ChatMessageBuffer` | Singleton | In-memory ring buffer for recent messages |
| `TwitchChatClient` | Singleton | One IRC connection per app |
| `BotConnectionService` | Singleton (IHostedService) | Manages IRC lifecycle |
| `TimedMessageService` | Singleton (IHostedService) | Checks timer intervals, tracks chat line count |
| `PollTimerService` | IHostedService | Auto-ends expired polls |
| `RaffleTimerService` | IHostedService | Auto-draws expired raffles |
| `UserTrackingService` | Singleton (IHostedService) | Awards watch time and points |
| `ISecureStorage` | Singleton | Stateless encrypted I/O |
| `IChatEventBroadcaster` | Singleton | Wraps SignalR HubContext |
| `IAuthStateNotifier` | Singleton | Wraps SignalR HubContext |
| `PollService` | Scoped | Poll lifecycle (create, vote, end) |
| `RaffleService` | Scoped | Raffle lifecycle (create, enter, draw, verify) |
| `SpamFilterService` | Scoped | Checks messages against spam rules |
| Repositories | Scoped | One per request/operation |
| `BotDbContext` | Scoped | Standard EF Core lifetime |

**Scoped-in-Singleton pattern:** `CommandProcessor` and `BotConnectionService` need scoped dependencies (repositories). They receive `IServiceScopeFactory` and create scopes for DB access internally.

---

## Database

The SQLite database file is stored in the OS application data directory:

- **Windows:** `%APPDATA%\Wrkzg\bot.db`
- **macOS:** `~/Library/Application Support/Wrkzg/bot.db`

### Schema

```
Users                — Id, TwitchId (unique), Username, DisplayName, Points, WatchedMinutes,
                       MessageCount, FollowDate, IsSubscriber, SubscriberTier, IsBanned, IsMod,
                       IsBroadcaster, FirstSeenAt, LastSeenAt

Commands             — Id, Trigger (unique), Aliases (JSON), ResponseTemplate, PermissionLevel,
                       GlobalCooldownSeconds, UserCooldownSeconds, IsEnabled, UseCount, CreatedAt

SystemCommandOverrides — Trigger (PK), IsEnabled, CustomResponseTemplate

Polls                — Id, Question, Options (JSON), DurationSeconds, IsActive, EndsAt, CreatedBy,
                       EndReason, Source, TwitchPollId, CreatedAt
PollVotes            — Id, PollId (FK), UserId (FK), OptionIndex (unique: PollId+UserId)

Raffles              — Id, Title, Keyword, DurationSeconds, MaxEntries, EntriesCloseAt, IsOpen,
                       ClosedAt, EndReason, CreatedBy, WinnerId (FK), PendingWinnerId (FK), CreatedAt
RaffleEntries        — Id, RaffleId (FK), UserId (FK), TicketCount (unique: RaffleId+UserId)
RaffleDraws          — Id, RaffleId (FK), UserId (FK), DrawNumber, IsAccepted, RedrawReason, DrawnAt

TimedMessages        — Id, Name, Messages (JSON), IntervalMinutes, MinChatLines, IsEnabled,
                       RunWhenOnline, RunWhenOffline, LastFiredAt, NextMessageIndex, CreatedAt

Counters             — Id, Name, Trigger (unique), Value, ResponseTemplate, CreatedAt

Settings             — Key (PK), Value
```

### Migrations

EF Core migrations are applied automatically on startup. When adding a new migration during development:

```bash
dotnet ef migrations add MigrationName \
  --project src/Wrkzg.Infrastructure \
  --startup-project src/Wrkzg.Host
```

---

## Twitch Integration

### Two OAuth Tokens

| Token | Account | Purpose | Scopes |
|---|---|---|---|
| Bot Token | Bot's Twitch account | Chat read/write via IRC, Helix chat send | `chat:read`, `chat:edit`, `user:write:chat` |
| Broadcaster Token | Streamer's account | Helix API, EventSub, polls, chat send | `moderator:read:followers`, `channel:read:polls`, `channel:manage:polls`, `bits:read`, `channel:read:subscriptions`, `user:write:chat` |

Both tokens are acquired via **OAuth 2.0 Authorization Code Flow** with Client Secret (Twitch does not support PKCE). Tokens are stored encrypted in the OS keychain and refreshed automatically.

### Credential Resolution Order

Both Client ID and Client Secret are resolved in this order:
1. **OS Keychain** (DPAPI / macOS Keychain) — production path, set by Setup Wizard
2. **appsettings.Development.json** — dev fallback for contributors only

### IRC Connection

`TwitchChatClient` connects to Twitch IRC via TwitchLib.Client (WebSocket). `BotConnectionService` manages the lifecycle as an `IHostedService`: auto-connects on startup if tokens and channel are configured, reconnects on disconnect (up to 10 attempts).

---

## Frontend Integration

### Development Mode

```
Vite Dev Server :5173
      │
      │  proxy /api  →  :5000
      │  proxy /hubs →  :5000 (WebSocket)
      ▼
Kestrel :5000

Photino detects Vite on :5173 → opens Vite URL (HMR enabled)
If Vite not running → falls back to Kestrel (static files from wwwroot/)
```

### Production Mode

```
npm run build → Wrkzg.Api/wwwroot/
      │
Kestrel serves index.html + static assets
      │
Photino opens localhost:{PORT}
```

### Static Files Resolution

Because `Wrkzg.Host` is the entry point but `wwwroot/` lives in `Wrkzg.Api`, the `Program.cs` uses a `ResolveWwwrootPath()` helper that searches for the built SPA files in multiple locations (next to Api assembly, relative to CWD, relative to Host directory) and creates an explicit `PhysicalFileProvider`.

---

## Real-Time Communication

The dashboard receives live updates via **SignalR**. The hub is hosted at `/hubs/chat`.

| SignalR Method | Payload | Trigger |
|---|---|---|
| `ChatMessage` | `{ userId, username, displayName, content, isMod, isSubscriber, isBroadcaster, timestamp }` | Chat message received via IRC |
| `ViewerCount` | `int` | Polling interval (60s) |
| `FollowEvent` | `{ username }` | EventSub follow event |
| `SubscribeEvent` | `{ username, tier }` | EventSub subscribe event |
| `BotStatus` | `{ isConnected, channel, reason? }` | IRC connection state change |
| `AuthStateChanged` | `{ tokenType, isAuthenticated, twitchUsername, ... }` | OAuth login/logout/token refresh failure |
| `PollCreated` | `{ id, question, options, durationSeconds, endsAt, createdBy, source }` | New poll started |
| `PollVote` | `{ pollId, optionIndex }` | Vote received |
| `PollEnded` | `{ id, question, totalVotes, options, winnerIndex }` | Poll ended |
| `RaffleCreated` | `{ id, title, keyword, durationSeconds, maxEntries, createdBy }` | New raffle started |
| `RaffleEntry` | `{ raffleId, username, entryCount }` | New raffle entry |
| `RaffleDrawPending` | `{ raffleId, winnerName, twitchId, totalEntries, drawNumber }` | Winner drawn, pending verification |
| `RaffleWinnerAccepted` | `{ raffleId, winnerName, drawNumber }` | Winner accepted |
| `RaffleDrawn` | `{ raffleId, winnerName, totalEntries }` | Final winner announced |
| `RaffleEnded` | `{ raffleId }` | Raffle closed |
| `RaffleCancelled` | `{ raffleId }` | Raffle cancelled |
| `CounterUpdated` | `{ counterId, name, value }` | Counter value changed |

---

## Security

- OAuth tokens and Twitch app credentials are **never stored in plaintext**
    - Windows: encrypted with DPAPI (`ProtectedData.Protect`, current-user scope)
    - macOS: stored in the system Keychain via `security` CLI
- Credentials are resolved from Keychain first, config files only as dev fallback
- Kestrel only binds to `localhost` — the dashboard is not accessible from the network
- OAuth flows open the system browser (not the embedded WebView) for security and compatibility
- CSRF protection: cryptographically random state parameter with 10-minute TTL
- No telemetry, no analytics, no external calls except Twitch API and GitHub Releases API
- All outbound HTTP calls use resilience pipelines (retry + timeout)

---

## System Commands

Built-in commands that live in code, not in the database. They can be enabled/disabled and have custom response overrides via `SystemCommandOverrides`.

| Command | Aliases | Description |
|---|---|---|
| `!commands` | `!help` | Lists all available commands |
| `!points` | — | Shows user's point balance |
| `!watchtime` | — | Shows user's total watch time |
| `!followage` | — | Shows how long user has followed |
| `!editcmd` | `!editcommand` | Edit a custom command's response |
| `!poll` | — | Start a poll (Mod+) |
| `!vote` | `!v` | Vote in active poll |
| `!pollend` | `!endpoll`, `!closepoll` | End the active poll (Mod+) |
| `!pollresult` | `!pollresults`, `!results` | Show poll results |
| `!raffle` | `!giveaway` | Start a raffle (Mod+) |
| `!join` | `!enter` | Enter the active raffle |
| `!draw` | — | Draw a raffle winner (Mod+) |
| `!cancelraffle` | `!rafflecancel` | Cancel the active raffle (Mod+) |

System commands are implemented via the `ISystemCommand` interface in Core and auto-registered in DI. They are checked before custom (DB) commands in the `CommandProcessor` pipeline. The API exposes them at `GET /api/commands/system`.

Each system command can be enabled/disabled and given a custom response override via the `SystemCommandOverrides` table. Overrides are managed from the Commands page in the dashboard.

---

## Background Services

| Service | Interval | Purpose |
|---|---|---|
| `PollTimerService` | 2s | Checks if active poll timer has expired, auto-ends poll |
| `RaffleTimerService` | 2s | Checks if active raffle timer has expired, auto-draws winner |
| `TimedMessageService` | 30s | Checks if any timed message should fire (interval + min chat lines + online/offline) |
| `UserTrackingService` | 60s | Awards watch time minutes and points to active users |

---

## Custom Title Bar

Photino runs in chromeless mode (`.SetChromeless(true)`) — no native OS title bar. The React frontend renders a custom title bar that adapts to the current OS:

- **macOS:** Traffic light buttons (close/minimize/maximize) on the left, centered app title
- **Windows:** Standard buttons (minimize/maximize/close) on the right, left-aligned title

Window controls communicate with the backend via REST endpoints (`POST /api/window/minimize|maximize|close|drag-start|drag-move`). The `IWindowController` interface in Core is implemented by `PhotinoWindowController` in Host.

OS detection: The server reports the platform via `GET /api/status` (`platform` field), since Photino's WKWebView user agent does not contain OS information.

---

## Design System

The color scheme is derived from the Wrkzg logo gradient (green → orange → red):

- **Brand Green `#8BBF4C`** — primary accent color for general UI
- **Twitch Purple `#A855F7`** — reserved for Twitch-specific UI elements only

Theming is implemented via CSS custom properties in `index.css`. Users can toggle between Dark (default) and Light themes via a button in the sidebar. The preference is persisted in localStorage.

All components use `var(--color-*)` properties instead of hardcoded Tailwind color classes, enabling seamless theme switching without component changes.

---

## Auto-Updater

The updater runs as a **completely separate process** (`Wrkzg.Updater`) to avoid file-locking issues when replacing the running application.

```
Main app checks GitHub Releases API on startup
   │  newer version found
   ▼
Download ZIP to temp directory
   │  verify SHA-256 checksum
   ▼
Launch Wrkzg.Updater with args: --zip --target --pid --exe
   │
   ▼
Main app exits → Updater waits → extracts ZIP → restarts app
```
