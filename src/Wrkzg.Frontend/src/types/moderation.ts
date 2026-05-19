export interface ModerationEvent {
  id: number;
  twitchUserId: string;
  displayName: string;
  eventType: string;
  actor: string;
  reason: string | null;
  durationSeconds: number | null;
  twitchSuccess: boolean | null;
  createdAt: string;
}

export interface LiveViewer {
  twitchId: string;
  username: string;
  displayName: string;
  isMod: boolean;
  isSubscriber: boolean;
  isBroadcaster: boolean;
  isBanned: boolean;
  isTwitchBanned: boolean;
}

export interface TimeoutRequest {
  twitchUserId: string;
  durationSeconds: number;
  displayName?: string;
  reason?: string;
}

export interface BanRequest {
  twitchUserId: string;
  displayName?: string;
  reason?: string;
}

export interface ShoutoutRequest {
  twitchUserId: string;
  displayName?: string;
}
