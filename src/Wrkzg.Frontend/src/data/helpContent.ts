export interface HelpChatCommand {
  command: string;
  description: string;
  permission?: string;
}

export interface HelpTemplateVariable {
  variable: string;
  description: string;
}

export interface HelpEntry {
  title: string;
  description: string;
  howToUse: string[];
  chatCommands?: HelpChatCommand[];
  templateVariables?: HelpTemplateVariable[];
  handbookSection?: string;
}

export const helpContent: Record<string, HelpEntry> = {

  // --- Dashboard ---
  "dashboard": {
    title: "Dashboard",
    description: "Your command center. See live chat, bot connection status, viewer count, and recent events at a glance. Messages appear in real-time via SignalR.",
    howToUse: [
      "The chat feed shows messages as they come in — no refresh needed",
      "Use the message input to send messages as your bot or broadcaster account",
      "The status bar shows whether the bot is connected to IRC and EventSub",
      "Viewer count updates automatically while the stream is live",
      "Klicke auf das Smiley-Icon um den Emote-Picker zu oeffnen",
    ],
    handbookSection: "#2-dashboard-overview",
  },

  // --- Commands ---
  "commands": {
    title: "Custom Commands",
    description: "Create chat commands that your bot responds to automatically. Commands support dynamic variables, permission levels, cooldowns, and aliases.",
    howToUse: [
      "Click 'Create Command' to add a new command",
      "Set a trigger (e.g. !discord) — must start with !",
      "Write a response template using variables like {user} or {points}",
      "Set permission levels to restrict who can use the command",
      "Add cooldowns to prevent spam (global and per-user)",
      "Add aliases so one command responds to multiple triggers (e.g. !dc, !disc)",
    ],
    chatCommands: [
      { command: "!commands", description: "Lists all available commands in chat" },
      { command: "!editcmd !trigger New response", description: "Edit a command's response on the fly", permission: "Mod" },
    ],
    templateVariables: [
      { variable: "{user}", description: "Display name of the user" },
      { variable: "{points}", description: "User's current point balance" },
      { variable: "{watchtime}", description: "User's total watch time" },
      { variable: "{random:min:max}", description: "Random number between min and max" },
      { variable: "{count}", description: "Command use count" },
    ],
    handbookSection: "#31-custom-commands",
  },

  // --- Quotes ---
  "quotes": {
    title: "Quotes",
    description: "Save memorable chat moments. Quotes are numbered sequentially and can be recalled randomly or by number. The game being played is saved automatically.",
    howToUse: [
      "Use '!quote add <text>' in chat or click 'Add Quote' here",
      "Each quote gets a sequential number (#1, #2, #3...)",
      "The current game/category is saved automatically if the stream is live",
      "Use the search bar to find quotes by text or author",
      "Delete quotes you no longer want — numbers are not reused",
    ],
    chatCommands: [
      { command: "!quote", description: "Show a random quote" },
      { command: "!quote <number>", description: "Show a specific quote by number" },
      { command: "!quote add <text>", description: "Save a new quote", permission: "Mod" },
    ],
    handbookSection: "#33-quotes",
  },

  // --- Users ---
  "users": {
    title: "Users & Points",
    description: "Track your community. Every viewer who chats earns points and watch time automatically. Points are awarded per minute while the stream is live.",
    howToUse: [
      "Points are earned automatically — no setup required",
      "Configure earn rates in Settings (points per minute, sub multiplier)",
      "Sort the table by any column (points, watch time, messages)",
      "Click on a user to see their details and manage their roles",
      "Points are used as currency in games, song requests, and other features",
    ],
    handbookSection: "#41-points-system",
  },

  // --- Polls ---
  "polls": {
    title: "Polls & Votes",
    description: "Create live polls that viewers vote on in chat. Results update in real-time with animated bar charts. Only one poll can be active at a time.",
    howToUse: [
      "Click 'Create Poll' or use !poll in chat",
      "Add 2-10 options for viewers to vote on",
      "Set an optional duration — the poll auto-closes when time runs out",
      "Viewers vote by typing !vote <number> in chat",
      "Watch results update live on the bar chart",
      "End the poll manually or let the timer run out",
    ],
    chatCommands: [
      { command: "!poll <question> | opt1 | opt2 | ...", description: "Start a new poll", permission: "Mod" },
      { command: "!vote <number>", description: "Vote for an option (1, 2, 3...)" },
      { command: "!pollend", description: "End the active poll", permission: "Mod" },
    ],
    handbookSection: "#42-polls--votes",
  },

  // --- Raffles ---
  "raffles": {
    title: "Raffles & Giveaways",
    description: "Run giveaways with keyword entry, animated draws, and winner verification. Supports multi-winner mode and custom entry keywords.",
    howToUse: [
      "Click 'Create Raffle' or use !raffle in chat",
      "Set a title, entry keyword, and optional duration",
      "Viewers enter by typing the keyword in chat",
      "Draw a winner with the 'Draw' button or !draw in chat",
      "Verify the winner is present — redraw if they don't respond",
      "Draw additional winners for multi-item giveaways",
    ],
    chatCommands: [
      { command: "!raffle <title>", description: "Start a new raffle", permission: "Mod" },
      { command: "!join", description: "Enter the active raffle" },
      { command: "!draw", description: "Draw a winner", permission: "Mod" },
      { command: "!cancelraffle", description: "Cancel the active raffle", permission: "Mod" },
    ],
    handbookSection: "#43-raffles--giveaways",
  },

  // --- Timed Messages ---
  "timed-messages": {
    title: "Timed Messages",
    description: "Automated recurring messages. Each timer can cycle through multiple messages and only fires when enough chat activity is happening — no talking to an empty room.",
    howToUse: [
      "Click 'Create Timer' to add a new timed message",
      "Set the interval (how often it fires) and minimum chat lines",
      "Add multiple messages — they cycle round-robin each time the timer fires",
      "Choose whether the timer runs when online, offline, or both",
      "Toggle timers on/off without deleting them",
    ],
    handbookSection: "#51-timed-messages",
  },

  // --- Notifications ---
  "notifications": {
    title: "Event Notifications",
    description: "Automatic chat announcements when someone follows, subscribes, gifts subs, or raids your channel. Each event type has its own customizable template.",
    howToUse: [
      "Toggle each event type on/off independently",
      "Edit the message template using the available variables",
      "Use the 'Test' button to preview how the message looks in chat",
      "Enable 'Auto Shoutout' for raids to automatically run !so for raiders",
      "Events also appear in the Dashboard activity feed in real-time",
    ],
    templateVariables: [
      { variable: "{user}", description: "Username of the person who triggered the event" },
      { variable: "{tier}", description: "Subscription tier (1, 2, or 3)" },
      { variable: "{months}", description: "Number of months subscribed (resub)" },
      { variable: "{count}", description: "Number of gift subs" },
      { variable: "{viewers}", description: "Number of viewers in a raid" },
      { variable: "{message}", description: "Resub message (if any)" },
    ],
    handbookSection: "#52-event-notifications",
  },

  // --- Spam Filter ---
  "spam-filter": {
    title: "Spam Filter",
    description: "Automatic chat moderation. Filters for links, excessive caps, banned words, emote spam, and repeated messages. Mods and subscribers can be exempted per filter.",
    howToUse: [
      "Toggle each filter type independently",
      "Set timeout durations (how long offenders are timed out)",
      "Configure exemptions — choose whether subs and/or mods bypass each filter",
      "Add whitelisted domains for the link filter (e.g. clips.twitch.tv)",
      "Add banned words/phrases to the word filter (case-insensitive)",
      "Adjust thresholds for caps percentage, emote count, etc.",
    ],
    handbookSection: "#61-spam-filter",
  },

  // --- Counters ---
  "counters": {
    title: "Counters",
    description: "Track anything with named counters — deaths, wins, fails, whatever you want. Increment/decrement from the dashboard or via chat commands.",
    howToUse: [
      "Click 'Create Counter' to add a new counter",
      "A chat command trigger is auto-generated from the name (e.g. 'Deaths' -> !deaths)",
      "Use the +/- buttons on the dashboard to update the value",
      "Viewers can check the counter with the trigger command",
      "Mods can modify the counter in chat: !deaths+, !deaths-, !deaths =0",
      "Customize the response template shown when viewers check the counter",
    ],
    chatCommands: [
      { command: "!<counter>", description: "Show the current counter value" },
      { command: "!<counter>+", description: "Increment by 1", permission: "Mod" },
      { command: "!<counter>-", description: "Decrement by 1", permission: "Mod" },
      { command: "!<counter> =<number>", description: "Set to a specific value", permission: "Mod" },
    ],
    templateVariables: [
      { variable: "{name}", description: "Counter display name" },
      { variable: "{value}", description: "Current counter value" },
    ],
    handbookSection: "#counters",
  },

  // --- Channel Points ---
  "channel-points": {
    title: "Channel Point Rewards",
    description: "React to Twitch Channel Point reward redemptions with configurable bot actions. Sync your rewards from Twitch and assign actions like chat messages or counter updates.",
    howToUse: [
      "Click 'Sync from Twitch' to load your channel's custom rewards",
      "Click 'Add Handler' to configure what happens when a reward is redeemed",
      "Select a reward, choose an action type, and set the payload",
      "Toggle handlers on/off without deleting them",
      "Channel Point Redemptions also appear as alerts in the Alert Box overlay",
    ],
    handbookSection: "#channel-point-rewards",
  },

  // --- Roles ---
  "roles": {
    title: "Roles & Ranks",
    description: "Create community roles that users earn automatically based on watch time, points, or messages. Roles can also be assigned manually. Higher priority = more privileges.",
    howToUse: [
      "Click 'Create Role' to add a new community role",
      "Set a priority (higher = more privileges) and a display color",
      "Optionally enable auto-assign with criteria (watch time, points, messages)",
      "Click 'Re-evaluate All' to check all users against auto-assign criteria",
      "Manually assign roles from the Users page",
    ],
    handbookSection: "#roles--ranks",
  },

  // --- Chat Games ---
  "chat-games": {
    title: "Chat Games",
    description: "Points-based chat games that boost viewer engagement. Players bet points, compete, and win prizes. Each game can be enabled/disabled and configured independently.",
    howToUse: [
      "Games are enabled by default — toggle them on/off from this page",
      "Each game has configurable settings (cooldown, min/max bet, etc.)",
      "Players need points to participate — earned from watching and chatting",
      "Trivia questions can be customized via the 'Trivia Questions' button",
      "Games can optionally require a minimum community role (from Roles & Ranks)",
    ],
    chatCommands: [
      { command: "!heist <amount>", description: "Join or start a group heist" },
      { command: "!duel @user <amount>", description: "Challenge someone to a 1v1 duel" },
      { command: "!accept", description: "Accept a pending duel challenge" },
      { command: "!slots <amount>", description: "Pull the slot machine lever" },
      { command: "!roulette <amount> <red|black>", description: "Bet on roulette" },
      { command: "!trivia", description: "Start a trivia question", permission: "Mod" },
    ],
    handbookSection: "#chat-games",
  },

  // --- Song Requests ---
  "song-requests": {
    title: "Song Requests",
    description: "Viewers request songs via !sr with a YouTube URL. Songs are queued and can be played through the OBS Song Player overlay. The queue is closed by default — open it when you're ready.",
    howToUse: [
      "Open the queue first — it's closed by default (use the toggle or !sr open)",
      "Viewers type !sr <YouTube URL> in chat to request a song",
      "The queue shows all pending songs in order",
      "Use the toolbar to skip, play next, or clear the queue",
      "Customize all bot messages via the Messages button",
      "Configure max duration, per-user limits, and points cost via Settings",
      "Add the Song Player overlay in OBS — use ?mode=slim for a compact bar",
    ],
    chatCommands: [
      { command: "!sr <YouTube URL>", description: "Request a song" },
      { command: "!sr open", description: "Open the queue", permission: "Mod" },
      { command: "!sr close", description: "Close the queue", permission: "Mod" },
      { command: "!skip", description: "Skip the current song", permission: "Mod" },
      { command: "!queue", description: "Show the next 5 songs" },
      { command: "!currentsong", description: "Show what's currently playing" },
    ],
    handbookSection: "#song-requests",
  },

  // --- Hotkeys ---
  "hotkeys": {
    title: "Hotkey Triggers",
    description: "Map keyboard shortcuts to bot actions. Each hotkey has an ID shown next to its name — use this ID for API triggers from Stream Deck or other tools.",
    howToUse: [
      "Click 'Add Hotkey' and press 'Record' to capture your key combination",
      "Choose an action: send chat message, or select a counter to modify",
      "Use the play button to test-trigger a hotkey immediately",
      "Each hotkey shows its ID — use it for API triggers",
      "API trigger: POST http://localhost:5050/api/hotkeys/{id}/trigger",
      "Stream Deck: use 'Website' action or 'API Ninja' plugin to send the POST request",
      "macOS: Accessibility permission is requested on first start",
      "Windows: Global hotkeys work automatically",
      "OBS Scene Switch: Wechselt eine OBS-Scene (erfordert OBS WebSocket Verbindung)",
      "OBS Source Toggle: Blendet eine OBS-Quelle ein/aus",
    ],
    handbookSection: "#hotkey-triggers",
  },

  // --- Effects ---
  "effects": {
    title: "Automations",
    description: "Erstelle individuelle Reaktionen auf Events, Chat-Befehle, Channel-Point-Einloesungen und mehr. Jede Automation besteht aus einem Trigger, optionalen Bedingungen und einer Aktionskette.",
    howToUse: [
      "Klicke 'New Automation' um den Visual Builder zu oeffnen",
      "Waehle einen Trigger (wann soll die Automation ausloesen?)",
      "Fuege optionale Bedingungen hinzu (wer/wann darf sie ausloesen?)",
      "Fuege Aktionen hinzu (was soll passieren?)",
      "Verwende Variablen wie {user} in Texten — sie werden automatisch ersetzt",
      "Der 'Test' Button fuehrt nur DIESE Automation aus (zum Testen)",
      "Wechsle zu 'JSON' fuer die rohe JSON-Konfiguration",
    ],
    templateVariables: [
      { variable: "{user}", description: "Name des Viewers" },
      { variable: "{args}", description: "Text nach dem Command" },
      { variable: "{target}", description: "Erstes Wort nach dem Command" },
      { variable: "{viewers}", description: "Zuschauerzahl bei Raids" },
      { variable: "{tier}", description: "Abo-Stufe bei Subscriptions" },
      { variable: "{months}", description: "Abo-Monate bei Resubs" },
      { variable: "{count}", description: "Anzahl geschenkter Subs" },
      { variable: "{message}", description: "Nachricht bei Resubs" },
      { variable: "{reward}", description: "Name der Channel-Point-Belohnung" },
      { variable: "{input}", description: "User-Eingabe bei Channel Points" },
      { variable: "{cost}", description: "Kosten der Channel-Point-Belohnung" },
      { variable: "{broadcaster}", description: "Name des Streamers" },
      { variable: "{hotkey}", description: "Tastenkombination des Hotkeys" },
    ],
    handbookSection: "5.4",
  },

  // --- Analytics ---
  "analytics": {
    title: "Stream Analytics",
    description: "Automatic stream tracking with viewer counts, category changes, and session history. Data is collected every 60 seconds while you're live — no setup required.",
    howToUse: [
      "Data collection starts automatically when you go live",
      "The Overview tab shows KPIs, viewer trends, and stream hours",
      "The Categories tab shows time distribution across games/categories",
      "The Stream History tab lets you explore individual sessions",
      "Select a session to see minute-by-minute viewer charts",
      "Data builds up over time — the more you stream, the more useful it gets",
    ],
    handbookSection: "#stream-analytics",
  },

  // --- Overlays ---
  "overlays": {
    title: "OBS Overlays",
    description: "Browser Sources for OBS Studio. Customize every detail — upload your own images and sounds, choose from 30+ Google Fonts, pick from 14 animations, and add Custom CSS. For advanced users: create fully custom overlays with HTML, CSS, and JavaScript.",
    howToUse: [
      "Click 'Edit' on any overlay to open the full editor",
      "Upload custom images and sounds in the Events tab (Alert Box)",
      "Choose from 30+ Google Fonts for your overlay text",
      "Pick from 14 animations or set 'No Animation' for clean alerts",
      "Add Custom CSS for advanced styling — no !important needed",
      "Use 'Test' buttons to preview changes before going live",
      "Copy the OBS URL and add as Browser Source in OBS",
      "Create custom overlays with HTML/CSS/JS for unlimited possibilities",
    ],
    handbookSection: "#8-obs-overlays",
  },

  // --- Import ---
  "import": {
    title: "Import Data",
    description: "Migrate your community data from another bot. Import points, watch time, and user data from Deepbot, Streamlabs Chatbot, or any CSV file. Your viewers keep their progress when you switch to Wrkzg.",
    howToUse: [
      "Select which bot you're importing from",
      "Upload your export file (CSV or JSON)",
      "Preview the data to make sure it looks correct",
      "Choose how to handle users that already exist in Wrkzg",
      "Optionally map VIP levels to Wrkzg Roles (Deepbot JSON only)",
      "Click Import to start the migration",
      "Imports laufen im Hintergrund — du kannst die Seite verlassen und weiterarbeiten",
      "Der Fortschritt wird in der Notification-Glocke (Sidebar) angezeigt",
      "Waehrend ein Import laeuft, sind die betroffenen Seiten read-only (Lock-Banner)",
    ],
    handbookSection: "#import-data",
  },

  // --- Integrations ---
  "integrations": {
    title: "Integrations",
    description: "Verbinde externe Dienste mit deinem Bot. Discord fuer Chat-Benachrichtigungen, OBS fuer Scene-Steuerung.",
    howToUse: [
      "Discord: Erstelle einen Webhook in deinem Discord-Server und fuege die URL hier ein",
      "OBS: Aktiviere den WebSocket Server in OBS (Tools -> WebSocket Server Settings)",
      "OBS: Gib Host (localhost), Port (4455) und optionales Passwort ein",
      "Nach der Verbindung kannst du OBS-Aktionen in Automations und Hotkeys verwenden",
    ],
    handbookSection: "5.5",
  },

  // --- Permissions ---
  "permissions": {
    title: "Berechtigungen",
    description: "Uebersicht welche Chat-Befehle welche Benutzerrolle erfordern.",
    howToUse: [
      "Rollen werden auf der Roles-Seite erstellt und automatisch oder manuell zugewiesen",
      "Hoehere Rollen beinhalten niedrigere (ein Moderator ist auch Viewer)",
      "System Commands haben feste Mindest-Rollen",
      "Custom Commands koennen ueber Automations mit Rollen-Bedingungen eingeschraenkt werden",
    ],
    handbookSection: "4.5",
  },

  // --- Settings ---
  "settings": {
    title: "Settings",
    description: "Configure your Twitch connections, bot behavior, and global settings. Your credentials are stored encrypted in your operating system's keychain — never in config files.",
    howToUse: [
      "Connect your Bot account and Broadcaster account via OAuth",
      "Set your channel name (the channel the bot joins)",
      "Configure points earn rates and subscriber multipliers",
      "Manage design preferences (Light/Dark theme)",
      "Re-authorize accounts if you need to update permissions",
    ],
    handbookSection: "#8-settings--configuration",
  },
};
