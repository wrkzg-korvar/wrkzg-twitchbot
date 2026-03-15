import { useState } from "react";
import logoSvg from "../../assets/icon_full-size.v7.0.svg";
import { useTitleBarDrag } from "../../hooks/useTitleBarDrag";

const minimize = () => fetch("/api/window/minimize", { method: "POST" });
const maximize = () => fetch("/api/window/maximize", { method: "POST" });
const close = () => fetch("/api/window/close", { method: "POST" });

export function MacTitleBar() {
  const [hovered, setHovered] = useState(false);
  const { onMouseDown } = useTitleBarDrag();

  return (
    <div
      className="flex h-7 shrink-0 items-center border-b border-[var(--color-border)] bg-[var(--color-bg)] select-none cursor-default"
      onMouseDown={onMouseDown}
    >
      {/* Traffic Light Buttons (left) */}
      <div
        className="flex items-center gap-2 pl-3"
        data-no-drag
        onMouseEnter={() => setHovered(true)}
        onMouseLeave={() => setHovered(false)}
      >
        <TrafficLight color="close" hovered={hovered} onClick={close} />
        <TrafficLight color="minimize" hovered={hovered} onClick={minimize} />
        <TrafficLight color="maximize" hovered={hovered} onClick={maximize} />
      </div>

      {/* Centered title */}
      <div className="flex flex-1 items-center justify-center gap-1.5">
        <img src={logoSvg} alt="" className="h-3.5 w-3.5 rounded-sm" />
        <span className="text-[11px] font-medium text-[var(--color-text-muted)]">Wrkzg</span>
      </div>

      {/* Spacer to balance the traffic lights */}
      <div className="w-16" />
    </div>
  );
}

function TrafficLight({
  color,
  hovered,
  onClick,
}: {
  color: "close" | "minimize" | "maximize";
  hovered: boolean;
  onClick: () => void;
}) {
  const config = {
    close: { bg: "bg-red-500", icon: "\u00d7" },
    minimize: { bg: "bg-yellow-500", icon: "\u2212" },
    maximize: { bg: "bg-green-500", icon: "+" },
  }[color];

  return (
    <button
      onClick={(e) => {
        e.stopPropagation();
        onClick();
      }}
      className={`flex h-3 w-3 items-center justify-center rounded-full transition-colors ${config.bg} hover:brightness-110 active:brightness-90`}
      title={color.charAt(0).toUpperCase() + color.slice(1)}
    >
      {hovered && (
        <span className="text-[8px] font-bold leading-none text-black/60">
          {config.icon}
        </span>
      )}
    </button>
  );
}
