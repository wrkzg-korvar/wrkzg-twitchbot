import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { BarChart3, TrendingUp, Clock, Users, Eye } from "lucide-react";
import {
  LineChart, Line, BarChart, Bar, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, AreaChart, Area,
} from "recharts";
import { analyticsApi } from "../api/analytics";
import { PageHeader } from "../components/ui/PageHeader";
import { Card } from "../components/ui/Card";
import { EmptyState } from "../components/ui/EmptyState";
import type { AnalyticsSession, AnalyticsSummary, AnalyticsCategory } from "../types/analytics";

const TABS = ["Overview", "Categories", "Stream History"] as const;
type Tab = (typeof TABS)[number];

const CHART_COLORS = ["#8b5cf6", "#06b6d4", "#f59e0b", "#10b981", "#ef4444", "#ec4899", "#6366f1", "#14b8a6"];

export function AnalyticsPage() {
  const [activeTab, setActiveTab] = useState<Tab>("Overview");
  const [selectedSessionId, setSelectedSessionId] = useState<number | null>(null);

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Analytics"
        description="Stream statistics, viewer trends, and category tracking."
        helpKey="analytics"
      />

      <div className="flex gap-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-1 w-fit">
        {TABS.map((tab) => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={`rounded-md px-4 py-1.5 text-sm font-medium transition-colors ${
              activeTab === tab
                ? "bg-[var(--color-brand)] text-[var(--color-bg)]"
                : "text-[var(--color-text-secondary)] hover:text-[var(--color-text)]"
            }`}
          >
            {tab}
          </button>
        ))}
      </div>

      {activeTab === "Overview" && <OverviewTab />}
      {activeTab === "Categories" && <CategoriesTab />}
      {activeTab === "Stream History" && (
        <StreamHistoryTab
          selectedSessionId={selectedSessionId}
          onSelectSession={setSelectedSessionId}
        />
      )}
    </div>
  );
}

// ─── Overview Tab ───────────────────────────────────────────

function OverviewTab() {
  const { data: summary } = useQuery<AnalyticsSummary>({
    queryKey: ["analytics-summary"],
    queryFn: () => analyticsApi.getSummary(30),
  });

  const { data: sessions } = useQuery<AnalyticsSession[]>({
    queryKey: ["analytics-sessions-overview"],
    queryFn: () => analyticsApi.getSessions(30),
  });

  if (!summary || summary.totalStreams === 0) {
    return (
      <EmptyState
        icon={BarChart3}
        title="No analytics data yet"
        description="Stream data will appear here once you go live. The bot tracks viewer counts and categories automatically."
      />
    );
  }

  // Aggregate per day — multiple sessions on the same day get merged
  const sessionsByDate = new Map<string, { avg: number[]; peak: number; hours: number }>();
  for (const s of (sessions ?? []).slice().reverse()) {
    const date = new Date(s.startedAt).toLocaleDateString("de-DE", { day: "2-digit", month: "2-digit" });
    const entry = sessionsByDate.get(date) ?? { avg: [], peak: 0, hours: 0 };
    if (s.averageViewers != null) { entry.avg.push(s.averageViewers); }
    entry.peak = Math.max(entry.peak, s.peakViewers);
    entry.hours += (s.durationMinutes ?? 0) / 60;
    sessionsByDate.set(date, entry);
  }

  const viewerTrend = [...sessionsByDate.entries()]
    .filter(([, v]) => v.avg.length > 0)
    .map(([date, v]) => ({
      date,
      avg: Math.round(v.avg.reduce((a, b) => a + b, 0) / v.avg.length),
      peak: v.peak,
    }));

  const streamHours = [...sessionsByDate.entries()]
    .filter(([, v]) => v.hours > 0)
    .map(([date, v]) => ({
      date,
      hours: Math.round(v.hours * 10) / 10,
    }));

  return (
    <div className="space-y-6">
      {/* KPI Cards */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
        <KpiCard icon={BarChart3} label="Total Streams" value={summary.totalStreams.toString()} />
        <KpiCard icon={Clock} label="Hours Streamed" value={`${summary.totalHoursStreamed}h`} />
        <KpiCard icon={Users} label="Avg Viewers" value={summary.averageViewers.toFixed(1)} />
        <KpiCard icon={Eye} label="Peak Viewers" value={summary.peakViewers.toString()} />
        <KpiCard icon={TrendingUp} label="Avg Duration" value={`${Math.round(summary.averageStreamDurationMinutes / 60)}h ${summary.averageStreamDurationMinutes % 60}m`} />
      </div>

      {/* Viewer Trend */}
      {viewerTrend.length > 1 && (
        <Card title="Viewer Trend (Last 30 Days)">
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={viewerTrend}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" />
                <XAxis dataKey="date" tick={{ fontSize: 11, fill: "var(--color-text-muted)" }} />
                <YAxis tick={{ fontSize: 11, fill: "var(--color-text-muted)" }} />
                <Tooltip contentStyle={{ background: "var(--color-surface)", border: "1px solid var(--color-border)", borderRadius: "8px", fontSize: "12px" }} />
                <Line type="monotone" dataKey="avg" stroke="#8b5cf6" name="Avg Viewers" strokeWidth={2} dot={false} />
                <Line type="monotone" dataKey="peak" stroke="#06b6d4" name="Peak Viewers" strokeWidth={1} strokeDasharray="4 4" dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </Card>
      )}

      {/* Stream Hours */}
      {streamHours.length > 1 && (
        <Card title="Stream Hours">
          <div className="h-48">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={streamHours}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" />
                <XAxis dataKey="date" tick={{ fontSize: 11, fill: "var(--color-text-muted)" }} />
                <YAxis tick={{ fontSize: 11, fill: "var(--color-text-muted)" }} />
                <Tooltip contentStyle={{ background: "var(--color-surface)", border: "1px solid var(--color-border)", borderRadius: "8px", fontSize: "12px" }} />
                <Bar dataKey="hours" fill="#8b5cf6" radius={[4, 4, 0, 0]} name="Hours" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Card>
      )}
    </div>
  );
}

// ─── Categories Tab ─────────────────────────────────────────

function CategoriesTab() {
  const { data: categories } = useQuery<AnalyticsCategory[]>({
    queryKey: ["analytics-categories"],
    queryFn: () => analyticsApi.getCategories(30),
  });

  if (!categories || categories.length === 0) {
    return (
      <EmptyState
        icon={BarChart3}
        title="No category data yet"
        description="Category tracking starts automatically when you stream."
      />
    );
  }

  const pieData = categories.map((c, i) => ({
    name: c.name,
    value: c.hours,
    color: CHART_COLORS[i % CHART_COLORS.length],
  }));

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Pie Chart */}
        <Card title="Time Distribution">
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie data={pieData} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={90} label={({ name, percent }) => `${name} ${((percent ?? 0) * 100).toFixed(0)}%`} labelLine={false}>
                  {pieData.map((entry, i) => (
                    <Cell key={i} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip contentStyle={{ background: "var(--color-surface)", border: "1px solid var(--color-border)", borderRadius: "8px", fontSize: "12px" }} />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </Card>

        {/* Category Table */}
        <Card title="Category Breakdown">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-[var(--color-border)]">
                  <th className="text-left py-2 px-1 font-medium text-[var(--color-text-secondary)]">Category</th>
                  <th className="text-right py-2 px-1 font-medium text-[var(--color-text-secondary)]">Hours</th>
                  <th className="text-right py-2 px-1 font-medium text-[var(--color-text-secondary)]">Avg</th>
                  <th className="text-right py-2 px-1 font-medium text-[var(--color-text-secondary)]">Peak</th>
                </tr>
              </thead>
              <tbody>
                {categories.map((cat, i) => (
                  <tr key={cat.name} className="border-b border-[var(--color-border)]">
                    <td className="py-2 px-1 flex items-center gap-2">
                      <span className="inline-block h-2.5 w-2.5 rounded-full" style={{ backgroundColor: CHART_COLORS[i % CHART_COLORS.length] }} />
                      {cat.name}
                    </td>
                    <td className="text-right py-2 px-1 text-[var(--color-text-muted)]">{cat.hours}h</td>
                    <td className="text-right py-2 px-1 text-[var(--color-text-muted)]">{cat.avgViewers}</td>
                    <td className="text-right py-2 px-1 text-[var(--color-text-muted)]">{cat.peakViewers}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      </div>
    </div>
  );
}

// ─── Stream History Tab ─────────────────────────────────────

function StreamHistoryTab({
  selectedSessionId,
  onSelectSession,
}: {
  selectedSessionId: number | null;
  onSelectSession: (id: number) => void;
}) {
  const [days, setDays] = useState(30);

  const { data: sessions } = useQuery<AnalyticsSession[]>({
    queryKey: ["analytics-sessions", days],
    queryFn: () => analyticsApi.getSessions(days),
  });

  const { data: selectedSession } = useQuery<AnalyticsSession>({
    queryKey: ["analytics-session", selectedSessionId],
    queryFn: () => analyticsApi.getSession(selectedSessionId!),
    enabled: selectedSessionId !== null,
  });

  if (!sessions || sessions.length === 0) {
    return (
      <EmptyState
        icon={BarChart3}
        title="No stream sessions yet"
        description="Go live and stream data will be recorded here."
      />
    );
  }

  return (
    <div className="flex gap-4" style={{ height: "calc(100vh - 250px)" }}>
      {/* Session List — scrollable container */}
      <div className="w-72 shrink-0 flex flex-col rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)]">
        <div className="flex items-center justify-between border-b border-[var(--color-border)] px-3 py-2">
          <span className="text-xs font-semibold text-[var(--color-text)]">Sessions</span>
          <select value={days} onChange={(e) => setDays(Number(e.target.value))}
            className="rounded border border-[var(--color-border)] bg-[var(--color-elevated)] px-2 py-0.5 text-xs text-[var(--color-text)]">
            <option value={7}>Last 7 days</option>
            <option value={14}>Last 14 days</option>
            <option value={30}>Last 30 days</option>
            <option value={90}>Last 90 days</option>
            <option value={365}>Last year</option>
            <option value={9999}>All time</option>
          </select>
        </div>
        <div className="flex-1 overflow-y-auto p-2 space-y-1.5">
          {sessions.map((session) => (
            <button
              key={session.id}
              onClick={() => onSelectSession(session.id)}
              className={`w-full text-left rounded-lg border p-2.5 transition-colors ${
                selectedSessionId === session.id
                  ? "border-[var(--color-brand)] bg-[var(--color-brand-subtle)]"
                  : "border-transparent hover:bg-[var(--color-elevated)]"
              }`}
            >
              <div className="text-sm font-medium text-[var(--color-text)]">
                {new Date(session.startedAt).toLocaleDateString("de-DE", { weekday: "short", day: "2-digit", month: "2-digit", year: "numeric" })}
              </div>
              <div className="text-xs text-[var(--color-text-muted)] mt-0.5">
                {session.durationMinutes ? `${Math.floor(session.durationMinutes / 60)}h ${session.durationMinutes % 60}m` : "Live"}
                {" · "}Peak: {session.peakViewers}
                {session.averageViewers != null && ` · Avg: ${Math.round(session.averageViewers)}`}
              </div>
              {session.title && (
                <div className="text-[11px] text-[var(--color-text-muted)] truncate mt-0.5">{session.title}</div>
              )}
            </button>
          ))}
        </div>
        <div className="border-t border-[var(--color-border)] px-3 py-1.5 text-[10px] text-[var(--color-text-muted)]">
          {sessions.length} session{sessions.length !== 1 ? "s" : ""}
        </div>
      </div>

      {/* Session Detail */}
      <div className="flex-1 overflow-y-auto">
        {!selectedSession ? (
          <div className="flex h-full items-center justify-center rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] text-sm text-[var(--color-text-muted)]">
            Select a session to view details
          </div>
        ) : (
          <div className="space-y-4">
            {/* Title */}
            {selectedSession.title && (
              <div className="text-sm font-medium text-[var(--color-text)]">{selectedSession.title}</div>
            )}

            {/* KPIs */}
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <KpiCard icon={Clock} label="Duration" value={selectedSession.durationMinutes ? `${Math.floor(selectedSession.durationMinutes / 60)}h ${selectedSession.durationMinutes % 60}m` : "Live"} />
              <KpiCard icon={Eye} label="Peak" value={selectedSession.peakViewers.toString()} />
              <KpiCard icon={Users} label="Average" value={selectedSession.averageViewers != null ? Math.round(selectedSession.averageViewers).toString() : "—"} />
              <KpiCard icon={BarChart3} label="Categories" value={selectedSession.categories.length.toString()} />
            </div>

            {/* Viewer Chart — always show, with fallback for no snapshots */}
            <Card title="Viewer Count">
              <div className="h-56">
                {selectedSession.snapshots && selectedSession.snapshots.length > 1 ? (
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart data={selectedSession.snapshots.map((s) => ({
                      time: new Date(s.timestamp).toLocaleTimeString("de-DE", { hour: "2-digit", minute: "2-digit" }),
                      viewers: s.viewerCount,
                    }))}>
                      <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" />
                      <XAxis dataKey="time" tick={{ fontSize: 10, fill: "var(--color-text-muted)" }} />
                      <YAxis tick={{ fontSize: 11, fill: "var(--color-text-muted)" }} />
                      <Tooltip contentStyle={{ background: "var(--color-surface)", border: "1px solid var(--color-border)", borderRadius: "8px", fontSize: "12px" }} />
                      <Area type="monotone" dataKey="viewers" stroke="#8b5cf6" fill="#8b5cf620" strokeWidth={2} name="Viewers" />
                    </AreaChart>
                  </ResponsiveContainer>
                ) : (
                  <div className="flex h-full flex-col items-center justify-center text-[var(--color-text-muted)]">
                    <BarChart3 className="h-8 w-8 mb-2 opacity-30" />
                    <p className="text-sm">No viewer snapshots for this session</p>
                    <p className="text-xs mt-1">Snapshots are recorded every 60 seconds while live</p>
                  </div>
                )}
              </div>
            </Card>

            {/* Category Timeline */}
            {selectedSession.categories.length > 0 && (
              <Card title="Categories">
                <div className="space-y-2">
                  {selectedSession.categories.map((cat, i) => (
                    <div key={i} className="flex items-center gap-3">
                      <span
                        className="inline-block h-3 w-3 rounded-full flex-shrink-0"
                        style={{ backgroundColor: CHART_COLORS[i % CHART_COLORS.length] }}
                      />
                      <span className="text-sm text-[var(--color-text)] flex-1">{cat.categoryName}</span>
                      <span className="text-xs text-[var(--color-text-muted)]">
                        {cat.durationMinutes ? `${cat.durationMinutes}m` : "Active"}
                      </span>
                    </div>
                  ))}
                </div>
              </Card>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Shared Components ──────────────────────────────────────

function KpiCard({ icon: Icon, label, value }: { icon: typeof BarChart3; label: string; value: string }) {
  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <div className="flex items-center gap-2 text-[var(--color-text-muted)] mb-1">
        <Icon className="h-4 w-4" />
        <span className="text-xs">{label}</span>
      </div>
      <div className="text-xl font-bold text-[var(--color-text)]">{value}</div>
    </div>
  );
}
