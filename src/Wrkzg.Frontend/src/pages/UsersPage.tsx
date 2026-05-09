import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search } from "lucide-react";
import { usersApi } from "../api/users";
import { PageHeader } from "../components/ui/PageHeader";
import { SmartDataTable } from "../components/ui/DataTable";
import { Pagination } from "../components/ui/Pagination";
import { UserDetailModal } from "../components/features/users/UserDetailModal";
import { LockBanner } from "../components/ui/LockBanner";
import { useModuleLock } from "../hooks/useModuleLock";
import type { SmartColumn } from "../components/ui/DataTable";
import type { PaginatedUsers } from "../api/users";
import type { User } from "../types/users";

// Frontend column keys → backend sort keys understood by IUserRepository.GetPaginatedAsync
const SORT_KEY_MAP: Record<string, string> = {
  displayName: "username",
  watchedMinutes: "watchtime",
  messageCount: "messages",
  points: "points",
};

export function UsersPage() {
  const { isLocked, lockReason } = useModuleLock("/users");
  const [selectedUser, setSelectedUser] = useState<User | null>(null);

  // Server-side pagination state
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [sortBy, setSortBy] = useState("points");
  const [sortDir, setSortDir] = useState<"asc" | "desc">("desc");

  // Debounce search input by 300ms before triggering a query
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);

  // Reset to first page whenever the underlying query parameters change
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, sortBy, sortDir, pageSize]);

  const { data, isLoading, isError } = useQuery<PaginatedUsers>({
    queryKey: ["users", debouncedSearch, sortBy, sortDir, page, pageSize],
    queryFn: () =>
      usersApi.getPaginated({
        search: debouncedSearch || undefined,
        sortBy: SORT_KEY_MAP[sortBy] ?? sortBy,
        order: sortDir,
        page,
        pageSize,
      }),
    placeholderData: (prev) => prev,
  });

  const users = data?.items ?? [];

  const columns: SmartColumn<User>[] = [
    {
      key: "displayName",
      header: "User",
      sortable: true,
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

      <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
        {/* Server-driven search */}
        <div className="flex items-center gap-3 border-b border-[var(--color-border)] px-4 py-3">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--color-text-muted)]" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search users..."
              className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] pl-9 pr-3 py-1.5 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:border-[var(--color-brand)]"
            />
          </div>
        </div>

        <SmartDataTable<User>
          data={users}
          columns={columns}
          pageSize={0}
          isLoading={isLoading && !data}
          getRowKey={(row) => row.id}
          onRowClick={isLocked ? undefined : (row) => setSelectedUser(row)}
          onSortChange={(key, dir) => {
            setSortBy(key);
            setSortDir(dir);
          }}
          emptyMessage={
            debouncedSearch
              ? `No users matching "${debouncedSearch}".`
              : "No users tracked yet. Users appear here when they send messages in your chat."
          }
          containerClassName=""
        />

        {data && data.totalCount > 0 && (
          <Pagination
            currentPage={page}
            totalPages={data.totalPages}
            totalItems={data.totalCount}
            pageSize={pageSize}
            onPageChange={setPage}
            onPageSizeChange={setPageSize}
          />
        )}
      </div>

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
