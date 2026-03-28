import type { ReactNode } from "react";

interface CardProps {
  title?: string;
  headerRight?: ReactNode;
  children: ReactNode;
  className?: string;
}

export function Card({ title, headerRight, children, className = "" }: CardProps) {
  return (
    <div
      className={`rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] ${className}`}
    >
      {(title || headerRight) && (
        <div className="flex items-center justify-between border-b border-[var(--color-border)] px-5 py-3">
          {title && (
            <h3 className="text-sm font-semibold text-[var(--color-text)]">{title}</h3>
          )}
          {headerRight}
        </div>
      )}
      <div className="p-5">{children}</div>
    </div>
  );
}
