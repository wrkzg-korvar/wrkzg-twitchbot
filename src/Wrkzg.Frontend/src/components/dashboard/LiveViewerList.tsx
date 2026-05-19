import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search, Users } from "lucide-react";
import { moderationApi } from "../../api/moderation";
import { ChatQuickActions } from "./ChatQuickActions";
import type { LiveViewer } from "../../types/moderation";

interface LiveViewerListProps {
  onViewProfile?: (twitchId: string) => void;
}

type ViewerFilter = "all" | "subscribers" | "moderators";

export function LiveViewerList({ onViewProfile }: LiveViewerListProps) {
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<ViewerFilter>("all");

  const { data: viewers } = useQuery<LiveViewer[]>({
    queryKey: ["live-viewers"],
    queryFn: () => moderationApi.getLiveViewers(),
    refetchInterval: 60_000,
  });

  const filtered = (viewers ?? []).filter((v) => {
    if (search) {
      const q = search.toLowerCase();
      if (!v.displayName.toLowerCase().includes(q) && !v.username.toLowerCase().includes(q)) {
        return false;
      }
    }
    if (filter === "subscribers" && !v.isSubscriber) return false;
    if (filter === "moderators" && !v.isMod) return false;
    return true;
  });

  const sorted = [...filtered].sort((a, b) => {
    if (a.isBroadcaster !== b.isBroadcaster) return a.isBroadcaster ? -1 : 1;
    if (a.isMod !== b.isMod) return a.isMod ? -1 : 1;
    if (a.isSubscriber !== b.isSubscriber) return a.isSubscriber ? -1 : 1;
    return a.displayName.localeCompare(b.displayName);
  });

  return (
    <div className="flex flex-1 min-h-0 flex-col rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)]">
      <div className="flex items-center justify-between border-b border-[var(--color-border)] px-3 py-2.5 shrink-0">
        <h2 className="text-sm font-semibold text-[var(--color-text)] flex items-center gap-1.5">
          <Users className="h-3.5 w-3.5" /> Viewers
        </h2>
        <select
          value={filter}
          onChange={(e) => setFilter(e.target.value as ViewerFilter)}
          className="rounded border border-[var(--color-border)] bg-[var(--color-elevated)] px-1.5 py-0.5 text-[10px] text-[var(--color-text)]"
        >
          <option value="all">All</option>
          <option value="subscribers">Subscribers</option>
          <option value="moderators">Moderators</option>
        </select>
      </div>

      <div className="px-3 py-2 border-b border-[var(--color-border)] shrink-0">
        <div className="relative">
          <Search className="absolute left-2.5 top-1/2 h-3 w-3 -translate-y-1/2 text-[var(--color-text-muted)]" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search viewers..."
            className="w-full rounded border border-[var(--color-border)] bg-[var(--color-elevated)] pl-7 pr-2 py-1 text-xs text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:border-[var(--color-brand)]"
          />
        </div>
      </div>

      <div className="flex-1 min-h-0 overflow-y-auto">
        {sorted.length === 0 ? (
          <p className="text-center text-xs text-[var(--color-text-muted)] py-8">
            {viewers?.length === 0 ? "No viewers in chat" : "No matches"}
          </p>
        ) : (
          <div className="divide-y divide-[var(--color-border)]">
            {sorted.map((viewer) => (
              <div
                key={viewer.twitchId}
                className="group flex items-center gap-2 px-3 py-1.5 hover:bg-[var(--color-elevated)] transition-colors"
              >
                <span className="w-3 text-center text-[10px]">
                  {viewer.isBroadcaster ? "🎙" : viewer.isMod ? "⚔" : viewer.isSubscriber ? "⭐" : ""}
                </span>

                <span className="flex-1 text-xs text-[var(--color-text)] truncate">
                  {viewer.displayName}
                </span>

                {viewer.isBanned && (
                  <span className="rounded bg-amber-500/20 px-1 py-0.5 text-[8px] text-amber-400">BOT</span>
                )}
                {viewer.isTwitchBanned && (
                  <span className="rounded bg-red-600/20 px-1 py-0.5 text-[8px] text-red-400">BAN</span>
                )}

                <ChatQuickActions
                  twitchUserId={viewer.twitchId}
                  displayName={viewer.displayName}
                  isBroadcaster={viewer.isBroadcaster}
                  isMod={viewer.isMod}
                  isTwitchBanned={viewer.isTwitchBanned}
                  onViewProfile={onViewProfile ? () => onViewProfile(viewer.twitchId) : undefined}
                />
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="border-t border-[var(--color-border)] px-3 py-1.5 text-[10px] text-[var(--color-text-muted)] shrink-0">
        {viewers?.length ?? 0} viewer{(viewers?.length ?? 0) !== 1 ? "s" : ""} in chat
        {search && ` · ${sorted.length} shown`}
      </div>
    </div>
  );
}
