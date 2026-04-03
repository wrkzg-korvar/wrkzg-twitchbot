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
5. [Automation](#5-automation)
   - [5.1 Timed Messages](#51-timed-messages)
   - [5.2 Event Notifications](#52-event-notifications)
6. [Moderation](#6-moderation)
   - [6.1 Spam Filter](#61-spam-filter)
7. [Stream Analytics](#7-stream-analytics)
8. [OBS Overlays](#8-obs-overlays)
   - [8.1 Alert Box](#81-alert-box)
   - [8.2 Chat Box](#82-chat-box)
   - [8.3 Poll Overlay](#83-poll-overlay)
   - [8.4 Raffle Overlay](#84-raffle-overlay)
   - [8.5 Counter Overlay](#85-counter-overlay)
   - [8.6 Event List](#86-event-list)
9. [Settings & Configuration](#9-settings--configuration)
10. [Troubleshooting & FAQ](#10-troubleshooting--faq)

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

Six overlay types available as OBS Browser Sources. No authentication needed — overlays connect via localhost.

### Setup (same for all overlays)

1. In the Wrkzg dashboard, go to **Overlays**
2. Click **Copy URL** next to the overlay you want
3. In OBS: **Sources** > **+** > **Browser** > paste the URL
4. Set the recommended width/height

All overlays auto-reconnect if the bot restarts. They poll for connectivity every 10 seconds and reload automatically when the connection is restored.

### 8.1 Alert Box

Displays animated alerts for follows, subscriptions, raids, gift subs, resubs, and channel point redemptions. Alerts queue up and display one at a time.

**Recommended size:** 800 x 200

### 8.2 Chat Box

Shows live chat messages with emote rendering and role badges.

**Recommended size:** 400 x 600

### 8.3 Poll Overlay

Displays the active poll with animated vote bars, percentages, and a countdown timer. Shows results for 10 seconds after the poll ends.

**Recommended size:** 500 x 400

### 8.4 Raffle Overlay

Shows the active raffle with entry instructions, animated winner reveal, and confetti animation.

**Recommended size:** 600 x 300

### 8.5 Counter Overlay

Displays a single counter value. Updates in real-time via SignalR.

**Recommended size:** 300 x 100

### 8.6 Event List

Scrolling feed of recent events (follows, subs, raids, etc.) with icons for each event type.

**Recommended size:** 400 x 500

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

## 10. Troubleshooting & FAQ

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
