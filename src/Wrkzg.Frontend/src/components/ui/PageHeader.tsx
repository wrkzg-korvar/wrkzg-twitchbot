import type { ReactNode } from "react";
import { HelpButton } from "./HelpButton";

interface PageHeaderProps {
  title: string;
  description?: string;
  badge?: ReactNode;
  actions?: ReactNode;
  helpKey?: string;
}

export function PageHeader({ title, description, badge, actions, helpKey }: PageHeaderProps) {
  return (
    <div className="flex items-start justify-between gap-4">
      <div className="space-y-1">
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-bold text-[var(--color-text)]">{title}</h1>
          {helpKey && <HelpButton helpKey={helpKey} />}
          {badge}
        </div>
        {description && (
          <p className="text-sm text-[var(--color-text-muted)]">{description}</p>
        )}
      </div>
      {actions && <div className="flex items-center gap-2">{actions}</div>}
    </div>
  );
}
