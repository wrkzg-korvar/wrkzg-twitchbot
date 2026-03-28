import { useState, useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Modal } from "../../ui/Modal";
import { Button } from "../../ui/Button";
import { Toggle } from "../../ui/Toggle";
import { overlaysApi } from "../../../api/overlays";
import { countersApi } from "../../../api/counters";
import { showToast } from "../../../hooks/useToast";
import { inputClass } from "../../../lib/constants";
import type { OverlayDefinition } from "./OverlayCard";
import type { OverlaySettings } from "../../../types/overlays";
import type { Counter } from "../../../types/counters";

interface OverlayConfigModalProps {
  open: boolean;
  onClose: () => void;
  overlay: OverlayDefinition;
}

interface FieldDef {
  key: string;
  label: string;
  type: "text" | "number" | "color" | "select" | "toggle" | "counter-select";
  options?: { value: string; label: string }[];
}

const FIELD_DEFS: Record<string, FieldDef[]> = {
  alerts: [
    {
      key: "animation",
      label: "Animation",
      type: "select",
      options: [
        { value: "slideDown", label: "Slide Down" },
        { value: "fadeIn", label: "Fade In" },
        { value: "bounceIn", label: "Bounce In" },
        { value: "zoomIn", label: "Zoom In" },
      ],
    },
    { key: "duration", label: "Duration (seconds)", type: "number" },
    { key: "fontSize", label: "Font Size (px)", type: "number" },
    { key: "textColor", label: "Text Color", type: "color" },
    { key: "accentColor", label: "Accent Color", type: "color" },
    { key: "followMessage", label: "Follow Message Template", type: "text" },
    { key: "subMessage", label: "Sub Message Template", type: "text" },
    { key: "raidMessage", label: "Raid Message Template", type: "text" },
    { key: "showFollows", label: "Show Follows", type: "toggle" },
    { key: "showSubs", label: "Show Subs", type: "toggle" },
    { key: "showRaids", label: "Show Raids", type: "toggle" },
  ],
  chat: [
    { key: "maxMessages", label: "Max Messages", type: "number" },
    { key: "fontSize", label: "Font Size (px)", type: "number" },
    { key: "fadeAfter", label: "Fade After (seconds)", type: "number" },
    {
      key: "direction",
      label: "Direction",
      type: "select",
      options: [
        { value: "bottomUp", label: "Bottom Up" },
        { value: "topDown", label: "Top Down" },
      ],
    },
    { key: "showBadges", label: "Show Badges", type: "toggle" },
  ],
  poll: [
    { key: "fontSize", label: "Font Size (px)", type: "number" },
    { key: "barColor", label: "Bar Color", type: "color" },
    { key: "showPercentage", label: "Show Percentage", type: "toggle" },
  ],
  raffle: [
    { key: "fontSize", label: "Font Size (px)", type: "number" },
    {
      key: "drawAnimation",
      label: "Draw Animation",
      type: "select",
      options: [
        { value: "spin", label: "Spin" },
        { value: "shuffle", label: "Shuffle" },
        { value: "reveal", label: "Reveal" },
      ],
    },
    { key: "confetti", label: "Confetti", type: "toggle" },
  ],
  counter: [
    { key: "counterId", label: "Counter", type: "counter-select" },
    { key: "fontSize", label: "Font Size (px)", type: "number" },
    { key: "textColor", label: "Text Color", type: "color" },
    { key: "labelTemplate", label: "Label Template", type: "text" },
    { key: "animateChange", label: "Animate Change", type: "toggle" },
  ],
  events: [
    { key: "maxItems", label: "Max Items", type: "number" },
    { key: "fontSize", label: "Font Size (px)", type: "number" },
    { key: "fadeAfter", label: "Fade After (seconds)", type: "number" },
    { key: "showFollows", label: "Show Follows", type: "toggle" },
    { key: "showSubs", label: "Show Subs", type: "toggle" },
    { key: "showRaids", label: "Show Raids", type: "toggle" },
  ],
};

export function OverlayConfigModal({ open, onClose, overlay }: OverlayConfigModalProps) {
  const queryClient = useQueryClient();
  const [formValues, setFormValues] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);

  const { data: settings } = useQuery<OverlaySettings>({
    queryKey: ["overlaySettings", overlay.type],
    queryFn: () => overlaysApi.getSettings(overlay.type),
    enabled: open,
  });

  // Fetch counters for counter-select dropdown
  const { data: counters } = useQuery<Counter[]>({
    queryKey: ["counters"],
    queryFn: countersApi.getAll,
    enabled: open && overlay.type === "counter",
  });

  useEffect(() => {
    if (settings) {
      setFormValues({ ...settings });
    }
  }, [settings]);

  const fields = FIELD_DEFS[overlay.type] ?? [];

  function updateField(key: string, value: string) {
    setFormValues((prev) => ({ ...prev, [key]: value }));
  }

  async function handleSave() {
    setSaving(true);
    try {
      await overlaysApi.updateSettings(overlay.type, formValues);
      await queryClient.invalidateQueries({ queryKey: ["overlaySettings", overlay.type] });
      showToast("success", `${overlay.title} settings saved`);
      onClose();
    } catch {
      showToast("error", "Failed to save settings");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal open={open} onClose={onClose} title={`Configure ${overlay.title}`} size="lg">
      <div className="space-y-4">
        {fields.map((field) => (
          <div key={field.key}>
            {field.type === "toggle" ? (
              <Toggle
                checked={formValues[field.key] === "true"}
                onChange={(checked) => updateField(field.key, String(checked))}
                label={field.label}
              />
            ) : (
              <>
                <label className="mb-1 block text-xs font-medium text-[var(--color-text-secondary)]">
                  {field.label}
                </label>
                {field.type === "select" ? (
                  <select
                    className={inputClass}
                    value={formValues[field.key] ?? ""}
                    onChange={(e) => updateField(field.key, e.target.value)}
                  >
                    {field.options?.map((opt) => (
                      <option key={opt.value} value={opt.value}>
                        {opt.label}
                      </option>
                    ))}
                  </select>
                ) : field.type === "color" ? (
                  <div className="flex items-center gap-2">
                    <input
                      type="color"
                      value={formValues[field.key] ?? "#ffffff"}
                      onChange={(e) => updateField(field.key, e.target.value)}
                      className="h-9 w-9 cursor-pointer rounded border border-[var(--color-border)] bg-transparent p-0.5"
                    />
                    <input
                      type="text"
                      className={inputClass}
                      value={formValues[field.key] ?? ""}
                      onChange={(e) => updateField(field.key, e.target.value)}
                      placeholder="#ffffff"
                    />
                  </div>
                ) : field.type === "counter-select" ? (
                  <select
                    className={inputClass}
                    value={formValues[field.key] ?? ""}
                    onChange={(e) => updateField(field.key, e.target.value)}
                  >
                    <option value="">Select a counter...</option>
                    {(counters ?? []).map((c) => (
                      <option key={c.id} value={String(c.id)}>
                        {c.name} ({c.trigger})
                      </option>
                    ))}
                  </select>
                ) : (
                  <input
                    type={field.type}
                    className={inputClass}
                    value={formValues[field.key] ?? ""}
                    onChange={(e) => updateField(field.key, e.target.value)}
                  />
                )}
              </>
            )}
          </div>
        ))}

        <div className="flex justify-end gap-2 pt-2">
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={handleSave} loading={saving}>
            Save
          </Button>
        </div>
      </div>
    </Modal>
  );
}
