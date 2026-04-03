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
] as const;
