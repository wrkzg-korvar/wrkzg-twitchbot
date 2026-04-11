export interface TimedMessage {
  id: number;
  name: string;
  messages: string[];
  nextMessageIndex: number;
  intervalMinutes: number;
  minChatLines: number;
  isEnabled: boolean;
  runWhenOnline: boolean;
  runWhenOffline: boolean;
  isAnnouncement: boolean;
  announcementColor?: string;
  lastFiredAt: string | null;
  createdAt: string;
}

export interface CreateTimerRequest {
  name: string;
  messages: string[];
  intervalMinutes: number;
  minChatLines: number;
  isEnabled: boolean;
  runWhenOnline: boolean;
  runWhenOffline: boolean;
  isAnnouncement: boolean;
  announcementColor?: string;
}

export type UpdateTimerRequest = Partial<CreateTimerRequest>;
