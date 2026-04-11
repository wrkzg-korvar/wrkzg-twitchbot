import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus, Trash2, Pencil, Play, ChevronRight, Zap, Shield, Sparkles } from "lucide-react";
import { effectsApi } from "../api/effects";
import { PageHeader } from "../components/ui/PageHeader";
import { Toggle } from "../components/ui/Toggle";
import { Badge } from "../components/ui/Badge";
import { Modal } from "../components/ui/Modal";
import { ConfirmDialog } from "../components/ui/ConfirmDialog";
import { showToast } from "../hooks/useToast";
import { VisualAutomationBuilder } from "../components/features/effects/VisualAutomationBuilder";
import type { EffectList, EffectTypes, ConditionConfig, EffectConfig } from "../types/effects";

const EXAMPLE_AUTOMATIONS = [
  {
    name: "Welcome New Followers",
    description: "Send a personalized welcome when someone follows",
    trigger: "event",
    triggerConfig: '{"event_type": "event.follow"}',
    effects: '[{"type":"chat_message","params":{"message":"Welcome to the community, {user}!"}}]',
  },
  {
    name: "Lucky Viewer (50% Chance)",
    description: "Random chance to give bonus points when someone types !lucky",
    trigger: "command",
    triggerConfig: '{"trigger": "!lucky"}',
    conditions: '[{"type":"random_chance","params":{"percent":"50"}}]',
    effects: '[{"type":"chat_message","params":{"message":"{user} got lucky! Bonus points incoming!"}}]',
  },
  {
    name: "Raid Alert Combo",
    description: "Multi-step: wait 2 seconds, then send a chat message on raid",
    trigger: "event",
    triggerConfig: '{"event_type": "event.raid"}',
    effects: '[{"type":"wait","params":{"seconds":"2"}},{"type":"chat_message","params":{"message":"Welcome raiders! Thanks for the raid, {user}!"}}]',
  },
  {
    name: "Discord Live Notification",
    description: "Send a Discord message when the stream goes live",
    trigger: "event",
    triggerConfig: '{"event_type": "event.stream_online"}',
    effects: '[{"type":"discord.send_message","params":{"message":"The stream is now LIVE! Come hang out!"}}]',
  },
];

export function EffectsPage() {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);

  const { data: effectLists, isLoading, isError } = useQuery<EffectList[]>({
    queryKey: ["effects"],
    queryFn: effectsApi.getAll,
  });

  const { data: types } = useQuery<EffectTypes>({
    queryKey: ["effect-types"],
    queryFn: effectsApi.getTypes,
  });

  const toggleMutation = useMutation({
    mutationFn: ({ id, isEnabled }: { id: number; isEnabled: boolean }) =>
      effectsApi.update(id, { isEnabled }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["effects"] }),
    onError: () => showToast("error", "Failed to toggle automation."),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => effectsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["effects"] });
      showToast("success", "Automation deleted.");
      setDeleteId(null);
    },
  });

  const testMutation = useMutation({
    mutationFn: (id: number) => effectsApi.test(id),
    onSuccess: (result) => showToast("success", `Tested: ${result.name}`),
    onError: () => showToast("error", "Test failed."),
  });

  const createFromExample = useMutation({
    mutationFn: (ex: typeof EXAMPLE_AUTOMATIONS[0]) =>
      effectsApi.create({
        name: ex.name,
        description: ex.description,
        triggerTypeId: ex.trigger,
        triggerConfig: ex.triggerConfig,
        conditionsConfig: ex.conditions ?? "[]",
        effectsConfig: ex.effects,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["effects"] });
      showToast("success", "Example automation created!");
    },
    onError: () => showToast("error", "Failed to create example automation."),
  });

  const triggerLabel = (typeId: string) =>
    types?.triggers.find((t) => t.id === typeId)?.displayName ?? typeId;

  const effectLabel = (typeId: string) =>
    types?.effects.find((e) => e.id === typeId)?.displayName ?? typeId;

  const conditionLabel = (typeId: string) =>
    types?.conditions.find((c) => c.id === typeId)?.displayName ?? typeId;

  const hasAutomations = effectLists && effectLists.length > 0;

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-border)] border-t-[var(--color-brand)]" />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-[var(--color-text-muted)]">
        <p className="text-lg font-medium">Failed to load data</p>
        <p className="mt-1 text-sm">Please check your connection and try again.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Automations"
        description="Build custom automations that react to events on your stream."
        helpKey="effects"
        actions={
          <button
            onClick={() => { setEditingId(null); setShowForm(true); }}
            className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-3 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] transition-colors"
          >
            <Plus className="h-4 w-4" /> New Automation
          </button>
        }
      />

      {/* Concept explanation — always visible when no automations exist */}
      {!hasAutomations && (
        <div className="space-y-4">
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6">
            <h3 className="text-lg font-semibold text-[var(--color-text)] mb-3">How it works</h3>
            <p className="text-sm text-[var(--color-text-secondary)] mb-4">
              Automations let you create custom reactions to anything that happens on your stream.
              Each automation follows a simple pattern:
            </p>
            <div className="flex items-center gap-3 mb-6">
              <div className="flex items-center gap-2 rounded-lg bg-blue-500/10 border border-blue-500/20 px-4 py-2.5">
                <Zap className="h-4 w-4 text-blue-700 dark:text-blue-400" />
                <div>
                  <div className="text-sm font-medium text-blue-700 dark:text-blue-400">Trigger</div>
                  <div className="text-[10px] text-blue-600/60 dark:text-blue-400/60">When this happens...</div>
                </div>
              </div>
              <ChevronRight className="h-4 w-4 text-[var(--color-text-muted)]" />
              <div className="flex items-center gap-2 rounded-lg bg-amber-500/10 border border-amber-500/20 px-4 py-2.5">
                <Shield className="h-4 w-4 text-amber-700 dark:text-amber-400" />
                <div>
                  <div className="text-sm font-medium text-amber-700 dark:text-amber-400">Conditions</div>
                  <div className="text-[10px] text-amber-600/60 dark:text-amber-400/60">If these are true...</div>
                </div>
              </div>
              <ChevronRight className="h-4 w-4 text-[var(--color-text-muted)]" />
              <div className="flex items-center gap-2 rounded-lg bg-green-500/10 border border-green-500/20 px-4 py-2.5">
                <Sparkles className="h-4 w-4 text-green-700 dark:text-green-400" />
                <div>
                  <div className="text-sm font-medium text-green-700 dark:text-green-400">Effects</div>
                  <div className="text-[10px] text-green-600/60 dark:text-green-400/60">Do these actions</div>
                </div>
              </div>
            </div>

            <h4 className="text-sm font-semibold text-[var(--color-text)] mb-2">Quick start — try an example:</h4>
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-3">
              {EXAMPLE_AUTOMATIONS.map((ex) => (
                <button
                  key={ex.name}
                  onClick={() => createFromExample.mutate(ex)}
                  disabled={createFromExample.isPending}
                  className="text-left rounded-lg border border-[var(--color-border)] p-3 hover:bg-[var(--color-elevated)] transition-colors"
                >
                  <div className="text-sm font-medium text-[var(--color-text)]">{ex.name}</div>
                  <div className="text-xs text-[var(--color-text-muted)] mt-1">{ex.description}</div>
                </button>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Automation list */}
      <div className="space-y-3">
        {(effectLists ?? []).map((el) => {
          let conditions: ConditionConfig[] = [];
          let effects: EffectConfig[] = [];
          try { conditions = JSON.parse(el.conditionsConfig || "[]"); } catch { /* ignore */ }
          try { effects = JSON.parse(el.effectsConfig || "[]"); } catch { /* ignore */ }

          return (
            <div key={el.id} className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
              <div className="flex items-center gap-4">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-[var(--color-text)]">{el.name}</span>
                    <Badge variant={el.isEnabled ? "success" : "default"}>
                      {el.isEnabled ? "ON" : "OFF"}
                    </Badge>
                  </div>
                  {el.description && (
                    <p className="text-xs text-[var(--color-text-muted)] mt-0.5">{el.description}</p>
                  )}
                </div>

                <Toggle checked={el.isEnabled}
                  onChange={(checked) => toggleMutation.mutate({ id: el.id, isEnabled: checked })} />

                <button onClick={() => testMutation.mutate(el.id)}
                  className="rounded p-1.5 text-[var(--color-brand-text)] hover:bg-[var(--color-elevated)]" title="Test this automation">
                  <Play className="h-4 w-4" />
                </button>
                <button onClick={() => { setEditingId(el.id); setShowForm(true); }}
                  className="rounded p-1.5 text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)]">
                  <Pencil className="h-4 w-4" />
                </button>
                <button onClick={() => setDeleteId(el.id)}
                  className="rounded p-1.5 text-[var(--color-error)] hover:bg-[var(--color-elevated)]">
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>

              {/* Flow visualization with labels */}
              <div className="flex flex-wrap items-center gap-2 mt-3 text-xs">
                <span className="rounded bg-blue-500/15 text-blue-700 dark:text-blue-400 px-2 py-0.5 font-medium">
                  {triggerLabel(el.triggerTypeId)}
                </span>
                {conditions.length > 0 && (
                  <>
                    <ChevronRight className="h-3 w-3 text-[var(--color-text-muted)]" />
                    {conditions.map((c, i) => (
                      <span key={i} className="rounded bg-amber-500/15 text-amber-700 dark:text-amber-400 px-2 py-0.5">
                        {conditionLabel(c.type)}
                      </span>
                    ))}
                  </>
                )}
                <ChevronRight className="h-3 w-3 text-[var(--color-text-muted)]" />
                {effects.map((e, i) => (
                  <span key={i} className="rounded bg-green-500/15 text-green-700 dark:text-green-400 px-2 py-0.5">
                    {effectLabel(e.type)}
                  </span>
                ))}
                {el.cooldown > 0 && (
                  <span className="text-[var(--color-text-muted)] ml-auto">{el.cooldown}s cooldown</span>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {showForm && types && (
        <EffectFormModal editingId={editingId} types={types}
          onClose={() => setShowForm(false)}
          onSaved={() => { setShowForm(false); queryClient.invalidateQueries({ queryKey: ["effects"] }); }} />
      )}

      <ConfirmDialog open={deleteId !== null} title="Delete Automation"
        message="Are you sure? This automation will be permanently deleted."
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
        onCancel={() => setDeleteId(null)} />
    </div>
  );
}

// ─── Effect Form Modal ──────────────────────────────────────

function EffectFormModal({ editingId, types, onClose, onSaved }: {
  editingId: number | null; types: EffectTypes; onClose: () => void; onSaved: () => void;
}) {
  const { data: existing } = useQuery<EffectList>({
    queryKey: ["effect", editingId],
    queryFn: () => effectsApi.getById(editingId!),
    enabled: editingId !== null,
  });

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [triggerTypeId, setTriggerTypeId] = useState(types.triggers[0]?.id ?? "");
  const [triggerConfig, setTriggerConfig] = useState("{}");
  const [conditionsConfig, setConditionsConfig] = useState("[]");
  const [effectsConfig, setEffectsConfig] = useState("[]");
  const [cooldown, setCooldown] = useState(0);

  useEffect(() => {
    if (existing) {
      setName(existing.name);
      setDescription(existing.description ?? "");
      setTriggerTypeId(existing.triggerTypeId);
      setTriggerConfig(existing.triggerConfig);
      setConditionsConfig(existing.conditionsConfig);
      setEffectsConfig(existing.effectsConfig);
      setCooldown(existing.cooldown);
    }
  }, [existing]);

  const mutation = useMutation({
    mutationFn: () => editingId
      ? effectsApi.update(editingId, { name, description: description || undefined, triggerTypeId, triggerConfig, conditionsConfig, effectsConfig, cooldown })
      : effectsApi.create({ name, description: description || undefined, triggerTypeId, triggerConfig, conditionsConfig, effectsConfig, cooldown }),
    onSuccess: () => { showToast("success", editingId ? "Automation updated." : "Automation created."); onSaved(); },
    onError: () => showToast("error", "Failed to save."),
  });

  return (
    <Modal open={true} title={editingId ? "Edit Automation" : "New Automation"} onClose={onClose} size="lg">
      <div className="space-y-5 max-h-[70vh] overflow-y-auto pr-1">
        {/* Name + Cooldown */}
        <div className="grid grid-cols-3 gap-4">
          <div className="col-span-2">
            <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Name</label>
            <input type="text" value={name} onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Welcome New Followers"
              className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]" />
          </div>
          <div>
            <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Cooldown</label>
            <div className="flex items-center gap-1">
              <input type="number" value={cooldown} onChange={(e) => setCooldown(Number(e.target.value))} min={0}
                className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]" />
              <span className="text-xs text-[var(--color-text-muted)] whitespace-nowrap">sec</span>
            </div>
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Description</label>
          <input type="text" value={description} onChange={(e) => setDescription(e.target.value)}
            placeholder="What does this automation do?"
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]" />
        </div>

        {/* Automation Builder: Trigger + Conditions + Effects */}
        <VisualAutomationBuilder
          triggerType={triggerTypeId}
          triggerConfig={triggerConfig}
          conditions={conditionsConfig}
          effects={effectsConfig}
          onTriggerTypeChange={(type) => { setTriggerTypeId(type); }}
          onTriggerConfigChange={setTriggerConfig}
          onConditionsChange={setConditionsConfig}
          onEffectsChange={setEffectsConfig}
        />
      </div>

      <div className="flex justify-end gap-2 pt-4 border-t border-[var(--color-border)] mt-4">
        <button onClick={onClose} className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)]">Cancel</button>
        <button onClick={() => mutation.mutate()} disabled={!name.trim() || mutation.isPending}
          className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] disabled:opacity-50">
          {editingId ? "Save" : "Create"}
        </button>
      </div>
    </Modal>
  );
}

