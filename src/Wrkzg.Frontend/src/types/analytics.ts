export interface AnalyticsSession {
  id: number;
  twitchStreamId: string | null;
  startedAt: string;
  endedAt: string | null;
  durationMinutes: number | null;
  peakViewers: number;
  averageViewers: number | null;
  title: string | null;
  categories: AnalyticsCategorySegment[];
  snapshots?: AnalyticsSnapshot[];
}

export interface AnalyticsCategorySegment {
  categoryName: string;
  twitchCategoryId?: string;
  durationMinutes: number | null;
  peakViewers?: number | null;
  averageViewers?: number | null;
  startedAt: string;
  endedAt: string | null;
}

export interface AnalyticsSnapshot {
  viewerCount: number;
  timestamp: string;
}

export interface AnalyticsSummary {
  period: { from: string; to: string };
  totalStreams: number;
  totalHoursStreamed: number;
  averageStreamDurationMinutes: number;
  averageViewers: number;
  peakViewers: number;
  topCategories: {
    name: string;
    hours: number;
    avgViewers: number;
    sessions: number;
  }[];
}

export interface AnalyticsCategory {
  name: string;
  totalMinutes: number;
  hours: number;
  avgViewers: number;
  peakViewers: number;
  sessions: number;
}
