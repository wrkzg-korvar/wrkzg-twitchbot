import { api } from "./client";

export interface AssetFile {
  fileName: string;
  url: string;
  size: number;
  lastModified: string;
}

export interface UploadResult {
  fileName: string;
  url: string;
  category: string;
  size: number;
}

export const assetsApi = {
  list: (category: "sounds" | "images") =>
    api.get<AssetFile[]>(`/api/assets/${category}`),

  upload: (category: "sounds" | "images", file: File) => {
    const form = new FormData();
    form.append("file", file);
    return api.upload<UploadResult>(`/api/assets/upload/${category}`, form);
  },

  delete: (category: "sounds" | "images", fileName: string) =>
    api.del(`/api/assets/${category}/${encodeURIComponent(fileName)}`),
};
