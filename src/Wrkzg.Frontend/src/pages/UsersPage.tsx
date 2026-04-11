import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { usersApi } from "../api/users";
import { PageHeader } from "../components/ui/PageHeader";
import { SmartDataTable } from "../components/ui/DataTable";
import { UserDetailModal } from "../components/features/users/UserDetailModal";
import { LockBanner } from "../components/ui/LockBanner";
import { useModuleLock } from "../hooks/useModuleLock";
import type { SmartColumn } from "../components/ui/DataTable";
import type { PaginatedUsers } from "../api/users";
import type { User } from "../types/users";

export function UsersPage() {
  const { isLocked, lockReason } = useModuleLock("/users");
  const [selectedUser, setSelectedUser] = useState<User | null>(null);

  const { data, isLoading, isError } = useQuery<PaginatedUsers>({
    queryKey: ["users"],
    queryFn: () => usersApi.getPaginated({ sortBy: "points", order: "desc", pageSize: 10000 }),
  });

  const users = data?.items ?? [];

  const columns: SmartColumn<User>[] = [
    {
      key: "displayName",
      header: "User",
      sortable: true,
      searchable: true,
      render: (_, row) => (
        <span>
          <span className="font-medium text-[var(--color-text)]">{row.displayName}</span>
          {row.isBroadcaster && (
            <span className="ml-2 rounded bg-red-500/20 px-1.5 py-0.5 text-[10px] text-red-400">
              BROADCASTER
            </span>
          )}
          {row.isMod && (
            <span className="ml-2 rounded bg-green-500/20 px-1.5 py-0.5 text-[10px] text-green-400">
              MOD
            </span>
          )}
          {row.isSubscriber && (
            <span className="ml-2 rounded bg-purple-500/20 px-1.5 py-0.5 text-[10px] text-purple-400">
              SUB
            </span>
          )}
          {row.isBanned && (
            <span className="ml-2 rounded bg-red-900/30 px-1.5 py-0.5 text-[10px] text-red-400">
              BANNED
            </span>
          )}
        </span>
      ),
    },
    {
      key: "username",
      header: "Username",
      searchable: true,
      className: "hidden",
      render: () => null,
    },
    {
      key: "points",
      header: "Points",
      sortable: true,
      className: "text-right font-mono text-[var(--color-text)]",
      render: (v) => (v as number).toLocaleString(),
    },
    {
      key: "watchedMinutes",
      header: "Watch Time",
      sortable: true,
      className: "text-right text-[var(--color-text-secondary)]",
      render: (v) => formatWatchTime(v as number),
    },
    {
      key: "messageCount",
      header: "Messages",
      sortable: true,
      className: "text-right text-[var(--color-text-secondary)]",
      render: (v) => (v as number).toLocaleString(),
    },
    {
      key: "isBroadcaster",
      header: "Role",
      className: "text-center",
      render: (_, row) => <RoleBadge user={row} />,
    },
    {
      key: "lastSeenAt",
      header: "Last Seen",
      sortable: true,
      className: "text-right text-xs text-[var(--color-text-muted)]",
      render: (v) => formatRelativeTime(v as string),
    },
  ];

  if (isError) {
    return (
      <div className="p-6">
        <PageHeader
          title="Users"
          description="Tracked viewers, their points, watch time, and activity."
          helpKey="users"
        />
        <div className="flex flex-col items-center justify-center py-20 text-[var(--color-text-muted)]">
          <p className="text-lg font-medium">Failed to load data</p>
          <p className="mt-1 text-sm">Please check your connection and try again.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      {lockReason && <LockBanner message={lockReason} />}
      <PageHeader
        title="Users"
        description="Tracked viewers, their points, watch time, and activity."
        helpKey="users"
        badge={
          data && data.totalCount > 0 ? (
            <span className="rounded-full bg-[var(--color-elevated)] px-2.5 py-0.5 text-xs font-medium text-[var(--color-text-secondary)] border border-[var(--color-border)]">
              {data.totalCount.toLocaleString()}
            </span>
          ) : undefined
        }
      />

      <SmartDataTable<User>
        data={users}
        columns={columns}
        pageSize={50}
        searchPlaceholder="Search users..."
        emptyMessage="No users tracked yet. Users appear here when they send messages in your chat."
        isLoading={isLoading}
        getRowKey={(row) => row.id}
        onRowClick={isLocked ? undefined : (row) => setSelectedUser(row)}
      />

      {selectedUser && (
        <UserDetailModal
          user={selectedUser}
          onClose={() => setSelectedUser(null)}
          readOnly={isLocked}
        />
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
