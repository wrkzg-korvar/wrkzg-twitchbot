# API Reference

This document describes all REST endpoints and SignalR events exposed by the Wrkzg local API.

> **Base URL:** `http://localhost:5000`
> The port defaults to `5000`. It is configurable via the `Bot.Port` setting.

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
- [Spam Filter](#spam-filter)
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
  "Bot.Port": "5000",
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

## Spam Filter

### `GET /api/spam-filter`

Returns the current spam filter configuration.

### `PUT /api/spam-filter`

Updates the spam filter configuration.

**Request/Response Body:** Full `SpamFilterConfig` object with all filter settings (links, caps, banned words, emote spam, repetition).

---

## SignalR — Real-Time Events

Connect to the SignalR hub at `/hubs/chat` for live dashboard updates.

### Connecting (TypeScript)

```typescript
import { HubConnectionBuilder } from "@microsoft/signalr";

const connection = new HubConnectionBuilder()
  .withUrl("/hubs/chat")
  .withAutomaticReconnect()
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
