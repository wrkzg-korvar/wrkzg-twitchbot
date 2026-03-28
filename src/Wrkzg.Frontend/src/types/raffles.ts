export interface RaffleEntryDto {
  username: string;
  twitchId: string;
  ticketCount: number;
}

export interface RaffleWinnerDto {
  displayName: string;
  twitchId: string;
}

export interface RaffleDrawDto {
  drawNumber: number;
  username: string;
  isAccepted: boolean;
  redrawReason: string | null;
  drawnAt: string;
}

export interface RaffleDto {
  id: number;
  title: string;
  keyword: string;
  isOpen: boolean;
  durationSeconds: number;
  entriesCloseAt: string;
  maxEntries: number;
  createdBy: string;
  createdAt: string;
  closedAt: string;
  endReason: string;
  winner: RaffleWinnerDto | null;
  pendingWinner: { displayName: string; twitchId: string } | null;
  draws: RaffleDrawDto[];
  entries: RaffleEntryDto[];
  entryCount: number;
}

export interface RaffleHistoryItem {
  id: number;
  title: string;
  keyword: string;
  isOpen: boolean;
  createdAt: string;
  closedAt: string;
  endReason: string;
  winnerName: string;
  entryCount: number;
}

export interface RaffleTemplate {
  key: string;
  default: string;
  description: string;
  variables: string[];
  current: string | null;
}

export interface CreateRaffleRequest {
  title: string;
  keyword?: string;
  durationSeconds?: number;
  maxEntries?: number;
}

export interface RedrawRequest {
  reason: string;
}
