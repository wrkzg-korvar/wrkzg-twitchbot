export interface SongRequest {
  id: number;
  videoId: string;
  title: string;
  thumbnailUrl: string | null;
  durationSeconds: number;
  requestedBy: string;
  pointsCost: number | null;
  status: SongRequestStatus;
  requestedAt: string;
  playedAt: string | null;
}

export const SongRequestStatus = {
  Queued: 0,
  Playing: 1,
  Played: 2,
  Skipped: 3,
} as const;

export type SongRequestStatus = (typeof SongRequestStatus)[keyof typeof SongRequestStatus];
