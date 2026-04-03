import { useState, useRef } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Upload, Trash2, Play, Square, Check } from "lucide-react";
import { assetsApi } from "../../api/assets";
import type { AssetFile } from "../../api/assets";
import { showToast } from "../../hooks/useToast";

interface AssetPickerProps {
  category: "sounds" | "images";
  value: string;
  onChange: (url: string) => void;
  label?: string;
}

export function AssetPicker({ category, value, onChange, label }: AssetPickerProps) {
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [playingUrl, setPlayingUrl] = useState<string | null>(null);
  const audioRef = useRef<HTMLAudioElement | null>(null);

  const { data: assets } = useQuery<AssetFile[]>({
    queryKey: ["assets", category],
    queryFn: () => assetsApi.list(category),
  });

  const uploadMutation = useMutation({
    mutationFn: (file: File) => assetsApi.upload(category, file),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ["assets", category] });
      onChange(result.url);
      showToast("success", `Uploaded ${result.fileName}`);
    },
    onError: (err) => showToast("error", err instanceof Error ? err.message : "Upload failed"),
  });

  const deleteMutation = useMutation({
    mutationFn: (fileName: string) => assetsApi.delete(category, fileName),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["assets", category] });
      showToast("success", "File deleted.");
    },
  });

  function handleUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (file) {
      uploadMutation.mutate(file);
    }
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  }

  function togglePlay(url: string) {
    if (playingUrl === url) {
      audioRef.current?.pause();
      setPlayingUrl(null);
    } else {
      audioRef.current?.pause();
      const audio = new Audio(url);
      audio.volume = 0.8;
      audio.onended = () => setPlayingUrl(null);
      audio.play().catch(() => {});
      audioRef.current = audio;
      setPlayingUrl(url);
    }
  }

  const accept = category === "sounds" ? ".mp3,.wav,.ogg" : ".png,.jpg,.jpeg,.gif,.webp,.webm,.svg";

  return (
    <div>
      {label && <label className="mb-1 block text-xs font-medium text-[var(--color-text-secondary)]">{label}</label>}

      {/* Current value display */}
      {value && (
        <div className="mb-2 flex items-center gap-2">
          {category === "images" ? (
            <img src={value} alt="" className="h-10 w-10 rounded border border-[var(--color-border)] object-cover" />
          ) : (
            <button onClick={() => togglePlay(value)}
              className="rounded border border-[var(--color-border)] p-1 text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]">
              {playingUrl === value ? <Square className="h-3 w-3" /> : <Play className="h-3 w-3" />}
            </button>
          )}
          <span className="flex-1 truncate text-xs font-mono text-[var(--color-text-secondary)]">{value.split("/").pop()}</span>
          <button onClick={() => onChange("")}
            className="text-xs text-[var(--color-error)] hover:underline">Remove</button>
        </div>
      )}

      {/* Asset grid */}
      <div className="flex flex-wrap gap-1.5 mb-2">
        {assets?.map((a) => (
          <button
            key={a.fileName}
            onClick={() => onChange(a.url)}
            className={`group relative rounded border p-1 transition-colors ${
              value === a.url
                ? "border-[var(--color-brand)] bg-[var(--color-brand-subtle)]"
                : "border-[var(--color-border)] hover:bg-[var(--color-elevated)]"
            }`}
            title={a.fileName}
          >
            {category === "images" ? (
              <img src={a.url} alt={a.fileName} className="h-10 w-10 rounded object-cover" />
            ) : (
              <div className="flex h-10 w-20 items-center justify-center gap-1">
                <Play className="h-3 w-3 text-[var(--color-text-muted)]" />
                <span className="max-w-14 truncate text-[10px] text-[var(--color-text-muted)]">{a.fileName}</span>
              </div>
            )}
            {value === a.url && (
              <div className="absolute -right-1 -top-1 rounded-full bg-[var(--color-brand)] p-0.5">
                <Check className="h-2.5 w-2.5 text-white" />
              </div>
            )}
            <button
              onClick={(e) => { e.stopPropagation(); deleteMutation.mutate(a.fileName); }}
              className="absolute -right-1 -bottom-1 hidden rounded-full bg-[var(--color-error)] p-0.5 group-hover:block"
            >
              <Trash2 className="h-2.5 w-2.5 text-white" />
            </button>
          </button>
        ))}
      </div>

      {/* Upload button */}
      <button
        onClick={() => fileInputRef.current?.click()}
        disabled={uploadMutation.isPending}
        className="flex items-center gap-1.5 rounded border border-dashed border-[var(--color-border)] px-3 py-1.5 text-xs text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]"
      >
        <Upload className="h-3 w-3" />
        {uploadMutation.isPending ? "Uploading..." : `Upload ${category === "sounds" ? "Sound" : "Image"}`}
      </button>
      <input ref={fileInputRef} type="file" accept={accept} onChange={handleUpload} className="hidden" />
    </div>
  );
}
