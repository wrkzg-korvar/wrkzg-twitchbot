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
    "scopes": ["chat:read", "chat:edit"]
  },
  "broadcaster": {
    "tokenType": "broadcaster",
    "isAuthenticated": true,
    "twitchUsername": "mystreamname",
    "twitchUserId": "987654321",
    "scopes": ["moderator:read:followers", "channel:read:polls", "channel:manage:polls", "bits:read", "channel:read:subscriptions"]
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
