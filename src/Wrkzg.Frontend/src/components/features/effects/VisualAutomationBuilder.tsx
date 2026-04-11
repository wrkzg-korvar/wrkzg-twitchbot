import { useState, useCallback } from "react";
import { Plus, X, ArrowUp, ArrowDown, Zap, Shield, Sparkles } from "lucide-react";
import {
  TRIGGER_REGISTRY,
  CONDITION_REGISTRY,
  EFFECT_REGISTRY,
  getTriggerDef,
  getConditionDef,
  getEffectDef,
  getVariablesForTrigger,
} from "../../../data/automationRegistry";
import type { FieldDef } from "../../../data/automationRegistry";
import { AutomationField } from "./AutomationField";

interface VisualAutomationBuilderProps {
  triggerType: string;
  triggerConfig: string;
  conditions: string;
  effects: string;
  onTriggerTypeChange: (type: string) => void;
  onTriggerConfigChange: (json: string) => void;
  onConditionsChange: (json: string) => void;
  onEffectsChange: (json: string) => void;
}

interface ConditionItem {
  type: string;
  params: Record<string, string>;
}

interface EffectItem {
  type: string;
  params: Record<string, string>;
}

function safeParse<T>(json: string, fallback: T): T {
  try {
    return JSON.parse(json) as T;
  } catch {
    return fallback;
  }
}

function buildDefaultParams(fields: FieldDef[]): Record<string, string> {
  const params: Record<string, string> = {};
  for (const f of fields) {
    params[f.key] = "";
  }
  return params;
}

export function VisualAutomationBuilder({
  triggerType,
  triggerConfig,
  conditions,
  effects,
  onTriggerTypeChange,
  onTriggerConfigChange,
  onConditionsChange,
  onEffectsChange,
}: VisualAutomationBuilderProps) {
  const [mode, setMode] = useState<"visual" | "json">("visual");

  const triggerDef = getTriggerDef(triggerType);
  const triggerParams = safeParse<Record<string, string>>(triggerConfig, {});
  const conditionItems = safeParse<ConditionItem[]>(conditions, []);
  const effectItems = safeParse<EffectItem[]>(effects, []);

  // --- Trigger helpers ---
  const handleTriggerFieldChange = useCallback(
    (key: string, val: string) => {
      const updated = { ...safeParse<Record<string, string>>(triggerConfig, {}), [key]: val };
      onTriggerConfigChange(JSON.stringify(updated));
    },
    [triggerConfig, onTriggerConfigChange],
  );

  const handleTriggerTypeSwitch = useCallback(
    (newType: string) => {
      onTriggerTypeChange(newType);
      const def = getTriggerDef(newType);
      if (def) {
        onTriggerConfigChange(JSON.stringify(buildDefaultParams(def.fields)));
      } else {
        onTriggerConfigChange("{}");
      }
    },
    [onTriggerTypeChange, onTriggerConfigChange],
  );

  // --- Condition helpers ---
  const updateConditions = useCallback(
    (items: ConditionItem[]) => onConditionsChange(JSON.stringify(items)),
    [onConditionsChange],
  );

  const addCondition = useCallback(() => {
    const first = CONDITION_REGISTRY[0];
    updateConditions([...conditionItems, { type: first.id, params: buildDefaultParams(first.fields) }]);
  }, [conditionItems, updateConditions]);

  const removeCondition = useCallback(
    (idx: number) => updateConditions(conditionItems.filter((_, i) => i !== idx)),
    [conditionItems, updateConditions],
  );

  const setConditionType = useCallback(
    (idx: number, newType: string) => {
      const def = getConditionDef(newType);
      const updated = [...conditionItems];
      updated[idx] = { type: newType, params: def ? buildDefaultParams(def.fields) : {} };
      updateConditions(updated);
    },
    [conditionItems, updateConditions],
  );

  const setConditionParam = useCallback(
    (idx: number, key: string, val: string) => {
      const updated = [...conditionItems];
      updated[idx] = { ...updated[idx], params: { ...updated[idx].params, [key]: val } };
      updateConditions(updated);
    },
    [conditionItems, updateConditions],
  );

  // --- Effect helpers ---
  const updateEffects = useCallback(
    (items: EffectItem[]) => onEffectsChange(JSON.stringify(items)),
    [onEffectsChange],
  );

  const addEffect = useCallback(() => {
    const first = EFFECT_REGISTRY[0];
    updateEffects([...effectItems, { type: first.id, params: buildDefaultParams(first.fields) }]);
  }, [effectItems, updateEffects]);

  const removeEffect = useCallback(
    (idx: number) => updateEffects(effectItems.filter((_, i) => i !== idx)),
    [effectItems, updateEffects],
  );

  const setEffectType = useCallback(
    (idx: number, newType: string) => {
      const def = getEffectDef(newType);
      const updated = [...effectItems];
      updated[idx] = { type: newType, params: def ? buildDefaultParams(def.fields) : {} };
      updateEffects(updated);
    },
    [effectItems, updateEffects],
  );

  const setEffectParam = useCallback(
    (idx: number, key: string, val: string) => {
      const updated = [...effectItems];
      updated[idx] = { ...updated[idx], params: { ...updated[idx].params, [key]: val } };
      updateEffects(updated);
    },
    [effectItems, updateEffects],
  );

  const moveEffect = useCallback(
    (idx: number, dir: -1 | 1) => {
      const target = idx + dir;
      if (target < 0 || target >= effectItems.length) {
        return;
      }
      const updated = [...effectItems];
      [updated[idx], updated[target]] = [updated[target], updated[idx]];
      updateEffects(updated);
    },
    [effectItems, updateEffects],
  );

  // --- Current trigger variables for effect fields ---
  const triggerVariables = getVariablesForTrigger(triggerType);

  const inputClass =
    "w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]";
  const monoTextareaClass =
    "w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] px-3 py-2 text-xs text-[var(--color-text)] font-mono";

  return (
    <div className="space-y-4">
      {/* Mode toggle */}
      <div className="flex gap-1 rounded-lg bg-[var(--color-elevated)] p-0.5 w-fit">
        <button
          type="button"
          onClick={() => setMode("visual")}
          className={`rounded-md px-3 py-1 text-xs font-medium transition-colors ${
            mode === "visual"
              ? "bg-[var(--color-brand)] text-[var(--color-bg)]"
              : "text-[var(--color-text-muted)] hover:text-[var(--color-text)]"
          }`}
        >
          Visual
        </button>
        <button
          type="button"
          onClick={() => setMode("json")}
          className={`rounded-md px-3 py-1 text-xs font-medium transition-colors ${
            mode === "json"
              ? "bg-[var(--color-brand)] text-[var(--color-bg)]"
              : "text-[var(--color-text-muted)] hover:text-[var(--color-text)]"
          }`}
        >
          JSON
        </button>
      </div>

      {mode === "json" ? (
        <div className="space-y-3">
          <div>
            <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">
              Trigger Config (JSON)
            </label>
            <textarea
              value={triggerConfig}
              onChange={(e) => onTriggerConfigChange(e.target.value)}
              rows={2}
              className={monoTextareaClass}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">
              Conditions (JSON Array)
            </label>
            <textarea
              value={conditions}
              onChange={(e) => onConditionsChange(e.target.value)}
              rows={3}
              className={monoTextareaClass}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">
              Effects (JSON Array)
            </label>
            <textarea
              value={effects}
              onChange={(e) => onEffectsChange(e.target.value)}
              rows={4}
              className={monoTextareaClass}
            />
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          {/* ---- TRIGGER ---- */}
          <div className="rounded-lg border border-blue-500/20 bg-blue-500/5 p-4">
            <div className="flex items-center gap-2 mb-1">
              <Zap className="h-4 w-4 text-blue-700 dark:text-blue-400" />
              <h4 className="text-sm font-semibold text-blue-700 dark:text-blue-400">Trigger</h4>
            </div>
            <p className="text-xs text-[var(--color-text-muted)] mb-3">When should this automation activate?</p>
            <select
              value={triggerType}
              onChange={(e) => handleTriggerTypeSwitch(e.target.value)}
              className={inputClass + " mb-2"}
            >
              {TRIGGER_REGISTRY.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.displayName}
                </option>
              ))}
            </select>
            {triggerDef && (
              <>
                <p className="text-xs text-[var(--color-text-muted)] mb-2">{triggerDef.description}</p>
                <div className="space-y-2">
                  {triggerDef.fields.map((f) => (
                    <AutomationField
                      key={f.key}
                      field={f}
                      value={triggerParams[f.key] ?? ""}
                      onChange={(val) => handleTriggerFieldChange(f.key, val)}
                    />
                  ))}
                </div>
              </>
            )}
          </div>

          {/* ---- CONDITIONS ---- */}
          <div className="rounded-lg border border-amber-500/20 bg-amber-500/5 p-4">
            <div className="flex items-center gap-2 mb-1">
              <Shield className="h-4 w-4 text-amber-700 dark:text-amber-400" />
              <h4 className="text-sm font-semibold text-amber-700 dark:text-amber-400">Conditions</h4>
            </div>
            <p className="text-xs text-[var(--color-text-muted)] mb-3">Optional: Only run if ALL of these are true</p>
          <div className="space-y-2">
            {conditionItems.length === 0 && (
              <p className="text-xs text-[var(--color-text-muted)] italic">
                Keine Bedingungen -- wird immer ausgeführt wenn der Trigger feuert.
              </p>
            )}
            {conditionItems.map((cond, idx) => {
              const def = getConditionDef(cond.type);
              return (
                <div
                  key={idx}
                  className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-3 space-y-2"
                >
                  <div className="flex items-center gap-2">
                    <select
                      value={cond.type}
                      onChange={(e) => setConditionType(idx, e.target.value)}
                      className="flex-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-xs text-[var(--color-text)]"
                    >
                      {CONDITION_REGISTRY.map((c) => (
                        <option key={c.id} value={c.id}>
                          {c.displayName}
                        </option>
                      ))}
                    </select>
                    <button
                      type="button"
                      onClick={() => removeCondition(idx)}
                      className="rounded p-1 text-[var(--color-error)] hover:bg-[var(--color-elevated)]"
                      title="Remove condition"
                    >
                      <X className="h-3.5 w-3.5" />
                    </button>
                  </div>
                  {def && (
                    <p className="text-[10px] text-[var(--color-text-muted)]">{def.description}</p>
                  )}
                  {def?.fields.map((f) => (
                    <AutomationField
                      key={f.key}
                      field={f}
                      value={cond.params[f.key] ?? ""}
                      onChange={(val) => setConditionParam(idx, f.key, val)}
                    />
                  ))}
                </div>
              );
            })}
            <button
              type="button"
              onClick={addCondition}
              className="flex items-center gap-1 rounded-lg border border-dashed border-[var(--color-border)] px-3 py-1.5 text-xs text-[var(--color-text-muted)] hover:border-[var(--color-brand)] hover:text-[var(--color-brand)] transition-colors"
            >
              <Plus className="h-3 w-3" /> Bedingung
            </button>
          </div>
          </div>

          {/* ---- EFFECTS ---- */}
          <div className="rounded-lg border border-green-500/20 bg-green-500/5 p-4">
            <div className="flex items-center gap-2 mb-1">
              <Sparkles className="h-4 w-4 text-green-700 dark:text-green-400" />
              <h4 className="text-sm font-semibold text-green-700 dark:text-green-400">Effects</h4>
            </div>
            <p className="text-xs text-[var(--color-text-muted)] mb-3">Actions to run in order when triggered</p>
          <div className="space-y-2">
            {effectItems.map((eff, idx) => {
              const def = getEffectDef(eff.type);
              return (
                <div
                  key={idx}
                  className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-3 space-y-2"
                >
                  <div className="flex items-center gap-2">
                    <span className="text-[10px] font-mono text-[var(--color-text-muted)] w-4 text-right">
                      {idx + 1}
                    </span>
                    <select
                      value={eff.type}
                      onChange={(e) => setEffectType(idx, e.target.value)}
                      className="flex-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-xs text-[var(--color-text)]"
                    >
                      {EFFECT_REGISTRY.map((e) => (
                        <option key={e.id} value={e.id}>
                          {e.displayName}
                        </option>
                      ))}
                    </select>
                    <button
                      type="button"
                      onClick={() => moveEffect(idx, -1)}
                      disabled={idx === 0}
                      className="rounded p-1 text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)] disabled:opacity-30"
                      title="Move up"
                    >
                      <ArrowUp className="h-3.5 w-3.5" />
                    </button>
                    <button
                      type="button"
                      onClick={() => moveEffect(idx, 1)}
                      disabled={idx === effectItems.length - 1}
                      className="rounded p-1 text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)] disabled:opacity-30"
                      title="Move down"
                    >
                      <ArrowDown className="h-3.5 w-3.5" />
                    </button>
                    <button
                      type="button"
                      onClick={() => removeEffect(idx)}
                      className="rounded p-1 text-[var(--color-error)] hover:bg-[var(--color-elevated)]"
                      title="Remove effect"
                    >
                      <X className="h-3.5 w-3.5" />
                    </button>
                  </div>
                  {def && (
                    <p className="text-[10px] text-[var(--color-text-muted)]">{def.description}</p>
                  )}
                  {def?.fields.map((f) => (
                    <AutomationField
                      key={f.key}
                      field={f}
                      value={eff.params[f.key] ?? ""}
                      onChange={(val) => setEffectParam(idx, f.key, val)}
                      variables={
                        def.supportsVariables && (f.type === "text" || f.type === "textarea")
                          ? triggerVariables
                          : undefined
                      }
                    />
                  ))}
                </div>
              );
            })}
            <button
              type="button"
              onClick={addEffect}
              className="flex items-center gap-1 rounded-lg border border-dashed border-[var(--color-border)] px-3 py-1.5 text-xs text-[var(--color-text-muted)] hover:border-[var(--color-brand)] hover:text-[var(--color-brand)] transition-colors"
            >
              <Plus className="h-3 w-3" /> Aktion
            </button>
          </div>
          </div>
        </div>
      )}
    </div>
  );
}
