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
  { name: "{user}", description: "Name des Viewers der das Event ausgelöst hat" },
];

export const TRIGGER_REGISTRY: TriggerDef[] = [
  {
    id: "command",
    displayName: "Chat Command",
    description: "Wird ausgelöst wenn ein Viewer einen bestimmten Chat-Befehl eingibt (z.B. !welcome). Der Text nach dem Befehl steht als {args} zur Verfügung.",
    fields: [
      { key: "trigger", label: "Command", type: "text", placeholder: "!welcome", helperText: "Der Befehl inkl. ! den Viewer eingeben müssen", required: true },
    ],
    variables: [
      { name: "{user}", description: "Name des Viewers" },
      { name: "{args}", description: "Gesamter Text nach dem Command" },
      { name: "{target}", description: "Erstes Wort nach dem Command (z.B. @username)" },
      { name: "{points}", description: "Aktuelle Punkte des Viewers" },
      { name: "{hours}", description: "Watchtime des Viewers in Stunden" },
      { name: "{channel}", description: "Name des Kanals" },
    ],
  },
  {
    id: "event",
    displayName: "Twitch Event",
    description: "Wird ausgelöst bei Twitch-Events wie Follows, Subscriptions, Raids oder wenn der Stream live geht.",
    fields: [
      {
        key: "event_type", label: "Event-Typ", type: "select", required: true,
        options: [
          { value: "event.follow", label: "Neuer Follower" },
          { value: "event.subscribe", label: "Neue Subscription" },
          { value: "event.resub", label: "Resubscription" },
          { value: "event.gift", label: "Gift-Subscription (geschenkt)" },
          { value: "event.raid", label: "Raid (eingehend)" },
          { value: "event.stream_online", label: "Stream geht Live" },
        ],
        helperText: "Wähle das Twitch-Event das diese Automation auslösen soll",
      },
    ],
    variables: [
      { name: "{user}", description: "Name des Viewers/Raiders" },
      { name: "{viewers}", description: "Zuschauerzahl bei Raids" },
      { name: "{tier}", description: "Abo-Stufe (1/2/3) bei Subscriptions" },
      { name: "{months}", description: "Abo-Monate bei Resubscriptions" },
      { name: "{count}", description: "Anzahl geschenkter Subs" },
      { name: "{message}", description: "Nachricht des Viewers bei Resubs" },
      { name: "{broadcaster}", description: "Name des Streamers" },
    ],
  },
  {
    id: "keyword",
    displayName: "Chat Keyword",
    description: "Wird ausgelöst wenn ein bestimmtes Wort in einer Chat-Nachricht vorkommt. Groß/Kleinschreibung wird ignoriert.",
    fields: [
      { key: "keyword", label: "Keyword", type: "text", placeholder: "hello", helperText: "Das Wort das in der Nachricht enthalten sein muss", required: true },
    ],
    variables: [
      { name: "{user}", description: "Name des Viewers" },
      { name: "{channel}", description: "Name des Kanals" },
    ],
  },
  {
    id: "channelpoint",
    displayName: "Channel Point Einlösung",
    description: "Wird ausgelöst wenn ein Viewer eine Channel-Point-Belohnung einlöst.",
    fields: [
      { key: "reward_id", label: "Reward ID", type: "text", placeholder: "Leer = alle Einlösungen", helperText: "Leer lassen um auf JEDE Einlösung zu reagieren." },
    ],
    variables: [
      { name: "{user}", description: "Name des Viewers" },
      { name: "{reward}", description: "Name der Belohnung" },
      { name: "{input}", description: "Text-Eingabe des Viewers" },
      { name: "{cost}", description: "Kosten in Channel Points" },
    ],
  },
  {
    id: "hotkey",
    displayName: "Hotkey",
    description: "Wird ausgelöst wenn ein konfigurierter Hotkey gedrückt oder über die API getriggert wird.",
    fields: [
      { key: "hotkey_id", label: "Hotkey Binding ID", type: "text", placeholder: "1", helperText: "Die ID des Hotkeys von der Hotkeys-Seite", required: true },
    ],
    variables: [
      { name: "{hotkey}", description: "Die gedrückte Tastenkombination" },
      { name: "{description}", description: "Beschreibung des Hotkeys" },
    ],
  },
];

export const CONDITION_REGISTRY: ConditionDef[] = [
  {
    id: "role_check",
    displayName: "Benutzerrolle prüfen",
    description: "Nur ausführen wenn der User eine bestimmte Mindest-Rolle hat.",
    fields: [
      {
        key: "min_priority", label: "Mindest-Rolle", type: "select", required: true,
        options: [
          { value: "1", label: "Viewer (jeder)" },
          { value: "2", label: "Follower" },
          { value: "3", label: "Subscriber" },
          { value: "5", label: "Moderator" },
          { value: "10", label: "Broadcaster" },
        ],
        helperText: "Alle Rollen ab dieser Stufe aufwärts",
      },
    ],
  },
  {
    id: "points_check",
    displayName: "Mindestpunkte prüfen",
    description: "Nur ausführen wenn der User genug Punkte hat. Punkte werden NICHT abgezogen.",
    fields: [
      { key: "min_points", label: "Mindestpunkte", type: "number", placeholder: "100", min: 0, required: true },
    ],
  },
  {
    id: "random_chance",
    displayName: "Zufallschance",
    description: "Nur mit einer bestimmten Wahrscheinlichkeit ausführen.",
    fields: [
      { key: "percent", label: "Wahrscheinlichkeit", type: "number", placeholder: "50", min: 1, max: 100, suffix: "%", required: true },
    ],
  },
  {
    id: "stream_status",
    displayName: "Stream Status prüfen",
    description: "Nur ausführen wenn der Stream live oder offline ist.",
    fields: [
      {
        key: "require_live", label: "Stream muss sein", type: "select", required: true,
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
    displayName: "Chat-Nachricht senden",
    description: "Sendet eine Nachricht in den Twitch-Chat.",
    supportsVariables: true,
    fields: [
      { key: "message", label: "Nachricht", type: "textarea", placeholder: "Willkommen {user}!", required: true, helperText: "Variablen werden automatisch ersetzt." },
    ],
  },
  {
    id: "wait",
    displayName: "Warten",
    description: "Pausiert vor der nächsten Aktion. Max 60 Sekunden.",
    supportsVariables: false,
    fields: [
      { key: "seconds", label: "Wartezeit", type: "number", placeholder: "2", min: 1, max: 60, suffix: "Sekunden", required: true },
    ],
  },
  {
    id: "counter",
    displayName: "Counter ändern",
    description: "Erhöht, verringert oder setzt einen Counter zurück.",
    supportsVariables: false,
    fields: [
      { key: "counter_id", label: "Counter ID", type: "text", placeholder: "1", required: true, helperText: "ID von der Counters-Seite" },
      {
        key: "action", label: "Aktion", type: "select", required: true,
        options: [
          { value: "increment", label: "Erhöhen (+1)" },
          { value: "decrement", label: "Verringern (-1)" },
          { value: "reset", label: "Auf 0 zurücksetzen" },
        ],
      },
    ],
  },
  {
    id: "alert",
    displayName: "Alert anzeigen",
    description: "Zeigt einen Alert im OBS Overlay.",
    supportsVariables: true,
    fields: [
      { key: "message", label: "Alert-Text", type: "textarea", placeholder: "{user} hat etwas gemacht!", required: true },
    ],
  },
  {
    id: "variable",
    displayName: "Variable setzen",
    description: "Setzt eine Variable für nachfolgende Aktionen.",
    supportsVariables: true,
    fields: [
      { key: "name", label: "Variablenname", type: "text", placeholder: "ergebnis", required: true, helperText: "Ohne {} eingeben. Nutzung: {ergebnis}" },
      { key: "value", label: "Wert", type: "text", placeholder: "{user} hat gewonnen" },
    ],
  },
  {
    id: "discord.send_message",
    displayName: "Discord-Nachricht senden",
    description: "Sendet eine Nachricht an den konfigurierten Discord Webhook.",
    supportsVariables: true,
    fields: [
      { key: "message", label: "Nachricht", type: "textarea", placeholder: "Der Stream ist live!", required: true },
    ],
  },
  {
    id: "discord.send_embed",
    displayName: "Discord Embed senden",
    description: "Sendet eine formatierte Embed-Nachricht an Discord.",
    supportsVariables: true,
    fields: [
      { key: "title", label: "Embed-Titel", type: "text", placeholder: "Stream ist Live!", required: true },
      { key: "description", label: "Beschreibung", type: "textarea", placeholder: "{user} streamt jetzt!" },
      {
        key: "color", label: "Farbe", type: "select",
        options: [
          { value: "5793266", label: "Blau" },
          { value: "5763719", label: "Grün" },
          { value: "15548997", label: "Rot" },
          { value: "16776960", label: "Gelb" },
          { value: "10181046", label: "Lila" },
          { value: "15105570", label: "Orange" },
        ],
      },
    ],
  },
  {
    id: "obs.scene_switch",
    displayName: "OBS: Scene wechseln",
    description: "Wechselt die aktive OBS-Scene",
    supportsVariables: false,
    fields: [
      { key: "scene_name", label: "Scene Name", type: "text", placeholder: "Gaming", required: true },
    ],
  },
  {
    id: "obs.source_toggle",
    displayName: "OBS: Quelle ein-/ausblenden",
    description: "Blendet eine OBS-Quelle ein oder aus",
    supportsVariables: false,
    fields: [
      { key: "scene_name", label: "Scene Name", type: "text", placeholder: "Gaming", required: true },
      { key: "source_name", label: "Quellen-Name", type: "text", placeholder: "Webcam", required: true },
      {
        key: "visible",
        label: "Sichtbarkeit",
        type: "select",
        options: [
          { value: "", label: "Umschalten (Toggle)" },
          { value: "true", label: "Einblenden" },
          { value: "false", label: "Ausblenden" },
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
