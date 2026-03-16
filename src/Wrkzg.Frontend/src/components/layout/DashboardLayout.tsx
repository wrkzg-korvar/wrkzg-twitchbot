import { NavLink, Outlet } from "react-router-dom";
import { LayoutDashboard, Terminal, Users, Settings, Wifi, WifiOff, Sun, Moon } from "lucide-react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { useSignalR } from "../../hooks/useSignalR";
import { useTheme } from "../../hooks/useTheme";
import { TitleBar } from "./TitleBar";

interface StatusResponse {
  bot: { isConnected: boolean; channel: string | null };
}

const NAV_ITEMS = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard },
  { to: "/commands", label: "Commands", icon: Terminal },
  { to: "/users", label: "Users", icon: Users },
  { to: "/settings", label: "Settings", icon: Settings },
];

export function DashboardLayout() {
  return (
    <div className="flex h-full flex-col bg-[var(--color-bg)]">
      <TitleBar />

      <div className="flex flex-1 overflow-hidden">
      <aside className="flex w-56 flex-col border-r border-[var(--color-border)] bg-[var(--color-bg)]">
        <nav className="flex-1 space-y-1 p-3">
          {NAV_ITEMS.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === "/"}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                  isActive
                    ? "bg-[var(--color-brand-subtle)] text-[var(--color-brand-text)]"
                    : "text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] hover:text-[var(--color-text)]"
                }`
              }
            >
              <item.icon className="h-4 w-4" />
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="border-t border-[var(--color-border)] p-3">
          <ThemeToggle />
        </div>

        <div className="border-t border-[var(--color-border)] p-3">
          <BotStatusIndicator />
        </div>
      </aside>

      <main className="flex-1 overflow-y-auto">
        <Outlet />
      </main>
      </div>
    </div>
  );
}

function ThemeToggle() {
  const { theme, toggle } = useTheme();
  const Icon = theme === "dark" ? Sun : Moon;

  return (
    <button
      onClick={toggle}
      className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-[var(--color-text-secondary)] transition-colors hover:bg-[var(--color-elevated)] hover:text-[var(--color-text)]"
    >
      <Icon className="h-4 w-4" />
      {theme === "dark" ? "Light mode" : "Dark mode"}
    </button>
  );
}

function BotStatusIndicator() {
  const queryClient = useQueryClient();
  const { isConnected: signalRConnected, on, off } = useSignalR("/hubs/chat");

  // Use the same query key as Dashboard — shared cache
  const { data } = useQuery<StatusResponse>({
    queryKey: ["status"],
    queryFn: async () => {
      const res = await fetch("/api/status");
      if (!res.ok) throw new Error("Failed");
      return res.json();
    },
    refetchInterval: 15_000,
  });

  // Refetch status when SignalR reports a change
  useEffect(() => {
    if (!signalRConnected) return;

    on("BotStatus", () => {
      queryClient.invalidateQueries({ queryKey: ["status"] });
    });

    return () => off("BotStatus");
  }, [signalRConnected, on, off, queryClient]);

  const botConnected = data?.bot.isConnected ?? false;
  const botChannel = data?.bot.channel ?? null;

  if (botConnected) {
    return (
      <div className="flex items-center gap-2 text-xs">
        <Wifi className="h-3.5 w-3.5 text-green-400" />
        <div>
          <p className="font-medium text-green-400">Bot online</p>
          {botChannel && <p className="text-[var(--color-text-muted)]">#{botChannel}</p>}
        </div>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-2 text-xs text-[var(--color-text-muted)]">
      <WifiOff className="h-3.5 w-3.5" />
      <span>Bot offline</span>
    </div>
  );
}
