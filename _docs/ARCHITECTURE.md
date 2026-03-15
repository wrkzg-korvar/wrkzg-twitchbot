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

- All business logic: `CommandProcessor`, `ChatMessagePipeline`
- Domain models: `User`, `Command`, `Raffle`, `Poll`, `Setting`, `ChatMessage`, `BotStatus`
- Service interfaces: `ICommandProcessor`, `ITwitchChatClient`, `IUserRepository`, etc.
- Chat game interfaces (`IChatGame`) — implementations will be auto-discovered
- DI registration via `AddCoreServices()`

### `Wrkzg.Infrastructure`

**Type:** Class Library (standard SDK)  
Implements all interfaces defined in Core using real external dependencies.

- `BotDbContext` (EF Core + SQLite) with automatic migrations on startup
- Repository implementations: `UserRepository`, `CommandRepository`, `RaffleRepository`, `PollRepository`, `SettingsRepository`
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

React 19 + TypeScript SPA with Tailwind CSS v4. Features:
- **Setup Wizard** — 5-step guided first-time configuration
- **Dashboard** — live chat feed via SignalR, bot status, viewer count
- **Commands** — CRUD management with inline create form
- **Users** — sortable viewer table with roles and stats
- **Settings** — Twitch account connections, credential management

In development, runs on `:5173` with a proxy to Kestrel. In production, built to `Wrkzg.Api/wwwroot/`.

---

## Data Flow

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
   ├── 1. UpdateUserStats (MessageCount, LastSeenAt, DisplayName sync)
   │
   ▼
   ├── 2. CommandProcessor.HandleMessageAsync
   │      checks: ! prefix → trigger match → permission → cooldown
   │      resolves: {user}, {points}, {watchtime}, {random:min:max}
   │
   ▼
TwitchChatClient.SendMessageAsync
   │
   ▼
Twitch IRC (response appears in chat)
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
| `TwitchChatClient` | Singleton | One IRC connection per app |
| `BotConnectionService` | Singleton (IHostedService) | Manages IRC lifecycle |
| `ISecureStorage` | Singleton | Stateless encrypted I/O |
| `IChatEventBroadcaster` | Singleton | Wraps SignalR HubContext |
| `IAuthStateNotifier` | Singleton | Wraps SignalR HubContext |
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
Users         — Id, TwitchId (unique), Username, DisplayName, Points, WatchedMinutes,
                MessageCount, FollowDate, IsSubscriber, SubscriberTier, IsBanned, IsMod,
                FirstSeenAt, LastSeenAt

Commands      — Id, Trigger (unique), Aliases (JSON), ResponseTemplate, PermissionLevel,
                GlobalCooldownSeconds, UserCooldownSeconds, IsEnabled, UseCount, CreatedAt

Raffles       — Id, Title, IsOpen, WinnerId (FK → Users), CreatedAt, ClosedAt
RaffleEntries — Id, RaffleId (FK), UserId (FK), TicketCount (unique: RaffleId+UserId)

Polls         — Id, Question, Options (JSON), IsActive, EndsAt, CreatedAt, Source
PollVotes     — Id, PollId (FK), UserId (FK), OptionIndex (unique: PollId+UserId)

Settings      — Key (PK), Value — seeded with default values on first run
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
| Bot Token | Bot's Twitch account | Chat read/write via IRC | `chat:read`, `chat:edit` |
| Broadcaster Token | Streamer's account | Helix API, EventSub, polls | `moderator:read:followers`, `channel:read:polls`, `channel:manage:polls`, `bits:read`, `channel:read:subscriptions` |

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

Built-in commands that live in code, not in the database. They cannot be deleted or disabled.

| Command | Aliases | Description |
|---|---|---|
| `!commands` | `!help` | Lists all available commands (system + custom) |
| `!points` | — | Shows the user's current point balance |
| `!watchtime` | — | Shows the user's total watch time |
| `!followage` | — | Shows how long the user has been following |

System commands are implemented via the `ISystemCommand` interface in Core and auto-registered in DI. They are checked before custom (DB) commands in the `CommandProcessor` pipeline. The API exposes them at `GET /api/commands/system`.

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
