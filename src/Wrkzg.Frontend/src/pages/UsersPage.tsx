import { useQuery } from "@tanstack/react-query";
import { usersApi } from "../api/users";
import { DataTable } from "../components/ui/DataTable";
import { PageHeader } from "../components/ui/PageHeader";
import type { User } from "../types/users";

export function UsersPage() {
  const { data: users, isLoading } = useQuery<User[]>({
    queryKey: ["users"],
    queryFn: () => usersApi.getAll("points", "desc", 100),
  });

  return (
    <div className="p-6 space-y-6">
      <PageHeader
        title="Users"
        description="Tracked viewers, their points, watch time, and activity."
      />

      {isLoading ? (
        <p className="text-sm text-[var(--color-text-muted)]">Loading users...</p>
      ) : !users || users.length === 0 ? (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-8 text-center">
          <p className="text-[var(--color-text-secondary)]">No users tracked yet.</p>
          <p className="mt-1 text-sm text-[var(--color-text-muted)]">
            Users appear here when they send messages in your chat.
          </p>
        </div>
      ) : (
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
                {users.map((user) => (
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
