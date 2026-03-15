import { NavLink, Outlet } from "react-router-dom";

const NAV_ITEMS = [
  { to: "/", label: "Dashboard", icon: "📊" },
  { to: "/commands", label: "Commands", icon: "⌨️" },
  { to: "/users", label: "Users", icon: "👥" },
  { to: "/settings", label: "Settings", icon: "⚙️" },
];

export function DashboardLayout() {
  return (
    <div className="flex h-screen bg-gray-950">
      {/* ─── Sidebar ────────────────────────────────────────── */}
      <aside className="flex w-56 flex-col border-r border-gray-800 bg-gray-950">
        {/* Logo */}
        <div className="flex h-14 items-center gap-2 border-b border-gray-800 px-4">
          <span className="text-lg font-bold text-purple-400">Wrkzg</span>
          <span className="rounded bg-purple-500/20 px-1.5 py-0.5 text-[10px] font-semibold text-purple-400">
            BETA
          </span>
        </div>

        {/* Navigation */}
        <nav className="flex-1 space-y-1 p-3">
          {NAV_ITEMS.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === "/"}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                  isActive
                    ? "bg-purple-500/15 text-purple-400"
                    : "text-gray-400 hover:bg-gray-800/60 hover:text-gray-200"
                }`
              }
            >
              <span className="text-base">{item.icon}</span>
              {item.label}
            </NavLink>
          ))}
        </nav>

        {/* Bottom info */}
        <div className="border-t border-gray-800 p-3">
          <BotStatusIndicator />
        </div>
      </aside>

      {/* ─── Main Content ───────────────────────────────────── */}
      <main className="flex-1 overflow-y-auto">
        <Outlet />
      </main>
    </div>
  );
}

function BotStatusIndicator() {
  // TODO: Use useSignalR to get live bot status
  // For now, show a static placeholder
  return (
    <div className="flex items-center gap-2 text-xs text-gray-500">
      <span className="h-2 w-2 rounded-full bg-gray-600" />
      <span>Bot offline</span>
    </div>
  );
}
