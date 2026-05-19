import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { X, Clock, Shield, ShieldOff, Megaphone, History, Ban } from "lucide-react";
import { usersApi } from "../../../api/users";
import { moderationApi } from "../../../api/moderation";
import { ConfirmDialog } from "../../ui/ConfirmDialog";
import { showToast } from "../../../hooks/useToast";
import type { User } from "../../../types/users";
import type { ModerationEvent } from "../../../types/moderation";

interface UserDetailModalProps {
  userId: number;
  onClose: () => void;
  readOnly?: boolean;
  botIsModerator?: boolean;
}

const TIMEOUT_PRESETS = [
  { label: "1m", seconds: 60 },
  { label: "5m", seconds: 300 },
  { label: "10m", seconds: 600 },
  { label: "30m", seconds: 1800 },
  { label: "1h", seconds: 3600 },
];

export function UserDetailModal({ userId, onClose, readOnly, botIsModerator = false }: UserDetailModalProps) {
  const queryClient = useQueryClient();
  const overlayRef = useRef<HTMLDivElement>(null);
  const [showBanConfirm, setShowBanConfirm] = useState(false);
  const [showTwitchBanConfirm, setShowTwitchBanConfirm] = useState(false);
  const [banReason, setBanReason] = useState("");
  const [historyDays, setHistoryDays] = useState(90);
  const [historyPage, setHistoryPage] = useState(1);
  const HISTORY_PAGE_SIZE = 15;

  // ─── Fetch user data reactively ─────────────────────
  const { data: user, isLoading: userLoading } = useQuery<User>({
    queryKey: ["user", userId],
    queryFn: () => usersApi.getById(userId),
  });

  const [points, setPoints] = useState("");

  // Sync points input when user data loads or refreshes
  useEffect(() => {
    if (user) {
      setPoints(String(user.points));
    }
  }, [user]);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [onClose]);

  // ─── Mutations ──────────────────────────────────────
  // Mutation callbacks close over `user` lazily — they're only invoked when a
  // button is clicked, which only happens after the loading guard passes.

  const updatePointsMutation = useMutation({
    mutationFn: () => usersApi.update(user!.id, { points: Number(points) }),
    onSuccess: () => {
      showToast("success", "Points updated");
      queryClient.invalidateQueries({ queryKey: ["user", userId] });
      queryClient.invalidateQueries({ queryKey: ["users"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const toggleBotBanMutation = useMutation({
    mutationFn: () => usersApi.update(user!.id, { isBanned: !user!.isBanned }),
    onSuccess: () => {
      showToast("success", user!.isBanned ? "Bot access restored" : "Excluded from bot features");
      queryClient.invalidateQueries({ queryKey: ["user", userId] });
      queryClient.invalidateQueries({ queryKey: ["users"] });
      queryClient.invalidateQueries({ queryKey: ["moderation-log", user!.twitchId] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const timeoutMutation = useMutation({
    mutationFn: (seconds: number) =>
      moderationApi.timeout({ twitchUserId: user!.twitchId, durationSeconds: seconds, displayName: user!.displayName }),
    onSuccess: (_, seconds) => {
      const label = TIMEOUT_PRESETS.find((p) => p.seconds === seconds)?.label ?? `${seconds}s`;
      showToast("success", `${user!.displayName} timed out for ${label}`);
      queryClient.invalidateQueries({ queryKey: ["user", userId] });
      queryClient.invalidateQueries({ queryKey: ["moderation-log", user!.twitchId] });
      queryClient.invalidateQueries({ queryKey: ["users"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const twitchBanMutation = useMutation({
    mutationFn: () =>
      moderationApi.ban({ twitchUserId: user!.twitchId, displayName: user!.displayName, reason: banReason || undefined }),
    onSuccess: () => {
      showToast("success", `${user!.displayName} banned on Twitch`);
      queryClient.invalidateQueries({ queryKey: ["user", userId] });
      queryClient.invalidateQueries({ queryKey: ["moderation-log", user!.twitchId] });
      queryClient.invalidateQueries({ queryKey: ["users"] });
      setShowTwitchBanConfirm(false);
      setBanReason("");
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const twitchUnbanMutation = useMutation({
    mutationFn: () => moderationApi.unban(user!.twitchId),
    onSuccess: () => {
      showToast("success", `${user!.displayName} unbanned on Twitch`);
      queryClient.invalidateQueries({ queryKey: ["user", userId] });
      queryClient.invalidateQueries({ queryKey: ["users"] });
      queryClient.invalidateQueries({ queryKey: ["moderation-log", user!.twitchId] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const shoutoutMutation = useMutation({
    mutationFn: () => moderationApi.shoutout({ twitchUserId: user!.twitchId, displayName: user!.displayName }),
    onSuccess: () => {
      showToast("success", `Shoutout sent for ${user!.displayName}`);
      queryClient.invalidateQueries({ queryKey: ["moderation-log", user!.twitchId] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  // ─── Activity History Query ─────────────────────────

  const { data: history } = useQuery<ModerationEvent[]>({
    queryKey: ["moderation-log", user?.twitchId, historyDays],
    queryFn: () => moderationApi.getUserLog(user!.twitchId, 500, historyDays > 0 ? historyDays : undefined),
    enabled: !!user?.twitchId,
  });

  if (userLoading || !user) {
    return (
      <div
        ref={overlayRef}
        onClick={(e) => { if (e.target === overlayRef.current) onClose(); }}
        className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      >
        <div className="w-full max-w-lg rounded-xl border border-[var(--color-border)] bg-[var(--color-bg)] p-8 flex items-center justify-center">
          <div className="h-6 w-6 animate-spin rounded-full border-2 border-[var(--color-border)] border-t-[var(--color-brand)]" />
        </div>
      </div>
    );
  }

  const totalHistoryPages = Math.ceil((history?.length ?? 0) / HISTORY_PAGE_SIZE);
  const paginatedHistory = (history ?? []).slice(
    (historyPage - 1) * HISTORY_PAGE_SIZE,
    historyPage * HISTORY_PAGE_SIZE
  );

  // ─── Status Badges ─────────────────────────────────

  const statusParts: string[] = [];
  if (user.isBroadcaster) statusParts.push("Broadcaster");
  if (user.isMod) statusParts.push("Moderator");
  if (user.isSubscriber) {
    const tierLabel = user.subscriberTier > 0 ? ` (Tier ${user.subscriberTier})` : "";
    statusParts.push(`Subscriber${tierLabel}`);
  }
  if (user.isBanned) statusParts.push("Bot Excluded");
  if (user.isTwitchBanned) statusParts.push("Twitch Banned");
  if (statusParts.length === 0) statusParts.push("Viewer");

  const isSelf = user.isBroadcaster;

  return (
    <>
      <div
        ref={overlayRef}
        onClick={(e) => { if (e.target === overlayRef.current) onClose(); }}
        className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      >
        <div className="w-full max-w-lg max-h-[90vh] flex flex-col rounded-xl border border-[var(--color-border)] bg-[var(--color-bg)] shadow-2xl">
          {/* Header */}
          <div className="flex items-center justify-between border-b border-[var(--color-border)] px-5 py-4 shrink-0">
            <div>
              <h2 className="text-lg font-semibold text-[var(--color-text)]">{user.displayName}</h2>
              <p className="text-xs text-[var(--color-text-muted)]">@{user.username}</p>
            </div>
            <button onClick={onClose} className="rounded p-1 text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-elevated)] transition-colors">
              <X className="h-5 w-5" />
            </button>
          </div>

          <div className="flex-1 overflow-y-auto space-y-5 p-5">
            {/* Statistics */}
            <div>
              <h3 className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-secondary)] mb-2">Statistics</h3>
              <div className="grid grid-cols-2 gap-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-3">
                <StatItem label="Points" value={user.points.toLocaleString()} />
                <StatItem label="Messages" value={user.messageCount.toLocaleString()} />
                <StatItem label="Watch Time" value={formatWatchTime(user.watchedMinutes)} />
                <StatItem label="First Seen" value={formatDate(user.firstSeenAt)} />
                <StatItem label="Last Seen" value={formatRelativeTime(user.lastSeenAt)} />
                <StatItem label="Follow" value={user.followDate ? formatDate(user.followDate) : "Not following"} />
              </div>
            </div>

            {/* Status */}
            <div>
              <h3 className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-secondary)] mb-2">Status</h3>
              <div className="flex flex-wrap gap-1.5">
                {statusParts.map((part) => (
                  <span key={part} className={`rounded px-2 py-0.5 text-xs font-medium ${getStatusColor(part)}`}>{part}</span>
                ))}
              </div>
            </div>

            {readOnly && (
              <div className="rounded-lg border border-amber-500/30 bg-amber-500/10 px-3 py-2 text-xs text-amber-200">
                Editing is disabled while a data import is running.
              </div>
            )}

            {/* Edit Points */}
            <div>
              <h3 className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-secondary)] mb-2">Edit Points</h3>
              <div className="flex items-center gap-2">
                <input type="number" value={points} onChange={(e) => setPoints(e.target.value)} disabled={readOnly}
                  className="flex-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] focus:border-[var(--color-brand)] focus:outline-none disabled:opacity-40" />
                <button onClick={() => updatePointsMutation.mutate()}
                  disabled={readOnly || updatePointsMutation.isPending || points === String(user.points)}
                  className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors">
                  {updatePointsMutation.isPending ? "Saving..." : "Save"}
                </button>
              </div>
            </div>

            {/* ─── Bot Access ─────────────────────────────── */}
            <div>
              <h3 className="text-xs font-semibold uppercase tracking-wider text-amber-400 mb-2 flex items-center gap-1.5">
                <ShieldOff className="h-3.5 w-3.5" /> Bot Access
              </h3>
              <div className="rounded-lg border border-amber-500/20 bg-amber-500/5 p-3">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="text-sm text-[var(--color-text)]">
                      {user.isBanned ? "Excluded from bot features" : "Full bot access"}
                    </p>
                    <p className="text-xs text-[var(--color-text-muted)] mt-0.5">
                      {user.isBanned
                        ? "This user cannot earn points, use commands, play games, or gain watchtime."
                        : "This user has access to all bot features. Excluding them does not affect their Twitch chat access."}
                    </p>
                  </div>
                  <button
                    onClick={() => setShowBanConfirm(true)}
                    disabled={readOnly || toggleBotBanMutation.isPending}
                    className={`shrink-0 rounded-lg px-3 py-1.5 text-xs font-medium transition-colors disabled:opacity-40 ${
                      user.isBanned
                        ? "bg-green-600 hover:bg-green-700 text-white"
                        : "bg-amber-600 hover:bg-amber-700 text-white"
                    }`}
                  >
                    {user.isBanned ? "Restore Access" : "Exclude"}
                  </button>
                </div>
              </div>
            </div>

            {/* ─── Twitch Moderation ─────────────────────── */}
            <div>
              <h3 className="text-xs font-semibold uppercase tracking-wider text-purple-400 mb-2 flex items-center gap-1.5">
                <Shield className="h-3.5 w-3.5" /> Twitch Moderation
              </h3>

              {!botIsModerator ? (
                <div className="rounded-lg border border-purple-500/20 bg-purple-500/5 p-3">
                  <p className="text-xs text-[var(--color-text-muted)]">
                    The bot needs moderator status to use Twitch moderation actions.
                    Type <code className="rounded bg-[var(--color-elevated)] px-1 py-0.5 text-purple-300">/mod YOUR_BOT_NAME</code> in your Twitch chat.
                  </p>
                </div>
              ) : isSelf ? (
                <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-3">
                  <p className="text-xs text-[var(--color-text-muted)]">Moderation actions cannot be performed on the broadcaster.</p>
                </div>
              ) : user.isMod ? (
                <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-3">
                  <p className="text-xs text-[var(--color-text-muted)]">This user is a moderator. Twitch does not allow timing out or banning moderators.</p>
                </div>
              ) : (
                <div className="rounded-lg border border-purple-500/20 bg-purple-500/5 p-3 space-y-3">
                  {/* Timeout Presets */}
                  <div>
                    <p className="text-xs font-medium text-[var(--color-text-secondary)] mb-1.5 flex items-center gap-1">
                      <Clock className="h-3 w-3" /> Timeout
                    </p>
                    <div className="flex flex-wrap gap-1.5">
                      {TIMEOUT_PRESETS.map((preset) => (
                        <button
                          key={preset.seconds}
                          onClick={() => timeoutMutation.mutate(preset.seconds)}
                          disabled={timeoutMutation.isPending}
                          className="rounded-md border border-purple-500/30 bg-purple-500/10 px-3 py-1 text-xs font-medium text-purple-300 hover:bg-purple-500/20 transition-colors disabled:opacity-40"
                        >
                          {preset.label}
                        </button>
                      ))}
                    </div>
                  </div>

                  {/* Ban / Unban + Shoutout */}
                  <div className="flex items-center gap-2 pt-1 border-t border-purple-500/10">
                    {user.isTwitchBanned ? (
                      <button
                        onClick={() => twitchUnbanMutation.mutate()}
                        disabled={twitchUnbanMutation.isPending}
                        className="rounded-md bg-green-600 hover:bg-green-700 px-3 py-1.5 text-xs font-medium text-white transition-colors disabled:opacity-40 flex items-center gap-1"
                      >
                        <Shield className="h-3 w-3" /> Unban from Twitch
                      </button>
                    ) : (
                      <button
                        onClick={() => setShowTwitchBanConfirm(true)}
                        disabled={twitchBanMutation.isPending}
                        className="rounded-md bg-red-600 hover:bg-red-700 px-3 py-1.5 text-xs font-medium text-white transition-colors disabled:opacity-40 flex items-center gap-1"
                      >
                        <Ban className="h-3 w-3" /> Ban
                      </button>
                    )}
                    <button
                      onClick={() => shoutoutMutation.mutate()}
                      disabled={shoutoutMutation.isPending}
                      className="rounded-md border border-[var(--color-border)] bg-[var(--color-elevated)] px-3 py-1.5 text-xs font-medium text-[var(--color-text)] hover:bg-[var(--color-surface)] transition-colors disabled:opacity-40 flex items-center gap-1"
                    >
                      <Megaphone className="h-3 w-3" /> Shoutout
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* ─── Activity History ──────────────────────── */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <h3 className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-secondary)] flex items-center gap-1.5">
                  <History className="h-3.5 w-3.5" /> Activity History
                </h3>
                <select
                  value={historyDays}
                  onChange={(e) => { setHistoryDays(Number(e.target.value)); setHistoryPage(1); }}
                  className="rounded border border-[var(--color-border)] bg-[var(--color-elevated)] px-2 py-0.5 text-[10px] text-[var(--color-text)]"
                >
                  <option value={30}>Last 30 days</option>
                  <option value={90}>Last 90 days</option>
                  <option value={180}>Last 6 months</option>
                  <option value={365}>Last year</option>
                  <option value={0}>All time</option>
                </select>
              </div>

              {!history || history.length === 0 ? (
                <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4 text-center">
                  <p className="text-xs text-[var(--color-text-muted)]">No events recorded for this period.</p>
                </div>
              ) : (
                <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)]">
                  <div className="divide-y divide-[var(--color-border)] max-h-60 overflow-y-auto">
                    {paginatedHistory.map((evt) => (
                      <div key={evt.id} className="flex items-start gap-3 px-3 py-2.5">
                        <span className="mt-0.5 text-sm">{getEventIcon(evt.eventType)}</span>
                        <div className="flex-1 min-w-0">
                          <p className="text-sm text-[var(--color-text)]">{getEventLabel(evt)}</p>
                          {evt.reason && (
                            <p className="text-xs text-[var(--color-text-muted)] mt-0.5 truncate" title={evt.reason}>{evt.reason}</p>
                          )}
                        </div>
                        <span className="text-[10px] text-[var(--color-text-muted)] whitespace-nowrap tabular-nums shrink-0">
                          {formatRelativeTime(evt.createdAt)}
                        </span>
                      </div>
                    ))}
                  </div>

                  {totalHistoryPages > 1 && (
                    <div className="flex items-center justify-between border-t border-[var(--color-border)] px-3 py-2">
                      <span className="text-[10px] text-[var(--color-text-muted)]">
                        {history.length} event{history.length !== 1 ? "s" : ""}
                      </span>
                      <div className="flex items-center gap-1">
                        <button
                          onClick={() => setHistoryPage((p) => Math.max(1, p - 1))}
                          disabled={historyPage <= 1}
                          className="rounded px-2 py-0.5 text-[10px] text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-elevated)] disabled:opacity-30 transition-colors"
                        >
                          ← Prev
                        </button>
                        <span className="text-[10px] text-[var(--color-text-muted)] tabular-nums">
                          {historyPage}/{totalHistoryPages}
                        </span>
                        <button
                          onClick={() => setHistoryPage((p) => Math.min(totalHistoryPages, p + 1))}
                          disabled={historyPage >= totalHistoryPages}
                          className="rounded px-2 py-0.5 text-[10px] text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-elevated)] disabled:opacity-30 transition-colors"
                        >
                          Next →
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Bot Access Confirm */}
      <ConfirmDialog
        open={showBanConfirm}
        title={user.isBanned ? "Restore Bot Access" : "Exclude from Bot"}
        message={
          user.isBanned
            ? `Restore bot access for "${user.displayName}"? They will be able to earn points, use commands, and participate in games again.`
            : `Exclude "${user.displayName}" from the bot? They will lose access to points, commands, games, and watchtime tracking. This does NOT ban them on Twitch — they can still chat normally.`
        }
        confirmLabel={user.isBanned ? "Restore" : "Exclude"}
        variant={user.isBanned ? "warning" : "danger"}
        onConfirm={() => { toggleBotBanMutation.mutate(); setShowBanConfirm(false); }}
        onCancel={() => setShowBanConfirm(false)}
      />

      {/* Twitch Ban Confirm */}
      <ConfirmDialog
        open={showTwitchBanConfirm}
        title="Ban on Twitch"
        message={
          <div className="space-y-3">
            <p>Permanently ban "{user.displayName}" from your Twitch channel? This action is executed via the Twitch API and takes effect immediately.</p>
            <input
              type="text"
              placeholder="Reason (optional)"
              value={banReason}
              onChange={(e) => setBanReason(e.target.value)}
              className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] focus:border-red-500 focus:outline-none"
            />
          </div>
        }
        confirmLabel="Ban on Twitch"
        variant="danger"
        onConfirm={() => twitchBanMutation.mutate()}
        onCancel={() => { setShowTwitchBanConfirm(false); setBanReason(""); }}
      />
    </>
  );
}

// ─── Helpers ─────────────────────────────────────────

function StatItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span className="text-xs text-[var(--color-text-muted)]">{label}</span>
      <p className="text-sm font-medium text-[var(--color-text)]">{value}</p>
    </div>
  );
}

function getStatusColor(status: string): string {
  switch (status) {
    case "Broadcaster": return "bg-red-500/20 text-red-400";
    case "Moderator": return "bg-green-500/20 text-green-400";
    case "Bot Excluded": return "bg-amber-500/20 text-amber-400";
    case "Twitch Banned": return "bg-red-600/20 text-red-400";
    default:
      if (status.startsWith("Subscriber")) return "bg-purple-500/20 text-purple-400";
      return "bg-[var(--color-elevated)] text-[var(--color-text-secondary)]";
  }
}

function getEventIcon(eventType: string): string {
  switch (eventType) {
    case "TwitchTimeout": return "⏱";
    case "TwitchBan": return "🚫";
    case "TwitchUnban": return "✅";
    case "TwitchShoutout": return "📣";
    case "BotBan": return "🤖";
    case "BotUnban": return "🤖";
    case "Follow": return "❤️";
    case "Subscribe": return "⭐";
    case "GiftSub": return "🎁";
    case "Resub": return "🔄";
    case "Raid": return "⚔️";
    default: return "📝";
  }
}

function getEventLabel(evt: ModerationEvent): string {
  switch (evt.eventType) {
    case "TwitchTimeout": {
      const duration = evt.durationSeconds
        ? evt.durationSeconds >= 3600 ? `${Math.floor(evt.durationSeconds / 3600)}h`
          : evt.durationSeconds >= 60 ? `${Math.floor(evt.durationSeconds / 60)}m`
          : `${evt.durationSeconds}s`
        : "";
      return `Timed out for ${duration}${evt.twitchSuccess === false ? " (failed)" : ""}`;
    }
    case "TwitchBan": return `Banned on Twitch${evt.twitchSuccess === false ? " (failed)" : ""}`;
    case "TwitchUnban": return `Unbanned on Twitch${evt.twitchSuccess === false ? " (failed)" : ""}`;
    case "TwitchShoutout": return `Shoutout sent${evt.twitchSuccess === false ? " (failed)" : ""}`;
    case "BotBan": return "Excluded from bot features";
    case "BotUnban": return "Bot access restored";
    case "Follow": return "Followed the channel";
    case "Subscribe": return `Subscribed${evt.reason ? ` (${evt.reason})` : ""}`;
    case "GiftSub": return `Gifted subs${evt.reason ? ` (${evt.reason})` : ""}`;
    case "Resub": return `Resubscribed${evt.reason ? ` (${evt.reason})` : ""}`;
    case "Raid": return `Raided${evt.reason ? ` (${evt.reason})` : ""}`;
    default: return evt.eventType;
  }
}

function formatWatchTime(minutes: number): string {
  if (minutes < 60) return `${minutes}m`;
  return `${Math.floor(minutes / 60)}h ${minutes % 60}m`;
}

function formatRelativeTime(isoDate: string): string {
  const diff = Date.now() - new Date(isoDate).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return "Just now";
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 30) return `${days}d ago`;
  return new Date(isoDate).toLocaleDateString();
}

function formatDate(isoDate: string): string {
  return new Date(isoDate).toLocaleDateString();
}
