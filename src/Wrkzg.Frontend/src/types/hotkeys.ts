export interface HotkeyBinding {
  id: number;
  keyCombination: string;
  actionType: string;
  actionPayload: string;
  description: string | null;
  isEnabled: boolean;
  createdAt: string;
}

export const ACTION_TYPES = [
  { value: "ChatMessage", label: "Send Chat Message" },
  { value: "CounterIncrement", label: "Counter +1" },
  { value: "CounterDecrement", label: "Counter -1" },
  { value: "CounterReset", label: "Counter Reset" },
  { value: "RunEffect", label: "Run Automation" },
  { value: "PollStart", label: "Start Poll" },
  { value: "PollEnd", label: "End Active Poll" },
  { value: "RaffleStart", label: "Start Raffle" },
  { value: "SongSkip", label: "Skip Song" },
  { value: "PlayAlert", label: "Show Alert" },
  { value: "ObsSceneSwitch", label: "OBS: Switch Scene" },
  { value: "ObsSourceToggle", label: "OBS: Toggle Source" },
] as const;
