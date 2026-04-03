import { useEffect } from "react";

const POPULAR_FONTS = [
  "System Default",
  "Roboto", "Open Sans", "Lato", "Montserrat", "Oswald",
  "Raleway", "Poppins", "Nunito", "Ubuntu", "Merriweather",
  "PT Sans", "Playfair Display", "Bebas Neue", "Lobster",
  "Pacifico", "Bangers", "Permanent Marker", "Press Start 2P",
  "VT323", "Orbitron", "Righteous", "Bungee", "Fredoka One",
  "Titan One", "Anton", "Archivo Black", "Russo One",
  "Lexend", "Inter", "Space Grotesk", "JetBrains Mono",
];

interface FontPickerProps {
  value: string;
  onChange: (font: string) => void;
  label?: string;
}

export function FontPicker({ value, onChange, label }: FontPickerProps) {
  useEffect(() => {
    if (value && value !== "System Default" && value !== "system-ui") {
      const link = document.createElement("link");
      link.href = `https://fonts.googleapis.com/css2?family=${encodeURIComponent(value)}:wght@400;700&display=swap`;
      link.rel = "stylesheet";
      document.head.appendChild(link);
      return () => { link.remove(); };
    }
  }, [value]);

  const displayValue = (!value || value === "system-ui") ? "System Default" : value;

  return (
    <div>
      {label && <label className="mb-1 block text-xs font-medium text-[var(--color-text-secondary)]">{label}</label>}
      <select
        className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
        value={displayValue}
        onChange={(e) => onChange(e.target.value === "System Default" ? "system-ui" : e.target.value)}
      >
        {POPULAR_FONTS.map(font => (
          <option key={font} value={font}>{font}</option>
        ))}
      </select>
      <p className="mt-1 text-xs text-[var(--color-text-muted)]" style={{ fontFamily: value || "system-ui" }}>
        The quick brown fox jumps over the lazy dog
      </p>
    </div>
  );
}
