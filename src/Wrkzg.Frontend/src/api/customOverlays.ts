import { api } from "./client";

export interface CustomOverlay {
  id: number;
  name: string;
  description?: string;
  html: string;
  css: string;
  javaScript: string;
  fieldDefinitions: string;
  fieldValues: string;
  width: number;
  height: number;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export const customOverlaysApi = {
  getAll: () => api.get<CustomOverlay[]>("/api/custom-overlays"),
  getById: (id: number) => api.get<CustomOverlay>(`/api/custom-overlays/${id}`),
  create: (data: Partial<CustomOverlay>) => api.post<CustomOverlay>("/api/custom-overlays", data),
  update: (id: number, data: Partial<CustomOverlay>) => api.put<CustomOverlay>(`/api/custom-overlays/${id}`, data),
  updateFields: (id: number, fieldValues: string) => api.put<CustomOverlay>(`/api/custom-overlays/${id}/fields`, { fieldValues }),
  delete: (id: number) => api.del(`/api/custom-overlays/${id}`),
};
