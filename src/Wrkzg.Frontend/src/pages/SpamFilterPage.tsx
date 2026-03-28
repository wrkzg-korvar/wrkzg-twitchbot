import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Shield, Save, Check } from "lucide-react";
import { spamFilterApi } from "../api/spamFilter";
import { showToast } from "../hooks/useToast";
import { PageHeader } from "../components/ui/PageHeader";
import {
  FilterCard,
  NumberField,
  CheckboxField,
  TextField,
  TextAreaField,
} from "../components/features/spamFilter/FilterCard";
import type { SpamFilterConfig } from "../types/spamFilter";

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

export function SpamFilterPage() {
  const queryClient = useQueryClient();
  const [config, setConfig] = useState<SpamFilterConfig>(defaultConfig);
  const [saved, setSaved] = useState(false);

  const { data } = useQuery<SpamFilterConfig>({
    queryKey: ["spamFilter"],
    queryFn: spamFilterApi.get,
  });

  useEffect(() => {
    if (data) {
      setConfig(data);
    }
  }, [data]);

  const saveMutation = useMutation({
    mutationFn: (cfg: SpamFilterConfig) => spamFilterApi.save(cfg),
    onSuccess: () => {
      setSaved(true);
      showToast("success", "Spam filter settings saved");
      setTimeout(() => setSaved(false), 2000);
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const update = <K extends keyof SpamFilterConfig>(key: K, value: SpamFilterConfig[K]) => {
    setConfig((prev) => {
      const updated = { ...prev, [key]: value };
      queryClient.setQueryData<SpamFilterConfig>(["spamFilter"], updated);
      return updated;
    });
  };

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Spam Filter"
        description="Configure chat moderation filters."
        badge={<Shield className="h-6 w-6 text-[var(--color-brand)]" />}
      />

      <FilterCard title="Link Filter" enabled={config.linksEnabled} onToggle={(v) => update("linksEnabled", v)}>
        <NumberField label="Timeout (seconds)" value={config.linksTimeoutSeconds} onChange={(v) => update("linksTimeoutSeconds", v)} min={1} />
        <CheckboxField label="Subscribers exempt" checked={config.linksSubsExempt} onChange={(v) => update("linksSubsExempt", v)} />
        <CheckboxField label="Moderators exempt" checked={config.linksModsExempt} onChange={(v) => update("linksModsExempt", v)} />
        <TextField label="Whitelisted domains (comma-separated)" value={config.linkWhitelist} onChange={(v) => update("linkWhitelist", v)} placeholder="clips.twitch.tv, youtube.com" />
      </FilterCard>

      <FilterCard title="Caps Filter" enabled={config.capsEnabled} onToggle={(v) => update("capsEnabled", v)}>
        <NumberField label="Min message length" value={config.capsMinLength} onChange={(v) => update("capsMinLength", v)} min={1} />
        <NumberField label="Max caps percent" value={config.capsMaxPercent} onChange={(v) => update("capsMaxPercent", v)} min={0} max={100} />
        <NumberField label="Timeout (seconds)" value={config.capsTimeoutSeconds} onChange={(v) => update("capsTimeoutSeconds", v)} min={1} />
        <CheckboxField label="Subscribers exempt" checked={config.capsSubsExempt} onChange={(v) => update("capsSubsExempt", v)} />
      </FilterCard>

      <FilterCard title="Banned Words" enabled={config.bannedWordsEnabled} onToggle={(v) => update("bannedWordsEnabled", v)}>
        <TextAreaField label="Banned words (comma-separated)" value={config.bannedWordsList} onChange={(v) => update("bannedWordsList", v)} placeholder="word1, word2, phrase three" />
        <NumberField label="Timeout (seconds)" value={config.bannedWordsTimeoutSeconds} onChange={(v) => update("bannedWordsTimeoutSeconds", v)} min={1} />
        <CheckboxField label="Subscribers exempt" checked={config.bannedWordsSubsExempt} onChange={(v) => update("bannedWordsSubsExempt", v)} />
      </FilterCard>

      <FilterCard title="Emote Spam" enabled={config.emoteSpamEnabled} onToggle={(v) => update("emoteSpamEnabled", v)}>
        <NumberField label="Max emotes per message" value={config.emoteSpamMaxEmotes} onChange={(v) => update("emoteSpamMaxEmotes", v)} min={1} />
        <NumberField label="Timeout (seconds)" value={config.emoteSpamTimeoutSeconds} onChange={(v) => update("emoteSpamTimeoutSeconds", v)} min={1} />
        <CheckboxField label="Subscribers exempt" checked={config.emoteSpamSubsExempt} onChange={(v) => update("emoteSpamSubsExempt", v)} />
      </FilterCard>

      <FilterCard title="Repetition Filter" enabled={config.repeatEnabled} onToggle={(v) => update("repeatEnabled", v)}>
        <NumberField label="Max repeat count" value={config.repeatMaxCount} onChange={(v) => update("repeatMaxCount", v)} min={2} />
        <NumberField label="Timeout (seconds)" value={config.repeatTimeoutSeconds} onChange={(v) => update("repeatTimeoutSeconds", v)} min={1} />
        <CheckboxField label="Subscribers exempt" checked={config.repeatSubsExempt} onChange={(v) => update("repeatSubsExempt", v)} />
      </FilterCard>

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
