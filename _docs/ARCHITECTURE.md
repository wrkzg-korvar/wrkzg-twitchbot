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
│                          │  ChatGameManager          │  │
│                          │  UserTrackingService      │  │
│                          │  RaffleService · PollService││
│                          └──────────────┬────────────┘  │
│                                         │               │
│                          ┌──────────────┴────────────┐  │
│                          │      Infrastructure       │  │
│                          │  EF Core + SQLite         │  │
│                          │  TwitchChatClient (IRC)   │  │
│                          │  TwitchHelixClient (REST) │  │
│                          │  EventSub (WebSocket)     │  │
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
| `Wrkzg.Host` | `Wrkzg.Api` |
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
- Opens the Photino browser window pointing at `localhost:{PORT}`
- Manages the system tray icon and application lifecycle
- On release builds: triggers the frontend build (`npm run build`) before publish

### `Wrkzg.Api`

**Type:** Class Library with `Microsoft.NET.Sdk.Web`  
No standalone entry point — embedded into the Host.

- Defines all REST endpoints (Minimal API pattern)
- Hosts SignalR hubs for real-time dashboard communication
- Serves the React SPA from `wwwroot/` in production
- Contains DTOs and request validators (FluentValidation)
- Registers middleware pipeline

### `Wrkzg.Core`

**Type:** Class Library (standard SDK)  
The heart of the application. Has zero framework dependencies.

- All business logic and use cases
- Domain models (`User`, `Command`, `Raffle`, `Poll`, etc.)
- Service interfaces (`ICommandProcessor`, `IChatGame`, `IUserRepository`, etc.)
- Chat game implementations (`IChatGame`)
- `IHostedService` implementations for background tasks (user tracking, polling)

### `Wrkzg.Infrastructure`

**Type:** Class Library (standard SDK)  
Implements all interfaces defined in Core using real external dependencies.

- `BotDbContext` (EF Core + SQLite)
- Repository implementations
- `TwitchChatClient` (TwitchLib IRC)
- `TwitchHelixClient` (HTTP Client for Helix REST API)
- `TwitchEventSubClient` (WebSocket for real-time Twitch events)
- `TwitchOAuthService` (PKCE OAuth flow)
- `ISecureStorage` implementations (DPAPI on Windows, Keychain on macOS)

### `Wrkzg.Updater`

**Type:** Executable — fully standalone, no references to other projects.

Launched as a separate OS process when an update is ready. Waits for the main process to exit, then replaces application files and restarts the app. See [Auto-Updater](#auto-updater).

### `Wrkzg.Frontend`

**Type:** Node.js / Vite project — not part of the .NET solution.

React + TypeScript SPA. In development, runs on `:5173` with a proxy to the Kestrel backend. In production, built to `Wrkzg.Api/wwwroot/` and served as static files.

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
CommandProcessor.HandleMessageAsync
   │  checks: trigger match, permission level, cooldown
   ▼
ICommandRepository.GetByTriggerOrAliasAsync  →  SQLite
   │
   ▼
Template resolved ({user}, {uptime}, {random})
   │
   ▼
TwitchChatClient.SendMessageAsync
   │
   ▼
Twitch IRC (response appears in chat)
   │
   ▼
IChatEventBroadcaster.BroadcastChatMessageAsync
   │
   ▼
SignalR Hub → Dashboard UI (live chat feed)
```

### Dashboard Action → Bot Response

```
Dashboard (React)
   │  HTTP PUT /api/commands/{id}
   ▼
CommandEndpoints (Wrkzg.Api)
   │  FluentValidation
   ▼
ICommandProcessor.UpdateCommandAsync
   │
   ▼
ICommandRepository.UpdateAsync  →  SQLite
   │
   ▼
HTTP 200 OK → Dashboard UI updates
```

---

## Key Design Decisions

### Why Photino.NET instead of Electron?

Photino.NET wraps the native OS browser engine (Chromium on Windows via WebView2, WebKit on macOS via WKWebView). This means:
- No bundled Chromium binary — significantly smaller install size
- No Node.js runtime required
- Pure .NET — no context switching between runtimes
- The trade-off is that macOS uses WebKit instead of Chromium, so the frontend must be tested on both platforms

### Why SQLite instead of a server database?

Wrkzg is a single-user, locally-run application. SQLite is a perfect fit:
- No installation or configuration for the end user
- File-based — easy to back up or move
- Fully supported by EF Core with automatic migrations on startup
- More than sufficient for the data volumes involved (one streamer, one channel)

### Why no plugin system?

Wrkzg is open source. Instead of a plugin API (which would require a stable ABI and significant maintenance), contributors extend the bot by submitting pull requests. The interface-based architecture makes this straightforward — adding a new chat game, for example, requires only implementing `IChatGame` and writing tests.

### Why Minimal API instead of MVC Controllers?

Minimal API reduces boilerplate and keeps endpoint definitions close to their route definitions. For a project of this size, the extra abstraction of full MVC controllers adds complexity without benefit.

---

## Dependency Injection

All services are registered using extension methods that are called from `Wrkzg.Host/Program.cs`:

```csharp
builder.Services.AddCoreServices();          // Wrkzg.Core
builder.Services.AddInfrastructure(config);  // Wrkzg.Infrastructure
builder.Services.AddApiServices();           // Wrkzg.Api
```

Chat games are auto-registered via assembly scan in `AddCoreServices()` — any class implementing `IChatGame` is automatically discovered and registered as a singleton.

**Lifetimes:**

| Service type | Lifetime |
|---|---|
| Chat games (`IChatGame`) | Singleton — maintain game state across messages |
| Core services (managers, processors) | Singleton — shared state |
| Repositories | Scoped — one per request/operation |
| Background services (`IHostedService`) | Singleton — managed by the .NET host |
| DbContext | Scoped (via factory for singletons) |

---

## Database

The SQLite database file is stored in the OS application data directory:

- **Windows:** `%APPDATA%\Wrkzg\bot.db`
- **macOS:** `~/Library/Application Support/Wrkzg/bot.db`

### Schema Migrations

EF Core migrations are applied automatically on startup:

```csharp
await db.Database.MigrateAsync();
```

When adding a new migration during development:

```bash
dotnet ef migrations add MigrationName \
  --project src/Wrkzg.Infrastructure \
  --startup-project src/Wrkzg.Host
```

---

## Twitch Integration

### Two OAuth Tokens

Wrkzg authenticates with two separate Twitch accounts:

| Token | Account | Purpose |
|---|---|---|
| Bot Token | The bot's Twitch account | Send/receive chat messages via IRC |
| Broadcaster Token | The streamer's Twitch account | Helix API, polls, EventSub subscriptions |

Both tokens are acquired via **OAuth 2.0 Authorization Code Flow with PKCE** — no client secret required, safe for local apps.

### EventSub WebSocket

For real-time events (follows, subscriptions, raids), Wrkzg connects to the Twitch EventSub WebSocket endpoint. This avoids polling and provides sub-second latency for event handling.

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
```

### Production Mode

```
dotnet publish (Release)
      │
      ├── npm run build  →  Wrkzg.Api/wwwroot/
      │
      └── Kestrel serves index.html + static assets
              │
          Photino opens localhost:{PORT}
```

---

## Real-Time Communication

The dashboard receives live updates via **SignalR**. The hub is hosted at `/hubs/chat`.

Events pushed from server to dashboard:

| SignalR Method | Payload | Trigger |
|---|---|---|
| `ChatMessage` | `{ username, displayName, content, isMod, isSubscriber, timestamp }` | New chat message received |
| `ViewerCount` | `int` | Polling interval (60s) |
| `FollowEvent` | `{ username }` | EventSub follow event |
| `SubscribeEvent` | `{ username, tier }` | EventSub subscribe event |
| `BotStatus` | `{ isConnected, channel }` | Connection state change |

---

## Security

- OAuth tokens are **never stored in plaintext**
    - Windows: encrypted with DPAPI (`ProtectedData.Protect`, current-user scope)
    - macOS: stored in the system Keychain
- Kestrel only binds to `localhost` — the dashboard is not accessible from other machines on the network
- No telemetry, no analytics, no external calls except Twitch API and GitHub Releases API
- All outbound HTTP calls use Polly retry policies with timeouts
- Update downloads are verified with SHA-256 checksums before installation

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
Main app exits
   │
   ▼
Updater waits for main process to exit
   │
   ▼
Updater extracts ZIP, overwrites files
   │
   ▼
Updater launches new version of main app
   │
   ▼
Updater exits
```
