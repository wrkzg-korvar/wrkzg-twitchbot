# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] — 2026-03-28

### Added

- **EventSub WebSocket Integration** — real-time Twitch event notifications via EventSub WebSocket (TwitchLib.EventSub.Websockets); automatic connection lifecycle with exponential backoff reconnect; token validation and refresh
- **Follow Notifications** — configurable chat message when someone follows; `{user}` template variable
- **Subscribe Notifications** — new sub, gift sub, and resub events; `{user}`, `{tier}`, `{count}`, `{months}`, `{message}` template variables
- **Raid Notifications** — chat message on incoming raids with `{user}` and `{viewers}` variables; optional auto-shoutout via Twitch Helix API
- **Notification Settings Dashboard** — per-event-type enable/disable toggle, custom template editor, test button to preview in chat; available variables shown per event type
- **Dashboard Activity Feed** — real-time "Recent Events" section showing follows, subs, raids via SignalR with relative timestamps
- **Notification API** — GET/PUT settings per event type, POST test endpoint
- **Broadcaster Scope** — added `moderator:manage:shoutouts` for auto-shoutout on raids

## [1.2.0] — 2026-03-28

### Added

- **Quotes System** — save memorable chat moments with `!quote add <text>`; retrieve random quotes with `!quote` or specific quotes with `!quote <number>`; delete with `!quote delete <number>` (mod only); game auto-detection from live stream; dashboard page with search, create, and delete; `!q` and `!addquote` aliases
- **Shoutout Command** — `!so @username` posts a shoutout with the target's last played game via Helix API; strips `@` prefix; mod/broadcaster only; `!shoutout` alias; handles unknown users gracefully
- **Uptime Command** — `!uptime` shows how long the stream has been live with smart formatting (days/hours/minutes/seconds); offline detection; `!live` alias
- **Command Aliases (Frontend)** — aliases can now be created and edited in the dashboard command forms; alias badges displayed in the command table; comma-separated input field

## [1.1.0] — 2026-03-22

### Added

- **Polls & Votes** — create polls via dashboard or `!poll` chat command; vote with `!vote`/`!v`; live bar chart with countdown timer; auto-end on expiry; customizable announcement templates; full history with results
- **Raffles & Giveaways** — create via dashboard or `!raffle` chat command; keyword-based entry (`!join` or custom keyword); draw animation with trophy overlay; winner verification flow with live chat polling; accept/redraw/end workflow; multi-winner support; customizable announcement templates; full history
- **Timed Messages** — recurring bot messages on configurable intervals; multi-message cycling (round-robin); minimum chat lines threshold; online/offline mode toggle; enable/disable per timer; dashboard CRUD
- **Spam Filter** — link detection with domain whitelist; excessive caps detection with configurable threshold; banned word list (case-insensitive); emote spam limit; message repetition detection; broadcaster/mod always exempt; subscriber exempt option; configurable timeout duration per filter; dashboard toggle per filter
- **Counters** — create named counters with custom chat triggers; increment/decrement via dashboard buttons or chat (`!trigger+`, `!trigger-` for mods); display via `!trigger`; custom response templates with `{count}` and `{name}` variables; real-time SignalR updates
- **Editable System Commands** — enable/disable toggle for all system commands; custom response override per command; reset to default option
- **Live Chat Improvements** — send messages as bot or broadcaster account; account selector in chat input; auto-scroll with smart pause on scroll-up

### Fixed

- **Raffle keyword entry** — keyword matching now runs before command processing in the chat pipeline, preventing `!join` from being treated as an unknown command
- **Bot auto-connect** — bot now automatically connects to IRC after completing the setup wizard

## [1.0.1] — 2026-03-16

### Fixed

- **Windows blank screen** — Added STA (Single-Threaded Apartment) threading required by WebView2 on Windows. Without this, Photino opened but showed only a white screen.
- **Resize border too small** — Added invisible resize frame (6px edges, 12px corners) around the chromeless window for easier resizing.
- **wwwroot not found in Release builds** — ResolveWwwrootPath now uses AppContext.BaseDirectory as primary check, works with SingleFile and all publish scenarios.
- **Chromeless mode on all platforms** — Re-enabled SetChromeless(true) for Windows now that STA fix resolves the rendering issue.

### Changed

- **Windows release ZIP** — Removed unnecessary files (XML docs, PDB, web.config, BuildHost folders, launchSettings). ZIP now contains only essential runtime files.
- **macOS release** — Now ships as proper `.app` bundle with Info.plist, icns icon, and ad-hoc code signing. Eliminates terminal window on launch.

## [1.0.0] - 2026-03-15

### Added

- **Setup Wizard** — guided first-time setup walks through Twitch app registration, credential entry, OAuth authorization for bot and broadcaster accounts, and channel selection
- **Twitch OAuth** — full OAuth 2.0 Authorization Code flow for both bot and broadcaster accounts with automatic token refresh; tokens encrypted in OS keychain via platform-specific secure storage
- **IRC Connection** — auto-connect on startup, automatic token refresh before expiry, exponential backoff reconnect on disconnect
- **Custom Commands** — create, edit, and delete commands from the dashboard; supports variables (`{user}`, `{points}`, `{random:1:6}`, `{watchtime}`, `{followage}`)
- **System Commands** — built-in `!commands`, `!points`, `!watchtime`, `!followage` available out of the box
- **Dashboard** — live chat feed via SignalR, bot connection status, viewer count, command management (CRUD), user table with sorting and search, settings page
- **User Tracking** — tracks message count, watch time, points, display name; syncs mod, subscriber, and broadcaster status from Twitch
- **Points System** — automatic point rewards per minute while the stream is live; configurable subscriber multiplier
- **Custom Title Bar** — OS-native window controls (macOS traffic lights, Windows caption buttons) with a custom-styled title bar replacing the default chrome
- **Design System** — brand colors extracted from the Wrkzg logo, Light and Dark theme toggle persisted in settings, all colors defined as CSS custom properties
- **Custom Bot Name** — connect any Twitch account as the bot identity
- **Cross-platform support** — runs on Windows 10/11 (x64) and macOS 12+ (x64 and Apple Silicon)
