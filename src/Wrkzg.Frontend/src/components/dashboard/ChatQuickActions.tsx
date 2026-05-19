import { useState, useRef, useEffect } from "react";
import { MoreHorizontal, Clock, Ban, Megaphone, User } from "lucide-react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { moderationApi } from "../../api/moderation";
import { showToast } from "../../hooks/useToast";

interface ChatQuickActionsProps {
  twitchUserId: string;
  displayName: string;
  isBroadcaster?: boolean;
  isMod?: boolean;
  isTwitchBanned?: boolean;
  onViewProfile?: () => void;
}

const TIMEOUT_PRESETS = [
  { label: "1m", seconds: 60 },
  { label: "5m", seconds: 300 },
  { label: "10m", seconds: 600 },
  { label: "30m", seconds: 1800 },
  { label: "1h", seconds: 3600 },
];

export function ChatQuickActions({
  twitchUserId,
  displayName,
  isBroadcaster,
  isMod,
  isTwitchBanned,
  onViewProfile,
}: ChatQuickActionsProps) {
  const [open, setOpen] = useState(false);
  const [showTimeouts, setShowTimeouts] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setOpen(false);
        setShowTimeouts(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [open]);

  const timeoutMutation = useMutation({
    mutationFn: (seconds: number) =>
      moderationApi.timeout({ twitchUserId, durationSeconds: seconds, displayName }),
    onSuccess: (_, seconds) => {
      const label = TIMEOUT_PRESETS.find((p) => p.seconds === seconds)?.label ?? `${seconds}s`;
      showToast("success", `${displayName} timed out for ${label}`);
      queryClient.invalidateQueries({ queryKey: ["moderation-log"] });
      setOpen(false);
      setShowTimeouts(false);
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const banMutation = useMutation({
    mutationFn: () => moderationApi.ban({ twitchUserId, displayName }),
    onSuccess: () => {
      showToast("success", `${displayName} banned on Twitch`);
      queryClient.invalidateQueries({ queryKey: ["users"] });
      queryClient.invalidateQueries({ queryKey: ["moderation-log"] });
      queryClient.invalidateQueries({ queryKey: ["live-viewers"] });
      setOpen(false);
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const unbanMutation = useMutation({
    mutationFn: () => moderationApi.unban(twitchUserId),
    onSuccess: () => {
      showToast("success", `${displayName} unbanned on Twitch`);
      queryClient.invalidateQueries({ queryKey: ["users"] });
      queryClient.invalidateQueries({ queryKey: ["live-viewers"] });
      queryClient.invalidateQueries({ queryKey: ["moderation-log"] });
      setOpen(false);
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const shoutoutMutation = useMutation({
    mutationFn: () => moderationApi.shoutout({ twitchUserId, displayName }),
    onSuccess: () => {
      showToast("success", `Shoutout sent for ${displayName}`);
      setOpen(false);
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const canModerate = !isBroadcaster && !isMod;

  return (
    <div ref={menuRef} className="relative">
      <button
        onClick={(e) => { e.stopPropagation(); setOpen(!open); setShowTimeouts(false); }}
        className="rounded p-0.5 text-[var(--color-text-muted)] opacity-0 group-hover:opacity-100 hover:text-[var(--color-text)] hover:bg-[var(--color-elevated)] transition-all"
      >
        <MoreHorizontal className="h-3.5 w-3.5" />
      </button>

      {open && (
        <div className="absolute right-0 top-full z-50 mt-1 w-44 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] shadow-xl py-1">
          {canModerate && (
            <>
              <button
                onMouseEnter={() => setShowTimeouts(true)}
                className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-[var(--color-text)] hover:bg-[var(--color-elevated)] transition-colors"
              >
                <Clock className="h-3 w-3 text-[var(--color-text-muted)]" />
                Timeout
                <span className="ml-auto text-[var(--color-text-muted)]">▸</span>
              </button>

              {showTimeouts && (
                <div className="border-t border-b border-[var(--color-border)] py-1 bg-[var(--color-surface)]">
                  {TIMEOUT_PRESETS.map((preset) => (
                    <button
                      key={preset.seconds}
                      onClick={() => timeoutMutation.mutate(preset.seconds)}
                      disabled={timeoutMutation.isPending}
                      className="w-full text-left px-6 py-1 text-xs text-[var(--color-text)] hover:bg-[var(--color-elevated)] transition-colors disabled:opacity-40"
                    >
                      {preset.label}
                    </button>
                  ))}
                </div>
              )}

              {isTwitchBanned ? (
                <button
                  onClick={() => { unbanMutation.mutate(); }}
                  disabled={unbanMutation.isPending}
                  className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-green-400 hover:bg-[var(--color-elevated)] transition-colors disabled:opacity-40"
                >
                  <Ban className="h-3 w-3" />
                  Unban
                </button>
              ) : (
                <button
                  onClick={() => { if (confirm(`Ban ${displayName} on Twitch?`)) banMutation.mutate(); }}
                  disabled={banMutation.isPending}
                  className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-red-400 hover:bg-[var(--color-elevated)] transition-colors disabled:opacity-40"
                >
                  <Ban className="h-3 w-3" />
                  Ban
                </button>
              )}

              <div className="border-t border-[var(--color-border)] my-1" />
            </>
          )}

          <button
            onClick={() => shoutoutMutation.mutate()}
            disabled={shoutoutMutation.isPending}
            className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-[var(--color-text)] hover:bg-[var(--color-elevated)] transition-colors disabled:opacity-40"
          >
            <Megaphone className="h-3 w-3 text-[var(--color-text-muted)]" />
            Shoutout
          </button>

          {onViewProfile && (
            <button
              onClick={() => { onViewProfile(); setOpen(false); }}
              className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-[var(--color-text)] hover:bg-[var(--color-elevated)] transition-colors"
            >
              <User className="h-3 w-3 text-[var(--color-text-muted)]" />
              View Profile
            </button>
          )}
        </div>
      )}
    </div>
  );
}
