import { useState } from "react";
import { SettingsAuthSection } from "./SettingsAuthSection";
import { diagnosticsApi } from "../api/diagnostics";
import { showToast } from "../hooks/useToast";

export function SettingsPage() {
  return (
    <div className="mx-auto max-w-3xl p-6">
      <h1 className="mb-8 text-2xl font-bold">Settings</h1>
      <SettingsAuthSection />
      <DiagnosticsSection />
    </div>
  );
}

function DiagnosticsSection() {
  const [isExporting, setIsExporting] = useState(false);

  const handleExport = async () => {
    setIsExporting(true);
    try {
      const result = await diagnosticsApi.exportLog();
      showToast("success", `Log saved to: ${result.path}`);
    } catch {
      showToast("error", "Failed to export diagnostic log");
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <section className="mt-8 rounded-lg border border-[var(--color-border)] p-6">
      <div>
        <h2 className="text-lg font-semibold">Diagnostics</h2>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">
          Export the current diagnostic log file to your Downloads folder.
          Share this file with the developer for bug reports. The log contains
          connection events, errors, and timing information. No sensitive data
          (tokens, passwords) is included.
        </p>
      </div>

      <div className="mt-4">
        <button
          onClick={handleExport}
          disabled={isExporting}
          className="rounded-md bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-50"
        >
          {isExporting ? "Exporting…" : "Export Diagnostic Log"}
        </button>
      </div>
    </section>
  );
}
