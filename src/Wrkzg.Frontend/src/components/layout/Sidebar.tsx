import { NavLink } from "react-router-dom";
import {
  LayoutDashboard,
  Terminal,
  MessageSquareQuote,
  BarChart3,
  Gift,
  Hash,
  Clock,
  Bell,
  Monitor,
  Shield,
  Users,
  Settings,
  Gem,
  Crown,
} from "lucide-react";
import { SidebarGroup } from "./SidebarGroup";
import { ThemeToggle } from "./ThemeToggle";
import { BotStatusIndicator } from "./BotStatusIndicator";

interface NavItem {
  to: string;
  label: string;
  icon: typeof LayoutDashboard;
}

const NAV_GROUPS: { label?: string; items: NavItem[] }[] = [
  {
    items: [
      { to: "/", label: "Dashboard", icon: LayoutDashboard },
    ],
  },
  {
    label: "Chat",
    items: [
      { to: "/commands", label: "Commands", icon: Terminal },
      { to: "/quotes", label: "Quotes", icon: MessageSquareQuote },
    ],
  },
  {
    label: "Engagement",
    items: [
      { to: "/polls", label: "Polls", icon: BarChart3 },
      { to: "/raffles", label: "Raffles", icon: Gift },
      { to: "/counters", label: "Counters", icon: Hash },
      { to: "/channel-points", label: "Channel Points", icon: Gem },
    ],
  },
  {
    label: "Automation",
    items: [
      { to: "/timers", label: "Timers", icon: Clock },
      { to: "/notifications", label: "Notifications", icon: Bell },
    ],
  },
  {
    label: "Stream",
    items: [
      { to: "/overlays", label: "Overlays", icon: Monitor },
    ],
  },
  {
    label: "Community",
    items: [
      { to: "/roles", label: "Roles & Ranks", icon: Crown },
    ],
  },
  {
    label: "Moderation",
    items: [
      { to: "/spam-filter", label: "Spam Filter", icon: Shield },
      { to: "/users", label: "Users", icon: Users },
    ],
  },
];

function navLinkClass({ isActive }: { isActive: boolean }) {
  return `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
    isActive
      ? "bg-[var(--color-brand-subtle)] text-[var(--color-brand-text)]"
      : "text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] hover:text-[var(--color-text)]"
  }`;
}

export function Sidebar() {
  return (
    <aside className="flex w-56 flex-col border-r border-[var(--color-border)] bg-[var(--color-bg)]">
      <nav className="flex-1 overflow-y-auto p-3">
        {NAV_GROUPS.map((group, idx) => (
          <SidebarGroup key={group.label ?? idx} label={group.label}>
            {group.items.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === "/"}
                className={navLinkClass}
              >
                <item.icon className="h-4 w-4" />
                {item.label}
              </NavLink>
            ))}
          </SidebarGroup>
        ))}

        <SidebarGroup>
          <NavLink to="/settings" end className={navLinkClass}>
            <Settings className="h-4 w-4" />
            Settings
          </NavLink>
        </SidebarGroup>
      </nav>

      <div className="border-t border-[var(--color-border)] p-3">
        <ThemeToggle />
      </div>

      <div className="border-t border-[var(--color-border)] p-3">
        <BotStatusIndicator />
      </div>
    </aside>
  );
}
