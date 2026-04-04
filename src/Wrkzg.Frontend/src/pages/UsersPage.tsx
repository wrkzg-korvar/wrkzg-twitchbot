import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { usersApi } from "../api/users";
import { DataTable } from "../components/ui/DataTable";
import { PageHeader } from "../components/ui/PageHeader";
import { SearchInput } from "../components/ui/SearchInput";
import type { User } from "../types/users";

export function UsersPage() {
  const [search, setSearch] = useState("");

  const { data: users, isLoading, isError } = useQuery<User[]>({
    queryKey: ["users"],
    queryFn: () => usersApi.getAll("points", "desc", 100),
  });

  const filteredUsers = (users ?? []).filter(
    (u) =>
      search === "" ||
      u.username.toLowerCase().includes(search.toLowerCase()) ||
      (u.displayName ?? "").toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="p-6 space-y-6">
      <PageHeader
        title="Users"
        description="Tracked viewers, their points, watch time, and activity."
        helpKey="users"
      />

      {isLoading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-border)] border-t-[var(--color-brand)]" />
        </div>
      ) : isError ? (
        <div className="flex flex-col items-center justify-center py-20 text-[var(--color-text-muted)]">
          <p className="text-lg font-medium">Failed to load data</p>
          <p className="mt-1 text-sm">Please check your connection and try again.</p>
        </div>
      ) : !users || users.length === 0 ? (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-8 text-center">
          <p className="text-[var(--color-text-secondary)]">No users tracked yet.</p>
          <p className="mt-1 text-sm text-[var(--color-text-muted)]">
            Users appear here when they send messages in your chat.
          </p>
        </div>
      ) : (
        <>
          {users.length > 3 && (
            <SearchInput
              value={search}
              onChange={setSearch}
              placeholder="Search users..."
            />
          )}

          <DataTable minWidth={700}>
                <thead>
                  <tr className="border-b border-[var(--color-border)] bg-[var(--color-surface)]">
                    <th className="px-4 py-3 text-left font-medium text-[var(--color-text-secondary)]">User</th>
                    <th className="px-4 py-3 text-right font-medium text-[var(--color-text-secondary)]">Points</th>
                    <th className="px-4 py-3 text-right font-medium text-[var(--color-text-secondary)]">Watch Time</th>
                    <th className="px-4 py-3 text-right font-medium text-[var(--color-text-secondary)]">Messages</th>
                    <th className="px-4 py-3 text-center font-medium text-[var(--color-text-secondary)]">Role</th>
                    <th className="px-4 py-3 text-right font-medium text-[var(--color-text-secondary)]">Last Seen</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredUsers.map((user) => (
                    <tr key={user.id} className="border-b border-[var(--color-border)] hover:bg-[var(--color-elevated)]">
                      <td className="px-4 py-3">
                        <span className="font-medium text-[var(--color-text)]">{user.displayName}</span>
                        {user.isBanned && (
                          <span className="ml-2 rounded bg-red-900/30 px-1.5 py-0.5 text-[10px] text-red-400">
                            BANNED
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-right text-[var(--color-text)] font-mono">
                        {user.points.toLocaleString()}
                      </td>
                      <td className="px-4 py-3 text-right text-[var(--color-text-secondary)]">
                        {formatWatchTime(user.watchedMinutes)}
                      </td>
                      <td className="px-4 py-3 text-right text-[var(--color-text-secondary)]">
                        {user.messageCount.toLocaleString()}
                      </td>
                      <td className="px-4 py-3 text-center">
                        <RoleBadge user={user} />
                      </td>
                      <td className="px-4 py-3 text-right text-xs text-[var(--color-text-muted)]">
                        {formatRelativeTime(user.lastSeenAt)}
                      </td>
                    </tr>
                  ))}
                </tbody>
          </DataTable>

          {search !== "" && filteredUsers.length === 0 && (
            <p className="text-sm text-[var(--color-text-muted)] text-center py-4">
              No users matching "{search}"
            </p>
          )}
        </>
      )}
    </div>
  );
}

function RoleBadge({ user }: { user: User }) {
  if (user.isBroadcaster) {
    return <span className="rounded bg-red-500/20 px-2 py-0.5 text-xs text-red-400">Broadcaster</span>;
  }
  if (user.isMod) {
    return <span className="rounded bg-green-500/20 px-2 py-0.5 text-xs text-green-400">Mod</span>;
  }
  if (user.isSubscriber) {
    const tierLabel = user.subscriberTier > 0 ? ` T${user.subscriberTier}` : "";
    return <span className="rounded bg-purple-500/20 px-2 py-0.5 text-xs text-purple-400">Sub{tierLabel}</span>;
  }
  return <span className="text-xs text-[var(--color-text-muted)]">Viewer</span>;
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
