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

  const viewerTrend = (sessions ?? [])
    .filter((s) => s.averageViewers != null)
    .map((s) => ({
      date: new Date(s.startedAt).toLocaleDateString("de-DE", { day: "2-digit", month: "2-digit" }),
      avg: Math.round(s.averageViewers ?? 0),
      peak: s.peakViewers,
    }))
    .reverse();

  const streamHours = (sessions ?? [])
    .filter((s) => s.durationMinutes != null)
    .map((s) => ({
      date: new Date(s.startedAt).toLocaleDateString("de-DE", { day: "2-digit", month: "2-digit" }),
      hours: Math.round(((s.durationMinutes ?? 0) / 60) * 10) / 10,
    }))
    .reverse();

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
  const { data: sessions } = useQuery<AnalyticsSession[]>({
    queryKey: ["analytics-sessions"],
    queryFn: () => analyticsApi.getSessions(50),
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
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
      {/* Session List */}
      <div className="space-y-2">
        <h3 className="text-sm font-semibold text-[var(--color-text)]">Sessions</h3>
        {sessions.map((session) => (
          <button
            key={session.id}
            onClick={() => onSelectSession(session.id)}
            className={`w-full text-left rounded-lg border p-3 transition-colors ${
              selectedSessionId === session.id
                ? "border-[var(--color-brand)] bg-[var(--color-brand-subtle)]"
                : "border-[var(--color-border)] hover:bg-[var(--color-elevated)]"
            }`}
          >
            <div className="text-sm font-medium text-[var(--color-text)]">
              {new Date(session.startedAt).toLocaleDateString("de-DE", { weekday: "short", day: "2-digit", month: "2-digit", year: "numeric" })}
            </div>
            <div className="text-xs text-[var(--color-text-muted)] mt-1">
              {session.durationMinutes ? `${Math.floor(session.durationMinutes / 60)}h ${session.durationMinutes % 60}m` : "Live"}
              {" · "}Peak: {session.peakViewers}
              {session.averageViewers != null && ` · Avg: ${Math.round(session.averageViewers)}`}
            </div>
            {session.title && (
              <div className="text-xs text-[var(--color-text-muted)] truncate mt-1">{session.title}</div>
            )}
          </button>
        ))}
      </div>

      {/* Session Detail */}
      <div className="lg:col-span-2">
        {!selectedSession ? (
          <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-8 text-center text-sm text-[var(--color-text-muted)]">
            Select a session to view details
          </div>
        ) : (
          <div className="space-y-4">
            {/* KPIs */}
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <KpiCard icon={Clock} label="Duration" value={selectedSession.durationMinutes ? `${Math.floor(selectedSession.durationMinutes / 60)}h ${selectedSession.durationMinutes % 60}m` : "Live"} />
              <KpiCard icon={Eye} label="Peak" value={selectedSession.peakViewers.toString()} />
              <KpiCard icon={Users} label="Average" value={selectedSession.averageViewers != null ? Math.round(selectedSession.averageViewers).toString() : "—"} />
              <KpiCard icon={BarChart3} label="Categories" value={selectedSession.categories.length.toString()} />
            </div>

            {/* Viewer Chart */}
            {selectedSession.snapshots && selectedSession.snapshots.length > 1 && (
              <Card title="Viewer Count">
                <div className="h-56">
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
                </div>
              </Card>
            )}

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
