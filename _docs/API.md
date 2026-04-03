# API Reference

This document describes all REST endpoints and SignalR events exposed by the Wrkzg local API.

> **Base URL:** `http://localhost:5050`
> The port defaults to `5050` (changed from 5000 to avoid macOS AirPlay Receiver conflict). It is configurable via the `Bot.Port` setting.

## Table of Contents

- [General](#general)
- [Setup & Authentication](#setup--authentication)
- [Commands](#commands)
- [Users](#users)
- [Settings](#settings)
- [Status](#status)
- [Window Control](#window-control)
- [Chat](#chat)
- [Bot Control](#bot-control)
- [Polls](#polls)
- [Raffles](#raffles)
- [Timers](#timers)
- [Counters](#counters)
- [Quotes](#quotes)
- [Notifications](#notifications)
- [Overlays](#overlays)
- [Spam Filter](#spam-filter)
- [Effects (Automations)](#effects-automations)
- [Integrations](#integrations)
- [Import](#import)
- [Assets](#assets)
- [Custom Overlays](#custom-overlays)
- [SignalR — Real-Time Events](#signalr--real-time-events)

---

## General

### Request Format

All request bodies are JSON. Set `Content-Type: application/json` on requests with a body.

### Response Format

All responses return JSON. Standard HTTP status codes:

| Code | Meaning |
|---|---|
| `200 OK` | Request succeeded |
| `201 Created` | Resource created |
| `204 No Content` | Request succeeded, no body |
| `400 Bad Request` | Validation failed |
| `404 Not Found` | Resource not found |
| `500 Internal Server Error` | Unexpected server error |

---

## Setup & Authentication

The API is **local-only** (`localhost`) and does not require an API key.

Twitch OAuth is handled via dedicated endpoints. The OAuth authorization page opens in the **system's default browser** (not the Photino window), and the app receives the callback on `localhost`.

---

### `GET /auth/setup-status`

Returns whether the initial setup is complete. Used by the frontend to decide whether to show the Setup Wizard or the Dashboard.

**Response `200 OK`:**

```json
{
  "hasCredentials": true,
  "hasBotToken": true,
  "hasBroadcasterToken": false,
  "setupComplete": false
}
```

---

### `POST /auth/credentials`

Saves Twitch Client ID and Client Secret to encrypted OS keychain storage. Called by the Setup Wizard.

**Request Body:**

```json
{
  "clientId": "abc123def456",
  "clientSecret": "secret789xyz"
}
```

**Response `200 OK`:** `{ "saved": true }`

---

### `POST /auth/open-browser/{type}`

Opens the system's default browser with the Twitch OAuth authorization URL. The `type` parameter is either `bot` or `broadcaster`.

**Response `200 OK`:** `{ "opened": true }`

After the user authorizes in their browser, Twitch redirects to `/auth/callback`. The callback page tells the user to close the browser tab. The Wrkzg app updates automatically via SignalR (`AuthStateChanged` event).

---

### `GET /auth/twitch/{type}`

Alternative to `open-browser` — redirects the current request to the Twitch authorization page. Useful for direct browser access.

**Response:** `302 Redirect` → Twitch authorization page

---

### `GET /auth/callback`

OAuth callback endpoint. Called by Twitch after authorization. Do not call this directly.

| Query Param | Type | Description |
|---|---|---|
| `code` | string | Authorization code from Twitch |
| `state` | string | CSRF state token |

**Response:** `200 OK` — HTML page instructing user to close the tab

---

### `GET /auth/status`

Returns the current authentication state for both accounts.

**Response `200 OK`:**

```json
{
  "bot": {
    "tokenType": "bot",
    "isAuthenticated": true,
    "twitchUsername": "mybotname",
    "twitchUserId": "123456789",
    "scopes": ["chat:read", "chat:edit", "user:write:chat"]
  },
  "broadcaster": {
    "tokenType": "broadcaster",
    "isAuthenticated": true,
    "twitchUsername": "mystreamname",
    "twitchUserId": "987654321",
    "scopes": ["moderator:read:followers", "channel:read:polls", "channel:manage:polls", "bits:read", "channel:read:subscriptions", "user:write:chat"]
  }
}
```

---

### `POST /auth/disconnect/{type}`

Revokes the token at Twitch and deletes it from the local keychain.

**Response `200 OK`:** `{ "disconnected": true, "tokenType": "bot" }`

---

## Commands

### `GET /api/commands`

Returns all custom commands.

**Response `200 OK`:**

```json
[
  {
    "id": 1,
    "trigger": "!discord",
    "aliases": ["!dc"],
    "responseTemplate": "Join our Discord: https://discord.gg/example",
    "permissionLevel": 0,
    "globalCooldownSeconds": 30,
    "userCooldownSeconds": 60,
    "isEnabled": true,
    "useCount": 47,
    "createdAt": "2026-03-01T12:00:00Z"
  }
]
```

**Permission Levels:** `0` Everyone, `1` Follower, `2` Subscriber, `3` Moderator, `4` Broadcaster

**Template Variables:** `{user}`, `{count}`, `{points}`, `{watchtime}`, `{random:min:max}`

---

### `GET /api/commands/{id}`

Returns a single command by ID.

---

### `POST /api/commands`

Creates a new custom command.

**Request Body:**

```json
{
  "trigger": "!socials",
  "aliases": ["!links"],
  "responseTemplate": "Follow {user} at twitch.tv/example",
  "permissionLevel": 0,
  "globalCooldownSeconds": 30,
  "userCooldownSeconds": 0
}
```

**Response `201 Created`:** Created command with assigned `id`

---

### `PUT /api/commands/{id}`

Updates an existing command. All fields are optional (partial update).

```json
{
  "responseTemplate": "Updated text",
  "isEnabled": false
}
```

**Response `200 OK`:** Updated command

---

### `DELETE /api/commands/{id}`

**Response `204 No Content`**

---

### `GET /api/commands/system`

Returns the list of built-in system commands.

**Response `200 OK`:**

```json
[
  {
    "trigger": "!commands",
    "aliases": ["!help"],
    "description": "Lists all available commands.",
    "isSystem": true
  },
  {
    "trigger": "!points",
    "aliases": [],
    "description": "Shows your current points.",
    "isSystem": true
  }
]
```

---

## Users

### `GET /api/users`

Returns tracked users. Supports sorting and limiting.

| Query Param | Type | Default | Description |
|---|---|---|---|
| `sortBy` | string | `points` | `points` or `watchtime` |
| `order` | string | `desc` | `asc` or `desc` |
| `limit` | int | `50` | Max results (1–500) |

**Response `200 OK`:**

```json
[
  {
    "id": 1,
    "twitchId": "123456789",
    "username": "viewername",
    "displayName": "ViewerName",
    "points": 4200,
    "watchedMinutes": 360,
    "messageCount": 84,
    "isSubscriber": true,
    "subscriberTier": 1,
    "isMod": false,
    "isBanned": false,
    "lastSeenAt": "2026-03-01T20:00:00Z"
  }
]
```

---

### `GET /api/users/{id}`

Returns a single user by internal ID.

---

### `PUT /api/users/{id}`

Updates a user's points or ban status.

```json
{
  "points": 5000,
  "isBanned": false
}
```

---

## Settings

### `GET /api/settings`

Returns all settings as a flat key-value map.

```json
{
  "Bot.Channel": "yourchannel",
  "Bot.BotUsername": "",
  "Bot.Port": "5050",
  "Points.PerMinute": "10",
  "Points.SubMultiplier": "1.5",
  "Points.FollowBonus": "100",
  "Updater.CheckOnStartup": "true",
  "Updater.AutoInstall": "false"
}
```

---

### `PUT /api/settings`

Updates one or more settings.

```json
{
  "Bot.Channel": "mynewchannel",
  "Points.PerMinute": "15"
}
```

**Response `200 OK`:** Full updated settings map

---

## Status

### `GET /api/status`

Returns the current bot, stream, auth, and platform status.

**Response `200 OK`:**

```json
{
  "bot": { "isConnected": true, "channel": "krinlin" },
  "stream": { "isLive": false, "viewerCount": 0, "title": null, "game": null, "startedAt": null },
  "auth": { "botTokenPresent": true, "broadcasterTokenPresent": true },
  "platform": "macos"
}
```

---

## Window Control

### `POST /api/window/minimize`
Minimizes the application window. **Response `200 OK`**

### `POST /api/window/maximize`
Toggles maximize/restore of the application window. **Response `200 OK`**

### `POST /api/window/close`
Closes the application window and shuts down the app. **Response `200 OK`**

### `POST /api/window/drag-start`
Starts a window drag operation. **Request:** `{ "screenX": 100, "screenY": 200 }` **Response `200 OK`**

### `POST /api/window/drag-move`
Moves the window during drag. **Request:** `{ "screenX": 150, "screenY": 250 }` **Response `200 OK`**

---

## Chat

### `GET /api/chat/recent`

Returns the most recent chat messages from the in-memory buffer (up to 15).

| Query Param | Type | Default | Description |
|---|---|---|---|
| `userId` | string | — | Optional: filter messages by Twitch user ID |

**Response `200 OK`:** Array of chat message objects

---

### `POST /api/chat/send`

Sends a chat message as the bot or broadcaster.

**Request Body:**

```json
{
  "message": "Hello chat!",
  "sendAs": "bot"
}
```

`sendAs`: `"bot"` (default) or `"broadcaster"`

**Response `200 OK`:** `{ "sent": true }`

---

## Bot Control

### `POST /api/bot/connect`

Connects the bot to Twitch IRC. **Response `200 OK`**

---

## Polls

### `GET /api/polls/active`

Returns the currently active poll with vote results. Returns `404` if no poll is active.

### `GET /api/polls/history`

Returns the last 10 completed polls.

### `GET /api/polls/{id}`

Returns a single poll by ID with full results.

### `POST /api/polls`

Creates a new bot-native poll. Fails if another poll is already active.

**Request Body:**

```json
{
  "question": "Best game?",
  "options": ["Minecraft", "Fortnite", "Valorant"],
  "durationSeconds": 60,
  "createdBy": "Dashboard"
}
```

**Response `201 Created`**

### `POST /api/polls/end`

Ends the active poll. **Response `200 OK`** or `400` if no active poll.

### `POST /api/polls/cancel`

Cancels the active poll without results. **Response `200 OK`** or `400`.

### `GET /api/polls/templates`

Returns all poll announcement template definitions with current overrides and defaults.

### `POST /api/polls/templates/reset/{key}`

Removes a custom template override, reverting to default. **Response `200 OK`** or `404`.

---

## Raffles

### `GET /api/raffles/active`

Returns the currently active raffle with entries, draws, and pending winner. Returns `404` if none active.

### `GET /api/raffles/history`

Returns the last 10 completed raffles.

### `GET /api/raffles/{id}`

Returns a single raffle by ID with full entries and draw history.

### `POST /api/raffles`

Creates a new raffle.

**Request Body:**

```json
{
  "title": "Gaming Maus Giveaway",
  "keyword": "win",
  "durationSeconds": 120,
  "maxEntries": 50,
  "createdBy": "Dashboard"
}
```

All fields except `title` are optional.

**Response `201 Created`**

### `POST /api/raffles/draw`

Draws a random winner (pending verification). **Response `200 OK`** or `400`.

### `POST /api/raffles/accept`

Accepts the pending winner. Raffle stays open for additional draws. **Response `200 OK`** or `400`.

### `POST /api/raffles/redraw`

Rejects the pending winner and draws a new one.

**Request Body:** `{ "reason": "User not present" }` (optional)

### `POST /api/raffles/end`

Closes the raffle and announces all accepted winners. **Response `200 OK`** or `400`.

### `POST /api/raffles/cancel`

Cancels the raffle without drawing. **Response `200 OK`** or `400`.

### `GET /api/raffles/{id}/draws`

Returns the draw history for a raffle.

### `GET /api/raffles/templates`

Returns all raffle announcement template definitions with current overrides.

### `POST /api/raffles/templates/reset/{key}`

Removes a custom template override. **Response `200 OK`** or `404`.

---

## Timers

### `GET /api/timers`

Returns all timed messages.

### `GET /api/timers/{id}`

Returns a single timer by ID.

### `POST /api/timers`

Creates a new timed message.

**Request Body:**

```json
{
  "name": "Follow Reminder",
  "messages": ["Don't forget to follow!", "Hit that follow button!"],
  "intervalMinutes": 15,
  "minChatLines": 5,
  "isEnabled": true,
  "runWhenOnline": true,
  "runWhenOffline": false
}
```

**Response `201 Created`**

### `PUT /api/timers/{id}`

Updates a timer. All fields are optional (partial update). **Response `200 OK`**

### `DELETE /api/timers/{id}`

**Response `204 No Content`**

---

## Counters

### `GET /api/counters`

Returns all counters.

### `POST /api/counters`

Creates a new counter. The trigger is auto-generated from the name (e.g. `"Deaths"` → `"!deaths"`).

**Request Body:**

```json
{
  "name": "Deaths",
  "value": 0,
  "responseTemplate": "{name}: {value}"
}
```

**Response `201 Created`**

### `PUT /api/counters/{id}`

Updates a counter. **Response `200 OK`**

### `DELETE /api/counters/{id}`

**Response `204 No Content`**

### `POST /api/counters/{id}/increment`

Increments the counter by 1. **Response `200 OK`** with updated counter.

### `POST /api/counters/{id}/decrement`

Decrements the counter by 1. **Response `200 OK`** with updated counter.

### `POST /api/counters/{id}/reset`

Resets the counter to 0. **Response `200 OK`** with updated counter.

---

## Quotes

### `GET /api/quotes`

Returns all quotes. Supports optional `search` query parameter.

### `GET /api/quotes/{id}`

Returns a single quote by ID.

### `GET /api/quotes/random`

Returns a random quote. Returns `404` if no quotes exist.

### `POST /api/quotes`

Creates a new quote.

**Request Body:**

```json
{
  "text": "This is a memorable moment!",
  "addedBy": "username"
}
```

Game name is auto-detected from the live stream if the broadcaster is live.

**Response `201 Created`**

### `DELETE /api/quotes/{id}`

**Response `204 No Content`**

---

## Notifications

### `GET /api/notifications/settings`

Returns notification settings for all event types (follow, subscribe, giftsub, resub, raid).

### `PUT /api/notifications/settings`

Updates notification settings. Each event type has `enabled` (bool) and `template` (string with variables).

### `POST /api/notifications/test/{eventType}`

Sends a test notification to chat. Valid event types: `follow`, `subscribe`, `giftsub`, `resub`, `raid`.

**Response `200 OK`**

---

## Overlays

All overlay endpoints are accessible **without authentication** (for OBS Browser Source compatibility).

### `GET /overlay/health`

Health check for overlay reconnect polling. Returns `200 OK` when the server is running.

### `GET /api/overlays/settings`

Returns all overlay settings grouped by overlay type.

### `GET /api/overlays/settings/{type}`

Returns settings for a specific overlay type. Valid types: `alerts`, `chat`, `poll`, `raffle`, `counter`, `events`.

### `PUT /api/overlays/settings/{type}`

Updates settings for a specific overlay type.

### `GET /api/overlays/url/{type}`

Returns the OBS Browser Source URL and recommended dimensions for an overlay type.

**Response `200 OK`:**

```json
{
  "url": "http://localhost:5050/overlay/alerts",
  "width": 400,
  "height": 200,
  "instructions": "Add as Browser Source in OBS. Set width to 400 and height to 200."
}
```

### `GET /api/overlays/data/poll/active`

Returns the currently active poll data for the poll overlay.

### `GET /api/overlays/data/counters`

Returns all counters for the counter overlay.

### `GET /api/overlays/data/counter/{id}`

Returns a single counter by ID for the counter overlay.

### `POST /api/overlays/test/{eventType}`

Sends a test event to overlay clients via SignalR. Valid types: `follow`, `subscribe`, `raid`, `giftsub`, `resub`.

---

## Spam Filter

### `GET /api/spam-filter`

Returns the current spam filter configuration.

### `PUT /api/spam-filter`

Updates the spam filter configuration.

**Request/Response Body:** Full `SpamFilterConfig` object with all filter settings (links, caps, banned words, emote spam, repetition).

---

## Effects (Automations)

### `GET /api/effects`

Returns all effect lists (automations).

### `GET /api/effects/{id}`

Returns a single effect list by ID.

### `POST /api/effects`

Creates a new effect list.

**Request Body:**
```json
{
  "name": "Welcome Followers",
  "description": "Send welcome message on follow",
  "triggerTypeId": "event",
  "triggerConfig": "{\"event_type\": \"event.follow\"}",
  "conditionsConfig": "[]",
  "effectsConfig": "[{\"type\":\"chat_message\",\"params\":{\"message\":\"Welcome {user}!\"}}]",
  "cooldown": 0
}
```

### `PUT /api/effects/{id}`

Updates an existing effect list. All fields are optional — only provided fields are updated.

### `DELETE /api/effects/{id}`

Deletes an effect list.

### `GET /api/effects/types`

Returns all available trigger types, condition types, and effect types with their parameter keys.

### `POST /api/effects/{id}/test`

Test-triggers an effect list. Builds an intelligent test context from the trigger configuration and runs the full chain.

---

## Integrations

### `GET /api/integrations/discord`

Returns the Discord integration status.

**Response:**
```json
{
  "configured": true,
  "webhookUrlSet": true
}
```

### `PUT /api/integrations/discord`

Configures the Discord webhook URL.

**Request Body:**
```json
{
  "webhookUrl": "https://discord.com/api/webhooks/..."
}
```

### `DELETE /api/integrations/discord`

Removes the Discord webhook URL.

### `POST /api/integrations/discord/test`

Sends a test message to the configured Discord webhook.

**Response:**
```json
{
  "success": true,
  "message": "Test message sent successfully!"
}
```

---

## Import

### `POST /api/import/preview`

Preview an import file without writing to the database. Accepts `multipart/form-data` with `file` and `config` (JSON string).

### `POST /api/import/execute`

Execute the import. Same format as preview. Returns `ImportResult` with counts and errors.

### `POST /api/import/preview-columns`

Detect CSV column structure. Accepts `file`, `hasHeader` (bool), `delimiter` (char). Returns headers and sample rows.

### `GET /api/import/templates`

Returns available import source templates (Deepbot CSV, Deepbot JSON, Streamlabs, Generic CSV).

---

## Assets

### `POST /api/assets/upload/{category}`

Upload a file. Category: `sounds` or `images`. Accepts `multipart/form-data` with `file`. Max 10 MB. Returns `{ fileName, url, category, size }`.

### `GET /api/assets/{category}`

List all uploaded assets in a category. Returns array of `{ fileName, url, size, lastModified }`.

### `DELETE /api/assets/{category}/{fileName}`

Delete an uploaded asset.

---

## Custom Overlays

### `GET /api/custom-overlays`

Returns all custom overlays.

### `GET /api/custom-overlays/{id}`

Returns a single custom overlay including code (HTML, CSS, JS, field definitions).

### `POST /api/custom-overlays`

Creates a new custom overlay.

### `PUT /api/custom-overlays/{id}`

Updates a custom overlay. All fields optional.

### `PUT /api/custom-overlays/{id}/fields`

Updates only the field values (not code).

### `DELETE /api/custom-overlays/{id}`

Deletes a custom overlay.

### `GET /overlay/custom/{id}`

Renders the custom overlay as a full HTML page (for OBS Browser Source). Add `?preview=true` for checkerboard background in the editor.

---

## Overlay Defaults

### `GET /api/overlays/defaults/{type}`

Returns the built-in default settings for an overlay type. Used by the editor's "Reset to Defaults" button.

---

## SignalR — Real-Time Events

Connect to the SignalR hub at `/hubs/chat` for live updates. The hub supports two client groups:

- **Dashboard** — Authenticated with `X-Wrkzg-Token` or `access_token` query param
- **Overlay** — No authentication required, connect with `?source=overlay` query param

### Connecting — Dashboard (TypeScript)

```typescript
import { HubConnectionBuilder } from "@microsoft/signalr";

const connection = new HubConnectionBuilder()
  .withUrl("/hubs/chat", { accessTokenFactory: () => apiToken })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

### Connecting — Overlay (TypeScript)

```typescript
const connection = new HubConnectionBuilder()
  .withUrl("/hubs/chat?source=overlay")
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
  .build();

await connection.start();
```

### Events — Server → Client

#### `ChatMessage`

Fired for every message received in the Twitch chat.

```typescript
connection.on("ChatMessage", (msg: {
  userId: string;
  username: string;
  displayName: string;
  content: string;
  isMod: boolean;
  isSubscriber: boolean;
  isBroadcaster: boolean;
  timestamp: string;
}) => { });
```

#### `BotStatus`

Fired when the bot's IRC connection state changes.

```typescript
connection.on("BotStatus", (status: {
  isConnected: boolean;
  channel: string | null;
  reason?: string;
}) => { });
```

#### `AuthStateChanged`

Fired when a Twitch account is connected, disconnected, or when a token refresh fails.

```typescript
connection.on("AuthStateChanged", (state: {
  tokenType: "bot" | "broadcaster";
  isAuthenticated: boolean;
  twitchUsername: string | null;
  twitchUserId: string | null;
  scopes: string[] | null;
}) => { });
```

#### `ViewerCount`

Fired every 60 seconds while the stream is live.

```typescript
connection.on("ViewerCount", (count: number) => { });
```

#### `FollowEvent`

```typescript
connection.on("FollowEvent", (event: { username: string }) => { });
```

#### `SubscribeEvent`

```typescript
connection.on("SubscribeEvent", (event: { username: string; tier: number }) => { });
```

#### Poll Events

| Event | Payload | Description |
|---|---|---|
| `PollCreated` | `{ id, question, options, durationSeconds, endsAt, createdBy, source }` | New poll started |
| `PollVote` | `{ pollId, optionIndex }` | Vote received |
| `PollEnded` | `{ id, question, isActive, totalVotes, options, winnerIndex }` | Poll ended |

#### Raffle Events

| Event | Payload | Description |
|---|---|---|
| `RaffleCreated` | `{ id, title, keyword, durationSeconds, entriesCloseAt, maxEntries, createdBy, entryCount }` | New raffle started |
| `RaffleEntry` | `{ raffleId, username, entryCount }` | New entry |
| `RaffleDrawPending` | `{ raffleId, winnerName, twitchId, totalEntries, drawNumber }` | Winner drawn, pending verification |
| `RaffleWinnerAccepted` | `{ raffleId, winnerName, drawNumber }` | Winner accepted |
| `RaffleDrawn` | `{ raffleId, winnerName, totalEntries }` | Final winner announced (legacy) |
| `RaffleEnded` | `{ raffleId }` | Raffle closed |
| `RaffleCancelled` | `{ raffleId }` | Raffle cancelled |

#### Counter Events

| Event | Payload | Description |
|---|---|---|
| `CounterUpdated` | `{ counterId, name, value }` | Counter value changed |

#### Stream Events

| Event | Payload | Description |
|---|---|---|
| `StreamOnline` | `{ broadcaster, timestamp }` | Stream went live |
