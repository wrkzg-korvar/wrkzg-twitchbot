import type { ReactNode } from "react";
import type { LucideIcon } from "lucide-react";

interface EmptyStateProps {
  icon: LucideIcon;
  title: string;
  description: string;
  action?: ReactNode;
}

export function EmptyState({ icon: Icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-16 text-center">
      <Icon className="mb-4 h-12 w-12 text-[var(--color-text-muted)]" />
      <h3 className="text-lg font-semibold text-[var(--color-text)]">{title}</h3>
      <p className="mt-1 max-w-sm text-sm text-[var(--color-text-muted)]">{description}</p>
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}
