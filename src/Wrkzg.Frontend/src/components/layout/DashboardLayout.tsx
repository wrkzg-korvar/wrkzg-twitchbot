import { Outlet } from "react-router-dom";
import { TitleBar } from "./TitleBar";
import { UpdateBanner } from "../ui/UpdateBanner";
import { Sidebar } from "./Sidebar";

export function DashboardLayout() {
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
