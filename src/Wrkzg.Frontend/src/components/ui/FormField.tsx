import type { ReactNode } from "react";

interface FormFieldProps {
  label: string;
  hint?: string;
  error?: string;
  children: ReactNode;
  className?: string;
}

export function FormField({ label, hint, error, children, className = "" }: FormFieldProps) {
  return (
    <div className={`space-y-1.5 ${className}`}>
      <label className="block text-sm font-medium text-[var(--color-text)]">
        {label}
      </label>
      {children}
      {error && (
        <p className="text-xs text-[var(--color-error)]">{error}</p>
      )}
      {!error && hint && (
        <p className="text-xs text-[var(--color-text-muted)]">{hint}</p>
      )}
    </div>
  );
}
