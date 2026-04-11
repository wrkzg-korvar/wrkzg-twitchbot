import { useEffect, useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { X } from "lucide-react";
import { usersApi } from "../../../api/users";
import { ConfirmDialog } from "../../ui/ConfirmDialog";
import { showToast } from "../../../hooks/useToast";
import type { User } from "../../../types/users";

interface UserDetailModalProps {
  user: User;
  onClose: () => void;
  readOnly?: boolean;
}

export function UserDetailModal({ user, onClose, readOnly }: UserDetailModalProps) {
  const queryClient = useQueryClient();
  const overlayRef = useRef<HTMLDivElement>(null);
  const [points, setPoints] = useState(String(user.points));
  const [showBanConfirm, setShowBanConfirm] = useState(false);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [onClose]);

  const updatePointsMutation = useMutation({
    mutationFn: () => usersApi.update(user.id, { points: Number(points) }),
    onSuccess: () => {
      showToast("success", "Points updated");
      queryClient.invalidateQueries({ queryKey: ["users"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const toggleBanMutation = useMutation({
    mutationFn: () => usersApi.update(user.id, { isBanned: !user.isBanned }),
    onSuccess: () => {
      showToast("success", user.isBanned ? "User unbanned" : "User banned");
      queryClient.invalidateQueries({ queryKey: ["users"] });
      onClose();
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const statusParts: string[] = [];
  if (user.isBroadcaster) statusParts.push("Broadcaster");
  if (user.isMod) statusParts.push("Moderator");
  if (user.isSubscriber) {
    const tierLabel = user.subscriberTier > 0 ? ` (Tier ${user.subscriberTier})` : "";
    statusParts.push(`Subscriber${tierLabel}`);
  }
  if (user.isBanned) statusParts.push("Banned");
  if (statusParts.length === 0) statusParts.push("Viewer");

  return (
    <>
      <div
        ref={overlayRef}
        onClick={(e) => {
          if (e.target === overlayRef.current) onClose();
        }}
        className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      >
        <div className="w-full max-w-lg rounded-xl border border-[var(--color-border)] bg-[var(--color-bg)] shadow-2xl">
          {/* Header */}
          <div className="flex items-center justify-between border-b border-[var(--color-border)] px-5 py-4">
            <div>
              <h2 className="text-lg font-semibold text-[var(--color-text)]">
                {user.displayName}
              </h2>
              <p className="text-xs text-[var(--color-text-muted)]">@{user.username}</p>
            </div>
            <button
              onClick={onClose}
              className="rounded p-1 text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-elevated)] transition-colors"
            >
              <X className="h-5 w-5" />
            </button>
          </div>

          <div className="space-y-5 p-5">
            {/* Statistics */}
            <div>
              <h3 className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-secondary)] mb-2">
                Statistics
              </h3>
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
              <h3 className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-secondary)] mb-2">
                Status
              </h3>
              <div className="flex flex-wrap gap-1.5">
                {statusParts.map((part) => (
                  <span
                    key={part}
                    className={`rounded px-2 py-0.5 text-xs font-medium ${getStatusColor(part)}`}
                  >
                    {part}
                  </span>
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
              <h3 className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-secondary)] mb-2">
                Edit Points
              </h3>
              <div className="flex items-center gap-2">
                <input
                  type="number"
                  value={points}
                  onChange={(e) => setPoints(e.target.value)}
                  disabled={readOnly}
                  className="flex-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] focus:border-[var(--color-brand)] focus:outline-none disabled:opacity-40"
                />
                <button
                  onClick={() => updatePointsMutation.mutate()}
                  disabled={readOnly || updatePointsMutation.isPending || points === String(user.points)}
                  className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
                >
                  {updatePointsMutation.isPending ? "Saving..." : "Save"}
                </button>
              </div>
            </div>

            {/* Moderation */}
            <div>
              <h3 className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-secondary)] mb-2">
                Moderation
              </h3>
              <button
                onClick={() => setShowBanConfirm(true)}
                disabled={readOnly}
                className={`rounded-lg px-4 py-2 text-sm font-medium transition-colors disabled:opacity-40 ${
                  user.isBanned
                    ? "bg-green-600 hover:bg-green-700 text-white"
                    : "bg-red-600 hover:bg-red-700 text-white"
                }`}
              >
                {user.isBanned ? "Unban User" : "Ban User"}
              </button>
            </div>
          </div>
        </div>
      </div>

      <ConfirmDialog
        open={showBanConfirm}
        title={user.isBanned ? "Unban User" : "Ban User"}
        message={
          user.isBanned
            ? `Are you sure you want to unban "${user.displayName}"?`
            : `Are you sure you want to ban "${user.displayName}"? They will be excluded from points, commands, and other features.`
        }
        confirmLabel={user.isBanned ? "Unban" : "Ban"}
        variant={user.isBanned ? "warning" : "danger"}
        onConfirm={() => {
          toggleBanMutation.mutate();
          setShowBanConfirm(false);
        }}
        onCancel={() => setShowBanConfirm(false)}
      />
    </>
  );
}

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
    case "Broadcaster":
      return "bg-red-500/20 text-red-400";
    case "Moderator":
      return "bg-green-500/20 text-green-400";
    case "Banned":
      return "bg-red-900/30 text-red-400";
    default:
      if (status.startsWith("Subscriber")) {
        return "bg-purple-500/20 text-purple-400";
      }
      return "bg-[var(--color-elevated)] text-[var(--color-text-secondary)]";
  }
}

function formatWatchTime(minutes: number): string {
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  const mins = minutes % 60;
  return `${hours}h ${mins}m`;
}

function formatRelativeTime(isoDate: string): string {
  const diff = Date.now() - new Date(isoDate).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return "Just now";
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

function formatDate(isoDate: string): string {
  return new Date(isoDate).toLocaleDateString();
}
