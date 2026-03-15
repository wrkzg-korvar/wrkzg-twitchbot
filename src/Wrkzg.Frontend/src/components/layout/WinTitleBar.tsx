import { Minus, Square, X } from "lucide-react";
import logoSvg from "../../assets/icon_full-size.v7.0.svg";
import { useTitleBarDrag } from "../../hooks/useTitleBarDrag";

const minimize = () => fetch("/api/window/minimize", { method: "POST" });
const maximize = () => fetch("/api/window/maximize", { method: "POST" });
const close = () => fetch("/api/window/close", { method: "POST" });

export function WinTitleBar() {
  const { onMouseDown } = useTitleBarDrag();

  return (
    <div
      className="flex h-8 shrink-0 items-center justify-between border-b border-[var(--color-border)] bg-[var(--color-bg)] select-none cursor-default"
      onMouseDown={onMouseDown}
    >
      {/* App icon + title (left) */}
      <div className="flex items-center gap-2 pl-3">
        <img src={logoSvg} alt="" className="h-4 w-4 rounded-sm" />
        <span className="text-xs font-medium text-[var(--color-text-secondary)]">Wrkzg</span>
      </div>

      {/* Window controls (right) */}
      <div className="flex items-center" data-no-drag>
        <button
          onClick={(e) => { e.stopPropagation(); minimize(); }}
          className="flex h-8 w-12 items-center justify-center text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)] hover:text-gray-300 transition-colors"
          title="Minimize"
        >
          <Minus className="h-4 w-4" />
        </button>
        <button
          onClick={(e) => { e.stopPropagation(); maximize(); }}
          className="flex h-8 w-12 items-center justify-center text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)] hover:text-gray-300 transition-colors"
          title="Maximize"
        >
          <Square className="h-3 w-3" />
        </button>
        <button
          onClick={(e) => { e.stopPropagation(); close(); }}
          className="flex h-8 w-12 items-center justify-center text-[var(--color-text-muted)] hover:bg-red-600 hover:text-white transition-colors"
          title="Close"
        >
          <X className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}
