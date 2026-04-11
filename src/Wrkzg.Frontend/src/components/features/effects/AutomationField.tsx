import type { FieldDef, VariableDef } from "../../../data/automationRegistry";

interface AutomationFieldProps {
  field: FieldDef;
  value: string;
  onChange: (value: string) => void;
  variables?: VariableDef[];
}

const inputClass =
  "w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]";

export function AutomationField({ field, value, onChange, variables }: AutomationFieldProps) {
  const showChips = variables && variables.length > 0 && (field.type === "text" || field.type === "textarea");

  return (
    <div>
      <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">
        {field.label}
        {field.required && <span className="text-[var(--color-error)] ml-0.5">*</span>}
      </label>

      {field.type === "text" && (
        <input
          type="text"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={field.placeholder}
          className={inputClass}
        />
      )}

      {field.type === "number" && (
        <div className="flex items-center gap-2">
          <input
            type="number"
            value={value}
            onChange={(e) => onChange(e.target.value)}
            placeholder={field.placeholder}
            min={field.min}
            max={field.max}
            className={inputClass}
          />
          {field.suffix && (
            <span className="text-xs text-[var(--color-text-muted)] whitespace-nowrap">{field.suffix}</span>
          )}
        </div>
      )}

      {field.type === "select" && (
        <select
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className={inputClass}
        >
          {!value && <option value="">-- Select --</option>}
          {field.options?.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      )}

      {field.type === "textarea" && (
        <textarea
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={field.placeholder}
          rows={2}
          className={inputClass}
        />
      )}

      {field.helperText && (
        <p className="text-[10px] text-[var(--color-text-muted)] mt-0.5">{field.helperText}</p>
      )}

      {showChips && (
        <div className="flex flex-wrap gap-1 mt-1.5">
          {variables.map((v) => (
            <button
              key={v.name}
              type="button"
              title={v.description}
              onClick={() => onChange(value + v.name)}
              className="rounded bg-[var(--color-elevated)] border border-[var(--color-border)] px-1.5 py-0.5 text-[10px] font-mono text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:border-[var(--color-brand)] transition-colors"
            >
              {v.name}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
