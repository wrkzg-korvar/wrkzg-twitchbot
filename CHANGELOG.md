# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
