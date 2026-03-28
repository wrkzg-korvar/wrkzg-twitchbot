interface DataTableProps {
  children: React.ReactNode;
  minWidth?: number;
  maxHeight?: number;
  className?: string;
}

export function DataTable({ children, minWidth = 640, maxHeight, className = "rounded-lg" }: DataTableProps) {
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
