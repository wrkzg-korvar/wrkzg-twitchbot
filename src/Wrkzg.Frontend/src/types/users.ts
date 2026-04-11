export interface User {
  id: number;
  twitchId: string;
  username: string;
  displayName: string;
  points: number;
  watchedMinutes: number;
  messageCount: number;
  isSubscriber: boolean;
  subscriberTier: number;
  isMod: boolean;
  isBroadcaster: boolean;
  isBanned: boolean;
  followDate: string | null;
  firstSeenAt: string;
  lastSeenAt: string;
}
