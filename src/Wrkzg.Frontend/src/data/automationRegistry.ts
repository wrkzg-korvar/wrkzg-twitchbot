// ─── Type Definitions ───────────────────────────────────────

export interface FieldDef {
  key: string;
  label: string;
  type: "text" | "number" | "select" | "textarea";
  placeholder?: string;
  options?: { value: string; label: string }[];
  helperText?: string;
  required?: boolean;
  min?: number;
  max?: number;
  suffix?: string;
}

export interface VariableDef {
  name: string;
  description: string;
}

export interface TriggerDef {
  id: string;
  displayName: string;
  description: string;
  fields: FieldDef[];
  variables: VariableDef[];
}

export interface ConditionDef {
  id: string;
  displayName: string;
  description: string;
  fields: FieldDef[];
}

export interface EffectDef {
  id: string;
  displayName: string;
  description: string;
  fields: FieldDef[];
  supportsVariables?: boolean;
}

export const COMMON_VARIABLES: VariableDef[] = [
  { name: "{user}", description: "Name of the viewer who triggered the event" },
];

export const TRIGGER_REGISTRY: TriggerDef[] = [
  {
    id: "command",
    displayName: "Chat Command",
    description: "Triggers when a viewer types a specific chat command (e.g. !welcome). The text after the command is available as {args}.",
    fields: [
      { key: "trigger", label: "Command", type: "text", placeholder: "!welcome", helperText: "The command including the leading ! that viewers must type", required: true },
    ],
    variables: [
      { name: "{user}", description: "Name of the viewer" },
      { name: "{args}", description: "Full text after the command" },
      { name: "{target}", description: "First word after the command (e.g. @username)" },
      { name: "{points}", description: "Viewer's current points" },
      { name: "{hours}", description: "Viewer's watchtime in hours" },
      { name: "{channel}", description: "Channel name" },
    ],
  },
  {
    id: "event",
    displayName: "Twitch Event",
    description: "Triggers on Twitch events such as follows, subscriptions, raids, or when the stream goes live.",
    fields: [
      {
        key: "event_type", label: "Event Type", type: "select", required: true,
        options: [
          { value: "event.follow", label: "New Follower" },
          { value: "event.subscribe", label: "New Subscription" },
          { value: "event.resub", label: "Resubscription" },
          { value: "event.gift", label: "Gift Subscription" },
          { value: "event.raid", label: "Raid (incoming)" },
          { value: "event.stream_online", label: "Stream Goes Live" },
        ],
        helperText: "Choose the Twitch event that should trigger this automation",
      },
    ],
    variables: [
      { name: "{user}", description: "Name of the viewer/raider" },
      { name: "{viewers}", description: "Viewer count for raids" },
      { name: "{tier}", description: "Subscription tier (1/2/3)" },
      { name: "{months}", description: "Subscription months for resubscriptions" },
      { name: "{count}", description: "Number of gifted subs" },
      { name: "{message}", description: "Viewer's message on resubs" },
      { name: "{broadcaster}", description: "Streamer's name" },
    ],
  },
  {
    id: "keyword",
    displayName: "Chat Keyword",
    description: "Triggers when a specific word appears in a chat message. Case-insensitive.",
    fields: [
      { key: "keyword", label: "Keyword", type: "text", placeholder: "hello", helperText: "The word that must appear in the message", required: true },
    ],
    variables: [
      { name: "{user}", description: "Name of the viewer" },
      { name: "{channel}", description: "Channel name" },
    ],
  },
  {
    id: "channelpoint",
    displayName: "Channel Point Redemption",
    description: "Triggers when a viewer redeems a Channel Points reward.",
    fields: [
      { key: "reward_id", label: "Reward ID", type: "text", placeholder: "Empty = all redemptions", helperText: "Leave empty to react to ANY redemption." },
    ],
    variables: [
      { name: "{user}", description: "Name of the viewer" },
      { name: "{reward}", description: "Reward name" },
      { name: "{input}", description: "Viewer's text input" },
      { name: "{cost}", description: "Cost in Channel Points" },
    ],
  },
  {
    id: "hotkey",
    displayName: "Hotkey",
    description: "Triggers when a configured hotkey is pressed or triggered via the API.",
    fields: [
      { key: "hotkey_id", label: "Hotkey Binding ID", type: "text", placeholder: "1", helperText: "The ID of the hotkey from the Hotkeys page", required: true },
    ],
    variables: [
      { name: "{hotkey}", description: "The pressed key combination" },
      { name: "{description}", description: "Hotkey description" },
    ],
  },
];

export const CONDITION_REGISTRY: ConditionDef[] = [
  {
    id: "role_check",
    displayName: "Check User Role",
    description: "Only run if the user has at least the given role.",
    fields: [
      {
        key: "min_priority", label: "Minimum Role", type: "select", required: true,
        options: [
          { value: "1", label: "Viewer (everyone)" },
          { value: "2", label: "Follower" },
          { value: "3", label: "Subscriber" },
          { value: "5", label: "Moderator" },
          { value: "10", label: "Broadcaster" },
        ],
        helperText: "All roles at this level and above",
      },
    ],
  },
  {
    id: "points_check",
    displayName: "Check Minimum Points",
    description: "Only run if the user has enough points. Points are NOT deducted.",
    fields: [
      { key: "min_points", label: "Minimum Points", type: "number", placeholder: "100", min: 0, required: true },
    ],
  },
  {
    id: "random_chance",
    displayName: "Random Chance",
    description: "Only run with a given probability.",
    fields: [
      { key: "percent", label: "Probability", type: "number", placeholder: "50", min: 1, max: 100, suffix: "%", required: true },
    ],
  },
  {
    id: "stream_status",
    displayName: "Check Stream Status",
    description: "Only run when the stream is live or offline.",
    fields: [
      {
        key: "require_live", label: "Stream must be", type: "select", required: true,
        options: [
          { value: "true", label: "Live (online)" },
          { value: "false", label: "Offline" },
        ],
      },
    ],
  },
];

export const EFFECT_REGISTRY: EffectDef[] = [
  {
    id: "chat_message",
    displayName: "Send Chat Message",
    description: "Sends a message to Twitch chat.",
    supportsVariables: true,
    fields: [
      { key: "message", label: "Message", type: "textarea", placeholder: "Welcome {user}!", required: true, helperText: "Variables are substituted automatically." },
    ],
  },
  {
    id: "wait",
    displayName: "Wait",
    description: "Pauses before the next action. Max 60 seconds.",
    supportsVariables: false,
    fields: [
      { key: "seconds", label: "Duration", type: "number", placeholder: "2", min: 1, max: 60, suffix: "seconds", required: true },
    ],
  },
  {
    id: "counter",
    displayName: "Modify Counter",
    description: "Increments, decrements, or resets a counter.",
    supportsVariables: false,
    fields: [
      { key: "counter_id", label: "Counter ID", type: "text", placeholder: "1", required: true, helperText: "ID from the Counters page" },
      {
        key: "action", label: "Action", type: "select", required: true,
        options: [
          { value: "increment", label: "Increment (+1)" },
          { value: "decrement", label: "Decrement (-1)" },
          { value: "reset", label: "Reset to 0" },
        ],
      },
    ],
  },
  {
    id: "alert",
    displayName: "Show Alert",
    description: "Shows an alert in the OBS overlay.",
    supportsVariables: true,
    fields: [
      { key: "message", label: "Alert Text", type: "textarea", placeholder: "{user} did something!", required: true },
    ],
  },
  {
    id: "variable",
    displayName: "Set Variable",
    description: "Sets a variable for subsequent actions.",
    supportsVariables: true,
    fields: [
      { key: "name", label: "Variable Name", type: "text", placeholder: "result", required: true, helperText: "Enter without {}. Usage: {result}" },
      { key: "value", label: "Value", type: "text", placeholder: "{user} won" },
    ],
  },
  {
    id: "discord.send_message",
    displayName: "Send Discord Message",
    description: "Sends a message to the configured Discord webhook.",
    supportsVariables: true,
    fields: [
      { key: "message", label: "Message", type: "textarea", placeholder: "The stream is live!", required: true },
    ],
  },
  {
    id: "discord.send_embed",
    displayName: "Send Discord Embed",
    description: "Sends a formatted embed message to Discord.",
    supportsVariables: true,
    fields: [
      { key: "title", label: "Embed Title", type: "text", placeholder: "Stream is Live!", required: true },
      { key: "description", label: "Description", type: "textarea", placeholder: "{user} is streaming now!" },
      {
        key: "color", label: "Color", type: "select",
        options: [
          { value: "5793266", label: "Blue" },
          { value: "5763719", label: "Green" },
          { value: "15548997", label: "Red" },
          { value: "16776960", label: "Yellow" },
          { value: "10181046", label: "Purple" },
          { value: "15105570", label: "Orange" },
        ],
      },
    ],
  },
  {
    id: "obs.scene_switch",
    displayName: "OBS: Switch Scene",
    description: "Switches the active OBS scene",
    supportsVariables: false,
    fields: [
      { key: "scene_name", label: "Scene Name", type: "text", placeholder: "Gaming", required: true },
    ],
  },
  {
    id: "obs.source_toggle",
    displayName: "OBS: Show/Hide Source",
    description: "Shows or hides an OBS source",
    supportsVariables: false,
    fields: [
      { key: "scene_name", label: "Scene Name", type: "text", placeholder: "Gaming", required: true },
      { key: "source_name", label: "Source Name", type: "text", placeholder: "Webcam", required: true },
      {
        key: "visible",
        label: "Visibility",
        type: "select",
        options: [
          { value: "", label: "Toggle" },
          { value: "true", label: "Show" },
          { value: "false", label: "Hide" },
        ],
      },
    ],
  },
];

export function getTriggerDef(id: string): TriggerDef | undefined {
  return TRIGGER_REGISTRY.find((t) => t.id === id);
}

export function getConditionDef(id: string): ConditionDef | undefined {
  return CONDITION_REGISTRY.find((c) => c.id === id);
}

export function getEffectDef(id: string): EffectDef | undefined {
  return EFFECT_REGISTRY.find((e) => e.id === id);
}

export function getVariablesForTrigger(triggerId: string): VariableDef[] {
  const trigger = getTriggerDef(triggerId);
  return trigger?.variables ?? COMMON_VARIABLES;
}
