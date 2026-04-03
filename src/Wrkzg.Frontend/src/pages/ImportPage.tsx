import { useState, useRef, useEffect, useCallback } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { Upload, ChevronRight, ChevronLeft, Check, AlertTriangle, Users, RefreshCw } from "lucide-react";
import { importApi } from "../api/import";
import type { ImportTemplate, ImportResult, CsvPreview } from "../api/import";
import { PageHeader } from "../components/ui/PageHeader";
import { Card } from "../components/ui/Card";
import { showToast } from "../hooks/useToast";

const CONFLICT_STRATEGIES = [
  { value: 0, label: "Skip", desc: "Keep existing data, don't import" },
  { value: 1, label: "Overwrite", desc: "Replace with imported data" },
  { value: 2, label: "Keep Higher", desc: "Use the higher value for each field" },
  { value: 3, label: "Add", desc: "Add imported values to existing values" },
];

// Known header names that map to our fields
const KNOWN_MAPPINGS: Record<string, string[]> = {
  username: ["username", "user", "name", "viewer", "login"],
  points: ["points", "currency", "coins", "balance"],
  watchedMinutes: ["watchtime", "watch_time", "watched", "minutes", "watchedminutes", "watch_mins"],
};

// Headers that indicate hours instead of minutes
const HOUR_INDICATORS = ["hours", "hour", "watch_hours", "watchtime(hours)", "time(hours)"];

function autoDetectMapping(headers: string[]): Record<string, string> {
  const mapping: Record<string, string> = {};
  const lowerHeaders = headers.map((h) => h.toLowerCase().trim());

  for (const [field, aliases] of Object.entries(KNOWN_MAPPINGS)) {
    for (const alias of aliases) {
      const idx = lowerHeaders.findIndex((h) => h === alias || h.includes(alias));
      if (idx >= 0 && !Object.values(mapping).includes(headers[idx])) {
        mapping[field] = headers[idx];
        break;
      }
    }
  }

  // Also check for hour-based watch time headers
  if (!mapping.watchedMinutes) {
    const hourIdx = lowerHeaders.findIndex((h) =>
      HOUR_INDICATORS.some((ind) => h === ind || h.includes(ind))
    );
    if (hourIdx >= 0) {
      mapping.watchedMinutes = headers[hourIdx];
    }
  }

  return mapping;
}

export function ImportPage() {
  const [step, setStep] = useState(0);
  const [selectedSource, setSelectedSource] = useState<ImportTemplate | null>(null);
  const [file, setFile] = useState<File | null>(null);
  const [conflictStrategy, setConflictStrategy] = useState(2);
  const [previewResult, setPreviewResult] = useState<ImportResult | null>(null);
  const [importResult, setImportResult] = useState<ImportResult | null>(null);
  const [csvPreview, setCsvPreview] = useState<CsvPreview | null>(null);
  const [columnMapping, setColumnMapping] = useState<Record<string, string>>({});
  const [hasHeader, setHasHeader] = useState(true);
  const [delimiter, setDelimiter] = useState(";");
  const fileInputRef = useRef<HTMLInputElement>(null);

  const isGenericCsv = selectedSource?.sourceType === 3;

  const { data: templates } = useQuery<ImportTemplate[]>({
    queryKey: ["import-templates"],
    queryFn: importApi.getTemplates,
  });

  const previewMutation = useMutation({
    mutationFn: () => {
      if (!file) { throw new Error("No file"); }
      return importApi.preview(file, buildConfig());
    },
    onSuccess: (result) => {
      setPreviewResult(result);
      setStep(2);
    },
    onError: () => showToast("error", "Failed to preview file."),
  });

  const columnPreviewMutation = useMutation({
    mutationFn: (params: { f: File; hdr: boolean; delim: string }) =>
      importApi.previewColumns(params.f, params.hdr, params.delim),
    onSuccess: (result) => {
      setCsvPreview(result);
      // Auto-detect column mapping from headers
      if (result.headers.length > 0) {
        const detected = autoDetectMapping(result.headers);
        setColumnMapping((prev) => {
          // Only auto-fill empty fields
          const merged = { ...prev };
          for (const [key, val] of Object.entries(detected)) {
            if (!merged[key]) {
              merged[key] = val;
            }
          }
          return merged;
        });
      }
    },
  });

  const executeMutation = useMutation({
    mutationFn: () => {
      if (!file) { throw new Error("No file"); }
      return importApi.execute(file, buildConfig());
    },
    onSuccess: (result) => {
      setImportResult(result);
      setStep(3);
      showToast("success", result.summary);
    },
    onError: () => showToast("error", "Import failed."),
  });

  // Re-run column preview when delimiter or hasHeader changes (only for Generic CSV with file loaded)
  const refreshPreview = useCallback(() => {
    if (file && isGenericCsv) {
      columnPreviewMutation.mutate({ f: file, hdr: hasHeader, delim: delimiter });
    }
  }, [file, isGenericCsv, hasHeader, delimiter]); // eslint-disable-line react-hooks/exhaustive-deps

  // Auto-refresh preview when settings change
  useEffect(() => {
    if (file && isGenericCsv) {
      refreshPreview();
    }
  }, [hasHeader, delimiter]); // eslint-disable-line react-hooks/exhaustive-deps

  function buildConfig() {
    return {
      sourceType: selectedSource?.sourceType ?? 0,
      conflictStrategy,
      mapVipToRoles: false,
      hasHeader,
      delimiter,
      columnMapping: isGenericCsv ? columnMapping : undefined,
    };
  }

  function handleFileSelect(e: React.ChangeEvent<HTMLInputElement>) {
    const f = e.target.files?.[0];
    if (f) {
      setFile(f);
      setCsvPreview(null);
      setColumnMapping({});
      if (isGenericCsv) {
        columnPreviewMutation.mutate({ f, hdr: hasHeader, delim: delimiter });
      }
    }
  }

  function handleReset() {
    setStep(0);
    setSelectedSource(null);
    setFile(null);
    setPreviewResult(null);
    setImportResult(null);
    setCsvPreview(null);
    setColumnMapping({});
    setConflictStrategy(2);
    if (fileInputRef.current) { fileInputRef.current.value = ""; }
  }

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Import Data"
        description="Migrate your community data from another bot."
        helpKey="import"
      />

      {/* Step indicator */}
      <div className="flex items-center gap-2 text-sm">
        {["Select Source", "Upload & Map", "Settings & Preview", "Result"].map((label, i) => (
          <div key={label} className="flex items-center gap-2">
            {i > 0 && <ChevronRight className="h-3 w-3 text-[var(--color-text-muted)]" />}
            <span className={step === i
              ? "font-medium text-[var(--color-brand)]"
              : step > i
                ? "text-[var(--color-text)]"
                : "text-[var(--color-text-muted)]"
            }>
              {step > i ? <Check className="inline h-3.5 w-3.5 mr-1" /> : null}
              {label}
            </span>
          </div>
        ))}
      </div>

      {/* Step 0: Select Source */}
      {step === 0 && (
        <div className="grid grid-cols-2 gap-4">
          {templates?.map((t) => (
            <button
              key={t.id}
              onClick={() => { setSelectedSource(t); setStep(1); }}
              className="rounded-lg border border-[var(--color-border)] p-4 text-left hover:bg-[var(--color-elevated)] transition-colors"
            >
              <div className="font-medium text-[var(--color-text)]">{t.name}</div>
              <div className="mt-1 text-sm text-[var(--color-text-secondary)]">{t.description}</div>
              <div className="mt-2 flex flex-wrap gap-1">
                {t.fields.map((f) => (
                  <span key={f} className="rounded bg-[var(--color-elevated)] px-2 py-0.5 text-xs text-[var(--color-text-muted)]">{f}</span>
                ))}
              </div>
              <div className="mt-2 text-xs text-[var(--color-text-muted)]">
                Accepts: {t.fileTypes.join(", ")}
              </div>
            </button>
          ))}
        </div>
      )}

      {/* Step 1: Upload File + Column Mapping */}
      {step === 1 && selectedSource && (
        <Card title={`Upload ${selectedSource.name} File`}>
          <div className="space-y-4">
            {/* For Generic CSV: show delimiter/header settings BEFORE file upload */}
            {isGenericCsv && (
              <div className="flex items-center gap-4 rounded-lg bg-[var(--color-elevated)] p-3">
                <label className="flex items-center gap-2 text-sm text-[var(--color-text-secondary)]">
                  <input type="checkbox" checked={hasHeader} onChange={(e) => setHasHeader(e.target.checked)} />
                  Has header row
                </label>
                <label className="flex items-center gap-2 text-sm text-[var(--color-text-secondary)]">
                  Delimiter:
                  <select value={delimiter} onChange={(e) => setDelimiter(e.target.value)}
                    className="rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1 text-sm text-[var(--color-text)]">
                    <option value=",">, (Comma)</option>
                    <option value=";">; (Semicolon)</option>
                    <option value="&#9;">Tab</option>
                  </select>
                </label>
                {file && (
                  <button onClick={refreshPreview}
                    className="flex items-center gap-1 rounded border border-[var(--color-border)] px-2 py-1 text-xs text-[var(--color-text-secondary)] hover:bg-[var(--color-surface)]">
                    <RefreshCw className="h-3 w-3" /> Refresh
                  </button>
                )}
              </div>
            )}

            <div
              onClick={() => fileInputRef.current?.click()}
              className="flex cursor-pointer flex-col items-center justify-center rounded-lg border-2 border-dashed border-[var(--color-border)] p-8 hover:bg-[var(--color-elevated)] transition-colors"
            >
              <Upload className="h-8 w-8 text-[var(--color-text-muted)] mb-2" />
              <p className="text-sm text-[var(--color-text-secondary)]">
                {file ? file.name : "Click to browse or drag and drop"}
              </p>
              <p className="text-xs text-[var(--color-text-muted)] mt-1">
                Supported: {selectedSource.fileTypes.join(", ")}
              </p>
              <input
                ref={fileInputRef}
                type="file"
                accept={selectedSource.fileTypes.join(",")}
                onChange={handleFileSelect}
                className="hidden"
              />
            </div>

            {file && (
              <div className="text-sm text-[var(--color-text-secondary)]">
                Selected: <span className="font-mono text-[var(--color-text)]">{file.name}</span>
                {" "}({(file.size / 1024).toFixed(1)} KB)
              </div>
            )}

            {/* Generic CSV column mapping */}
            {isGenericCsv && file && csvPreview && (
              <div className="space-y-3 rounded-lg bg-[var(--color-elevated)] p-4">
                <div className="flex items-center justify-between">
                  <p className="text-sm font-medium text-[var(--color-text)]">Column Mapping</p>
                  {csvPreview.headers.length > 0 && Object.keys(columnMapping).length > 0 && (
                    <span className="text-xs text-green-400">Auto-detected from headers</span>
                  )}
                </div>

                {["username", "points", "watchedMinutes"].map((field) => (
                  <div key={field} className="flex items-center gap-3">
                    <span className="w-32 text-sm text-[var(--color-text-secondary)]">
                      {field === "watchedMinutes" ? "Watch Time:" : field === "username" ? "Username:" : "Points:"}
                    </span>
                    <select
                      value={columnMapping[field] ?? ""}
                      onChange={(e) => setColumnMapping((prev) => ({ ...prev, [field]: e.target.value }))}
                      className="flex-1 rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1 text-sm text-[var(--color-text)]"
                    >
                      <option value="">— not mapped —</option>
                      {csvPreview.headers.length > 0
                        ? csvPreview.headers.map((h, i) => <option key={i} value={h}>{h}</option>)
                        : Array.from({ length: csvPreview.columnCount }, (_, i) => (
                          <option key={i} value={String(i)}>Column {i + 1}</option>
                        ))}
                    </select>
                  </div>
                ))}

                {csvPreview.sampleRows.length > 0 && (
                  <div className="mt-2">
                    <p className="text-xs text-[var(--color-text-muted)] mb-1">Preview (first {csvPreview.sampleRows.length} rows):</p>
                    <div className="overflow-x-auto">
                      <table className="w-full text-xs">
                        <thead>
                          <tr>
                            {(csvPreview.headers.length > 0 ? csvPreview.headers : Array.from({ length: csvPreview.columnCount }, (_, i) => `Col ${i + 1}`))
                              .map((h, i) => <th key={i} className="px-2 py-1 text-left text-[var(--color-text-muted)] font-medium">{h}</th>)}
                          </tr>
                        </thead>
                        <tbody>
                          {csvPreview.sampleRows.map((row, ri) => (
                            <tr key={ri}>
                              {row.map((cell, ci) => <td key={ci} className="px-2 py-1 text-[var(--color-text-secondary)] font-mono">{cell}</td>)}
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}
              </div>
            )}

            <div className="flex gap-2">
              <button onClick={() => { setStep(0); setFile(null); setCsvPreview(null); setColumnMapping({}); }}
                className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)]">
                <ChevronLeft className="inline h-3.5 w-3.5 mr-1" /> Back
              </button>
              <button
                onClick={() => previewMutation.mutate()}
                disabled={!file || previewMutation.isPending || (isGenericCsv && !columnMapping.username)}
                className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-50"
              >
                {previewMutation.isPending ? "Analyzing..." : "Next"}
                {!previewMutation.isPending && <ChevronRight className="inline h-3.5 w-3.5 ml-1" />}
              </button>
            </div>
          </div>
        </Card>
      )}

      {/* Step 2: Settings & Preview */}
      {step === 2 && previewResult && (
        <Card title="Import Settings">
          <div className="space-y-4">
            <div>
              <p className="text-sm font-medium text-[var(--color-text)] mb-2">When a user already exists in Wrkzg:</p>
              <div className="space-y-2">
                {CONFLICT_STRATEGIES.map((s) => (
                  <label key={s.value} className="flex items-start gap-2 cursor-pointer">
                    <input
                      type="radio"
                      name="conflict"
                      checked={conflictStrategy === s.value}
                      onChange={() => setConflictStrategy(s.value)}
                      className="mt-1"
                    />
                    <div>
                      <span className="text-sm font-medium text-[var(--color-text)]">{s.label}</span>
                      <span className="text-sm text-[var(--color-text-secondary)]"> — {s.desc}</span>
                    </div>
                  </label>
                ))}
              </div>
            </div>

            <div className="rounded-lg bg-[var(--color-elevated)] p-4 space-y-1">
              <p className="text-sm font-medium text-[var(--color-text)]">Preview</p>
              <div className="grid grid-cols-2 gap-2 text-sm">
                <span className="text-[var(--color-text-secondary)]">Total users in file:</span>
                <span className="text-[var(--color-text)] font-mono">{previewResult.totalRows.toLocaleString()}</span>
                <span className="text-[var(--color-text-secondary)]">New users (will be created):</span>
                <span className="text-[var(--color-text)] font-mono">{previewResult.createdCount.toLocaleString()}</span>
                <span className="text-[var(--color-text-secondary)]">Existing users (will be merged):</span>
                <span className="text-[var(--color-text)] font-mono">{previewResult.updatedCount.toLocaleString()}</span>
                {previewResult.skippedCount > 0 && (
                  <>
                    <span className="text-[var(--color-text-secondary)]">Skipped (empty/invalid):</span>
                    <span className="text-[var(--color-text)] font-mono">{previewResult.skippedCount.toLocaleString()}</span>
                  </>
                )}
                {previewResult.rolesAssignedCount > 0 && (
                  <>
                    <span className="text-[var(--color-text-secondary)]">Roles to assign:</span>
                    <span className="text-[var(--color-text)] font-mono">{previewResult.rolesAssignedCount.toLocaleString()}</span>
                  </>
                )}
              </div>
            </div>

            <div className="flex gap-2">
              <button onClick={() => setStep(1)}
                className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)]">
                <ChevronLeft className="inline h-3.5 w-3.5 mr-1" /> Back
              </button>
              <button
                onClick={() => executeMutation.mutate()}
                disabled={executeMutation.isPending}
                className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-50"
              >
                {executeMutation.isPending ? "Importing..." : "Import"}
                {!executeMutation.isPending && <ChevronRight className="inline h-3.5 w-3.5 ml-1" />}
              </button>
            </div>
          </div>
        </Card>
      )}

      {/* Step 3: Result */}
      {step === 3 && importResult && (
        <Card title="Import Complete">
          <div className="space-y-4">
            <div className="flex items-center gap-2 text-green-400">
              <Check className="h-5 w-5" />
              <span className="font-medium">{importResult.summary}</span>
            </div>

            <div className="grid grid-cols-2 gap-2 text-sm rounded-lg bg-[var(--color-elevated)] p-4">
              <span className="text-[var(--color-text-secondary)]">New users created:</span>
              <span className="text-[var(--color-text)] font-mono">{importResult.createdCount.toLocaleString()}</span>
              <span className="text-[var(--color-text-secondary)]">Existing users updated:</span>
              <span className="text-[var(--color-text)] font-mono">{importResult.updatedCount.toLocaleString()}</span>
              <span className="text-[var(--color-text-secondary)]">Skipped:</span>
              <span className="text-[var(--color-text)] font-mono">{importResult.skippedCount.toLocaleString()}</span>
              {importResult.rolesAssignedCount > 0 && (
                <>
                  <span className="text-[var(--color-text-secondary)]">Roles assigned:</span>
                  <span className="text-[var(--color-text)] font-mono">{importResult.rolesAssignedCount.toLocaleString()}</span>
                </>
              )}
            </div>

            {importResult.errors.length > 0 && (
              <div className="space-y-1">
                <p className="flex items-center gap-1 text-sm font-medium text-yellow-400">
                  <AlertTriangle className="h-4 w-4" /> {importResult.errors.length} warning(s):
                </p>
                <div className="max-h-40 overflow-y-auto rounded-lg bg-[var(--color-elevated)] p-3 text-xs font-mono">
                  {importResult.errors.map((err, i) => (
                    <div key={i} className="text-[var(--color-text-secondary)]">
                      Row {err.rowNumber}: {err.message}
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className="flex gap-2">
              <a href="/users"
                className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)]">
                <Users className="h-3.5 w-3.5" /> View Users
              </a>
              <button onClick={handleReset}
                className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)]">
                Import Another File
              </button>
            </div>
          </div>
        </Card>
      )}
    </div>
  );
}
