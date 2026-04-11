import { Outlet } from "react-router-dom";
import { TitleBar } from "./TitleBar";
import { UpdateBanner } from "../ui/UpdateBanner";
import { Sidebar } from "./Sidebar";
import { useSignalR } from "../../hooks/useSignalR";
import { useImportSignalR } from "../../hooks/useImportSignalR";

export function DashboardLayout() {
  const { isConnected, on, off } = useSignalR("/hubs/chat");

  // Global import listener — survives page navigation
  useImportSignalR(isConnected, on, off);

  return (
    <div className="flex h-full flex-col bg-[var(--color-bg)]">
      <TitleBar />
      <UpdateBanner />
      <div className="flex flex-1 overflow-hidden">
        <Sidebar />
        <main className="flex-1 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
