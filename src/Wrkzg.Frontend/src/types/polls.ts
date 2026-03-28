export interface PollOptionResult {
  index: number;
  label: string;
  votes: number;
  percentage: number;
}

export interface PollResults {
  id: number;
  question: string;
  isActive: boolean;
  source: string;
  createdBy: string;
  createdAt: string;
  endsAt: string;
  endReason: string;
  totalVotes: number;
  options: PollOptionResult[];
  winnerIndex: number | null;
}

export interface PollHistoryItem {
  id: number;
  question: string;
  options: string[];
  isActive: boolean;
  source: string;
  createdBy: string;
  createdAt: string;
  endsAt: string;
  durationSeconds: number;
  endReason: string;
  totalVotes: number;
  winnerIndex: number | null;
}

export interface PollTemplate {
  key: string;
  default: string;
  description: string;
  variables: string[];
  current: string | null;
}

export interface CreatePollRequest {
  question: string;
  options: string[];
  durationSeconds: number;
  createdBy: string;
}
