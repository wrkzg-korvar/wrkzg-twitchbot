import { useState, useEffect, useMemo } from "react";
import { Search, ArrowUp, ArrowDown } from "lucide-react";
import { Pagination } from "./Pagination";

// ─── Legacy wrapper (backward compat for existing table usages) ──────────────

interface LegacyDataTableProps {
  children: React.ReactNode;
  minWidth?: number;
  maxHeight?: number;
  className?: string;
}

/**
 * Simple table wrapper. Used by CommandTable, QuoteTable etc. for basic layout.
 */
export function DataTable({ children, minWidth = 640, maxHeight, className = "rounded-lg" }: LegacyDataTableProps) {
  return (
    <div className={`border border-[var(--color-border)] overflow-hidden ${className}`}>
      <div
        className="overflow-auto"
        style={maxHeight ? { maxHeight: `${maxHeight}px` } : undefined}
      >
        <table
          className="w-full text-sm"
          style={{ minWidth: `${minWidth}px` }}
        >
          {children}
        </table>
      </div>
    </div>
  );
}

// ─── Smart DataTable with search, sort, pagination ───────────────────────────

export interface SmartColumn<T> {
  key: string;
  header: string;
  sortable?: boolean;
  searchable?: boolean;
  /** Custom render function for the cell */
  render?: (value: unknown, row: T) => React.ReactNode;
  /** CSS class for the column */
  className?: string;
  /** Minimum width */
  minWidth?: string;
}

interface SmartDataTableProps<T> {
  data: T[];
  columns: SmartColumn<T>[];
  /** Items per page. Set to 0 for no pagination. Default: 50 */
  pageSize?: number;
  /** Show page size selector. Default: true */
  showPageSizeSelector?: boolean;
  /** Placeholder for the search input */
  searchPlaceholder?: string;
  /** Message when data is empty */
  emptyMessage?: string;
  /** Show loading skeleton */
  isLoading?: boolean;
  /** Max height of the table body. Default: "600px" */
  maxHeight?: string;
  /** Render function for actions above the table (next to search) */
  headerActions?: React.ReactNode;
  /** Key extractor for row identity */
  getRowKey?: (row: T) => string | number;
  /** Callback when a row is clicked */
  onRowClick?: (row: T) => void;
  /** Custom row class */
  rowClassName?: (row: T) => string;
}

function getNestedValue(obj: unknown, key: string): unknown {
  if (!obj || typeof obj !== "object") {
    return undefined;
  }
  return (obj as Record<string, unknown>)[key];
}

export function SmartDataTable<T>({
  data,
  columns,
  pageSize: initialPageSize = 50,
  showPageSizeSelector = true,
  searchPlaceholder = "Search...",
  emptyMessage = "No data found.",
  isLoading = false,
  maxHeight = "600px",
  headerActions,
  getRowKey,
  onRowClick,
  rowClassName,
}: SmartDataTableProps<T>) {
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [sortKey, setSortKey] = useState<string | null>(null);
  const [sortDir, setSortDir] = useState<"asc" | "desc">("asc");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(initialPageSize);

  // Debounce search
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);

  // Reset page when search/sort changes
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, sortKey, sortDir, pageSize]);

  const searchableKeys = columns.filter((c) => c.searchable).map((c) => c.key);

  // Filter → Sort → Paginate
  const processedData = useMemo(() => {
    let result = [...data];

    // Filter
    if (debouncedSearch && searchableKeys.length > 0) {
      const lower = debouncedSearch.toLowerCase();
      result = result.filter((row) =>
        searchableKeys.some((key) => {
          const val = getNestedValue(row, key);
          return val != null && String(val).toLowerCase().includes(lower);
        })
      );
    }

    // Sort
    if (sortKey) {
      result.sort((a, b) => {
        const aVal = getNestedValue(a, sortKey);
        const bVal = getNestedValue(b, sortKey);

        if (aVal == null && bVal == null) return 0;
        if (aVal == null) return 1;
        if (bVal == null) return -1;

        let cmp: number;
        if (typeof aVal === "number" && typeof bVal === "number") {
          cmp = aVal - bVal;
        } else if (typeof aVal === "boolean" && typeof bVal === "boolean") {
          cmp = (aVal ? 1 : 0) - (bVal ? 1 : 0);
        } else {
          cmp = String(aVal).localeCompare(String(bVal), undefined, { sensitivity: "base" });
        }

        return sortDir === "desc" ? -cmp : cmp;
      });
    }

    return result;
  }, [data, debouncedSearch, searchableKeys, sortKey, sortDir]);

  const totalPages = pageSize > 0 ? Math.max(1, Math.ceil(processedData.length / pageSize)) : 1;
  const pagedData = pageSize > 0
    ? processedData.slice((page - 1) * pageSize, page * pageSize)
    : processedData;

  function handleSort(key: string) {
    if (sortKey === key) {
      setSortDir(sortDir === "asc" ? "desc" : "asc");
    } else {
      setSortKey(key);
      setSortDir("asc");
    }
  }

  function handlePageSizeChange(newSize: number) {
    setPageSize(newSize);
    setPage(1);
  }

  const hasSearch = searchableKeys.length > 0;

  // Loading skeleton
  if (isLoading) {
    return (
      <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
        <div className="space-y-0">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="flex gap-4 border-b border-[var(--color-border)] px-4 py-3">
              {columns.map((_, ci) => (
                <div key={ci} className="flex-1">
                  <div className="h-4 rounded bg-[var(--color-elevated)] animate-pulse" style={{ width: `${60 + Math.random() * 30}%` }} />
                </div>
              ))}
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
      {/* Header bar with search + actions */}
      {(hasSearch || headerActions) && (
        <div className="flex items-center gap-3 border-b border-[var(--color-border)] px-4 py-3">
          {hasSearch && (
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--color-text-muted)]" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder={searchPlaceholder}
                className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] pl-9 pr-3 py-1.5 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:border-[var(--color-brand)]"
              />
            </div>
          )}
          {headerActions && <div className="ml-auto flex items-center gap-2">{headerActions}</div>}
        </div>
      )}

      {/* Table */}
      <div className="overflow-auto" style={{ maxHeight }}>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-[var(--color-border)] bg-[var(--color-surface)] sticky top-0 z-10">
              {columns.map((col) => (
                <th
                  key={col.key}
                  className={`px-4 py-3 text-left font-medium text-[var(--color-text-secondary)] ${col.sortable ? "cursor-pointer select-none hover:text-[var(--color-text)]" : ""} ${col.className ?? ""}`}
                  style={col.minWidth ? { minWidth: col.minWidth } : undefined}
                  onClick={col.sortable ? () => handleSort(col.key) : undefined}
                >
                  <span className="inline-flex items-center gap-1">
                    {col.header}
                    {col.sortable && sortKey === col.key && (
                      sortDir === "asc"
                        ? <ArrowUp className="h-3 w-3" />
                        : <ArrowDown className="h-3 w-3" />
                    )}
                  </span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {pagedData.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-4 py-12 text-center text-[var(--color-text-muted)]">
                  {debouncedSearch ? `No results for "${debouncedSearch}"` : emptyMessage}
                </td>
              </tr>
            ) : (
              pagedData.map((row, rowIdx) => {
                const key = getRowKey ? getRowKey(row) : rowIdx;
                return (
                  <tr
                    key={key}
                    onClick={onRowClick ? () => onRowClick(row) : undefined}
                    className={`border-b border-[var(--color-border)] hover:bg-[var(--color-elevated)] transition-colors ${onRowClick ? "cursor-pointer" : ""} ${rowClassName ? rowClassName(row) : ""}`}
                  >
                    {columns.map((col) => (
                      <td key={col.key} className={`px-4 py-3 ${col.className ?? ""}`}>
                        {col.render
                          ? col.render(getNestedValue(row, col.key), row)
                          : String(getNestedValue(row, col.key) ?? "")}
                      </td>
                    ))}
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {pageSize > 0 && processedData.length > 0 && (
        <Pagination
          currentPage={page}
          totalPages={totalPages}
          totalItems={processedData.length}
          pageSize={pageSize}
          onPageChange={setPage}
          onPageSizeChange={showPageSizeSelector ? handlePageSizeChange : undefined}
        />
      )}
    </div>
  );
}
