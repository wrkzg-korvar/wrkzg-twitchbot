# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.4.4] — 2026-05-19

### Fixed
- **Stream Analytics — Session Lifecycle:** Stream sessions now close reliably when the stream goes offline. Previously, sessions could remain open for days if the Twitch API poll failed during the offline transition (the cached stream status retained the last successful "live" response indefinitely). Four interconnected bugs were fixed:
  - `StreamStatusProvider` now resets to offline after 3 consecutive API poll failures (previously it served stale "live" data indefinitely on error).
  - `StreamAnalyticsService` now detects when the Twitch Stream ID changes between polls and closes the old session before opening a new one.
  - On startup, sessions that have been open for more than 48 hours are automatically closed using the timestamp of the last recorded viewer snapshot as the estimated end time.
  - On graceful shutdown, any active session is now properly closed instead of being left open with `EndedAt = null`.
- **Stream Analytics — Shutdown Guard:** The `StopAsync` method now performs a database-level check for any unclosed session in addition to the in-memory reference check. This catches edge cases where the in-memory session reference was lost but the database still had an open session with `EndedAt = null`.
- **Program.cs:** Eliminated duplicate endpoint registration that existed in both branches of the wwwroot if/else block. Endpoints are now registered exactly once.
- **Custom Rewards Logging:** Non-affiliate/non-partner streamers no longer produce `Warning`-level log entries when the Channel Points page fetches custom rewards (403 Forbidden is expected and now logged at `Debug` level).
- **Analytics Repository:** `GetSessionsSinceAsync` no longer loads all stream sessions into memory for client-side filtering. Uses server-side Ticks comparison where supported.
- **Analytics — Segment Viewer Stats:** Category segments now track average and peak viewers. Previously, `AverageViewers` and `PeakViewers` on `CategorySegment` were never computed — the fields existed in the schema but were always null, causing the Category Breakdown table to show 0 for Avg and Peak columns.
- **Analytics — Pie Chart Tooltip:** Fixed unreadable tooltip text in dark mode. The Recharts Tooltip defaulted to black text against a dark background. All chart tooltips now use theme-aware text colors.
- **Moderation — isMod Property Mismatch:** The Users page queried `isModerator` from the bot-requirements API, but the endpoint returns `isMod`. This caused the Twitch Moderation section to always show as disabled even when the bot was a moderator.
- **Users Table — Viewport Height:** The Users table now fills the remaining viewport height and resizes with the window, matching the Dashboard's Live Chat behavior. Previously it was capped at 600px due to the SmartDataTable default `maxHeight`.
- **User Detail Modal — Live Updates:** The modal now fetches its own user data via `useQuery` instead of receiving a static snapshot. After any moderation action (ban, unban, exclude), the modal updates in real-time without needing to be closed and reopened.

### Added
- **Analytics — Category Overlay:** The viewer count chart in Stream History now shows semi-transparent colored background regions for each category segment. Hover any point in the chart to see the viewer count and current category. A matching color legend is displayed below the chart.
- **Analytics — Category Segment Times:** The category breakdown below the viewer chart now shows exact start and end times for each segment alongside the duration.
- **Analytics — Session Stats:** Stream sessions now track unique chatters, total messages, new followers, and new subscribers. Chat stats are accumulated inside the existing `UserStatsBatcher.Enqueue()` call (zero additional per-message overhead), EventSub events (follows/subscriptions) are recorded via the same `ISessionStatsCollector` interface. Data is written to the session when it closes. Displayed as KPI cards in both the Overview and Stream History tabs, and included in session list previews.
- **Analytics — Category Redesign:** The Categories tab now features a horizontal bar chart ranking categories by hours alongside the pie chart, a period selector (7 days to all time), a richer breakdown table with percentage of total time, session count, and a summary footer row. Avg and Peak viewer columns now show actual data from per-segment tracking.
- **Moderation — User Detail Redesign:** The user detail modal now separates "Bot Access" (internal soft-ban with amber toggle) from "Twitch Moderation" (timeout presets, ban, shoutout with purple/red actions). Twitch moderation is disabled with a hint when the bot lacks moderator status.
- **Moderation — Activity History:** Every user's detail modal shows a chronological timeline of all events: timeouts, bans, unbans, shoutouts, follows, subscriptions, gift subs, resubs, and raids. Data sourced from the new ModerationEvent log.
- **Dashboard — Moderation Hub:** The Dashboard is now a two-panel layout: Live Chat (left, ~65%) with per-message quick action menus (Timeout presets, Ban, Shoutout, View Profile) and a Live Viewer List (right, ~35%) showing all current chatters with search, role filters, and quick actions. The Event Feed is compacted into a horizontal ticker. Counters have been removed from the Dashboard (still available on the Counters page).
- **Moderation — Twitch Ban Tracking:** New `IsTwitchBanned` field on the User model. The ban/unban endpoints set this field after successful Twitch API calls. The Users table shows distinct badges: amber "Bot Excluded" for internal bot bans, red "Twitch Banned" for Twitch bans. Both can appear simultaneously.
- **Moderation — Dashboard Unban:** Twitch-banned users can be unbanned directly from the Dashboard's Live Viewer List. The quick action menu shows "Unban" (green) instead of "Ban" (red) when a user is Twitch-banned. A red "BAN" badge appears next to banned viewers.
- **Moderation — Activity History Filters:** The user detail modal's Activity History now has time-based quick filters (30 days, 90 days, 6 months, 1 year, all time) and paginated navigation (15 entries per page) instead of an infinite-growing list.
- **Moderation — Event Logging:** All Twitch moderation actions (timeout, ban, unban, shoutout) and EventSub events (follow, subscribe, gift sub, resub, raid) are logged to the new `ModerationEvent` table with timestamp, actor, reason, and Twitch API result. The log can be cleaned up via `DELETE /api/moderation/log/cleanup`.
- **Moderation — REST API:** New endpoint group `/api/moderation/*` with 8 endpoints: timeout, ban, unban, shoutout, global log, per-user log, log cleanup, and live viewers.

## [2.4.3] — 2026-05-09

### Fixed — 2026-05-17
- **Import:** Command triggers from DeepBot config imports are now lowercased to match the trigger-lookup convention. Imports with mixed-case triggers no longer silently bypass the duplicate check.
- **Follower Status:** EventSub follow events now write `FollowDate` to the user record. Existing followers without a `FollowDate` are verified once per session via the Helix follower check when they first chat. Commands gated to the `Follower` permission level now correctly recognise actual followers (previously every follower was treated as `Everyone`).
- **/me Commands:** Custom command responses starting with `/me ` are now sent as IRC ACTION messages (`.me` via TwitchLib) and display as italic/coloured action text in chat instead of literal `/me` text.
- **Slots Per-User Cooldown:** Slots now honours the `Games.Slots.UserCooldown` setting — previously the configured value was ignored and the cooldown always fell back to the hardcoded 10s default.
- **Game Cooldown Priority:** Roulette, Duel, Heist and Trivia now check the per-user cooldown *before* the global cooldown. Previously the global cooldown shadowed the per-user one (defaults: Roulette/Duel 60s global vs 30s user, Heist 300s vs 30s, Trivia 30s vs 30s), so the per-user message was never shown.
- **Slots UI:** Removed the meaningless "Global CD" input for Slots (stateless game with no shared round). The remaining cooldown input is now labelled simply "Cooldown".

### Added — 2026-05-17
- **Random Command Responses:** Separate multiple responses in a command's response template with `|||` for random selection. Example: `Hello! ||| Hi there! ||| Hey!` rolls a random greeting on each use.
- **{counter:name} Template Variable:** Custom commands can reference and auto-increment counters. Example: `!deaths` with response `RIP! Death count: {counter:deaths}` bumps the `deaths` counter on every invocation and substitutes the new value into the response.
- **Resizable Response Field:** The command form's Response input is now a multi-line, vertically resizable textarea spanning the full width of the form. Multi-line responses (using `|||`) are easier to author.
- **Per-User Game Cooldowns:** All chat games (Slots, Roulette, Duel, Heist, Trivia) now support per-user cooldowns in addition to global cooldowns. Configurable via the Chat Games page.
- **Inline Game Settings:** The Chat Games page exposes Global Cooldown, User Cooldown, Min Bet, and Max Bet inputs directly on each game card — no need to dig into the messages modal.

### Added
- **Bot Requirements Panel:** New section on the Settings page showing whether the bot account meets feature-gated requirements. Displays green/red status indicators for Moderator and Follower status with actionable fix instructions (e.g. "Type /mod YOUR_BOT_NAME in your Twitch chat"). Polls every 30 seconds.
- **{target} Command Variable:** Custom commands can now address other users. `!hug krinlin` with template `{user} gives {target} a big hug!` produces "Chuck gives krinlin a big hug!". The `@` prefix is stripped automatically. Works in both custom commands and system command overrides.
- **Bot Follower Status Check:** `IsBotFollower` property on `ITwitchChatClient`, checked once via Helix API (`GET /channels/followers`) after IRC connect. Cached for the session — no repeated API calls. Displayed in the Bot Requirements panel.

### Changed
- **Command Trigger Editing:** Triggers and aliases on existing commands are now editable at any time. Previously the trigger input was read-only after creation. Backend validates trigger uniqueness on update to prevent duplicates.
- **Bot Emote Separation:** Emotes are now tagged with an `owner` field (`bot`, `broadcaster`, or `shared`). The EmotePicker filters available emotes based on the selected "Send as" account — selecting Bot shows only emotes the bot can actually use. Global emotes are available to both accounts.

### Security — 2026-05-17
- Pinned all npm dependencies to exact versions (removed caret/tilde ranges)
- Updated `vite` to `7.3.3` (fixes GHSA-4w7w-66w2-5vf9, GHSA-v2wj-q39q-566r, GHSA-p9ff-h696-f583)
- Pinned all GitHub Actions to full commit SHA (prevents mutable tag attacks)
- Added `--ignore-scripts` to all CI/CD `npm ci` calls (blocks lifecycle hook attacks)
- Added least-privilege `permissions` to all workflow jobs

## [2.4.2] — 2026-05-09

### Added
- **Diagnostic Logging:** Structured log files are now written to the Wrkzg data directory with 7-day rolling retention (50 MB per day). A "Download Diagnostic Log" button on the Settings page enables easy log file sharing for bug reports.
- **Diagnostics API:** `GET /api/diagnostics/log` returns the current log file as a download. `GET /api/diagnostics/log/entries?count=100` returns the last N log lines as JSON.
- **StreamStatusProvider:** New centralized singleton service that caches stream live status. All background services consume from this cache instead of making independent Helix API calls.
- **UserStatsBatcher:** Batches per-message user stat updates and flushes them to the database every 30 seconds.

### Fixed
- **Watchtime Tracking:** Users who are in chat but not actively sending messages now correctly receive watchtime and points. The bot polls the Twitch Helix "Get Chatters" endpoint every 60 seconds to detect all viewers, not just message senders. Requires the `moderator:read:chatters` scope — re-connect your Bot account in Settings to grant this scope.
- **Performance:** Eliminated redundant Twitch Helix API calls. Three separate services were independently polling stream status (4+ API calls/minute for the same data). A centralized StreamStatusProvider reduces this to exactly 1 call/minute.
- **Performance:** Per-message database writes for user stats (message count, last seen, role sync) are now batched every 30 seconds instead of written individually for each chat message. In active chats, this reduces database writes from 50+/minute to ~2/minute.
- **Users Page:** Server-side pagination now works correctly. Previously, the frontend loaded all users at once (pageSize: 10000) and paginated client-side. Search, sort, and page changes now query the backend API directly.
- **SmartDataTable Headers:** Section headers above SmartDataTable (System Commands, Raffle History, Poll History) no longer produce double borders and misaligned rounding. Components are wrapped in a single `overflow-hidden` container with the new `containerClassName` prop.
- **Raffle Form:** Added proper visible labels, duration preset buttons (None / 1 min / 2 min / 5 min), and helper text. Raw seconds input replaced with intuitive button selection.
- **Poll Form:** Added proper visible labels. Removed the permanently disabled "Start Twitch Poll" button that confused users.
- **Raffles & Polls History:** Empty states now use the consistent EmptyState component. Section order standardized: Create → Active → History → Settings.
- **NuGet Security:** Updated `System.Security.Cryptography.Xml` to 10.0.7 (fixes CVE-2026-26171, CVE-2026-33116). Updated `Microsoft.SourceLink.GitHub` from 8.0.0 to 10.0.203 to eliminate vulnerable transitive dependency.

### Changed
- **SmartDataTable:** New `containerClassName` prop allows parent-controlled border and rounding styles. New `onSortChange` callback enables external (server-side) sort control.
- **Users Page:** Sort by "Last Seen" and "First Seen" columns removed due to a SQLite limitation (`DateTimeOffset` cannot be used in `ORDER BY` clauses). All other sort columns work via server-side sorting.

### Removed
- **Song Request Feature:** Removed entirely — service, API endpoints, system commands (`!sr`, `!skip`, `!queue`, `!currentsong`), dashboard page, OBS overlay, and mini-player. No satisfactory audio playback solution exists within the current architecture constraints. The database table is preserved for future use. The feature will be redesigned in a future version.

## [2.4.0] — 2026-04-11

### Added
- **Twitch Emote Support:** EmoteService loads all available emotes via the User Emotes API (Global, Subscriber, Bits, follower emotes from subscribed channels), EmotePicker in the dashboard chat with category grouping, client-side emote rendering for sent messages
- **User Emotes API:** New `user:read:emotes` scope for Bot + Broadcaster tokens, `GetUserEmotesAsync` with pagination, fallback to Global+Channel for older tokens
- **Visual Automation Builder:** Dual-mode editor (Visual/JSON) with dynamic fields, context-aware variable chips, descriptions, and validation
- **Mod Commands:** !title/!title (change stream title), !game/!category (change category) — require moderator role
- **OBS WebSocket Integration:** Switch scenes and show/hide sources via hotkeys and automations
- **Async Import:** Imports run in the background with progress notifications, module locking during import
- **Notification Center:** Sidebar bell with notification history, replaces transient toast messages
- **Users Overhaul:** SmartDataTable with sorting, search, pagination; user detail modal with point editing and ban/unban
- **Timer Announcements:** Send timed messages as colored announcements (Primary, Blue, Green, Orange, Purple)
- **Dual Helix Client:** Separate IBroadcasterHelixClient + IBotHelixClient with separate token management
- **RFC 7807 Problem Details:** Standardized error responses on all API endpoints
- **SmartDataTable:** Consistent tables with integrated search, sorting, and pagination across all pages
- **Permission Matrix:** Documentation of all role requirements for system commands
- **Channel Point Trigger:** Automations can react to channel point redemptions (was previously implemented but not wired up)
- **Automatic Release Notes:** GitHub Actions generates release notes from CHANGELOG.md

### Fixed
- Command Parser: Correct handling of commands with special characters and Unicode
- CORS: X-Wrkzg-Token header is no longer sent to external API calls (GitHub)
- Windows Taskbar Icon: .ico file correctly embedded in release builds
- Announcement Color: DB migration default corrected (was "" instead of "primary")
- Bot Token Scopes: New scopes require reauthorization (no more silent failures)
- Emote Cache: Frontend-driven refresh when cache is empty — POST /api/emotes/refresh instead of blindly polling the empty GET cache
- Emote Auth Callback: Fire-and-forget Task.Run replaced with awaited call plus logging
- EmoteService Retry: 30s retry for empty initial load, diagnostic token-state logging
- Test Endpoint: Executes only the tested automation (not all)
- Hotkey RunEffect: Executes only the specific automation

### Changed
- ITwitchHelixClient split into IBroadcasterHelixClient (10 consumers) + IBotHelixClient (2 consumers)
- Timer IntervalMinutes: LastFiredAt is no longer set to now for new timers
- Import: Synchronous POST /execute replaced by asynchronous POST /start + job tracking
- showToast() internally redirected to the notification system (0 migration needed)

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
