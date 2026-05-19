import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { BarChart3, TrendingUp, Clock, Users, Eye, MessageSquare, UserPlus, Heart } from "lucide-react";
import {
  LineChart, Line, BarChart, Bar, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, AreaChart, Area, ReferenceArea,
} from "recharts";
import { analyticsApi } from "../api/analytics";
import { PageHeader } from "../components/ui/PageHeader";
import { Card } from "../components/ui/Card";
import { EmptyState } from "../components/ui/EmptyState";
import type {
  AnalyticsSession,
  AnalyticsSummary,
  AnalyticsCategory,
  AnalyticsCategorySegment,
  AnalyticsSnapshot,
} from "../types/analytics";

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
  const { data: summary, isLoading, isError } = useQuery<AnalyticsSummary>({
    queryKey: ["analytics-summary"],
    queryFn: () => analyticsApi.getSummary(30),
  });

  const { data: sessions } = useQuery<AnalyticsSession[]>({
    queryKey: ["analytics-sessions-overview"],
    queryFn: () => analyticsApi.getSessions(30),
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-border)] border-t-[var(--color-brand)]" />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-[var(--color-text-muted)]">
        <p className="text-lg font-medium">Failed to load data</p>
        <p className="mt-1 text-sm">Please check your connection and try again.</p>
      </div>
    );
  }

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
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4 lg:grid-cols-4">
        <KpiCard icon={BarChart3} label="Total Streams" value={summary.totalStreams.toString()} />
        <KpiCard icon={Clock} label="Hours Streamed" value={`${summary.totalHoursStreamed}h`} />
        <KpiCard icon={Users} label="Avg Viewers" value={summary.averageViewers.toFixed(1)} />
        <KpiCard icon={Eye} label="Peak Viewers" value={summary.peakViewers.toString()} />
        <KpiCard icon={TrendingUp} label="Avg Duration" value={`${Math.round(summary.averageStreamDurationMinutes / 60)}h ${summary.averageStreamDurationMinutes % 60}m`} />
        <KpiCard icon={Users} label="Unique Chatters" value={(summary.totalUniqueChatters ?? 0).toLocaleString()} />
        <KpiCard icon={MessageSquare} label="Messages" value={(summary.totalMessages ?? 0).toLocaleString()} />
        <KpiCard icon={Heart} label="New Followers" value={(summary.totalNewFollowers ?? 0).toLocaleString()} />
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
                <Tooltip
                  contentStyle={{ background: "var(--color-surface)", border: "1px solid var(--color-border)", borderRadius: "8px", fontSize: "12px", color: "var(--color-text)" }}
                  itemStyle={{ color: "var(--color-text)" }}
                />
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
                <Tooltip
                  contentStyle={{ background: "var(--color-surface)", border: "1px solid var(--color-border)", borderRadius: "8px", fontSize: "12px", color: "var(--color-text)" }}
                  itemStyle={{ color: "var(--color-text)" }}
                />
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
  const [days, setDays] = useState(30);

  const { data: categories } = useQuery<AnalyticsCategory[]>({
    queryKey: ["analytics-categories", days],
    queryFn: () => analyticsApi.getCategories(days),
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

  const totalHours = categories.reduce((sum, c) => sum + c.hours, 0);

  const pieData = categories.map((c, i) => ({
    name: c.name,
    value: c.hours,
    color: CHART_COLORS[i % CHART_COLORS.length],
  }));

  const barData = categories.slice(0, 10).map((c, i) => ({
    name: c.name.length > 20 ? c.name.slice(0, 18) + "…" : c.name,
    fullName: c.name,
    hours: c.hours,
    fill: CHART_COLORS[i % CHART_COLORS.length],
  }));

  const tooltipStyle = {
    background: "var(--color-surface)",
    border: "1px solid var(--color-border)",
    borderRadius: "8px",
    fontSize: "12px",
    color: "var(--color-text)",
  };

  return (
    <div className="space-y-6">
      {/* Period selector */}
      <div className="flex justify-end">
        <select
          value={days}
          onChange={(e) => setDays(Number(e.target.value))}
          className="rounded border border-[var(--color-border)] bg-[var(--color-elevated)] px-3 py-1.5 text-sm text-[var(--color-text)]"
        >
          <option value={7}>Last 7 days</option>
          <option value={14}>Last 14 days</option>
          <option value={30}>Last 30 days</option>
          <option value={90}>Last 90 days</option>
          <option value={365}>Last year</option>
          <option value={9999}>All time</option>
        </select>
      </div>

      {/* Charts row */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Pie Chart */}
        <Card title="Time Distribution">
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={pieData}
                  dataKey="value"
                  nameKey="name"
                  cx="50%"
                  cy="50%"
                  outerRadius={90}
                  innerRadius={45}
                  label={({ name, percent }) => {
                    const displayName = String(name ?? "");
                    const truncated = displayName.length > 15 ? displayName.slice(0, 13) + "…" : displayName;
                    return `${truncated} ${((percent ?? 0) * 100).toFixed(0)}%`;
                  }}
                  labelLine={false}
                >
                  {pieData.map((entry, i) => (
                    <Cell key={i} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip
                  contentStyle={tooltipStyle}
                  itemStyle={{ color: "var(--color-text)" }}
                  formatter={(value) => [`${value}h`, "Hours"]}
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </Card>

        {/* Horizontal Bar Chart — hours per category */}
        <Card title="Hours by Category">
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={barData} layout="vertical" margin={{ left: 10, right: 20 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" horizontal={false} />
                <XAxis
                  type="number"
                  tick={{ fontSize: 11, fill: "var(--color-text-muted)" }}
                  tickFormatter={(v: number) => `${v}h`}
                />
                <YAxis
                  type="category"
                  dataKey="name"
                  width={120}
                  tick={{ fontSize: 11, fill: "var(--color-text-muted)" }}
                />
                <Tooltip
                  contentStyle={tooltipStyle}
                  itemStyle={{ color: "var(--color-text)" }}
                  labelFormatter={(label) => {
                    const item = barData.find((b) => b.name === label);
                    return item?.fullName ?? String(label ?? "");
                  }}
                  formatter={(value) => [`${value}h`, "Hours"]}
                />
                <Bar dataKey="hours" radius={[0, 4, 4, 0]}>
                  {barData.map((entry, i) => (
                    <Cell key={i} fill={entry.fill} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Card>
      </div>

      {/* Full-width category table */}
      <Card title="Category Breakdown">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[var(--color-border)]">
                <th className="text-left py-2.5 px-3 font-medium text-[var(--color-text-secondary)]">Category</th>
                <th className="text-right py-2.5 px-3 font-medium text-[var(--color-text-secondary)]">Hours</th>
                <th className="text-right py-2.5 px-3 font-medium text-[var(--color-text-secondary)]">% of Total</th>
                <th className="text-right py-2.5 px-3 font-medium text-[var(--color-text-secondary)]">Avg Viewers</th>
                <th className="text-right py-2.5 px-3 font-medium text-[var(--color-text-secondary)]">Peak Viewers</th>
                <th className="text-right py-2.5 px-3 font-medium text-[var(--color-text-secondary)]">Sessions</th>
              </tr>
            </thead>
            <tbody>
              {categories.map((cat, i) => {
                const pct = totalHours > 0 ? ((cat.hours / totalHours) * 100).toFixed(1) : "0.0";
                return (
                  <tr key={cat.name} className="border-b border-[var(--color-border)] hover:bg-[var(--color-elevated)] transition-colors">
                    <td className="py-2.5 px-3">
                      <div className="flex items-center gap-2.5">
                        <span
                          className="inline-block h-3 w-3 rounded-sm flex-shrink-0"
                          style={{ backgroundColor: CHART_COLORS[i % CHART_COLORS.length] }}
                        />
                        <span className="text-[var(--color-text)]">{cat.name}</span>
                      </div>
                    </td>
                    <td className="text-right py-2.5 px-3 text-[var(--color-text)] tabular-nums font-medium">{cat.hours}h</td>
                    <td className="text-right py-2.5 px-3 text-[var(--color-text-muted)] tabular-nums">{pct}%</td>
                    <td className="text-right py-2.5 px-3 text-[var(--color-text-muted)] tabular-nums">
                      {cat.avgViewers > 0 ? cat.avgViewers.toLocaleString() : "—"}
                    </td>
                    <td className="text-right py-2.5 px-3 text-[var(--color-text-muted)] tabular-nums">
                      {cat.peakViewers > 0 ? cat.peakViewers.toLocaleString() : "—"}
                    </td>
                    <td className="text-right py-2.5 px-3 text-[var(--color-text-muted)] tabular-nums">{cat.sessions}</td>
                  </tr>
                );
              })}
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-[var(--color-border)]">
                <td className="py-2.5 px-3 font-semibold text-[var(--color-text)]">Total</td>
                <td className="text-right py-2.5 px-3 font-semibold text-[var(--color-text)] tabular-nums">
                  {Math.round(totalHours * 10) / 10}h
                </td>
                <td className="text-right py-2.5 px-3 text-[var(--color-text-muted)]">100%</td>
                <td className="text-right py-2.5 px-3 text-[var(--color-text-muted)] tabular-nums">
                  {categories.length > 0 && totalHours > 0
                    ? Math.round(
                        categories.reduce((s, c) => s + c.avgViewers * c.hours, 0) / totalHours
                      ).toLocaleString()
                    : "—"}
                </td>
                <td className="text-right py-2.5 px-3 text-[var(--color-text-muted)] tabular-nums">
                  {Math.max(...categories.map((c) => c.peakViewers)).toLocaleString()}
                </td>
                <td className="text-right py-2.5 px-3"></td>
              </tr>
            </tfoot>
          </table>
        </div>
      </Card>
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
                {session.totalMessages != null && session.totalMessages > 0 && ` · ${session.totalMessages.toLocaleString()} msgs`}
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
              <KpiCard icon={Eye} label="Peak Viewers" value={selectedSession.peakViewers.toString()} />
              <KpiCard icon={Users} label="Avg Viewers" value={selectedSession.averageViewers != null ? Math.round(selectedSession.averageViewers).toString() : "—"} />
              <KpiCard icon={MessageSquare} label="Messages" value={selectedSession.totalMessages != null ? selectedSession.totalMessages.toLocaleString() : "—"} />
              <KpiCard icon={Users} label="Unique Chatters" value={selectedSession.uniqueChatters != null ? selectedSession.uniqueChatters.toLocaleString() : "—"} />
              <KpiCard icon={Heart} label="New Followers" value={selectedSession.newFollowers != null ? `+${selectedSession.newFollowers}` : "—"} />
              <KpiCard icon={UserPlus} label="New Subscribers" value={selectedSession.newSubscribers != null ? `+${selectedSession.newSubscribers}` : "—"} />
              <KpiCard icon={BarChart3} label="Categories" value={selectedSession.categories.length.toString()} />
            </div>

            {/* Viewer Chart with Category Overlay */}
            <Card title="Viewer Count">
              <div className="h-64">
                {selectedSession.snapshots && selectedSession.snapshots.length > 1 ? (
                  <ViewerChartWithCategories
                    snapshots={selectedSession.snapshots}
                    categories={selectedSession.categories}
                  />
                ) : (
                  <div className="flex h-full flex-col items-center justify-center text-[var(--color-text-muted)]">
                    <BarChart3 className="h-8 w-8 mb-2 opacity-30" />
                    <p className="text-sm">No viewer snapshots for this session</p>
                    <p className="text-xs mt-1">Snapshots are recorded every 60 seconds while live</p>
                  </div>
                )}
              </div>
            </Card>

            {/* Category Segments */}
            {selectedSession.categories.length > 0 && (
              <Card title="Category Segments">
                <div className="space-y-2">
                  {selectedSession.categories.map((cat, i) => {
                    const uniqueNames = [...new Set(selectedSession.categories.map((c) => c.categoryName))];
                    const color = CHART_COLORS[uniqueNames.indexOf(cat.categoryName) % CHART_COLORS.length];
                    const startTime = new Date(cat.startedAt).toLocaleTimeString("de-DE", { hour: "2-digit", minute: "2-digit" });
                    const endTime = cat.endedAt
                      ? new Date(cat.endedAt).toLocaleTimeString("de-DE", { hour: "2-digit", minute: "2-digit" })
                      : "Live";
                    return (
                      <div key={i} className="flex items-center gap-3">
                        <span
                          className="inline-block h-3 w-3 rounded-sm flex-shrink-0"
                          style={{ backgroundColor: color, opacity: 0.6 }}
                        />
                        <span className="text-sm text-[var(--color-text)] flex-1">{cat.categoryName}</span>
                        <span className="text-xs text-[var(--color-text-muted)] tabular-nums">
                          {startTime} – {endTime}
                        </span>
                        <span className="text-xs text-[var(--color-text-muted)] w-14 text-right tabular-nums">
                          {cat.durationMinutes ? `${cat.durationMinutes}m` : "Active"}
                        </span>
                      </div>
                    );
                  })}
                </div>
              </Card>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Viewer Chart with Category Overlay ─────────────────────

function ViewerChartWithCategories({
  snapshots,
  categories,
}: {
  snapshots: AnalyticsSnapshot[];
  categories: AnalyticsCategorySegment[];
}) {
  // Build a stable color map: each unique category name → one color.
  const categoryColorMap = new Map<string, string>();
  categories.forEach((cat) => {
    if (!categoryColorMap.has(cat.categoryName)) {
      categoryColorMap.set(
        cat.categoryName,
        CHART_COLORS[categoryColorMap.size % CHART_COLORS.length]
      );
    }
  });

  // Transform snapshots into chart data with numeric timestamps.
  // Attach the active category to each data point for the tooltip.
  const chartData = snapshots.map((s) => {
    const ts = new Date(s.timestamp).getTime();
    const activeCat = categories.find((c) => {
      const start = new Date(c.startedAt).getTime();
      const end = c.endedAt ? new Date(c.endedAt).getTime() : Infinity;
      return ts >= start && ts <= end;
    });
    return {
      timestamp: ts,
      viewers: s.viewerCount,
      category: activeCat?.categoryName ?? "",
    };
  });

  const firstTs = chartData[0]?.timestamp ?? 0;
  const lastTs = chartData[chartData.length - 1]?.timestamp ?? 0;

  const categoryAreas = categories.map((cat) => {
    const rawStart = new Date(cat.startedAt).getTime();
    const rawEnd = cat.endedAt ? new Date(cat.endedAt).getTime() : lastTs;
    return {
      x1: Math.max(rawStart, firstTs),
      x2: Math.min(rawEnd, lastTs),
      color: categoryColorMap.get(cat.categoryName) ?? CHART_COLORS[0],
      label: cat.categoryName,
    };
  });

  const formatTime = (ts: number) =>
    new Date(ts).toLocaleTimeString("de-DE", { hour: "2-digit", minute: "2-digit" });

  return (
    <div className="space-y-3">
      <ResponsiveContainer width="100%" height={220}>
        <AreaChart data={chartData}>
          {categoryAreas.map((area, i) => (
            <ReferenceArea
              key={i}
              x1={area.x1}
              x2={area.x2}
              fill={area.color}
              fillOpacity={0.08}
              ifOverflow="hidden"
            />
          ))}

          <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" />
          <XAxis
            dataKey="timestamp"
            type="number"
            domain={["dataMin", "dataMax"]}
            tickFormatter={formatTime}
            tick={{ fontSize: 10, fill: "var(--color-text-muted)" }}
          />
          <YAxis tick={{ fontSize: 11, fill: "var(--color-text-muted)" }} />
          <Tooltip
            content={({ active, payload, label }) => {
              if (!active || !payload?.length) {
                return null;
              }
              const point = payload[0]?.payload as { viewers: number; category: string } | undefined;
              return (
                <div
                  style={{
                    background: "var(--color-surface)",
                    border: "1px solid var(--color-border)",
                    borderRadius: "8px",
                    padding: "8px 12px",
                    fontSize: "12px",
                  }}
                >
                  <div style={{ color: "var(--color-text-muted)", marginBottom: "4px" }}>
                    {formatTime(label as number)}
                  </div>
                  <div style={{ color: "var(--color-text)", fontWeight: 600 }}>
                    {point?.viewers?.toLocaleString()} viewers
                  </div>
                  {point?.category && (
                    <div
                      style={{
                        color: categoryColorMap.get(point.category) ?? "var(--color-text-muted)",
                        marginTop: "2px",
                        fontSize: "11px",
                      }}
                    >
                      {point.category}
                    </div>
                  )}
                </div>
              );
            }}
          />
          <Area
            type="monotone"
            dataKey="viewers"
            stroke="#8b5cf6"
            fill="#8b5cf620"
            strokeWidth={2}
            name="Viewers"
          />
        </AreaChart>
      </ResponsiveContainer>

      {categoryColorMap.size > 0 && (
        <div className="flex flex-wrap gap-x-4 gap-y-1 px-1">
          {[...categoryColorMap.entries()].map(([name, color]) => (
            <div key={name} className="flex items-center gap-1.5">
              <span
                className="inline-block h-2.5 w-2.5 rounded-sm"
                style={{ backgroundColor: color, opacity: 0.6 }}
              />
              <span className="text-xs text-[var(--color-text-muted)]">{name}</span>
            </div>
          ))}
        </div>
      )}
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
