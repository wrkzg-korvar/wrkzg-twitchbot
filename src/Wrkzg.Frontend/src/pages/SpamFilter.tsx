import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Shield, Save, Check } from "lucide-react";

// ─── Types ───────────────────────────────────────────────

interface SpamFilterConfig {
  linksEnabled: boolean;
  linksTimeoutSeconds: number;
  linksSubsExempt: boolean;
  linksModsExempt: boolean;
  linkWhitelist: string;
  capsEnabled: boolean;
  capsMinLength: number;
  capsMaxPercent: number;
  capsTimeoutSeconds: number;
  capsSubsExempt: boolean;
  bannedWordsEnabled: boolean;
  bannedWordsList: string;
  bannedWordsTimeoutSeconds: number;
  bannedWordsSubsExempt: boolean;
  emoteSpamEnabled: boolean;
  emoteSpamMaxEmotes: number;
  emoteSpamTimeoutSeconds: number;
  emoteSpamSubsExempt: boolean;
  repeatEnabled: boolean;
  repeatMaxCount: number;
  repeatTimeoutSeconds: number;
  repeatSubsExempt: boolean;
}

const defaultConfig: SpamFilterConfig = {
  linksEnabled: false,
  linksTimeoutSeconds: 10,
  linksSubsExempt: true,
  linksModsExempt: true,
  linkWhitelist: "",
  capsEnabled: false,
  capsMinLength: 15,
  capsMaxPercent: 70,
  capsTimeoutSeconds: 10,
  capsSubsExempt: true,
  bannedWordsEnabled: false,
  bannedWordsList: "",
  bannedWordsTimeoutSeconds: 30,
  bannedWordsSubsExempt: false,
  emoteSpamEnabled: false,
  emoteSpamMaxEmotes: 10,
  emoteSpamTimeoutSeconds: 10,
  emoteSpamSubsExempt: true,
  repeatEnabled: false,
  repeatMaxCount: 3,
  repeatTimeoutSeconds: 10,
  repeatSubsExempt: true,
};

// ─── API ─────────────────────────────────────────────────

async function fetchSpamFilter(): Promise<SpamFilterConfig> {
  const res = await fetch("/api/spam-filter");
  if (!res.ok) throw new Error("Failed to fetch spam filter config");
  return res.json();
}

async function saveSpamFilter(config: SpamFilterConfig): Promise<void> {
  const res = await fetch("/api/spam-filter", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(config),
  });
  if (!res.ok) throw new Error("Failed to save spam filter config");
}

// ─── Component ───────────────────────────────────────────

export function SpamFilter() {
  const queryClient = useQueryClient();
  const [config, setConfig] = useState<SpamFilterConfig>(defaultConfig);
  const [saved, setSaved] = useState(false);

  const { data } = useQuery<SpamFilterConfig>({
    queryKey: ["spamFilter"],
    queryFn: fetchSpamFilter,
  });

  useEffect(() => {
    if (data) {
      setConfig(data);
    }
  }, [data]);

  const saveMutation = useMutation({
    mutationFn: (cfg: SpamFilterConfig) => saveSpamFilter(cfg),
    onSuccess: () => {
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    },
  });

  const update = <K extends keyof SpamFilterConfig>(key: K, value: SpamFilterConfig[K]) => {
    setConfig((prev) => {
      const updated = { ...prev, [key]: value };
      // Update query cache immediately so all reads are fresh
      queryClient.setQueryData<SpamFilterConfig>(["spamFilter"], updated);
      return updated;
    });
  };

  return (
    <div className="flex h-full flex-col gap-6 overflow-y-auto p-6">
      <div>
        <div className="flex items-center gap-2">
          <Shield className="h-6 w-6 text-[var(--color-brand)]" />
          <h1 className="text-2xl font-bold text-[var(--color-text)]">Spam Filter</h1>
        </div>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">
          Configure chat moderation filters.
        </p>
      </div>

      {/* Link Filter */}
      <FilterSection
        title="Link Filter"
        enabled={config.linksEnabled}
        onToggle={(v) => update("linksEnabled", v)}
      >
        <NumberField
          label="Timeout (seconds)"
          value={config.linksTimeoutSeconds}
          onChange={(v) => update("linksTimeoutSeconds", v)}
          min={1}
        />
        <CheckboxField
          label="Subscribers exempt"
          checked={config.linksSubsExempt}
          onChange={(v) => update("linksSubsExempt", v)}
        />
        <CheckboxField
          label="Moderators exempt"
          checked={config.linksModsExempt}
          onChange={(v) => update("linksModsExempt", v)}
        />
        <TextField
          label="Whitelisted domains (comma-separated)"
          value={config.linkWhitelist}
          onChange={(v) => update("linkWhitelist", v)}
          placeholder="clips.twitch.tv, youtube.com"
        />
      </FilterSection>

      {/* Caps Filter */}
      <FilterSection
        title="Caps Filter"
        enabled={config.capsEnabled}
        onToggle={(v) => update("capsEnabled", v)}
      >
        <NumberField
          label="Min message length"
          value={config.capsMinLength}
          onChange={(v) => update("capsMinLength", v)}
          min={1}
        />
        <NumberField
          label="Max caps percent"
          value={config.capsMaxPercent}
          onChange={(v) => update("capsMaxPercent", v)}
          min={0}
          max={100}
        />
        <NumberField
          label="Timeout (seconds)"
          value={config.capsTimeoutSeconds}
          onChange={(v) => update("capsTimeoutSeconds", v)}
          min={1}
        />
        <CheckboxField
          label="Subscribers exempt"
          checked={config.capsSubsExempt}
          onChange={(v) => update("capsSubsExempt", v)}
        />
      </FilterSection>

      {/* Banned Words */}
      <FilterSection
        title="Banned Words"
        enabled={config.bannedWordsEnabled}
        onToggle={(v) => update("bannedWordsEnabled", v)}
      >
        <TextAreaField
          label="Banned words (comma-separated)"
          value={config.bannedWordsList}
          onChange={(v) => update("bannedWordsList", v)}
          placeholder="word1, word2, phrase three"
        />
        <NumberField
          label="Timeout (seconds)"
          value={config.bannedWordsTimeoutSeconds}
          onChange={(v) => update("bannedWordsTimeoutSeconds", v)}
          min={1}
        />
        <CheckboxField
          label="Subscribers exempt"
          checked={config.bannedWordsSubsExempt}
          onChange={(v) => update("bannedWordsSubsExempt", v)}
        />
      </FilterSection>

      {/* Emote Spam */}
      <FilterSection
        title="Emote Spam"
        enabled={config.emoteSpamEnabled}
        onToggle={(v) => update("emoteSpamEnabled", v)}
      >
        <NumberField
          label="Max emotes per message"
          value={config.emoteSpamMaxEmotes}
          onChange={(v) => update("emoteSpamMaxEmotes", v)}
          min={1}
        />
        <NumberField
          label="Timeout (seconds)"
          value={config.emoteSpamTimeoutSeconds}
          onChange={(v) => update("emoteSpamTimeoutSeconds", v)}
          min={1}
        />
        <CheckboxField
          label="Subscribers exempt"
          checked={config.emoteSpamSubsExempt}
          onChange={(v) => update("emoteSpamSubsExempt", v)}
        />
      </FilterSection>

      {/* Repetition Filter */}
      <FilterSection
        title="Repetition Filter"
        enabled={config.repeatEnabled}
        onToggle={(v) => update("repeatEnabled", v)}
      >
        <NumberField
          label="Max repeat count"
          value={config.repeatMaxCount}
          onChange={(v) => update("repeatMaxCount", v)}
          min={2}
        />
        <NumberField
          label="Timeout (seconds)"
          value={config.repeatTimeoutSeconds}
          onChange={(v) => update("repeatTimeoutSeconds", v)}
          min={1}
        />
        <CheckboxField
          label="Subscribers exempt"
          checked={config.repeatSubsExempt}
          onChange={(v) => update("repeatSubsExempt", v)}
        />
      </FilterSection>

      {/* Save Button */}
      <div className="flex items-center gap-3">
        <button
          onClick={() => saveMutation.mutate(config)}
          disabled={saveMutation.isPending}
          className="flex items-center gap-2 rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
        >
          {saved ? (
            <>
              <Check className="h-4 w-4" />
              Saved
            </>
          ) : (
            <>
              <Save className="h-4 w-4" />
              {saveMutation.isPending ? "Saving..." : "Save All"}
            </>
          )}
        </button>
        {saveMutation.isError && (
          <p className="text-xs text-red-400">{(saveMutation.error as Error).message}</p>
        )}
      </div>
    </div>
  );
}

// ─── Filter Section ──────────────────────────────────────

function FilterSection({
  title,
  enabled,
  onToggle,
  children,
}: {
  title: string;
  enabled: boolean;
  onToggle: (value: boolean) => void;
  children: React.ReactNode;
}) {
  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)]">
      <div className="flex items-center justify-between px-4 py-3">
        <h2 className="text-sm font-semibold text-[var(--color-text)]">{title}</h2>
        <button
          onClick={() => onToggle(!enabled)}
          className={`relative inline-flex h-5 w-9 items-center rounded-full transition-colors ${
            enabled ? "bg-[var(--color-brand)]" : "bg-[var(--color-elevated)]"
          }`}
        >
          <span
            className={`inline-block h-3.5 w-3.5 rounded-full bg-white transition-transform ${
              enabled ? "translate-x-4" : "translate-x-0.5"
            }`}
          />
        </button>
      </div>

      {enabled && (
        <div className="border-t border-[var(--color-border)] p-4 space-y-3">
          {children}
        </div>
      )}
    </div>
  );
}

// ─── Field Components ────────────────────────────────────

function NumberField({
  label,
  value,
  onChange,
  min,
  max,
}: {
  label: string;
  value: number;
  onChange: (value: number) => void;
  min?: number;
  max?: number;
}) {
  return (
    <div className="flex items-center justify-between">
      <label className="text-sm text-[var(--color-text)]">{label}</label>
      <input
        type="number"
        value={value}
        onChange={(e) => onChange(Number(e.target.value))}
        min={min}
        max={max}
        className="w-24 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-1.5 text-sm text-[var(--color-text)] focus:border-[var(--color-brand)] focus:outline-none"
      />
    </div>
  );
}

function CheckboxField({
  label,
  checked,
  onChange,
}: {
  label: string;
  checked: boolean;
  onChange: (value: boolean) => void;
}) {
  return (
    <label className="flex items-center justify-between cursor-pointer">
      <span className="text-sm text-[var(--color-text)]">{label}</span>
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="h-4 w-4 rounded border-[var(--color-border)] accent-[var(--color-brand)]"
      />
    </label>
  );
}

function TextField({
  label,
  value,
  onChange,
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}) {
  return (
    <div className="space-y-1">
      <label className="text-sm text-[var(--color-text)]">{label}</label>
      <input
        type="text"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-1.5 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
      />
    </div>
  );
}

function TextAreaField({
  label,
  value,
  onChange,
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}) {
  return (
    <div className="space-y-1">
      <label className="text-sm text-[var(--color-text)]">{label}</label>
      <textarea
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        rows={3}
        className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
      />
    </div>
  );
}
