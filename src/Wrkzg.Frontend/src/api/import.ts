import { api } from "./client";

export interface ImportResult {
  totalRows: number;
  importedCount: number;
  skippedCount: number;
  createdCount: number;
  updatedCount: number;
  rolesAssignedCount: number;
  commandsImportedCount: number;
  commandsSkippedCount: number;
  quotesImportedCount: number;
  timersImportedCount: number;
  errors: ImportRowError[];
  success: boolean;
  summary: string;
}

export interface ImportRowError {
  rowNumber: number;
  field: string;
  message: string;
  severity: number;
}

export interface ImportTemplate {
  id: string;
  name: string;
  sourceType: number;
  description: string;
  fields: string[];
  fileTypes: string[];
  fileHint?: string;
}

export interface CsvPreview {
  headers: string[];
  sampleRows: string[][];
  columnCount: number;
  totalRows: number;
}

export const importApi = {
  getTemplates: () => api.get<ImportTemplate[]>("/api/import/templates"),

  preview: (file: File, config: Record<string, unknown>) => {
    const form = new FormData();
    form.append("file", file);
    form.append("config", JSON.stringify(config));
    return api.upload<ImportResult>("/api/import/preview", form);
  },

  execute: (file: File, config: Record<string, unknown>) => {
    const form = new FormData();
    form.append("file", file);
    form.append("config", JSON.stringify(config));
    return api.upload<ImportResult>("/api/import/execute", form);
  },

  previewColumns: (file: File, hasHeader: boolean, delimiter: string) => {
    const form = new FormData();
    form.append("file", file);
    form.append("hasHeader", hasHeader.toString());
    form.append("delimiter", delimiter);
    return api.upload<CsvPreview>("/api/import/preview-columns", form);
  },
};
