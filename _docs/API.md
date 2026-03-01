# API Reference

This document describes all REST endpoints and SignalR events exposed by the Wrkzg local API.

> **Base URL:** `http://localhost:5000`  
> The port defaults to `5000` in development. In production it is stored in the bot's settings and may differ.

## Table of Contents

- [General](#general)
- [Authentication](#authentication)
- [Status](#status)
- [Commands](#commands)
- [Users](#users)
- [Raffles](#raffles)
- [Polls](#polls)
- [Settings](#settings)
- [SignalR — Real-Time Events](#signalr--real-time-events)

---

## General

### Request Format

All request bodies are JSON. Set `Content-Type: application/json` on requests with a body.

### Response Format

All responses return JSON. Successful responses use standard HTTP status codes:

| Code | Meaning |
|---|---|
| `200 OK` | Request succeeded, body contains result |
| `201 Created` | Resource created, body contains new resource |
| `204 No Content` | Request succeeded, no body |
| `400 Bad Request` | Validation failed, body contains error details |
| `404 Not Found` | Resource not found |
| `500 Internal Server Error` | Unexpected server error |

### Error Response

```json
{
  "errors": {
    "trigger": ["Trigger must start with ! and contain only alphanumeric characters."]
  }
}
```

---

## Authentication

The API is **local-only** (`localhost`) and does not require an API key for dashboard requests.

Twitch OAuth is handled via dedicated endpoints that open the Twitch authorization page in a new browser window.

---

### `GET /auth/twitch/bot`

Starts the OAuth flow for the **bot account** (IRC chat permissions).

Opens the Twitch authorization page. After the user authorizes, Twitch redirects to `/auth/callback` and the token is stored securely.

**Response:** `302 Redirect` → Twitch authorization page

---

### `GET /auth/twitch/broadcaster`

Starts the OAuth flow for the **broadcaster account** (Helix API, polls, EventSub).

**Response:** `302 Redirect` → Twitch authorization page

---

### `GET /auth/callback`

OAuth callback endpoint. Handled automatically by Twitch after authorization — do not call this directly.

| Query Param | Type | Description |
|---|---|---|
| `code` | string | Authorization code from Twitch |
| `state` | string | Token type (`bot` or `broadcaster`) |

**Response:** `200 OK` — HTML page that closes the browser window

---

## Status

### `GET /api/status`

Returns the current connection and stream status.

**Response `200 OK`:**

```json
{
  "bot": {
    "isConnected": true,
    "channel": "yourchannel",
    "botUsername": "yourbotname",
    "connectedAt": "2026-03-01T18:00:00Z"
  },
  "stream": {
    "isLive": true,
    "viewerCount": 142,
    "title": "Chill coding stream",
    "game": "Software and Game Development",
    "startedAt": "2026-03-01T17:00:00Z"
  },
  "auth": {
    "botTokenPresent": true,
    "broadcasterTokenPresent": true
  }
}
```

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

**Permission Levels:**

| Value | Label |
|---|---|
| `0` | Everyone |
| `1` | Follower |
| `2` | Subscriber |
| `3` | Moderator |
| `4` | Broadcaster |

**Response Template Variables:**

| Variable | Replaced with |
|---|---|
| `{user}` | Display name of the user who triggered the command |
| `{count}` | Number of messages the user has sent |
| `{uptime}` | Current stream uptime (e.g. `2h 14m`) |
| `{random:min:max}` | Random integer between min and max (inclusive) |

---

### `GET /api/commands/{id}`

Returns a single command by ID.

**Response `200 OK`:** Single command object (see above)  
**Response `404 Not Found`**

---

### `POST /api/commands`

Creates a new custom command.

**Request Body:**

```json
{
  "trigger": "!socials",
  "aliases": ["!links", "!social"],
  "responseTemplate": "Follow {user} at twitch.tv/example — Twitter: @example",
  "permissionLevel": 0,
  "globalCooldownSeconds": 30,
  "userCooldownSeconds": 0
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `trigger` | string | ✅ | Must start with `!`, alphanumeric + underscore only |
| `aliases` | string[] | — | Same constraints as `trigger` |
| `responseTemplate` | string | ✅ | Max 500 characters |
| `permissionLevel` | int | ✅ | `0`–`4` |
| `globalCooldownSeconds` | int | ✅ | `>= 0` |
| `userCooldownSeconds` | int | ✅ | `>= 0` |

**Response `201 Created`:** Created command object with assigned `id`  
**Response `400 Bad Request`:** Validation errors

---

### `PUT /api/commands/{id}`

Updates an existing command. All fields are optional — only include fields you want to change.

**Request Body (partial update):**

```json
{
  "responseTemplate": "Updated response text",
  "isEnabled": false
}
```

**Response `200 OK`:** Updated command object  
**Response `404 Not Found`**

---

### `DELETE /api/commands/{id}`

Deletes a command permanently.

**Response `204 No Content`**  
**Response `404 Not Found`**

---

## Users

### `GET /api/users`

Returns all tracked users. Supports optional query parameters for sorting and filtering.

| Query Param | Type | Default | Description |
|---|---|---|---|
| `sortBy` | string | `points` | `points`, `watchtime`, `messages`, `username` |
| `order` | string | `desc` | `asc` or `desc` |
| `limit` | int | `50` | Max results (1–500) |
| `search` | string | — | Filter by username (partial match) |

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
    "followDate": "2025-01-15T00:00:00Z",
    "isSubscriber": true,
    "subscriberTier": 1,
    "isMod": false,
    "isBanned": false,
    "firstSeenAt": "2025-01-15T18:00:00Z",
    "lastSeenAt": "2026-03-01T20:00:00Z"
  }
]
```

---

### `GET /api/users/{id}`

Returns a single user by internal ID.

**Response `200 OK`:** Single user object  
**Response `404 Not Found`**

---

### `PUT /api/users/{id}`

Updates a user's points or ban status. Only the fields listed below can be modified.

**Request Body:**

```json
{
  "points": 5000,
  "isBanned": false
}
```

| Field | Type | Description |
|---|---|---|
| `points` | long | Set the user's point balance directly |
| `isBanned` | bool | Ban or unban the user from earning points and chat games |

**Response `200 OK`:** Updated user object  
**Response `404 Not Found`**

---

### `POST /api/users/{id}/points`

Add or subtract points from a user (relative, not absolute).

**Request Body:**

```json
{
  "amount": 500,
  "reason": "Manual bonus"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `amount` | long | ✅ | Positive to add, negative to subtract |
| `reason` | string | — | Optional note shown in logs |

**Response `200 OK`:**

```json
{
  "userId": 1,
  "previousPoints": 4200,
  "newPoints": 4700,
  "delta": 500
}
```

---

## Raffles

### `GET /api/raffles`

Returns all raffles (open and closed).

**Response `200 OK`:**

```json
[
  {
    "id": 1,
    "title": "Game key giveaway",
    "isOpen": false,
    "entryCount": 23,
    "winner": {
      "id": 7,
      "displayName": "LuckyViewer"
    },
    "createdAt": "2026-03-01T19:00:00Z",
    "closedAt": "2026-03-01T19:15:00Z"
  }
]
```

---

### `GET /api/raffles/active`

Returns the currently open raffle, if one exists.

**Response `200 OK`:** Single raffle object  
**Response `404 Not Found`** if no raffle is currently open

---

### `POST /api/raffles`

Creates and opens a new raffle. Only one raffle can be open at a time.

**Request Body:**

```json
{
  "title": "Steam game giveaway",
  "keyword": "!join",
  "subscriberMultiplier": 2
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `title` | string | ✅ | Displayed in the dashboard |
| `keyword` | string | ✅ | Chat command viewers use to enter (e.g. `!join`) |
| `subscriberMultiplier` | int | — | Subscribers get this many tickets (default: `1`) |

**Response `201 Created`:** Created raffle object  
**Response `400 Bad Request`:** If a raffle is already open

---

### `PUT /api/raffles/active/draw`

Draws a winner from the current open raffle and closes it.

**Response `200 OK`:**

```json
{
  "raffleId": 1,
  "winner": {
    "id": 7,
    "displayName": "LuckyViewer",
    "ticketCount": 2
  },
  "totalEntries": 23,
  "totalTickets": 31
}
```

**Response `404 Not Found`:** If no raffle is open  
**Response `400 Bad Request`:** If no entries exist

---

### `DELETE /api/raffles/active`

Cancels the active raffle without drawing a winner.

**Response `204 No Content`**  
**Response `404 Not Found`**

---

## Polls

### `GET /api/polls`

Returns all polls.

**Response `200 OK`:**

```json
[
  {
    "id": 1,
    "question": "What game should I play next?",
    "options": ["Hollow Knight", "Hades", "Dead Cells"],
    "results": [12, 8, 5],
    "isActive": false,
    "source": "BotNative",
    "createdAt": "2026-03-01T18:30:00Z",
    "endsAt": "2026-03-01T18:35:00Z"
  }
]
```

**Poll Sources:**

| Value | Description |
|---|---|
| `BotNative` | Chat-based poll managed by the bot |
| `TwitchNative` | Twitch native poll created via Helix API |

---

### `GET /api/polls/active`

Returns the currently active poll, if one exists.

**Response `200 OK`:** Single poll object  
**Response `404 Not Found`**

---

### `POST /api/polls`

Creates a new poll. Only one poll can be active at a time.

**Request Body:**

```json
{
  "question": "Should I do a viewer game session?",
  "options": ["Yes!", "No", "Later"],
  "durationSeconds": 120,
  "useNativeTwitchPoll": false
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `question` | string | ✅ | Max 200 characters |
| `options` | string[] | ✅ | 2–5 options, max 50 chars each |
| `durationSeconds` | int | ✅ | `15`–`1800` |
| `useNativeTwitchPoll` | bool | — | Creates a Twitch native poll if `true` (requires broadcaster token) |

**Response `201 Created`:** Created poll object  
**Response `400 Bad Request`:** Validation errors or poll already active

---

### `PUT /api/polls/active/end`

Ends the active poll immediately and returns the final result.

**Response `200 OK`:**

```json
{
  "pollId": 1,
  "question": "Should I do a viewer game session?",
  "results": [
    { "option": "Yes!", "votes": 31, "percentage": 62.0 },
    { "option": "No",   "votes": 12, "percentage": 24.0 },
    { "option": "Later","votes": 7,  "percentage": 14.0 }
  ],
  "winner": "Yes!",
  "totalVotes": 50
}
```

**Response `404 Not Found`**

---

## Settings

### `GET /api/settings`

Returns all current bot settings as a flat key-value map.

**Response `200 OK`:**

```json
{
  "Bot.Channel": "yourchannel",
  "Bot.BotUsername": "yourbotname",
  "Points.PerMinute": "10",
  "Points.SubMultiplier": "1.5",
  "Updater.CheckOnStartup": "true"
}
```

---

### `PUT /api/settings`

Updates one or more settings. Only include the keys you want to change.

**Request Body:**

```json
{
  "Points.PerMinute": "15",
  "Points.SubMultiplier": "2.0"
}
```

**Response `200 OK`:** Full updated settings map

---

### Available Settings Keys

| Key | Type | Default | Description |
|---|---|---|---|
| `Bot.Channel` | string | `""` | Twitch channel name (lowercase) |
| `Bot.BotUsername` | string | `""` | Bot's Twitch username |
| `Bot.Port` | int | `5000` | Local port for the dashboard |
| `Points.PerMinute` | int | `10` | Points awarded per minute while stream is live |
| `Points.SubMultiplier` | float | `1.5` | Point multiplier for subscribers |
| `Points.FollowBonus` | int | `100` | One-time bonus points awarded on follow |
| `Updater.CheckOnStartup` | bool | `true` | Check for updates when the bot starts |
| `Updater.AutoInstall` | bool | `false` | Install updates without prompting |

---

## SignalR — Real-Time Events

Connect to the SignalR hub at `/hubs/chat` for live dashboard updates.

### Connecting (TypeScript / React)

```typescript
import { HubConnectionBuilder } from "@microsoft/signalr";

const connection = new HubConnectionBuilder()
  .withUrl("/hubs/chat")
  .withAutomaticReconnect()
  .build();

await connection.start();
```

---

### Events — Server → Client

The server pushes the following events to all connected dashboard clients.

---

#### `ChatMessage`

Fired for every message received in the Twitch chat.

```typescript
connection.on("ChatMessage", (message: {
  userId: string;
  username: string;
  displayName: string;
  content: string;
  isMod: boolean;
  isSubscriber: boolean;
  isBroadcaster: boolean;
  timestamp: string; // ISO 8601
}) => { });
```

---

#### `ViewerCount`

Fired every 60 seconds while the stream is live.

```typescript
connection.on("ViewerCount", (count: number) => { });
```

---

#### `FollowEvent`

Fired when a new user follows the channel (via EventSub).

```typescript
connection.on("FollowEvent", (event: {
  userId: string;
  username: string;
  displayName: string;
  followedAt: string;
}) => { });
```

---

#### `SubscribeEvent`

Fired when a user subscribes or re-subscribes.

```typescript
connection.on("SubscribeEvent", (event: {
  userId: string;
  username: string;
  displayName: string;
  tier: number;       // 1, 2, or 3
  isGift: boolean;
  months: number;     // cumulative months subscribed
}) => { });
```

---

#### `RaidEvent`

Fired when another streamer raids the channel.

```typescript
connection.on("RaidEvent", (event: {
  fromUsername: string;
  displayName: string;
  viewerCount: number;
}) => { });
```

---

#### `BotStatus`

Fired when the bot's connection state changes.

```typescript
connection.on("BotStatus", (status: {
  isConnected: boolean;
  channel: string;
  reason?: string;    // present on disconnect
}) => { });
```

---

#### `PointsAwarded`

Fired every time points are awarded in bulk (once per tracking interval).

```typescript
connection.on("PointsAwarded", (event: {
  userCount: number;
  pointsPerUser: number;
  timestamp: string;
}) => { });
```

---

#### `UpdateAvailable`

Fired on startup if a newer version is available on GitHub Releases.

```typescript
connection.on("UpdateAvailable", (info: {
  currentVersion: string;
  latestVersion: string;
  releaseNotes: string;
  downloadUrl: string;
}) => { });
```

---

### Full Connection Example (React Hook)

```typescript
// src/hooks/useSignalR.ts
import { useEffect, useRef, useState } from "react";
import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

export function useSignalR() {
  const connectionRef = useRef<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const conn = new HubConnectionBuilder()
      .withUrl("/hubs/chat")
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = conn;

    conn.start()
      .then(() => setIsConnected(true))
      .catch(console.error);

    conn.onclose(() => setIsConnected(false));
    conn.onreconnected(() => setIsConnected(true));

    return () => { conn.stop(); };
  }, []);

  function on<T>(event: string, handler: (data: T) => void) {
    connectionRef.current?.on(event, handler);
  }

  function off(event: string) {
    connectionRef.current?.off(event);
  }

  return { isConnected, on, off };
}
```
