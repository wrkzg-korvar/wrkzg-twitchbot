interface SidebarGroupProps {
  label?: string;
  children: React.ReactNode;
}

export function SidebarGroup({ label, children }: SidebarGroupProps) {
  return (
    <div className="space-y-1">
      {label && (
        <p className="px-3 pt-3 pb-1 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
          {label}
        </p>
      )}
      {children}
    </div>
  );
}
