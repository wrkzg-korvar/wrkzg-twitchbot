import { Sun, Moon } from "lucide-react";
import { useTheme } from "../../hooks/useTheme";

export function ThemeToggle() {
  const { theme, toggle } = useTheme();
  const Icon = theme === "dark" ? Sun : Moon;

  return (
    <button
      onClick={toggle}
      className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-[var(--color-text-secondary)] transition-colors hover:bg-[var(--color-elevated)] hover:text-[var(--color-text)]"
    >
      <Icon className="h-4 w-4" />
      {theme === "dark" ? "Light mode" : "Dark mode"}
    </button>
  );
}
