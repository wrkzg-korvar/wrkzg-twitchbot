import { Bell, MessageSquare, BarChart3, Gift, Hash, List } from "lucide-react";
import { PageHeader } from "../components/ui/PageHeader";
import { OverlayCard } from "../components/features/overlays/OverlayCard";
import type { OverlayDefinition } from "../components/features/overlays/OverlayCard";

const OVERLAYS: OverlayDefinition[] = [
  {
    type: "alerts",
    title: "Alert Box",
    description: "Animated notifications for follows, subs, and raids.",
    icon: Bell,
    width: 800,
    height: 200,
    testEvents: ["follow", "subscribe", "giftsub", "resub", "raid"],
  },
  {
    type: "chat",
    title: "Chat Box",
    description: "Live chat display for your stream.",
    icon: MessageSquare,
    width: 400,
    height: 600,
  },
  {
    type: "poll",
    title: "Poll",
    description: "Live poll results with animated bars.",
    icon: BarChart3,
    width: 600,
    height: 300,
  },
  {
    type: "raffle",
    title: "Raffle",
    description: "Animated raffle draws and winner reveals.",
    icon: Gift,
    width: 600,
    height: 300,
  },
  {
    type: "counter",
    title: "Counter",
    description: "Display a counter value on your stream.",
    icon: Hash,
    width: 300,
    height: 80,
    testEvents: ["counter"],
  },
  {
    type: "events",
    title: "Event List",
    description: "Recent events feed for your stream.",
    icon: List,
    width: 350,
    height: 400,
    testEvents: ["follow", "subscribe", "raid"],
  },
];

export function OverlaysPage() {
  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Overlays"
        description="Add browser sources in OBS to display overlays on your stream."
      />

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        {OVERLAYS.map((overlay) => (
          <OverlayCard key={overlay.type} overlay={overlay} />
        ))}
      </div>
    </div>
  );
}
