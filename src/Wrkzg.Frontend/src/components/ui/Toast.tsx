import { CheckCircle, XCircle, Info } from "lucide-react";
import { useToast, type ToastType } from "../../hooks/useToast";

const icons: Record<ToastType, typeof CheckCircle> = {
  success: CheckCircle,
  error: XCircle,
  info: Info,
};

const colorClasses: Record<ToastType, string> = {
  success: "border-green-600/30 bg-green-950/80 text-green-400",
  error: "border-red-600/30 bg-red-950/80 text-red-400",
  info: "border-blue-600/30 bg-blue-950/80 text-blue-400",
};

export function ToastContainer() {
  const toasts = useToast();

  if (toasts.length === 0) {
    return null;
  }

  return (
    <div className="fixed bottom-4 right-4 z-[100] flex flex-col gap-2">
      {toasts.map((toast) => {
        const Icon = icons[toast.type];
        return (
          <div
            key={toast.id}
            className={`flex items-center gap-2 rounded-lg border px-4 py-2.5 text-sm shadow-lg ${colorClasses[toast.type]}`}
          >
            <Icon className="h-4 w-4 shrink-0" />
            <span>{toast.message}</span>
          </div>
        );
      })}
    </div>
  );
}
