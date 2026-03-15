import { useQuery } from "@tanstack/react-query";

interface User {
  id: number;
  twitchId: string;
  username: string;
  displayName: string;
  points: number;
  watchedMinutes: number;
  messageCount: number;
  isSubscriber: boolean;
  subscriberTier: number;
  isMod: boolean;
  isBanned: boolean;
  lastSeenAt: string;
}

async function fetchUsers(): Promise<User[]> {
  const res = await fetch("/api/users?sortBy=points&order=desc&limit=100");
  if (!res.ok) throw new Error(`Failed to load users: ${res.status}`);
  return res.json();
}

export function Users() {
  const { data: users, isLoading } = useQuery({ queryKey: ["users"], queryFn: fetchUsers });

  return (
    <div className="p-6 space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-white">Users</h1>
        <p className="mt-1 text-sm text-gray-500">
          Tracked viewers, their points, watch time, and activity.
        </p>
      </div>

      {isLoading ? (
        <p className="text-sm text-gray-500">Loading users…</p>
      ) : !users || users.length === 0 ? (
        <div className="rounded-lg border border-gray-800 bg-gray-900/50 p-8 text-center">
          <p className="text-gray-400">No users tracked yet.</p>
          <p className="mt-1 text-sm text-gray-500">
            Users appear here when they send messages in your chat.
          </p>
        </div>
      ) : (
        <div className="rounded-lg border border-gray-800 overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-800 bg-gray-900/80">
                <th className="px-4 py-3 text-left font-medium text-gray-400">User</th>
                <th className="px-4 py-3 text-right font-medium text-gray-400">Points</th>
                <th className="px-4 py-3 text-right font-medium text-gray-400">Watch Time</th>
                <th className="px-4 py-3 text-right font-medium text-gray-400">Messages</th>
                <th className="px-4 py-3 text-center font-medium text-gray-400">Role</th>
                <th className="px-4 py-3 text-right font-medium text-gray-400">Last Seen</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id} className="border-b border-gray-800/50 hover:bg-gray-900/40">
                  <td className="px-4 py-3">
                    <span className="font-medium text-gray-200">{user.displayName}</span>
                    {user.isBanned && (
                      <span className="ml-2 rounded bg-red-900/30 px-1.5 py-0.5 text-[10px] text-red-400">
                        BANNED
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-right text-gray-300 font-mono">
                    {user.points.toLocaleString()}
                  </td>
                  <td className="px-4 py-3 text-right text-gray-400">
                    {formatWatchTime(user.watchedMinutes)}
                  </td>
                  <td className="px-4 py-3 text-right text-gray-400">
                    {user.messageCount.toLocaleString()}
                  </td>
                  <td className="px-4 py-3 text-center">
                    <RoleBadge user={user} />
                  </td>
                  <td className="px-4 py-3 text-right text-xs text-gray-500">
                    {formatRelativeTime(user.lastSeenAt)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function RoleBadge({ user }: { user: User }) {
  if (user.isMod) {
    return <span className="rounded bg-green-500/20 px-2 py-0.5 text-xs text-green-400">Mod</span>;
  }
  if (user.isSubscriber) {
    const tierLabel = user.subscriberTier > 0 ? ` T${user.subscriberTier}` : "";
    return <span className="rounded bg-purple-500/20 px-2 py-0.5 text-xs text-purple-400">Sub{tierLabel}</span>;
  }
  return <span className="text-xs text-gray-600">Viewer</span>;
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
