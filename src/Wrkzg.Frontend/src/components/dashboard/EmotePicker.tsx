import { useState, useRef, useEffect } from "react";
import { Smile } from "lucide-react";
import type { EmoteDto } from "../../api/emotes";

interface EmotePickerProps {
  emotes: EmoteDto[];
  onSelect: (emoteName: string) => void;
}

export function EmotePicker({ emotes, onSelect }: EmotePickerProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState("");
  const panelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (panelRef.current && !panelRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    }

    if (isOpen) {
      document.addEventListener("mousedown", handleClickOutside);
    }
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isOpen]);

  const filtered = search.trim()
    ? emotes.filter((e) =>
        e.name.toLowerCase().includes(search.toLowerCase()),
      )
    : emotes;

  const subscriberEmotes = filtered.filter((e) => e.source === "subscriber");
  const bitsEmotes = filtered.filter((e) => e.source === "bits");
  const followerEmotes = filtered.filter((e) => e.source === "follower");
  const channelEmotes = filtered.filter((e) => e.source === "channel");
  const globalEmotes = filtered.filter((e) => e.source === "global");

  const handleSelect = (name: string) => {
    onSelect(name);
    setIsOpen(false);
    setSearch("");
  };

  return (
    <div className="relative" ref={panelRef}>
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:border-[var(--color-brand)] transition-colors"
        title="Emotes"
      >
        <Smile size={18} />
      </button>

      {isOpen && (
        <div className="absolute bottom-full right-0 mb-2 w-80 max-h-80 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] shadow-lg flex flex-col overflow-hidden z-50">
          <div className="p-2 border-b border-[var(--color-border)]">
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search emotes..."
              autoFocus
              className="w-full rounded border border-[var(--color-border)] bg-[var(--color-bg)] px-2 py-1 text-xs text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
            />
          </div>

          <div className="flex-1 overflow-y-auto p-2 space-y-2">
            {subscriberEmotes.length > 0 && (
              <EmoteGroup
                label="Subscriber"
                emotes={subscriberEmotes}
                onSelect={handleSelect}
              />
            )}
            {bitsEmotes.length > 0 && (
              <EmoteGroup
                label="Bits"
                emotes={bitsEmotes}
                onSelect={handleSelect}
              />
            )}
            {followerEmotes.length > 0 && (
              <EmoteGroup
                label="Follower"
                emotes={followerEmotes}
                onSelect={handleSelect}
              />
            )}
            {channelEmotes.length > 0 && (
              <EmoteGroup
                label="Channel"
                emotes={channelEmotes}
                onSelect={handleSelect}
              />
            )}
            {globalEmotes.length > 0 && (
              <EmoteGroup
                label="Global"
                emotes={globalEmotes}
                onSelect={handleSelect}
              />
            )}
            {filtered.length === 0 && (
              <p className="text-center text-xs text-[var(--color-text-muted)] py-4">
                No emotes found
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

function EmoteGroup({
  label,
  emotes,
  onSelect,
}: {
  label: string;
  emotes: EmoteDto[];
  onSelect: (name: string) => void;
}) {
  return (
    <div>
      <p className="text-[10px] font-semibold uppercase text-[var(--color-text-muted)] mb-1">
        {label}
      </p>
      <div className="grid grid-cols-8 gap-1">
        {emotes.map((emote) => (
          <button
            key={emote.id}
            type="button"
            onClick={() => onSelect(emote.name)}
            title={emote.name}
            className="flex items-center justify-center rounded p-0.5 hover:bg-[var(--color-border)] transition-colors"
          >
            <img
              src={emote.url}
              alt={emote.name}
              loading="lazy"
              style={{ width: 28, height: 28 }}
            />
          </button>
        ))}
      </div>
    </div>
  );
}
