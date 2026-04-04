# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.3.3] — 2026-04-04

### Added
- **6 new hotkey actions:** Run Automation, Start Poll, End Poll, Start Raffle, Skip Song, Show Alert
- Counter summary cards on Dashboard with real-time SignalR updates
- Delete confirmation dialogs on Timers and Counters pages
- Toast notifications on command toggle and timer toggle mutations

### Improved
- Hotkey form now shows context-specific payload editors (poll config, raffle config, automation dropdown)
- Light theme contrast improvements on Notifications, Automations, and Timers pages

### Fixed
- macOS hotkeys: Ctrl maps to Command (⌘) key matching Mac UX conventions
- macOS hotkeys: CapsLock no longer prevents hotkey matching
- macOS hotkeys: RequestPermission now auto-starts event tap after granting
- TwitchHelixClient: guard against empty channel login (prevents 400 Bad Request spam)
- EF Core: AsSplitQuery on Raffle queries (eliminates MultipleCollectionIncludeWarning)
- SignalR: PascalCase → camelCase property names on Poll, Raffle, Analytics, Effects endpoints
- EventListOverlay: `user` → `username` property name matching backend
- Program.cs: 5 missing endpoint mappings in fallback block
- StatusEndpoints: now returns app version from version.json
- CSP: allows api.github.com for update banner
- HotkeyEndpoints: RefreshBindingsAsync after create/update/delete

## [2.3.0] — 2026-04-03

### Added

- **Overlay Editor** — full visual editor with split-view live preview, replacing the previous config modal
- **Per-Event Alert Customization** — each event type (follow, subscribe, gift sub, resub, raid, channel point) has individual image, sound, volume, message, and animation settings
- **Asset Management** — upload custom sounds (.mp3, .wav, .ogg) and images (.png, .jpg, .gif, .webp, .svg) locally; max 10 MB per file; served via localhost
- **Google Fonts** — 30+ popular fonts available in a picker with live preview; loaded dynamically from Google CDN
- **14 Animations** — slideDown, slideUp, slideLeft, slideRight, fadeIn, bounceIn, zoomIn, flipIn, rotateIn, jackInTheBox, rubberBand, heartBeat, tada, none
- **Custom CSS** — per-overlay CSS textarea; loaded after default styles so no !important needed
- **Custom Overlays (Developer Mode)** — create fully custom overlays with HTML, CSS, and JavaScript; full SignalR event access via `Wrkzg.on()` API
- **5 Custom Overlay Templates** — Follow Goal Bar, Recent Follower Ticker, Stream Clock, Sub Counter with Effects, Raid Alert Banner
- **JSON Field Definitions** — define configurable fields for custom overlays; supported types: text, number, color, toggle, select, sound, image, font
- **Overlay Defaults API** — `GET /api/overlays/defaults/{type}` for editor reset-to-defaults
- **Asset API** — `POST /api/assets/upload/{category}`, `GET /api/assets/{category}`, `DELETE /api/assets/{category}/{fileName}`
- **Custom Overlay API** — full CRUD at `/api/custom-overlays`, render at `/overlay/custom/{id}`
- **Custom Overlay Render** — renders as full HTML page with embedded SignalR; checkerboard preview background indicates transparency
- **Test Buttons in Editor** — fire test events directly from the Alert Box editor
- **Live Preview via postMessage** — overlay settings update in the preview iframe without page reload

### Changed

- **Overlay Cards** — "Configure" button replaced with "Edit" that opens the full editor page
- **Preview Backgrounds** — checkerboard pattern (adapts to light/dark theme) replaces solid backgrounds

## [2.2.0] — 2026-04-03

### Added

- **Bot Data Import** — import community data from Deepbot (CSV + JSON), Streamlabs Chatbot, and generic CSV files
- **4-Step Import Wizard** — select source, upload file, configure conflict strategy, view results
- **Deepbot CSV Parser** — 3-column format (Username, Points, MinutesWatched) with float support
- **Deepbot JSON Parser** — full data including VIP levels, mod status, join dates; VIP 10 correctly mapped as Regular
- **Generic CSV Parser** — user-defined column mapping, header detection, configurable delimiter
- **Conflict Strategies** — Skip, Overwrite, Keep Higher, Add — choose how to handle existing users
- **Auto Column Detection** — headers matching common names (Username, Points, Watchtime) are mapped automatically
- **Imported User ID Resolution** — placeholder IDs (`imported_{username}`) are automatically resolved when users first chat
- **VIP-to-Role Mapping** — Deepbot JSON VIP levels can be mapped to Wrkzg Roles during import
- **Import Preview** — dry-run analysis showing counts before committing
- **Import API** — `POST /api/import/preview`, `POST /api/import/execute`, `POST /api/import/preview-columns`, `GET /api/import/templates`
- **FormData Upload** — `api.upload()` method added to frontend API client

## [2.1.0] — 2026-04-03

### Added

- **Discord Integration** — send messages and rich embeds to Discord channels via webhooks; no Discord bot token needed
- **Discord Effect Types** — `discord.send_message` and `discord.send_embed` available in the Effect System automations
- **Stream Online Event** — `stream.online` EventSub subscription; triggers automations when the stream goes live
- **EventSub → Effect Engine** — all EventSub events (follow, subscribe, gift, resub, raid, stream online) are now dispatched to the Effect Engine for custom automations
- **Integrations Page** — dashboard page with Discord webhook setup, step-by-step instructions, test button, and webhook management
- **Discord Live Notification Example** — quick-start automation template that sends a Discord message when the stream goes live
- **Help entry** for Integrations page

## [2.0.0] — 2026-04-02

### Added

- **Effect System** — visual automation editor with Trigger → Conditions → Effects chains
- **5 Trigger Types** — Chat Command, Twitch Event, Chat Keyword, Hotkey Press, Channel Point Redemption
- **4 Condition Types** — Role Check, Points Check, Random Chance, Stream Status
- **5 Effect Types** — Send Chat Message, Wait (delay), Update Counter, Show Alert, Set Variable
- **Quick-Start Examples** — one-click creation of common automations (Welcome Followers, Lucky Viewer, Raid Alert)
- **Test Button** — simulate any automation trigger without waiting for the real event
- **Effect List API** — full CRUD endpoints at `/api/effects` with types discovery at `/api/effects/types`
- **Cooldown Management** — per-automation cooldowns to prevent spam
- **Variable System** — effects can set variables (`{variable_name}`) used by later effects in the same chain

## [1.9.0] — 2026-04-01

### Added

- **Hotkey Triggers** — map global keyboard shortcuts to bot actions (chat message, counter update)
- **Key Recorder** — visual key combination recorder in the dashboard (no freetext input)
- **Counter Dropdown** — select counters from a dropdown instead of entering IDs manually
- **Auth-Free API Trigger** — `POST /api/hotkeys/{id}/trigger` works without authentication for Stream Deck integration
- **macOS Accessibility Permission** — automatic detection with "Open System Settings" button and permission check
- **Hotkey Bindings API** — full CRUD at `/api/hotkeys` with trigger endpoint

## [1.8.0] — 2026-03-31

### Added

- **Song Requests** — viewers request YouTube songs via `!sr <URL>`; queue management with open/close, skip, clear
- **Song Player Overlay** — OBS Browser Source with Apple Music inspired design; full mode (440x100) and slim mode (380x48, `?mode=slim`)
- **Song Request Commands** — `!sr`, `!skip`, `!queue`, `!currentsong` with aliases
- **Queue Settings** — max duration, max per user, points cost; queue closed by default
- **Customizable Messages** — all bot responses configurable via Messages modal
- **Auth-Free Overlay Data** — `/api/overlays/data/song-queue` for overlay access without token

### Fixed

- **YouTube thumbnails blocked by CSP** — added `img.youtube.com` and `i.ytimg.com` to `img-src`
- **SQLite ORDER BY DateTimeOffset** — replaced with `ORDER BY Id` to avoid `NotSupportedException`

## [1.7.0] — 2026-03-30

### Added

- **Stream Analytics** — automatic stream session tracking with minute-by-minute viewer snapshots
- **Category Tracking** — automatic detection of game/category changes with time segments
- **Analytics Dashboard** — three tabs: Overview (KPIs, viewer trends, stream hours), Categories (pie chart, breakdown table), Stream History (session explorer with viewer chart and category timeline)
- **StreamAnalyticsService** — IHostedService polling Twitch API every 60 seconds while live

## [1.6.0] — 2026-03-30

### Added

- **Chat Games** — 5 points-based games: Heist (group), Duel (1v1), Slots (solo), Roulette (group), Trivia (group)
- **Game Configuration** — per-game settings (cooldown, bet limits, multipliers, join duration, success rate)
- **Customizable Game Messages** — every bot response is configurable via Messages modal with variable reference
- **Custom Trivia Questions** — add your own questions alongside built-in ones
- **Role-Based Access** — optionally restrict games to minimum community role
- **Chat Games Dashboard** — enable/disable toggle, settings, messages, trivia question management per game

## [1.5.0] — 2026-03-29

### Added

- **Channel Point Rewards** — sync Twitch channel point rewards, configure bot actions per reward (chat message, counter update, overlay alert)
- **Roles & Ranks** — community role system with auto-assign criteria (watch time, points, messages, subscriber status)
- **Role Priority** — higher priority roles grant more privileges; color-coded display
- **Re-evaluate All** — bulk check all users against auto-assign criteria
- **Channel Points Dashboard** — sync, add handler, toggle handlers
- **Roles Dashboard** — create/edit/delete roles with auto-assign configuration

## [1.4.1] — 2026-03-28

### Added

- **OBS Overlay Browser Sources** — 6 real-time overlay types for OBS Studio:
  - Alert Box: animated follow/sub/raid notifications with configurable animations (slideDown, fadeIn, bounceIn, zoomIn) and event queue
  - Chat Box: live chat display with role-colored usernames, text shadows, and auto-fade
  - Poll Overlay: animated bar chart with live vote updates and countdown timer
  - Raffle Overlay: winner reveal animation with confetti effect
  - Counter Overlay: single counter display with animated value changes (URL param: ?id=)
  - Event List: scrolling recent events feed with slide-in animation
- **Overlays Dashboard Page** — configure all overlay types with live iframe preview, Copy URL button, and settings modal (font size, colors, animations, message templates)
- **SignalR Dual Groups** — dashboard and overlay clients in separate groups; overlays connect without auth token (`?source=overlay`)
- **Overlay Settings API** — GET/PUT per overlay type with defaults, read-only accessible without auth for OBS Browser Sources
- **Frontend Reorganization** — component library (PageHeader, Button, Card, Modal, Toggle, Badge, EmptyState, DataTable, SearchInput, FormField, Toast, ConfirmDialog, UpdateBanner), grouped sidebar, slim page shells, centralized API client
- **GitHub Update Check** — banner notification when new release is available, dismissable per version

### Changed

- **Default port changed from 5000 to 5050** — avoids conflict with macOS AirPlay Receiver (ControlCenter) which listens on port 5000; port is configurable via `Bot:Port` in appsettings.json
- All pages decomposed into feature subcomponents (max 150 lines per page shell)
- All browser `confirm()` dialogs replaced with custom ConfirmDialog component
- All tables wrapped in DataTable component with horizontal scroll on narrow viewports
- Dashboard StatusCards: replaced "SignalR" with "Stream" info (live/offline + game + uptime)
- Sidebar navigation grouped into logical sections (Chat, Engagement, Automation, Stream, Moderation)
- Overlay URL generator now uses dynamic host from request (no hardcoded port)
- API client uses `response.text()` + `JSON.parse()` instead of `response.json()` for WKWebView compatibility

### Fixed

- **WKWebView Headers error** — "The string did not match the expected pattern" when cancelling raffles; caused by WKWebView rejecting the `Headers` class constructor; fetch patch now uses plain `Record<string, string>` objects
- **Empty response body parsing** — `response.json()` failed in WKWebView on endpoints returning `Results.Ok()` without a body (e.g. raffle cancel, raffle end)
- **Overlay config key mismatches** — AlertOverlay used `"alert"` (singular) and EventListOverlay used `"eventlist"`, but backend expects `"alerts"` and `"events"`
- **Browser caching of error responses** — Added `Cache-Control: no-cache` to overlay routes; prevents browsers from serving stale 403/error responses after bot restart
- **Overlay reconnect reliability** — Health poll uses cache-busting timestamps (`?_=${Date.now()}`) to prevent browser caching; polls every 10 seconds

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
