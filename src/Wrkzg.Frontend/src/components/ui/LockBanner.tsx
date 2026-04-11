import { Info } from "lucide-react";

interface LockBannerProps {
  message: string;
}

export function LockBanner({ message }: LockBannerProps) {
  return (
    <div className="rounded-lg border border-amber-500/30 bg-amber-500/10 px-4 py-3 flex items-center gap-3 mb-4">
      <Info className="h-5 w-5 text-amber-500 flex-shrink-0" />
      <p className="text-sm text-amber-200">{message}</p>
    </div>
  );
}
