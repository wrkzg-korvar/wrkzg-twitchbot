# Wrkzg User Handbook

> Complete guide to every feature in Wrkzg.
> This handbook covers all chat commands, dashboard pages, and configuration options.

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Dashboard Overview](#2-dashboard-overview)
3. [Chat & Commands](#3-chat--commands)
   - [3.1 Custom Commands](#31-custom-commands)
   - [3.2 System Commands Reference](#32-system-commands-reference)
   - [3.3 Quotes](#33-quotes)
4. [Community Engagement](#4-community-engagement)
   - [4.1 Points System](#41-points-system)
   - [4.2 Polls & Votes](#42-polls--votes)
   - [4.3 Raffles & Giveaways](#43-raffles--giveaways)
   - [4.4 Channel Point Rewards](#44-channel-point-rewards)
   - [4.5 Roles & Ranks](#45-roles--ranks)
   - [4.6 Chat Games](#46-chat-games)
   - [4.7 Song Requests](#47-song-requests)
5. [Automation](#5-automation)
   - [5.1 Timed Messages](#51-timed-messages)
   - [5.2 Event Notifications](#52-event-notifications)
   - [5.3 Hotkey Triggers](#53-hotkey-triggers)
   - [5.4 Effect System (Automations)](#54-effect-system-automations)
   - [5.5 Integrations](#55-integrations)
6. [Moderation](#6-moderation)
   - [6.1 Spam Filter](#61-spam-filter)
7. [Stream Analytics](#7-stream-analytics)
8. [OBS Overlays](#8-obs-overlays)
   - [8.1 Setup](#81-setup-same-for-all-overlays)
   - [8.2 Overlay Editor](#82-overlay-editor)
   - [8.3 Alert Box](#83-alert-box)
   - [8.4 Chat Box](#84-chat-box)
   - [8.5 Poll Overlay](#85-poll-overlay)
   - [8.6 Raffle Overlay](#86-raffle-overlay)
   - [8.7 Counter Overlay](#87-counter-overlay)
   - [8.8 Event List](#88-event-list)
   - [8.9 Song Player](#89-song-player)
   - [8.10 Asset Management](#810-asset-management)
   - [8.11 Google Fonts](#811-google-fonts)
   - [8.12 Custom CSS](#812-custom-css)
   - [8.13 Custom Overlays (Developer Mode)](#813-custom-overlays-developer-mode)
9. [Settings & Configuration](#9-settings--configuration)
10. [Data Management](#10-data-management)
    - [10.1 Import Data from Another Bot](#101-import-data-from-another-bot)
11. [Troubleshooting & FAQ](#11-troubleshooting--faq)

---

## 1. Getting Started

### First-Time Setup

When you launch Wrkzg for the first time, the Setup Wizard guides you through:

1. **Create a Twitch Application** — Go to [dev.twitch.tv](https://dev.twitch.tv/console) and create an app. Copy the Client ID and Client Secret into the wizard.
2. **Connect your Bot Account** — Authorize the Twitch account your bot will use to chat.
3. **Connect your Broadcaster Account** — Authorize your main streaming account for EventSub events (follows, subs, raids).
4. **Select your Channel** — Choose which channel the bot should join.

After setup, the bot connects automatically every time you launch the app.

### Understanding the Dashboard

The Wrkzg window is a single desktop application. The sidebar on the left navigates between features. The main area shows the selected page. Everything updates in real-time — no page refreshes needed.

### Your First Command

1. Go to **Commands** in the sidebar
2. Click **Create Command**
3. Set the trigger to `!hello`
4. Set the response to `Hey {user}, welcome to the stream!`
5. Save it

Now when anyone types `!hello` in chat, the bot responds with their name.

---

## 2. Dashboard Overview

Your command center. The dashboard shows:

- **Bot Status** — Whether the bot is connected to Twitch IRC
- **Stream Status** — Live/offline, viewer count, stream title, current game
- **Live Chat** — Messages appear in real-time with emote rendering and role badges
- **Event Feed** — Recent follows, subscriptions, raids, and gift subs

Use the chat input at the bottom to send messages as your bot account.

---

## 3. Chat & Commands

### 3.1 Custom Commands

Custom commands let you create automated responses to chat triggers. When a viewer types a command like `!discord`, the bot automatically responds.

#### Creating a Command

1. Go to **Commands** in the sidebar
2. Click **Create Command**
3. Fill in:
   - **Trigger** — The command viewers type (must start with `!`)
   - **Response** — What the bot says. Use variables for dynamic content.
   - **Permission** — Who can use this command (Everyone, Follower, Subscriber, Moderator, Broadcaster)
   - **Cooldown** — Minimum seconds between uses (global and per-user)
   - **Aliases** — Alternative triggers (comma-separated)
4. Click **Save**

#### Variables

| Variable | What it shows | Example output |
|---|---|---|
| `{user}` | Viewer's display name | `NightOwl42` |
| `{points}` | Viewer's point balance | `1,250` |
| `{watchtime}` | Total watch time | `12h 30m` |
| `{random:1:6}` | Random number between min and max | `4` |
| `{count}` | Times this command was used | `847` |

#### Examples

| Trigger | Response Template | Chat Output |
|---|---|---|
| `!discord` | `Join our Discord: discord.gg/abc` | `Join our Discord: discord.gg/abc` |
| `!lurk` | `{user} is now lurking!` | `NightOwl42 is now lurking!` |
| `!roll` | `{user} rolled a {random:1:6}!` | `NightOwl42 rolled a 4!` |

#### Tips

- **Aliases are great for shortcuts.** Set `!dc` and `!disc` as aliases for `!discord`.
- **Per-user cooldowns** prevent individuals from spamming, while **global cooldowns** limit overall usage.
- **Mods bypass cooldowns** by default.

### 3.2 System Commands Reference

Built-in commands that come with Wrkzg. You can customize their response text and enable/disable each one, but their core behavior is fixed.

| Command | Aliases | Permission | Description |
|---|---|---|---|
| `!commands` | — | Everyone | Lists all available commands |
| `!points` | `!p` | Everyone | Shows your current points |
| `!watchtime` | `!wt` | Everyone | Shows your total watch time |
| `!followage` | — | Everyone | Shows how long you've been following |
| `!uptime` | `!live` | Everyone | Shows current stream duration |
| `!shoutout` | `!so` | Moderator | Shoutout another streamer with game lookup |
| `!poll` | — | Moderator | Start a poll (`!poll Question \| Opt1 \| Opt2`) |
| `!vote` | — | Everyone | Vote in the active poll (`!vote 1`) |
| `!pollend` | — | Moderator | End the active poll |
| `!pollresults` | — | Everyone | Show poll results |
| `!raffle` | — | Moderator | Start a raffle (`!raffle Prize Name`) |
| `!join` | — | Everyone | Enter the active raffle |
| `!draw` | — | Moderator | Draw a raffle winner |
| `!cancelraffle` | — | Moderator | Cancel the active raffle |
| `!quote` | `!q` | Everyone | Show a random or specific quote |
| `!quote add` | — | Moderator | Save a new quote |
| `!editcmd` | — | Moderator | Edit a command response on the fly |
| `!sr` | `!songrequest` | Everyone | Request a song (`!sr <YouTube URL>`) |
| `!sr open` | — | Moderator | Open the song request queue |
| `!sr close` | — | Moderator | Close the song request queue |
| `!skip` | — | Moderator | Skip the current song |
| `!queue` | `!songlist` | Everyone | Show the next songs in the queue |
| `!currentsong` | `!song`, `!nowplaying` | Everyone | Show what's currently playing |

### 3.3 Quotes

Save memorable chat moments. Quotes are numbered sequentially and can be recalled randomly or by number.

| Command | Description | Example |
|---|---|---|
| `!quote` | Show a random quote | `#42: "That's what she said" — NightOwl42` |
| `!quote 42` | Show quote #42 | `#42: "That's what she said" — NightOwl42` |
| `!quote add That was amazing` | Save a new quote (Mod) | `Quote #43 saved!` |

The current game/category is saved automatically when a quote is added during a live stream.

---

## 4. Community Engagement

### 4.1 Points System

Every viewer who chats earns points and watch time automatically. Points are awarded per minute while the stream is live.

- **Default rate:** 10 points per minute (configurable in Settings)
- **Subscriber multiplier:** 2x (configurable)
- **Watch time:** Tracked in minutes, displayed as hours

Points are visible on the **Users** page and through the `!points` command. They serve as currency for future features like Chat Games and Song Requests.

### 4.2 Polls & Votes

Create live polls that viewers vote on in chat. Results update in real-time.

**From the Dashboard:**
1. Go to **Polls**
2. Click **Create Poll**
3. Enter a question, add 2-10 options, set a duration
4. Click **Start**

**From Chat:**
```
!poll What should we play next? | Valorant | Minecraft | Just Chatting
```

Viewers vote with `!vote 1`, `!vote 2`, etc. Only one poll can be active at a time.

### 4.3 Raffles & Giveaways

Run giveaways with keyword entry, animated winner reveals, and verification.

**Creating a Raffle:**
1. Go to **Raffles** and click **Create Raffle**
2. Set a title, entry keyword (e.g. `!join`), and optional duration
3. Viewers enter by typing the keyword in chat
4. Click **Draw** to pick a winner

**Winner Verification:** After drawing, the winner's name is shown with Accept/Redraw options. If the winner doesn't respond, click Redraw. You can draw multiple winners for multi-item giveaways.

### 4.4 Channel Point Rewards

React to Twitch Channel Point reward redemptions with configurable bot actions.

**Setup:**
1. Go to **Channel Points** in the sidebar
2. Click **Sync from Twitch** to load your channel's custom rewards
3. Click **Add Handler** to configure an action for a reward
4. Choose an action type:
   - **Chat Message** — Send a customizable message (supports `{user}` and `{input}`)
   - **Counter +1 / -1** — Increment or decrement a counter
   - **Overlay Alert** — Show an alert in the Alert Box overlay

Redemptions also appear as alerts in the OBS Alert Box overlay.

### 4.5 Roles & Ranks

Create community roles that users earn based on watch time, points, or messages.

**Creating a Role:**
1. Go to **Roles & Ranks** in the sidebar
2. Click **Create Role**
3. Set a name, priority (higher = more privileges), and color
4. Optionally enable auto-assign with criteria:
   - Minimum watch time (hours)
   - Minimum points
   - Minimum messages
   - Must be subscriber

Roles are evaluated automatically every 60 seconds. Click **Re-evaluate All** to check all users immediately. You can also assign roles manually from the Users page.

### 4.6 Chat Games

Five points-based chat games that drive viewer engagement. Players bet points, compete, and win prizes.

#### Games Overview

| Game | Command | Type | Description |
|---|---|---|---|
| **Heist** | `!heist <amount>` | Group | Join a group heist. After a join phase, each player individually succeeds or fails. Winners get bet x multiplier. |
| **Duel** | `!duel @user <amount>` | 1v1 | Challenge another viewer. They type `!accept` to fight. 50/50 chance. |
| **Slots** | `!slots <amount>` | Solo | Pull the lever. Three symbols roll. Triple match = jackpot (up to 50x). |
| **Roulette** | `!roulette <amount> <red\|black>` | Group | Bet on red or black. Multiple players can bet before the wheel spins. 2x payout. |
| **Trivia** | `!trivia` | Group | Bot asks a question. First correct answer in chat wins points. |

#### Chat Commands

| Command | Description | Permission |
|---|---|---|
| `!heist <amount>` | Join or start a group heist | Everyone |
| `!duel @username <amount>` | Challenge another viewer to a duel | Everyone |
| `!accept` | Accept a pending duel challenge | Everyone |
| `!slots <amount>` | Pull the slot machine lever | Everyone |
| `!roulette <amount> <red\|black>` | Bet on roulette | Everyone |
| `!trivia` | Start a trivia question | Moderator |

#### Game Settings

Each game has configurable settings accessible from the **Chat Games** page:

- **Cooldown** — Seconds between rounds (prevents spam)
- **Min/Max Bet** — Allowed bet range
- **Success Rate** — Win probability (Heist)
- **Multiplier** — Payout multiplier (Heist)
- **Join Duration** — How long the join phase lasts (Heist, Roulette)
- **Accept Timeout** — How long the challenged player has to accept (Duel)
- **Answer Duration** — How long to answer a trivia question
- **Reward** — Points awarded for correct trivia answers
- **Min Players** — Minimum players required (Heist)

Toggle games on/off from the dashboard without changing settings.

#### Customizing Bot Messages

Every message the bot sends during a game can be customized:

1. Go to **Chat Games** in the sidebar
2. Click **Messages** on any game card
3. Edit any message template — use `{variables}` for dynamic content
4. Click **Save Changes**
5. Use the **Reset** button next to any message to restore the default

**Variable Reference (per Game):**

**Heist:** `{user}`, `{amount}`, `{payout}`, `{duration}`, `{count}`, `{survivors}`, `{total}`, `{remaining}`, `{min}`, `{max}`

**Duel:** `{challenger}`, `{target}`, `{amount}`, `{winner}`, `{timeout}`, `{remaining}`, `{min}`, `{max}`

**Slots:** `{s1}`, `{s2}`, `{s3}`, `{payout}`, `{amount}`, `{multiplier}`, `{min}`, `{max}`

**Roulette:** `{user}`, `{amount}`, `{color}`, `{payout}`, `{emoji}`, `{number}`, `{duration}`, `{remaining}`, `{min}`, `{max}`

**Trivia:** `{user}`, `{question}`, `{answer}`, `{category}`, `{duration}`, `{reward}`, `{remaining}`

#### Trivia Questions

Wrkzg comes with built-in trivia questions. You can also add your own:

1. Go to **Chat Games** and click **Trivia Questions**
2. Enter a question, answer, and optional category
3. Click the **+** button to add
4. Custom and built-in questions are mixed randomly during gameplay

#### Tips

- **Start with low bets** while your community learns the games
- **Use roles** (from Roles & Ranks) to restrict games to active viewers
- **Heist is best with 3+ players** — set Min Players accordingly
- **Duel cooldowns** prevent the same two players from dueling non-stop
- **Trivia rewards** should be balanced — not too high or viewers only play trivia
- **Customize messages** in your language — all bot responses are fully translatable

### 4.7 Song Requests

Viewers request songs via YouTube URLs. Songs are queued and can be played through the OBS Song Player overlay.

> **Note:** The song request queue is **closed by default**. Open it from the Song Requests page or with `!sr open` in chat before viewers can request songs.

#### Chat Commands

| Command | Description | Permission |
|---|---|---|
| `!sr <YouTube URL>` | Request a song | Everyone |
| `!sr open` | Open the request queue | Moderator |
| `!sr close` | Close the request queue | Moderator |
| `!skip` | Skip the current song | Moderator |
| `!queue` | Show the next 5 songs in the queue | Everyone |
| `!currentsong` | Show what's currently playing | Everyone |

#### Dashboard

The **Song Requests** page shows:
- **Now Playing** — Current song with skip button
- **Queue** — All pending songs with remove buttons
- **Queue Toggle** — Open/close the queue for new requests
- **Clear Queue** — Remove all pending songs at once

#### OBS Overlay

Add the **Song Player** overlay as a Browser Source in OBS:
1. Go to **Overlays** and copy the Song Player URL
2. In OBS: Sources > + > Browser > paste the URL
3. Recommended size: 480 x 270 (16:9 video)

The overlay auto-plays songs from the queue and advances automatically.

#### Settings

- **Max Duration** — Maximum song length in seconds (default: 600 = 10 minutes)
- **Max Per User** — Maximum songs one viewer can have in the queue (default: 3)
- **Points Cost** — Points required per request (default: 0 = free)

---

## 5. Automation

### 5.1 Timed Messages

Automated recurring messages that only fire when enough chat activity is happening.

**Creating a Timer:**
1. Go to **Timers** in the sidebar
2. Click **Create Timer**
3. Set the interval (e.g. every 15 minutes) and minimum chat lines between fires
4. Add one or more messages — they cycle round-robin
5. Choose when to run: online only, offline only, or both

**Tips:**
- Set minimum chat lines to 5-10 to avoid the bot talking to an empty room
- Use multiple messages per timer to keep things varied
- Toggle timers on/off without deleting them

### 5.2 Event Notifications

Automatic chat announcements for Twitch events.

| Event | Default Message | Variables |
|---|---|---|
| Follow | `Welcome {user}! Thanks for the follow!` | `{user}` |
| Subscribe | `{user} just subscribed (Tier {tier})! Thank you!` | `{user}`, `{tier}` |
| Gift Subs | `{user} gifted {count} Tier {tier} subs! Amazing!` | `{user}`, `{count}`, `{tier}` |
| Resub | `{user} resubscribed for {months} months (Tier {tier})! {message}` | `{user}`, `{months}`, `{tier}`, `{message}` |
| Raid | `{user} is raiding with {viewers} viewers! Welcome raiders!` | `{user}`, `{viewers}` |

Each event type can be toggled on/off and has a customizable message template. Enable **Auto Shoutout** for raids to automatically run `!so` for the raider.

### 5.3 Hotkey Triggers

Map keyboard shortcuts to bot actions. Each hotkey has a unique ID that can be used for API triggers.

**Creating a Hotkey:**
1. Go to **Hotkeys** in the sidebar
2. Click **Add Hotkey**
3. Click **Record** and press your desired key combination
4. Choose an action and configure the payload:
   - **Send Chat Message** — enter the message text
   - **Counter +1 / -1 / Reset** — select a counter from the dropdown
5. Optionally add a description
6. Click **Create**

**Testing:**
Use the **Play button** next to each hotkey to trigger it immediately without pressing the keys.

**API Trigger (No Auth Required):**
Each hotkey shows its ID in the list. Trigger any hotkey via HTTP — no authentication token needed:

```
POST http://localhost:5050/api/hotkeys/{id}/trigger
```

Example with curl:
```bash
curl -X POST http://localhost:5050/api/hotkeys/3/trigger
```

This is the most reliable method and works on all platforms.

**Stream Deck Integration:**
- **Option A (Recommended):** Use Stream Deck's "Website" action or the "API Ninja" plugin to send the POST request above
- **Option B:** Use Stream Deck's built-in "Hotkey" action with the same key combination (Windows only, macOS requires Accessibility permission)

**macOS Accessibility Permission:**
Global keyboard hotkeys on macOS require Accessibility permission. If the permission is missing:
1. A yellow "Permission Required" badge appears on the Hotkeys page
2. Click it to open the permission dialog
3. Click "Open System Settings" to go directly to the Accessibility settings
4. Enable Wrkzg in the list
5. Click "Check Again" to verify

> **Tip:** The API trigger method (`POST /api/hotkeys/{id}/trigger`) works without any permissions and is recommended for Stream Deck users on macOS.

### 5.4 Effect System (Automations)

Create custom automations with Trigger → Conditions → Effects chains. Combine any trigger with any effect to build powerful workflows.

#### How It Works

1. **Trigger** — What starts the automation (chat command, Twitch event, keyword, hotkey, channel point redemption)
2. **Conditions** (optional) — Gates that must pass before effects run (role check, points check, random chance, stream status)
3. **Effects** — Actions that execute sequentially (send chat message, wait, update counter, show alert, set variable, send Discord message/embed)

#### Creating an Automation

1. Go to **Automations** in the sidebar
2. Click **New Automation**
3. Choose a trigger type and configure it
4. Optionally add conditions
5. Add one or more effects in the desired order
6. Set a cooldown to prevent spam
7. Save and enable

#### Trigger Types

| Trigger | Config Key | Description |
|---|---|---|
| **Chat Command** | `trigger` | Fires when a specific command is used (e.g. `!welcome`) |
| **Twitch Event** | `event_type` | Fires on follow, sub, raid, gift sub, resub, or stream online |
| **Chat Keyword** | `keyword` | Fires when a keyword appears anywhere in a message |
| **Hotkey** | `hotkey_id` | Fires when a specific hotkey is pressed |
| **Channel Point** | `reward_id` | Fires on a channel point redemption |

#### Available Event Types

| Event Type | When it fires |
|---|---|
| `event.follow` | Someone follows the channel |
| `event.subscribe` | Someone subscribes |
| `event.gift` | Someone gifts subscriptions |
| `event.resub` | Someone resubscribes |
| `event.raid` | Someone raids the channel |
| `event.stream_online` | The stream goes live |

#### Condition Types

| Condition | Config Keys | Description |
|---|---|---|
| **Role Check** | `role_id` | User must have a specific community role |
| **Points Check** | `min_points` | User must have at least X points |
| **Random Chance** | `percent` | Only fires X% of the time |
| **Stream Status** | `required_status` | Only fires when stream is online or offline |

#### Effect Types

| Effect | Config Keys | Description |
|---|---|---|
| **Chat Message** | `message` | Send a message in chat. Use `{user}` for the viewer's name |
| **Wait** | `seconds` | Pause before the next effect (max 60 seconds) |
| **Counter** | `counter_id`, `action`, `amount` | Increment, decrement, or reset a counter |
| **Alert** | `title`, `message` | Show an alert in the OBS Alert Box overlay |
| **Variable** | `name`, `value` | Set a variable for use in later effects with `{variable_name}` |
| **Discord Message** | `message` | Send a text message to the configured Discord webhook |
| **Discord Embed** | `title`, `description`, `color` | Send a rich embed to Discord |

#### Variables in Effects

All effect templates support these variables:

| Variable | Description |
|---|---|
| `{user}` | Display name of the user who triggered the automation |
| Any trigger data | Event-specific data like `{viewers}`, `{tier}`, `{months}` |
| Custom variables | Variables set by earlier effects in the same chain |

#### Quick-Start Examples

The Automations page includes ready-to-use examples you can create with one click:

- **Welcome New Followers** — Send a personalized welcome message on follow
- **Lucky Viewer (50% Chance)** — Random chance to give bonus points on `!lucky`
- **Raid Alert Combo** — Wait 2 seconds, then send a welcome message on raid
- **Discord Live Notification** — Send a Discord message when the stream goes live

#### Tips

- **Use conditions to gate effects** — e.g. only allow `!lucky` for subscribers
- **Chain multiple effects** — wait + message creates a delayed response
- **Use the Test button** to simulate any automation without waiting for the real trigger
- **Set cooldowns** to prevent spam — especially important for event-triggered automations

### 5.5 Integrations

Connect external services to your bot. Currently supports Discord via webhooks.

#### Discord Webhook

Send messages and rich embeds to any Discord channel — no Discord bot token or OAuth needed. Just a webhook URL.

**Setup:**

1. Go to **Integrations** in the sidebar
2. Follow the step-by-step instructions to create a Discord webhook:
   - Open Discord and go to the target channel
   - Click the gear icon (Edit Channel)
   - Go to **Integrations** > **Webhooks**
   - Click **New Webhook**, name it (e.g. "Wrkzg Bot")
   - Click **Copy Webhook URL**
3. Paste the URL and click **Save**
4. Click **Test** to verify the connection

**Using Discord in Automations:**

Once configured, two new effect types are available in the Effect System:

| Effect | Description |
|---|---|
| `discord.send_message` | Send a plain text message to Discord |
| `discord.send_embed` | Send a rich embed with title, description, and color |

**Example: Discord Live Notification**

Create an automation with:
- **Trigger:** Twitch Event → `event.stream_online`
- **Effect:** Discord Message → "The stream is now LIVE! Come hang out!"

This sends a message to your Discord channel every time your stream goes live.

**Removing the Webhook:**

Click the trash icon on the Integrations page to remove the webhook URL. The webhook is stored encrypted — only you can access it.

---

## 6. Moderation

### 6.1 Spam Filter

Automatic chat moderation with five filter types:

| Filter | What it catches | Default timeout |
|---|---|---|
| **Link Filter** | Messages containing URLs | 10 seconds |
| **Caps Filter** | Messages with excessive CAPS LOCK | 5 seconds |
| **Banned Words** | Messages containing banned words/phrases | 30 seconds |
| **Emote Spam** | Messages with too many emotes | 5 seconds |
| **Repetition** | Repeated identical messages | 10 seconds |

Each filter can be configured independently:
- **Timeout duration** — How long offenders are timed out
- **Exemptions** — Whether subscribers and/or moderators bypass the filter
- **Thresholds** — Caps percentage, max emotes, etc.
- **Whitelist** — Allowed domains for the link filter (e.g. `clips.twitch.tv`)

---

## 7. Stream Analytics

Automatic stream tracking with viewer counts, category changes, and session history. Data is collected every 60 seconds while you're live — no setup required.

### How It Works

The bot automatically polls the Twitch API every 60 seconds while your stream is live:
- **Viewer count** is recorded as a snapshot (for minute-by-minute charts)
- **Category changes** are detected and tracked as segments with exact durations
- **Stream sessions** are opened when you go live and closed when you go offline

### Dashboard Tabs

**Overview** — KPI cards (total streams, hours streamed, avg/peak viewers), viewer trend line chart, stream hours bar chart for the last 30 days.

**Categories** — Pie chart showing time distribution across games/categories, breakdown table with hours, avg viewers, and peak viewers per category.

**Stream History** — Browse individual stream sessions. Select a session to see:
- Minute-by-minute viewer count chart (area chart)
- Category timeline showing which games you played and when
- Session KPIs (duration, peak, average, categories played)

### Tips

- **Data builds up over time** — the more you stream, the more useful the analytics become
- **Category tracking is automatic** — just switch games in your Twitch dashboard and the bot detects the change
- **No manual setup needed** — the StreamAnalyticsService starts automatically with the bot

---

## 8. OBS Overlays

Seven built-in overlay types plus a Developer Mode for fully custom HTML/CSS/JS overlays. No authentication needed — overlays connect via localhost.

### 8.1 Setup (same for all overlays)

1. In the Wrkzg dashboard, go to **Overlays**
2. Click **Copy URL** next to the overlay you want
3. In OBS: **Sources** > **+** > **Browser** > paste the URL
4. Set the recommended width/height

All overlays auto-reconnect if the bot restarts. They poll for connectivity every 10 seconds and reload automatically when the connection is restored.

### 8.2 Overlay Editor

Every built-in overlay has a full visual editor. Click **Edit** on any overlay card to open it.

**Layout:**
- **Left side** — Live preview that updates in real-time as you change settings
- **Right side** — Tabbed settings panel

**Tabs:**
- **General** — Font size, font family (30+ Google Fonts), text color, animation, duration
- **Events** (Alert Box only) — Per-event customization with image, sound, message, and animation overrides
- **Style** — Colors, accents, additional visual settings
- **Custom CSS** — Write your own CSS to override any style (no `!important` needed)

**Saving:** Click **Save** to persist your changes. Click **Reset to Defaults** to restore all settings to the built-in defaults.

**Test Buttons:** The Alert Box editor has test buttons for each event type (Follow, Subscribe, Gift Sub, Resub, Raid). Click to fire a test event and see/hear it in the live preview.

### 8.3 Alert Box

Displays animated alerts for follows, subscriptions, raids, gift subs, resubs, and channel point redemptions. Alerts queue up and display one at a time.

**Recommended size:** 800 x 200

#### Per-Event Customization

Each event type (Follow, Subscribe, Gift Sub, Resub, Raid, Channel Point) can be individually customized:

| Setting | Description |
|---|---|
| **Image** | Upload a custom image or GIF that displays with the alert |
| **Sound** | Upload a custom sound (.mp3, .wav, .ogg) that plays when the alert shows |
| **Volume** | Sound volume (0-100%) |
| **Message** | Custom message template with variables like `{user}`, `{tier}`, `{viewers}` |
| **Animation** | Override the global animation for this specific event type |
| **Enabled** | Toggle individual event types on/off |

If a per-event setting is left empty, the global default is used (e.g. if Follow has no animation override, it uses the global animation setting).

#### Animations

14 animations are available:

| Animation | Description |
|---|---|
| Slide Down | Slides in from the top |
| Slide Up | Slides in from the bottom |
| Slide Left | Slides in from the right |
| Slide Right | Slides in from the left |
| Fade In | Fades in smoothly |
| Bounce In | Bounces in with elastic effect |
| Zoom In | Zooms in from small to full size |
| Flip In | 3D flip rotation |
| Rotate In | Rotates in from off-screen |
| Jack in the Box | Springs up with a wobble |
| Rubber Band | Stretchy elastic effect |
| Heart Beat | Pulsing heartbeat effect |
| Tada | Attention-grabbing shake |
| No Animation | Appears instantly |

### 8.4 Chat Box

Shows live chat messages with emote rendering and role badges.

**Recommended size:** 400 x 600

**Settings:** Max messages, font size, font family, fade after (seconds), direction, show badges.

### 8.5 Poll Overlay

Displays the active poll with animated vote bars, percentages, and a countdown timer. Shows results for 10 seconds after the poll ends.

**Recommended size:** 500 x 400

### 8.6 Raffle Overlay

Shows the active raffle with entry instructions, animated winner reveal, and confetti animation.

**Recommended size:** 600 x 300

### 8.7 Counter Overlay

Displays a single counter value. Updates in real-time via SignalR.

**Recommended size:** 300 x 100

### 8.8 Event List

Scrolling feed of recent events (follows, subs, raids, etc.) with icons for each event type.

**Recommended size:** 400 x 500

### 8.9 Song Player

Displays the currently playing or next queued song with thumbnail, title, and requester name.

**Two modes:**
- **Full** (default) — Thumbnail, title, requester, queue count. 440 x 100.
- **Slim** — Compact bar. Add `?mode=slim` to the overlay URL. 380 x 48.

**Recommended size:** 440 x 100 (full) or 380 x 48 (slim)

### 8.10 Asset Management

Upload your own sounds and images for use in overlays.

**Supported formats:**
- **Sounds:** .mp3, .wav, .ogg (max 10 MB per file)
- **Images:** .png, .jpg, .jpeg, .gif, .webp, .webm, .svg (max 10 MB per file)

Assets are stored locally in your app data directory (`%APPDATA%/Wrkzg/assets/` on Windows, `~/Library/Application Support/Wrkzg/assets/` on macOS). No cloud upload — files are served directly from your machine via localhost.

Upload assets through the **Asset Picker** in the Overlay Editor's Events tab. Each asset shows a preview (image thumbnail or sound play button) and can be selected or deleted.

### 8.11 Google Fonts

The Overlay Editor includes a font picker with 30+ popular Google Fonts. Select a font in the General tab to use it in your overlay.

Google Fonts require an active internet connection on the streaming PC. The font CSS is loaded dynamically from Google's CDN when the overlay renders in OBS.

Fonts available include: Roboto, Open Sans, Lato, Montserrat, Poppins, Nunito, Bangers, Permanent Marker, Press Start 2P, VT323, Orbitron, JetBrains Mono, and many more.

### 8.12 Custom CSS

Every overlay type supports custom CSS. Open the Overlay Editor, go to the **Custom CSS** tab, and write your styles.

**Key advantage:** Custom CSS is loaded AFTER the default styles, so your rules naturally override the defaults. No `!important` needed — this is a deliberate design choice that makes custom CSS much cleaner than competing overlay systems.

**Available CSS classes per overlay:**

| Overlay | CSS Classes |
|---|---|
| Alert Box | `.overlay-text`, `.alert-image` |
| Chat Box | `.chat-message`, `.chat-username` |
| Poll | `.poll-bar` |
| Counter | `.counter-value` |
| All | `.overlay-root` (root container) |

**Example — Add text shadow to alerts:**
```css
.overlay-text {
  text-shadow: 2px 2px 4px rgba(0,0,0,0.5);
}
```

### 8.13 Custom Overlays (Developer Mode)

Create fully custom overlays with HTML, CSS, and JavaScript. Your overlays have full access to Wrkzg's real-time SignalR events and run as standalone Browser Sources in OBS.

#### Creating a Custom Overlay

1. Go to **Overlays** in the sidebar
2. Scroll to the **Custom Overlays** section
3. Click **Blank Overlay** or choose a template (Follow Goal Bar, Ticker, Clock, Sub Counter, Raid Banner)
4. The code editor opens with HTML, CSS, JS, and Fields tabs
5. Write your overlay code
6. Click **Save** and copy the OBS URL

#### Code Editor

The editor has a split-view layout:
- **Left** — Live preview with checkerboard background (indicates transparency). Click **Refresh** to reload after saving.
- **Right** — Code tabs and settings

**Code Tabs:**
- **HTML** — Your overlay's markup
- **CSS** — Your styles (applied after the base reset styles)
- **JS** — Your JavaScript (runs after SignalR connects)
- **Fields (JSON)** — Define configurable fields (see below)

**Settings Tab:**
- Description, recommended width/height for OBS

#### Wrkzg JavaScript API

Every custom overlay has access to the `Wrkzg` object:

```javascript
// Listen for stream events
Wrkzg.on('FollowEvent', function(data) {
  console.log(data.username + ' followed!');
});

// Read a configured field value
const title = Wrkzg.getField('title');
```

**`Wrkzg.on(eventName, callback)`** — Register a listener for a SignalR event. The callback receives the event data as its first argument.

**`Wrkzg.getField(key)`** — Read a field value from the Fields JSON definition. Returns the configured value, or the default from the field definition, or `null`.

#### Available Events

| Event Name | Data Fields | When it fires |
|---|---|---|
| `FollowEvent` | `username` | Someone follows |
| `SubscribeEvent` | `username`, `tier` | Someone subscribes |
| `GiftSubEvent` | `username`, `count`, `tier` | Someone gifts subs |
| `ResubEvent` | `username`, `months`, `tier`, `message` | Someone resubscribes |
| `RaidEvent` | `username`, `viewers` | Someone raids |
| `ChannelPointRedemption` | `username`, `rewardTitle`, `cost`, `userInput` | Channel point redeemed |
| `ChatMessage` | `username`, `displayName`, `content`, `isMod`, `isSubscriber` | Chat message received |
| `CounterUpdated` | `counterId`, `name`, `value` | Counter value changes |
| `StreamOnline` | `broadcaster` | Stream goes live |

#### Field Definitions (JSON)

Define configurable fields that appear as a visual form in the Settings tab. This lets other users customize your overlay without editing code.

```json
{
  "title": {
    "type": "text",
    "label": "Widget Title",
    "value": "My Widget"
  },
  "fontSize": {
    "type": "number",
    "label": "Font Size (px)",
    "value": 24,
    "min": 8,
    "max": 128
  },
  "backgroundColor": {
    "type": "color",
    "label": "Background Color",
    "value": "#000000"
  },
  "showIcon": {
    "type": "toggle",
    "label": "Show Icon",
    "value": true
  }
}
```

**Supported field types:** `text`, `number`, `color`, `toggle`, `select`, `sound` (asset picker), `image` (asset picker), `font` (font picker).

Access field values in JavaScript with `Wrkzg.getField('title')`.

#### Example: Follow Goal Bar

This overlay shows a progress bar that fills up as followers come in:

**HTML:**
```html
<div id="goal">
  <div id="label">Follower Goal</div>
  <div id="bar"><div id="fill"></div></div>
  <div id="count">0 / 100</div>
</div>
```

**CSS:**
```css
#goal { font-family: system-ui; color: white; padding: 16px; }
#bar { background: rgba(255,255,255,0.15); border-radius: 8px; height: 24px; overflow: hidden; }
#fill { background: linear-gradient(90deg, #8BBF4C, #6da832); height: 100%; width: 0%; transition: width 0.5s ease; border-radius: 8px; }
#count { font-size: 12px; margin-top: 4px; text-align: right; opacity: 0.7; }
```

**JavaScript:**
```javascript
let count = 0;
const goal = 100;

function update() {
  document.getElementById("fill").style.width = (count / goal * 100) + "%";
  document.getElementById("count").textContent = count + " / " + goal;
}

Wrkzg.on("FollowEvent", function() {
  count++;
  update();
});

update();
```

#### Tips

- **Transparency:** In OBS, Browser Sources render transparent backgrounds correctly. The checkerboard pattern in the editor preview indicates transparency.
- **No framework needed:** Write vanilla HTML/CSS/JS. No build step, no bundler.
- **Everything is local:** No upload limits, no CDN, no cloud dependency. Your overlays load instantly from localhost.
- **Test with events:** Use the Test buttons on the Alert Box overlay card (or the Automations test feature) to fire test events while developing.
- **Templates:** Start from one of the 5 built-in templates to see working examples of event handling, animations, and styling

---

## 9. Settings & Configuration

### Twitch Account Connections

- **Bot Account** — The account that sends chat messages. Requires `chat:read` and `chat:edit` scopes.
- **Broadcaster Account** — Your main account. Requires additional scopes for EventSub events, polls, and channel point redemptions.

Credentials are stored encrypted in your operating system's keychain (macOS Keychain / Windows DPAPI) — never in config files.

### Points Configuration

Configure in Settings:
- **Points per minute** — How many points viewers earn per minute (default: 10)
- **Subscriber multiplier** — Points multiplier for subscribers (default: 2x)

### Theme

Toggle between Light and Dark mode using the theme toggle at the bottom of the sidebar.

---

## 10. Data Management

### 10.1 Import Data from Another Bot

Wrkzg can import your community data from other Twitch bots so your viewers keep their points, watch time, and status when you switch.

#### Supported Formats

| Source | Format | What's imported |
|---|---|---|
| **Deepbot (CSV)** | .csv, 3 columns, no header | Username, Points, Watch Time |
| **Deepbot (JSON)** | .json from API export | All above + VIP level, Mod status, Join Date, Last Seen |
| **Deepbot Users (Save File)** | users*.bin | Username, Points, Watch Time, Display Name, Twitch ID |
| **Deepbot Commands & Quotes (Save File)** | chanmsgconfig*.bin | Commands, Quotes, Timed Messages |
| **Streamlabs Chatbot** | .csv export | Username, Points, Watch Hours |
| **Generic CSV** | Any .csv | Customizable column mapping |

#### How to Export from Deepbot

**CSV Export (simple):**
1. Open Deepbot
2. Go to the "User Database" tab
3. Right-click anywhere in the list
4. Select "Export" and save as .csv
5. The file contains: Username, Points, Minutes Watched

**JSON Export (full data):**
1. Connect to Deepbot's WebSocket API (port 3337)
2. Authenticate with your API secret
3. Use `get_users` with pagination to export all users
4. Save the response as a .json file

#### Import Steps

1. Go to **Import Data** in the sidebar
2. Select your previous bot format
3. Upload your export file
4. Preview the data to verify it looks correct
5. Choose a conflict strategy for existing users
6. Click **Import** to start the migration

#### Conflict Strategies

When importing, some users may already exist in Wrkzg. Choose how to handle them:

| Strategy | Points | Watch Time | Best for |
|---|---|---|---|
| **Skip** | Keep Wrkzg value | Keep Wrkzg value | Keeping current data untouched |
| **Overwrite** | Use imported value | Use imported value | Starting fresh with old data |
| **Keep Higher** | Use the bigger value | Use the bigger value | Merging data from two sources |
| **Add** | Sum both values | Sum both values | Combining multiple bot exports |

#### DeepBot Save Files (.bin)

If Deepbot doesn't offer a CSV/JSON export (common in v0.12.x), you can import directly from DeepBot's save files. These are two separate files that can be imported independently, in any order:

**User data (`users*.bin`):**
1. Navigate to DeepBot's data folder (usually next to the Deepbot.exe)
2. Find the file starting with `users` and ending with `.bin` (e.g. `users20241201.bin`)
3. This file contains all user records: usernames, points, watch time, and (for some users) display names and Twitch IDs
4. Import using the **Deepbot Users (Save File)** template

**Commands, Quotes & Timers (`chanmsgconfig*.bin`):**
1. In the same folder, find the file starting with `chanmsgconfig` and ending with `.bin`
2. This file contains custom chat commands, saved quotes, and timed messages
3. Import using the **Deepbot Commands & Quotes (Save File)** template
4. Existing commands with the same trigger are skipped (not overwritten)
5. DeepBot variables (`@user@`, `@target@`, etc.) are automatically converted to Wrkzg format

**Note:** `usersconfig*.bin` files contain only bot settings (point multipliers, UI preferences) and are not imported.

#### Imported User ID Resolution

Imported users don't have a Twitch ID yet (Deepbot CSV/JSON only stores usernames). Wrkzg creates them with a temporary placeholder ID. The first time an imported user types in your chat, Wrkzg automatically links their Twitch account to the imported data. This happens silently — no action needed from you or the viewer. DeepBot Save File imports may include real Twitch IDs for some users, which are used directly.

---

## 11. Troubleshooting & FAQ

### Bot Won't Connect

1. Check that your Bot account is connected in Settings
2. Make sure the channel name is set correctly
3. Try disconnecting and reconnecting the Bot account
4. Check the console log for error messages

### Commands Not Working

1. Verify the command is enabled (check the toggle on the Commands page)
2. Check the permission level — viewers can't use Mod-only commands
3. Check cooldowns — the command may be on cooldown
4. Make sure the trigger starts with `!`

### Overlay Not Showing in OBS

1. Verify the URL is correct (copy it fresh from the Overlays page)
2. Make sure the Wrkzg app is running
3. Check that the Browser Source width/height matches the recommendations
4. Try refreshing the Browser Source in OBS (right-click > Refresh)

### EventSub Not Receiving Events

1. Ensure your Broadcaster account is connected in Settings
2. Check the console for EventSub connection errors
3. The Broadcaster account needs the correct OAuth scopes — try disconnecting and reconnecting
4. EventSub events require a live stream for some event types (follows work anytime, but viewer count requires being live)

### Port Conflicts

Wrkzg runs on port 5050 by default. If another application uses this port:
1. Close the conflicting application
2. Or change the port in Settings

> **Note:** macOS AirPlay Receiver uses port 5000 — that's why Wrkzg defaults to 5050.
