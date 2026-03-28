export interface ChatMsg {
  username: string;
  displayName: string;
  content: string;
  isMod: boolean;
  isSubscriber: boolean;
  isBroadcaster: boolean;
  timestamp: string;
  emotes?: Record<string, string[]> | null;
}

export interface LiveEvent {
  type: "follow" | "subscribe" | "gift" | "resub" | "raid";
  username: string;
  detail?: string;
  timestamp: string;
}

export interface StatusResponse {
  bot: { isConnected: boolean; channel: string | null };
  stream: {
    isLive: boolean;
    viewerCount: number;
    title: string | null;
    game: string | null;
    startedAt: string | null;
  };
  auth: { botTokenPresent: boolean; broadcasterTokenPresent: boolean };
}

export interface SendChatRequest {
  message: string;
  sendAs: "bot" | "broadcaster";
}
