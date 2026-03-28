import type { ReactNode } from "react";

type BadgeVariant = "default" | "success" | "warning" | "error" | "brand";

interface BadgeProps {
  variant?: BadgeVariant;
  children: ReactNode;
}

const variantClasses: Record<BadgeVariant, string> = {
  default:
    "bg-[var(--color-elevated)] text-[var(--color-text-secondary)]",
  success:
    "bg-[rgba(34,197,94,0.15)] text-[var(--color-success)]",
  warning:
    "bg-[rgba(245,158,11,0.15)] text-[var(--color-warning)]",
  error:
    "bg-[rgba(239,68,68,0.15)] text-[var(--color-error)]",
  brand:
    "bg-[var(--color-brand-subtle)] text-[var(--color-brand-text)]",
};

export function Badge({ variant = "default", children }: BadgeProps) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${variantClasses[variant]}`}
    >
      {children}
    </span>
  );
}
