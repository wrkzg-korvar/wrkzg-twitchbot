export interface ChannelPointReward {
  id: number;
  twitchRewardId: string;
  title: string;
  cost: number;
  actionType: RewardActionType;
  actionPayload: string;
  autoFulfill: boolean;
  isEnabled: boolean;
  createdAt: string;
}

export const RewardActionType = {
  ChatMessage: 0,
  CounterIncrement: 1,
  CounterDecrement: 2,
  Timeout: 3,
  Highlight: 4,
  SoundAlert: 5,
} as const;

export type RewardActionType = (typeof RewardActionType)[keyof typeof RewardActionType];

export const ACTION_TYPE_LABELS: Record<number, string> = {
  [RewardActionType.ChatMessage]: "Chat Message",
  [RewardActionType.CounterIncrement]: "Counter +1",
  [RewardActionType.CounterDecrement]: "Counter -1",
  [RewardActionType.Timeout]: "Timeout",
  [RewardActionType.Highlight]: "Overlay Alert",
  [RewardActionType.SoundAlert]: "Sound Alert",
};

export interface TwitchReward {
  id: string;
  title: string;
  cost: number;
  isEnabled: boolean;
  prompt: string | null;
  isUserInputRequired: boolean;
}
